from enum import Enum
from typing import Any

from pydantic import BaseModel

class OllamaRole(str, Enum):
    user = 'user'
    tool = 'tool'
    system = 'system'
    assistant = 'assistant'

class OllamaFunction(BaseModel):
    name: str = ""
    arguments: dict[str, Any] | None = None
    index: int | None = None


class OllamaToolCall(BaseModel):
    # None in the case of a single tool call
    id: str | None = None
    type: str | None = None
    function: OllamaFunction | None = None


class OllamaMessage(BaseModel):
    role: str | None
    content: str | None
    thinking: str | None = None
    tool_calls: list[OllamaToolCall] | None = None


class OllamaResponse(BaseModel):
    model: str
    message: OllamaMessage
    created_at: str | None = None
    done: bool | None = False
    done_reason: str | None = None
    total_duration: int | None = None

class PendingToolCall(OllamaToolCall):
    response: str | None = None

    @property
    def is_resolved(self):
        return self.response is not None