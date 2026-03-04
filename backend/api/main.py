import json
import logging
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

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("api")


sys.path.insert(0, str(Path(__file__).parent / "generated"))
from generated.Edgar import Message
from generated.Edgar.HeaderValue import HeaderValueT

load_dotenv()  # reads variables from a .env file and sets them in os.environ


@dataclass(frozen=True)  # frozen=True makes it immutable (like const)
class MessageHeaders:
    id: str
    is_partial: str
    chunk_id: str


known_headers = MessageHeaders(id="id", is_partial="partial-response", chunk_id="chunk-id")


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
            self.__body = bytes(b & 0xFF for b in self.message.body).decode('utf-8')
        return self.__body

    async def respond(self, message: str, headers: dict[str, str]):
        data = message.encode('utf-8')
        bytes_data = create_message(data, headers)
        logger.info(
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
    idx = 0
    async with httpx.AsyncClient() as client:
        base_url = getenv("API_BASE_URL")
        url = f"{base_url}/api/generate"
        async  with client.stream('POST', url,
                                  headers={
                                      'Content-Type': 'application/json',
                                      'CF-Access-Client-Id': getenv("CF_ACCESS_CLIENT_ID"),
                                      'CF-Access-Client-Secret': getenv("CF_ACCESS_CLIENT_SECRET")
                                  },
                                  json={'model': 'qwen2.5:3b', 'prompt': val, 'stream': True}) as r:
            async for line in r.aiter_lines():
                chunk = json.loads(line)
                is_done = chunk.get('done')

                is_partial: str
                if is_done:
                    is_partial = str(False)
                else:
                    is_partial = str(True)

                await request.respond(chunk["response"], {
                    known_headers.chunk_id: str(idx),
                    known_headers.is_partial: is_partial,
                    known_headers.id: request.id
                })

                if is_done:
                    break
                idx += 1

            r.raise_for_status()

@app.get("/healthz", include_in_schema=False)
async def healthz():
    return {"status": "ok"}

@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket):
    try:
        await websocket.accept()
        logger.info("Websocket connected")
        while True:
            logger.info("Waiting for message")
            b = await websocket.receive_bytes()
            logger.info("Received message")
            message = parse_message(b)
            req = TerminalRequest(message, websocket)
            manager.requests[req.id] = req
            await send_prompt(req.body, req)
            manager.complete(req.id)
    except Exception as e:
        logger.error(f"Error: {e}")
