import logging
from typing import Any
from pydantic import BaseModel, TypeAdapter

from messaging.ollama import OllamaToolCall

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("api/sessions")


class SystemToolParameters(BaseModel):
    type: str
    properties: dict[str, Any] | None = None
    required: list[str] | None = None


class SystemTool(BaseModel):
    name: str
    description: str
    parameters: SystemToolParameters | None = None


class OllamaToolWrapper(BaseModel):
    type: str = "function"
    function: SystemTool


class ChatMessage(BaseModel):
    role: str
    content: str | None = None
    thinking: str | None = None
    tool_calls: list[OllamaToolCall] | None = None
    tool_name: str | None = None


class OllamaModelOptions(BaseModel):
    seed: int | None = None
    temperature: float = 0.7
    top_k: int = 20
    top_p: float = 0.8
    min_p: float = 0.0
    stop: str | list[str] | None = None
    num_ctx: int = 32768
    num_predict: int = 8192

class SessionConfig(BaseModel):
    model: str
    system_prompt: str | None = None
    all_tools: list[OllamaToolWrapper] | None
    options: OllamaModelOptions | None = None


tools_adapter = TypeAdapter(list[SystemTool])

default_session_configuration = SessionConfig(model='qwen2.5:7b', system_prompt='You are a helpful assistant.',
                                              all_tools=[], options=OllamaModelOptions())
adapter = TypeAdapter(list[ChatMessage])
