from dataclasses import dataclass
import logging
from enum import Enum

import flatbuffers
from starlette.websockets import WebSocket

from generated.Edgar.HeaderValue import HeaderValueT
from generated.Edgar import Message
from generated.Edgar.Message import MessageT
from messaging.headers import get_header_value

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("messaging/client_models")


@dataclass(frozen=True)  # frozen=True makes it immutable (like const)
class MessageHeaders:
    prompt_id: str
    tool_call_id: str
    chunk_id: str
    content_type: str
    role: str
    signal: str


@dataclass(frozen=True)
class ContentType:
    text_plain = "text/plain"
    json = "application/json",
    json_tool_call = "application/json+tool_call"
    empty = "empty"


class SignalType(str, Enum):
    request_complete = "request_complete"
    tool_call_waiting = "tool_call_waiting"


known_headers = MessageHeaders(prompt_id="prompt-id",
                               tool_call_id="tool-call-id",
                               chunk_id="chunk-id",
                               content_type="content-type",
                               role="role",
                               signal="signal")


def _create_message(body: bytes | None, headers: dict[str, str]):
    """Serializes a message with headers and body into flatbuffers bytes"""
    builder = flatbuffers.Builder(0)
    req = Message.MessageT()
    req.headers = []
    for key, value in headers.items():
        req.headers.append(HeaderValueT(key, value))

    if body is not None:
        req.body = body

    builder.Finish(req.Pack(builder))

    return builder.Output()


class TerminalRequest:
    def __init__(self, message: MessageT, websocket: WebSocket):
        self.message = message
        self.websocket = websocket
        self.__id: str | None = None
        self.__body: str | None = None

    @property
    def id(self) -> str:
        if self.__id is None:
            self.__id = get_header_value(self.message, known_headers.prompt_id)
        return self.__id

    @property
    def body(self) -> str | None:
        if self.__body is None:
            self.__body = bytes(b & 0xFF for b in self.message.body).decode('utf-8')
        return self.__body

    async def respond(self, message: str, headers: dict[str, str]):
        data = message.encode('utf-8')
        bytes_data = _create_message(data, headers)
        logger.info(
            f"Sending message with id {self.id} to websocket: {len(bytes_data)}"
        )
        await self.websocket.send_bytes(bytes_data)

    async def empty_respond(self, headers: dict[str, str]):
        bytes_data = _create_message(body=None, headers=headers)
        logger.info(
            f"Sending message with id {self.id} to websocket: {len(bytes_data)}"
        )
        await self.websocket.send_bytes(bytes_data)

    @property
    def role(self):
        return get_header_value(self.message, known_headers.role)

    def get_header(self, header: str) -> str | None:
        return get_header_value(self.message, header)

    @property
    def tool_call_id(self):
        return get_header_value(self.message, known_headers.tool_call_id)
