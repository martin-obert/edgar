from pathlib import Path
import logging

import uuid
from pydantic import BaseModel, TypeAdapter

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("api/sessions")


class ChatMessage(BaseModel):
    role: str
    content: str
    thinking: str | None = None


class ShipSystem(BaseModel):
    name: str


_default_systems: list[ShipSystem] = []


class SessionConfig(BaseModel):
    model: str
    system_prompt: str | None = None
    ship_system: list[ShipSystem] = _default_systems


default_session_configuration = SessionConfig(model='qwen2.5:3b', system_prompt='You are a helpful assistant.')
adapter = TypeAdapter(list[ChatMessage])


class SessionManager:
    def __init__(self, session_id: uuid.UUID):
        self._session_id = session_id
        self._chat_messages: list[ChatMessage] | None = None
        self._config: SessionConfig | None = None
        self._chat_file = self._get_root_dir() / f"{session_id}_chat.json"
        self._config_file = self._get_root_dir() / f"{session_id}_config.json"

    @property
    def configuration(self):
        return self._load_config()._config

    @property
    def chat_messages(self):
        return self._load_chat()._chat_messages

    def update_configuration(self, config: SessionConfig):
        self._config = config
        return self

    def _get_chat_file_name(self):
        return f"{self._get_root_dir()}{self._session_id}_chat_log.json"

    def _get_config_file_name(self):
        return f"{self._get_root_dir()}{self._session_id}_config.json"

    def _load_config(self):
        if self._config:
            return self

        if not self._config_file.exists():
            self._config = default_session_configuration
            return self

        with open(self._config_file, "r", encoding="utf-8") as f:
            logger.info(f"Reading config file: {self._config_file}")
            self._config = SessionConfig.model_validate_json(f.read())
            return self

    def _load_chat(self):
        if self._chat_messages:
            return self

        if not self._chat_file.exists():
            self._chat_messages = []
            return self

        with open(self._chat_file, "r", encoding="utf-8") as f:
            logger.info(f"Reading chat file: {self._chat_file}")
            self._chat_messages = adapter.validate_json(f.read())
            return self

    def save_config(self):
        if not self._config:
            return self

        logger.info(f"Saving config file: {self._config_file}")
        with open(self._config_file, "w", encoding="utf-8") as f:
            f.write(adapter.dump_json(self._config).decode())
        logger.info(f"Session file saved: {self._config_file}")
        return self

    def save_chat(self):
        if not self._chat_messages:
            return self

        logger.info(f"Saving chat file: {self._chat_file}")
        with open(self._chat_file, "w", encoding="utf-8") as f:
            f.write(adapter.dump_json(self._chat_messages).decode())
        logger.info(f"Session file saved: {self._chat_file}")
        return self

    @staticmethod
    def _get_root_dir() -> Path:
        path = Path.joinpath(Path.cwd(), "user_sessions")
        path.mkdir(exist_ok=True)
        return path

    def append_chat_message(self, message: ChatMessage):
        self._load_chat()._chat_messages.append(message)
