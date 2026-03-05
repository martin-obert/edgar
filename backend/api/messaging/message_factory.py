import flatbuffers

from generated.Edgar import Message
from messaging.client_models import TerminalRequest, known_headers, ContentType, SignalType
from messaging.ollama import OllamaToolCall, OllamaRole
from sessions.models import ChatMessage


def parse_message(b: bytes) -> Message.MessageT:
    message = Message.Message.GetRootAs(b, 0)
    return Message.MessageT.InitFromObj(message)


async def partial_content_response(r: TerminalRequest, content: str, chunk_id: int, role: str):
    await r.respond(content, headers={
        known_headers.chunk_id: str(chunk_id),
        known_headers.content_type: ContentType.text_plain,
        known_headers.prompt_id: r.id,
        known_headers.role: role,
    })


async def signal(r: TerminalRequest, signal_type: SignalType, role: str):
    await r.empty_respond(headers={
        known_headers.content_type: ContentType.empty,
        known_headers.prompt_id: r.id,
        known_headers.signal: signal_type,
        known_headers.role: role
    })


async def request_tool_call(r: TerminalRequest, tool_call: OllamaToolCall):
    await r.respond(tool_call.function.model_dump_json(), headers={
        known_headers.content_type: ContentType.json_tool_call,
        known_headers.prompt_id: r.id,
        known_headers.role: OllamaRole.tool.value,
        known_headers.tool_call_id: tool_call.id,
    })


def pending_tool_call_response(func_name: str, content: str) -> ChatMessage:
    return ChatMessage(role=OllamaRole.tool.value, content=content, tool_name=func_name)
