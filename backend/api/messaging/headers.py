from generated.Edgar import Message

def get_header_value(message: Message.MessageT, header_key: str) -> str | None:
    for header in message.headers:
        if header.name.decode('utf-8') == header_key:
            return header.value.decode('utf-8')
    return None

