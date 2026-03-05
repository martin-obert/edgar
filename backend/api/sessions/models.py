import logging
from typing import Any
from pydantic import BaseModel, TypeAdapter

from messaging.ollama import OllamaToolCall

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("api/sessions")



class SystemToolParameters(BaseModel):
    type: str
    properties: dict[str, Any]
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



_default_tools: list[OllamaToolWrapper] = [
    OllamaToolWrapper(type="function", function=SystemTool(name="search",
                                                           description="Search the web",
                                                           parameters=SystemToolParameters(
                                                               type="object",
                                                               properties={
                                                                   "query": {"type": "string",
                                                                             "description": "The search query"},
                                                               },
                                                               required=["query"]))
                      )
]


class SessionConfig(BaseModel):
    model: str
    system_prompt: str | None = None
    all_tools: list[OllamaToolWrapper] = _default_tools


tools_adapter = TypeAdapter(list[SystemTool])

default_session_configuration = SessionConfig(model='qwen2.5:7b', system_prompt='You are a helpful assistant.', all_tools=_default_tools)
adapter = TypeAdapter(list[ChatMessage])

