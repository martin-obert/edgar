import type {MessageT} from "./generated/edgar/message.ts";
import {
    createWebSocketMessage,
    getBody, getChunkId,
    getRequestId,
    isPartialResponse,
    messageType
} from "./websocket-messaging.ts";
import {v4 as uuidv4} from "uuid";

export declare type Errors = 'ALREADY_RUNNING_PROMPT' | 'STREAM_NOT_WRITABLE';

export class MessageManagerError extends Error {
    private readonly _errorType: Errors;
    get errorType() {
        return this._errorType
    }

    constructor(message: string, errorType: Errors) {
        super(message);
        this.name = "MessageManagerError";
        this._errorType = errorType;
        Object.setPrototypeOf(this, MessageManagerError.prototype); // fix prototype chain
    }
}

interface WsRequest {
    id: string

    wait(timeout: number): Promise<WsRequestResult>
}

class WsRequestWrapper implements WsRequest {
    private _state: 'pending' | 'complete' | 'error' = 'pending'
    private _timeoutHandle: number | undefined;
    private _error: string | undefined;

    get hasCompleted(): boolean {
        return this._state === 'complete' || this._state === 'error'
    }

    constructor(public id: string, private _onResponse?: (response: WsResponseChunk) => void) {

    }

    private _resolve?: any;
    private _reject?: any;

    wait(timeout: number): Promise<WsRequestResult> {
        return new Promise<WsRequestResult>((resolve, reject) => {
            this._resolve = resolve
            this._reject = reject

            this._timeoutHandle = setTimeout(() => {
                if (this._state === 'pending') {
                    this._error = 'Timeout after ' + timeout + 'ms'
                    this.finalize()
                }
            }, timeout)
        })
    }

    onMessage(message: MessageT): void {
        if (this.hasCompleted) return

        const id = getRequestId(message.headers)
        if (!id) {
            console.error("No id header found")
            return
        }
        if (id !== this.id) {
            console.error("Id mismatch: ", id, " != ", this.id)
            return
        }

        const isPartial = isPartialResponse(message.headers)
        this._state = isPartial ? 'pending' : 'complete'
        const chunkId = getChunkId(message.headers)
        if (this._onResponse)
            this._onResponse({
                content: getBody(message.body),
                complete: this.hasCompleted,
                chunkId: chunkId ?? 0
            })

        if (this.hasCompleted)
            this.finalize()
    }

    private finalize() {
        if (this._state === 'pending')
            throw new Error("Request is still pending")

        if (this._state === 'error') {
            this._reject!(new Error(this._error))
            return;
        }


        this._resolve!({
            state: this._state,
            error: this._error
        })

        this.dispose()
    }

    private dispose() {
        this._resolve = undefined
        this._reject = undefined
        this._onResponse = undefined
        if (this._timeoutHandle !== undefined) {
            clearTimeout(this._timeoutHandle);
            this._timeoutHandle = undefined;
        }

    }


}

export interface WsRequestResult {
    state: 'pending' | 'complete' | 'error'
    error?: string
}

export interface WsResponseChunk {
    chunkId: number;
    content: string
    complete: boolean
}

export interface IMessageStream {
    get canWrite(): boolean;

    get canRead(): boolean;

    onMessage?: (message: MessageT) => void;
    sendMessage: (message: MessageT) => void;
}

export interface IMessageManager {
    init(): void;

    dispose(): void;

    sendPromptRequest(content: string, options: {
        onResponse: (response: WsResponseChunk) => void
    }): WsRequest;
}

class MessageManager implements IMessageManager {
    private readonly _inboxBuffer: MessageT[] = []
    private readonly _outboxBuffer: MessageT[] = []

    private _currentPrompt: WsRequestWrapper | undefined;

    private loopHandle: number | undefined;
    private readonly loopTimeout: number = 100;

    constructor(private readonly _messageStream: IMessageStream) {
    }

    sendPromptRequest(content: string, options: {
        onResponse: (response: WsResponseChunk) => void
    }): WsRequest {
        if (this._currentPrompt) {
            if (this._currentPrompt.hasCompleted)
                this._currentPrompt = undefined
            else
                throw new MessageManagerError("Prompt already in progress", 'ALREADY_RUNNING_PROMPT')
        }

        if (!this._messageStream.canWrite) {
            throw new MessageManagerError("Message stream is not writable", 'STREAM_NOT_WRITABLE')
        }

        const id = uuidv4()
        this._currentPrompt = new WsRequestWrapper(id, options.onResponse)

        this._outboxBuffer.push(createWebSocketMessage(content, {id: id, type: messageType.PROMPT_REQUEST}))

        return this._currentPrompt
    }

    init(): void {
        this._messageStream.onMessage = (message) => this._inboxBuffer.push(message)
        this.startLoop()
    }

    dispose(): void {
        this._messageStream.onMessage = undefined
        this._currentPrompt = undefined
        this._inboxBuffer.length = 0
        this._outboxBuffer.length = 0
        this.endLoop()
    }

    private startLoop() {
        if (this.loopHandle) throw new Error("Loop already running")

        this.loopHandle = setInterval(() => {
            const outMessage = this._outboxBuffer.shift()
            if (outMessage) {
                this._messageStream.sendMessage(outMessage)
            }
            for (let i = 0; i < this._inboxBuffer.length; i++) {
                const inMessage = this._inboxBuffer[i]
                if (!inMessage) continue
                const messageId = getRequestId(inMessage.headers)
                const isPartial = isPartialResponse(inMessage.headers)
                const body = getBody(inMessage.body)
                const chunkId = getChunkId(inMessage.headers)
                console.log(`Processing message: ${messageId} - ${isPartial ? 'PARTIAL' : 'FULL'} - ${chunkId} - ${body}`)

                if (this._currentPrompt && this._currentPrompt.id === messageId) {
                    this._currentPrompt.onMessage(inMessage)
                    if (this._currentPrompt.hasCompleted)
                        this._currentPrompt = undefined
                }
            }

            this._inboxBuffer.length = 0

        }, this.loopTimeout)
    }

    private endLoop() {
        if (this.loopHandle) {
            clearInterval(this.loopHandle)
            this.loopHandle = undefined
        }
    }
}

export const createMessageManager = (ms: IMessageStream) => {
    return new MessageManager(ms)
}