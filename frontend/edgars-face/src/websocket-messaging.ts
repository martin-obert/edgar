import type {OllamaFunctionCall} from "./message-manager.ts";

export const getHeader = (headers: HeaderValue[], key: string): string | undefined => {
    const idHeader = headers.find(header => header.name === key);
    if (idHeader) return getHeaderValue(idHeader)
    return undefined
}


export const isPartialResponse = (headers: HeaderValue[]) => {
    const val = getHeader(headers, 'partial-response')
    if (!val) return false

    return val.toLowerCase() === 'true' || val === '1'
}

export const getChunkId = (headers: HeaderValue[]): number | undefined => {
    const n = getHeader(headers, KnownHeaders.chunk_id)
    if (!n) return undefined
    return parseInt(n)
}

export const getPromptId = (headers: HeaderValue[]) => {
    return getHeader(headers, KnownHeaders.prompt_id)
}

export const SignalType = {
    request_complete: 'request_complete',
    tool_call_waiting: 'tool_call_waiting',
}

export const KnownHeaders = {
    prompt_id: 'prompt-id',
    tool_call_id: 'tool-call-id',
    chunk_id: 'chunk-id',
    content_type: 'content-type',
    role: 'role',
    signal: 'signal',
}

export const ContentType = {
    text_plain: 'text/plain',
    json: 'application/json',
    json_tool_call: 'application/json+tool_call',
    empty: 'empty'
}

export const KnownRoles = {
    user: 'user',
    tool: 'tool',
    system: 'system',
    assistant: 'assistant',
}

export interface HeaderValue {
    name: string
    value?: string
}


export interface TerminalRequestJson {
    headers: HeaderValue[]
    body?: string
}

export class TerminalRequest {
    constructor(private readonly _message: TerminalRequestJson = {
        body: undefined,
        headers: []
    } as TerminalRequestJson) {
    }

    private addOrSetHeader(key: string, value: string) {
        const header = this._message.headers.find(header => header.name === key)
        if (header) {
            header.value = value
        } else {
            this._message.headers.push({name: key, value: value})
        }
    }

    get body(): string | undefined {
        return this._message.body
    }

    set body(val: string | undefined) {
        this._message.body = val;
    }

    get toolCallId(): string | undefined {
        return getHeader(this._message.headers, KnownHeaders.tool_call_id)
    }

    set toolCallId(val: string) {
        this.addOrSetHeader(KnownHeaders.tool_call_id, val)
    }

    get chunkId(): number | undefined {
        return getChunkId(this._message.headers)
    }

    get promptId(): string | undefined {
        return getPromptId(this._message.headers)
    }

    set promptId(val: string) {
        this.addOrSetHeader(KnownHeaders.prompt_id, val)
    }

    get contentType(): string | undefined {
        return getContentType(this._message.headers)
    }

    set contentType(val: string) {
        this.addOrSetHeader(KnownHeaders.content_type, val)
    }

    get signalType(): string | undefined {
        return getHeader(this._message.headers, KnownHeaders.signal)
    }

    get role(): string | undefined {
        return getHeader(this._message.headers, KnownHeaders.role)
    }

    set role(val: string) {
        this.addOrSetHeader(KnownHeaders.role, val)
    }

    get isSignalRequestComplete(): boolean {
        return this.signalType === SignalType.request_complete
    }

    get isToolCallWaiting(): boolean {
        return this.signalType === SignalType.tool_call_waiting
    }

    hasRole(role: string): boolean {
        return this.role === role
    }

    serialize() {
        return serializeMessage(this._message)
    }
}

export const getContentType = (headers: HeaderValue[]) => {
    return getHeader(headers, 'content-type')
}

export const getBody = (body: number[]) => {
    return new TextDecoder().decode(new Uint8Array(body.map(b => b & 0xFF)));
}

export const getHeaderValue = (headerValueT: HeaderValue): string | undefined => {

    if (!headerValueT) return undefined;
    if (typeof headerValueT.value === 'string') {
        return headerValueT.value
    } else if (headerValueT.value != null) {
        return new TextDecoder().decode(headerValueT.value as Uint8Array);
    } else {
        return undefined
    }
}

export const serializeMessage = (message: TerminalRequestJson): Uint8Array => {
    return new TextEncoder().encode(JSON.stringify(message))
}
export const deserializeMessage = (data: Uint8Array): TerminalRequestJson => {
    return JSON.parse(new TextDecoder().decode(data))
}
export const getFunctionFromBody = (body: string) => {
    return JSON.parse(body) as OllamaFunctionCall
}

export class OllamaModelOptions {
    seed?: number
    temperature?: number = 0.7
    top_p?: number = 0.8
    top_k?: number = 20
    // repeat_penalty?: number
    // repeat_last_n?: number
    num_ctx?: number = 32768
    // num_batch?: number
    // num_gqa?: number
    num_predict?: number = 8192
    // num_predict_batch?: number
}
