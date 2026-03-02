import flatbuffers
from fastapi import FastAPI
from dotenv import load_dotenv
from starlette.middleware.cors import CORSMiddleware
from starlette.websockets import WebSocket
from generated.Edgar import Message

load_dotenv()  # reads variables from a .env file and sets them in os.environ

Headers_Id = "id"
headers = {Headers_Id: Headers_Id}


def parse_message(b: bytes) -> Message.MessageT:
    message = Message.Message.GetRootAs(b, 0)
    return Message.MessageT.InitFromObj(message)


def create_message(body: bytes):
    builder = flatbuffers.Builder(0)
    req = Message.MessageT()
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

    async def write(self, data: bytes):
        bytes_data = create_message(data)
        print(f"Sending {len(bytes_data)} bytes: {bytes_data.hex()}")
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
            print("Message received:")
            message = parse_message(b)
            req_id = get_header_value(message, Headers_Id)
            if req_id is None:
                req_id = "default"
            print(req_id)
            manager.requests[req_id] = TerminalRequest(message, websocket)
            await manager.requests[req_id].write(b"Hello")
            manager.complete(req_id)
    except Exception as e:
        print(e)
