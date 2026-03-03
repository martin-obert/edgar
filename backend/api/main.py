import json
from dataclasses import dataclass
from os import getenv
import flatbuffers
import httpx
from fastapi import FastAPI, requests
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
    is_partial: str


known_headers = MessageHeaders(id="id", is_partial="partial-response")


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
        self.__body: str | None = None

    @property
    def id(self) -> str:
        if self.__id is None:
            self.__id = get_header_value(self.message, known_headers.id)
        return self.__id

    @property
    def body(self) -> str | None:
        if self.__body is None:
            self.__body = bytes(self.message.body).decode('utf-8')
        return self.__body

    async def respond(self, message: str, is_partial: bool = False):
        data = message.encode('utf-8')
        bytes_data = create_message(data, {known_headers.id: self.id, known_headers.is_partial: str(is_partial)})
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


async def send_prompt(val: str, request: TerminalRequest):
    async with httpx.AsyncClient() as client:
        url = f"{getenv("API_BASE_URL")}/api/generate"
        print(url)
        async  with client.stream('POST', url,
                                  headers={
                                      'Content-Type': 'application/json',
                                      'CF-Access-Client-Id': getenv("CF_ACCESS_CLIENT_ID"),
                                      'CF-Access-Client-Secret': getenv("CF_ACCESS_CLIENT_SECRET")
                                  },
                                  json={'model': 'qwen2.5:3b', 'prompt': val, 'stream': True}) as r:
            async for line in r.aiter_lines():
                chunk = json.loads(line)
                print(chunk["response"])
                if chunk.get('done'):
                    await request.respond("")
                    break
                await request.respond(chunk["response"], True)

            r.raise_for_status()


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
            print(f"Message received: {req.id} - {req.body}")
            await send_prompt(req.body, req)
            manager.complete(req.id)
    except Exception as e:
        print(e)
