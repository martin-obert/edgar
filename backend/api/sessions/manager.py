import uuid
import logging
from pathlib import Path

from messaging.client_models import TerminalRequest
from messaging.message_factory import pending_tool_call_response
from messaging.ollama import PendingToolCall, OllamaToolCall
from sessions.models import SessionConfig, ChatMessage, default_session_configuration, adapter

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("sessions/manager")


class SessionManager:
    def __init__(self, session_id: uuid.UUID):

        # TODO: Transient, make persistent!
        self.pending_request: TerminalRequest | None = None
        self._pending_tool_calls: list[PendingToolCall] = []
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

    @property
    def has_pending_request(self):
        return self.pending_request is not None

    @property
    def has_pending_tool_calls(self):
        return len(self._pending_tool_calls) > 0

    def add_pending_tool_call(self, tool_call: OllamaToolCall):
        pending = PendingToolCall(**tool_call.model_dump())
        self._pending_tool_calls.append(pending)

    def clear_pending_request(self):
        self._pending_tool_calls = []
        self.pending_request = None

    @property
    def all_tool_calls_resolved(self):
        for tc in self._pending_tool_calls:
            if not tc.is_resolved:
                return False
        return True

    def resolve_pending_tool_call(self, tool_call_id: str, result: str):
        for tc in self._pending_tool_calls:
            if tc.id == tool_call_id:
                tc.response = result
                return

    def dump_tool_calls_to_chat(self):
        for tc in self._pending_tool_calls:
            if tc.is_resolved:
                self.append_chat_message(pending_tool_call_response(tc.function.name, tc.response))

        self.clear_pending_request()