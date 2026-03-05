from messaging.ollama import TerminalRequestJson


def get_header_value(message: TerminalRequestJson, header_key: str) -> str | None:
    for header in message.headers:
        if header.name == header_key:
            return header.value
    return None

