from pathlib import Path
import logging

import uuid
from pydantic import BaseModel

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("api/sessions")


class ChatMessage(BaseModel):
    role: str
    content: str


class SessionConfig(BaseModel):
    model: str


class Session(BaseModel):
    id: str
    chats: list[ChatMessage]
    config: SessionConfig


class SessionManager:
    def __init__(self, session_id: uuid.UUID):
        self._session_id = session_id
        m_dir = self._get_sessions_dir()
        self._filepath = f"{m_dir}/{self._session_id.hex}.json"
        self._session: Session | None = None

    @property
    def session_model(self):
        return self._session.config.model

    @property
    def chat_messages(self):
        return self._session.chats

    def __enter__(self):
        logger.info(f"Opening session file: {self._filepath}")

        Path(self._filepath).touch(exist_ok=True)

        with open(self._filepath, "r+") as f:
            logger.info(f"Reading session file: {self._filepath}")
            content = f.read()  # read everything
            if content:
                self._session = Session.model_validate_json(content)
            else:
                self._session = Session(id=str(self._session_id), chats=[], config=SessionConfig(model='qwen2.5:3b'))

        return self

    def __exit__(self, exc_type, exc_value, traceback):
        logger.info(f"Closing session file: {self._filepath}")
        if self._session:
            logger.info(f"Saving session file: {self._filepath}")
            with open(self._filepath, "w") as f:
                f.write(self._session.model_dump_json())
            logger.info(f"Session file saved: {self._filepath}")

        return False

    @staticmethod
    def _get_sessions_dir() -> str:
        path = Path.joinpath(Path.cwd(), "chats")
        path.mkdir(exist_ok=True)
        return str(path)

    def append_chat_message(self, message: ChatMessage):
        self._session.chats.append(message)
