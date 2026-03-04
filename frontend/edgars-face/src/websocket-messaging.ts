import {MessageT} from "./generated/edgar/message.ts";
import {Builder} from "flatbuffers";
import {HeaderValueT} from "./generated/edgar/header-value.ts";

export const getHeader = (headers: HeaderValueT[], key: string): string | undefined => {
    const idHeader = headers.find(header => header.name === key);
    if (idHeader) return getHeaderValue(idHeader)
    return undefined
}

export const isPartialResponse = (headers: HeaderValueT[]) => {
    const val = getHeader(headers, 'partial-response')
    if (!val) return false

    return val.toLowerCase() === 'true' || val === '1'
}

export const getChunkId = (headers: HeaderValueT[]): number | undefined => {
    const n = getHeader(headers, 'chunk-id')
    if (!n) return undefined
    return parseInt(n)
}

export const getRequestId = (headers: HeaderValueT[]) => {
    return getHeader(headers, 'id')
}
export const ContentType = {
    chunk: 'message/content+chunk',
    tool_call: 'message/tool_call',
    request_done: 'signal/request+done',
    request_prompt: 'signal/request+prompt',
}
export const getContentType = (headers: HeaderValueT[]) => {
    return getHeader(headers, 'content-type')
}

export const getBody = (body: number[]) => {
    return new TextDecoder().decode(new Uint8Array(body.map(b => b & 0xFF)));
}

export const getHeaderValue = (headerValueT: HeaderValueT): string | undefined => {

    if (!headerValueT) return undefined;
    if (typeof headerValueT.value === 'string') {
        return headerValueT.value
    } else if (headerValueT.value != null) {
        return new TextDecoder().decode(headerValueT.value as Uint8Array);
    } else {
        return undefined
    }
}


export const createWebSocketMessage = (body: string, headers: Record<string, string>): MessageT => {
    const r = new MessageT();
    const requestHeaders: HeaderValueT[] = []


    for (const [key, value] of Object.entries(headers)) {
        requestHeaders.push(new HeaderValueT(key, value))
    }

    r.headers = requestHeaders
    r.body = Array.from(new TextEncoder().encode(body));
    return r
};

export const serializeMessage = (message: MessageT) => {
    const b = new Builder(0)
    b.finish(message.pack(b))
    return b.asUint8Array()
}
