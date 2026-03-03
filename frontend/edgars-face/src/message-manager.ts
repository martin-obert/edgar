import type {MessageT} from "./generated/edgar/message.ts";
import {createWebSocketMessage, getBody, getHeader, messageType} from "./websocket-messaging.ts";
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

    cancel(): void

    wait(timeout: number): Promise<WsRequestResult>
}

class WsRequestWrapper implements WsRequest {
    private _state: 'pending' | 'complete' | 'error' = 'pending'
    private _timeoutHandle: number | undefined;
    private _error: string | undefined;

    constructor(public id: string, private readonly _onResponse: (response: WsResponseChunk) => void) {

    }

    wait(timeout: number): Promise<WsRequestResult> {
        return new Promise<WsRequestResult>((resolve, reject) => {
            this._timeoutHandle = setTimeout(() => {
                if (this._state === 'pending') {
                    this._state = 'error'
                    this._error = 'Timeout after ' + timeout + 'ms'
                    reject(new Error(this._error))
                    return
                }
                resolve({
                    state: this._state,
                    error: this._error
                })
            }, timeout)
        })
    }

    onMessage(message: MessageT): void {
        const id = getHeader(message.headers, 'id')
        if (!id) {
            console.error("No id header found")
            return
        }
        if (id !== this.id) {
            console.error("Id mismatch: ", id, " != ", this.id)
            return
        }

        this._onResponse({
            content: getBody(message.body),
            complete: true
        })

        this._state = 'complete'
    }


    cancel(): void {
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
        this.endLoop()
    }

    private startLoop() {
        if (this.loopHandle) throw new Error("Loop already running")

        this.loopHandle = setInterval(() => {
            const outMessage = this._outboxBuffer.shift()
            if (outMessage) {
                this._messageStream.sendMessage(outMessage)
            }
            const inMessage = this._inboxBuffer.shift()
            if (inMessage) {
                if (this._currentPrompt && this._currentPrompt.id === getHeader(inMessage.headers, 'id')) {
                    this._currentPrompt.onMessage(inMessage)
                    // TODO: check if prompt is complete
                    this._currentPrompt = undefined
                }
            }
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