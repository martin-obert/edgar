import flatbuffers
from fastapi import FastAPI
from dotenv import load_dotenv
from starlette.middleware.cors import CORSMiddleware
from starlette.websockets import WebSocket

from generated.Edgar import Request

load_dotenv()  # reads variables from a .env file and sets them in os.environ

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # tighten this for production
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket):
    await websocket.accept()
    print("Connected")
    while True:
        b = await websocket.receive_bytes()
        req_buf = Request.Request.GetRootAsRequest(b, 0)
        print("Received request")
        req = Request.RequestT.InitFromObj(req_buf)
        req.body = b"Hello"
        builder = flatbuffers.Builder(0)
        builder.Finish(req.Pack(builder))
        buf = builder.Output()
        await websocket.send_bytes(buf)