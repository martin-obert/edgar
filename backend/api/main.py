import logging
import sys
from pathlib import Path

from httpx import HTTPError

sys.path.insert(0, str(Path(__file__).parent / "generated"))

from uuid import UUID
from os import getenv
from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException
from starlette.middleware.cors import CORSMiddleware
from starlette.websockets import WebSocket

from message_router import handle
from messaging.client_models import TerminalRequest
from messaging.message_factory import parse_message
from sessions.manager import SessionManager
from sessions.models import SessionConfig

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("api")

load_dotenv()  # reads variables from a .env file and sets them in os.environ

active_sessions: dict[UUID, SessionManager] = {}

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # tighten this for production
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/healthz", include_in_schema=False)
async def healthz():
    return {"status": "ok"}


@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket):
    session_id = UUID(websocket.query_params.get("session_id"))
    logger.info(f"Websocket connected with session_id: {session_id}")
    session_manager = active_sessions.get(session_id, SessionManager(session_id=session_id))
    logger.info(f"Session manager: {session_manager.configuration}")
    try:
        active_sessions[session_id] = session_manager
        await websocket.accept()
        logger.info("Websocket connected")
        while True:
            logger.info("Waiting for message")
            b = await websocket.receive_bytes()
            logger.info("Received message")
            message = parse_message(b)
            req = TerminalRequest(message, websocket)
            logger.info(f"Processing request: {req.id}")
            await handle(req, session_manager)
    except Exception as e:
        logger.exception(f"Error: {e}")
    finally:
        active_sessions.pop(session_id)
        logger.info("Closing websocket")

        if getenv("PERSISTENT_SESSIONS") == "true":
            session_manager.save_chat()


@app.put("/api/v1/sessions/{session_id}/configuration")
async def update_session_configuration(session_id: UUID, configuration: SessionConfig):
    if session_id not in active_sessions:
        active_sessions[session_id] = SessionManager(session_id=session_id)
    session_manager = active_sessions[session_id]
    session_manager.update_configuration(configuration)
    return session_manager.configuration


@app.delete("/api/v1/sessions/{session_id}")
async def update_session_configuration(session_id: UUID):
    if session_id in active_sessions:
        active_sessions.pop(session_id)
    return {"status": "ok"}
