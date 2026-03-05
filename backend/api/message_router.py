from collections import defaultdict
from os import getenv
import logging
from typing import Any

import httpx
from fastapi.routing import request_response
from httpx import Response

import sessions.manager
from messaging.client_models import TerminalRequest, known_headers, SignalType
from messaging.message_factory import partial_content_response, signal, request_tool_call
from messaging.ollama import OllamaResponse, OllamaRole, OllamaToolCall
from sessions.manager import SessionManager
from sessions.models import ChatMessage

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("messaging/router")


async def _chat(request: TerminalRequest, session_manager: sessions.manager.SessionManager):
    async with httpx.AsyncClient() as client:
        logger.info(f"Preparing request")
        chat = [m.model_dump(exclude_none=True) for m in session_manager.chat_messages]
        tools = [t.model_dump() for t in session_manager.configuration.all_tools]
        base_url = getenv("API_BASE_URL")
        url = f"{base_url}/api/chat"
        options = None
        if session_manager.configuration.options:
            options = session_manager.configuration.options.model_dump(exclude_none=True)
        logger.info(f"Sending request to {url}")
        async  with client.stream('POST', url,
                                  timeout=60.0,
                                  headers={
                                      'Content-Type': 'application/json',
                                      'CF-Access-Client-Id': getenv("CF_ACCESS_CLIENT_ID"),
                                      'CF-Access-Client-Secret': getenv("CF_ACCESS_CLIENT_SECRET")
                                  },
                                  json={
                                      'model': session_manager.configuration.model,
                                      'messages': [
                                          {'role': 'system', 'content': session_manager.configuration.system_prompt},
                                          # System prompt comes first
                                          *chat
                                      ],
                                      'tools': tools,
                                      'stream': True,
                                      'options': options,
                                  }) as r:
            logger.info(f"Response status: {r.status_code}")

            r.raise_for_status()

            await process_chunks(r, request, session_manager)


async def process_chunks(r: Response, request: TerminalRequest,
                         session_manager: sessions.manager.SessionManager):
    idx = 0
    content_bits: dict[str, list[str]] = defaultdict(list)
    tool_calls: list[OllamaToolCall] = []
    async for line in r.aiter_lines():
        logger.info(f"Received chunk: {line}")
        m = OllamaResponse.model_validate_json(line)

        if len(m.message.content) > 0:
            content_bits[m.message.role].append(m.message.content)
            await partial_content_response(request, m.message.content, idx, m.message.role)

        if m.message.tool_calls is not None:
            for tool_call in m.message.tool_calls:
                tool_calls.append(tool_call)
                logger.info(f"Adding tool call: {tool_call}")
                session_manager.add_pending_tool_call(tool_call)
                await request_tool_call(request, tool_call)

        if m.done and len(tool_calls) == 0:
            await signal(request, SignalType.request_complete, m.message.role)
            break

        idx += 1

    content: str | None = None
    tc: list[OllamaToolCall] | None = None

    contents = content_bits.get(OllamaRole.assistant, [])

    if len(contents) > 0:
        content = "".join(contents)

    if len(tool_calls) > 0:
        tc = tool_calls

    if content is None and tc is None:
        logger.warning("No content or tool calls found in the response, maybe thinking?")
        return None

    logger.info(f"Tool calls: {session_manager.pending_tool_calls}")

    session_manager.append_chat_message(ChatMessage(role=OllamaRole.assistant, content=content, tool_calls=tc))
    session_manager.pending_request.assistant_responded = True

    await process_tool_calls(request, session_manager)

    try_clear_request(session_manager)
    return None


def try_clear_request(session_manager: SessionManager) -> Any:
    if (session_manager.all_tool_calls_resolved and
            session_manager.pending_request and
            session_manager.pending_request.assistant_responded):
        logger.info(f"Clearing pending request: {session_manager.pending_request.id}")
        session_manager.clear_pending()

    return None


async def handle_user_prompt(request: TerminalRequest, session_manager: sessions.manager.SessionManager):
    if request.role != OllamaRole.user:
        raise ValueError(f"Expected role to be user, got {request.role}")

    if not session_manager.has_pending_request:
        session_manager.pending_request = request
    else:
        if request.id is not session_manager.pending_request.id:
            # Maybe useful if partials tool responses are supported
            session_manager.dump_tool_calls_to_chat()
            session_manager.pending_request = request

    # First, append the user prompt to the chat
    session_manager.append_chat_message(ChatMessage(role=request.role, content=request.body))
    return await _chat(request, session_manager)


async def handle_tool_response(request: TerminalRequest, session_manager: SessionManager):
    if request.role != OllamaRole.tool:
        raise ValueError(f"Expected role to be user, got {request.role}")

    logger.info(
        f"Received tool response - Tool Call ID: {request.tool_call_id}, Request ID: {request.id}, Body: {request.body}")

    # If there are no pending tool calls, ignore this response
    if not session_manager.has_pending_request or not session_manager.has_pending_tool_calls:
        logger.warning(f"No pending tool call found for request ID: {request.id}")
        return None

    # If the request ID doesn't match the pending request, ignore this response
    if request.id != session_manager.pending_request.id:
        logger.warning(f"Received tool response for unexpected request ID: {request.id}")
        return None

    # Remove a pending tool call from the session manager
    session_manager.resolve_pending_tool_call(request.tool_call_id, request.body)
    logger.info(f"Resolved tool call: {request.tool_call_id}")

    await process_tool_calls(request, session_manager)

    try_clear_request(session_manager)
    return None


async def process_tool_calls(request: TerminalRequest, session_manager: SessionManager) -> Any:
    if not session_manager.has_pending_tool_calls:
        logger.info(f"No tool calls pending for {request.id}")
        return None

    if not session_manager.pending_request.assistant_responded:
        logger.info(f"Assistant has not responded yet {request.id}, waiting for assistant to respond")

    if not session_manager.all_tool_calls_resolved:
        logger.info(f"Not all tool calls resolved yet for {request.id}, waiting for more tool calls")
        return None

    logger.info("Assistant already responded, can process tool calls now")
    session_manager.dump_tool_calls_to_chat()
    return await _chat(request, session_manager)


async def handle(request: TerminalRequest, session_manager: sessions.manager.SessionManager):
    match request.role:
        case OllamaRole.user:
            await handle_user_prompt(request, session_manager)
        case OllamaRole.tool:
            await handle_tool_response(request, session_manager)
        case _:
            logger.error(f"Unknown role: {request.role}")
