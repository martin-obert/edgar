from dataclasses import dataclass

import flatbuffers
from fastapi import FastAPI
from dotenv import load_dotenv
from starlette.middleware.cors import CORSMiddleware
from starlette.websockets import WebSocket
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent / "generated"))

from generated.Edgar import Message
from generated.Edgar.HeaderValue import HeaderValueT

load_dotenv()  # reads variables from a .env file and sets them in os.environ


@dataclass(frozen=True)  # frozen=True makes it immutable (like const)
class MessageHeaders:
    id: str


known_headers = MessageHeaders(id="id")


def parse_message(b: bytes) -> Message.MessageT:
    message = Message.Message.GetRootAs(b, 0)
    return Message.MessageT.InitFromObj(message)


def create_message(body: bytes, headers: dict[str, str]):
    """Serializes message with headers and body into flatbuffers bytes"""
    builder = flatbuffers.Builder(0)
    req = Message.MessageT()
    req.headers = []
    for key, value in headers.items():
        req.headers.append(HeaderValueT(key, value))

    req.body = body

    builder.Finish(req.Pack(builder))

    return builder.Output()


def get_header_value(message: Message.MessageT, header_key: str) -> str | None:
    for header in message.headers:
        if header.name.decode('utf-8') == header_key:
            return header.value
    return None


class TerminalRequest:
    def __init__(self, message: Message.MessageT, websocket: WebSocket):
        self.message = message
        self.websocket = websocket
        self.__id: str | None = None

    @property
    def id(self) -> str:
        if self.__id is None:
            self.__id = get_header_value(self.message, known_headers.id)
        return self.__id

    def body(self) -> str | None:
        return self.message.body

    async def respond(self, message: str):
        data = message.encode('utf-8')
        bytes_data = create_message(data, {known_headers.id: self.id})
        print(
            f"Sending message with id {self.id} to websocket: {len(bytes_data)}"
        )
        await self.websocket.send_bytes(bytes_data)


class RequestManager:
    def __init__(self):
        self.requests: dict[str, TerminalRequest] = {}

    def complete(self, req_id: str):
        self.requests.pop(req_id)


manager = RequestManager()
app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # tighten this for production
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket):
    try:
        await websocket.accept()
        print("Connected")
        while True:
            b = await websocket.receive_bytes()
            message = parse_message(b)
            req = TerminalRequest(message, websocket)
            manager.requests[req.id] = req
            print(f"Message received: {req.id}")
            await manager.requests[req.id].respond("Hello")
            manager.complete(req.id)
    except Exception as e:
        print(e)
