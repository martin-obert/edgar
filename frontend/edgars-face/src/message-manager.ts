import {
    ContentType,
    KnownRoles,
    TerminalRequest,
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

    onMessage(message: TerminalRequest): void {
        if (this.hasCompleted) return

        if (!message.promptId) {
            console.error("No id header found")
            return
        }
        if (message.promptId !== this.id) {
            console.error("Id mismatch: ", message.promptId, " != ", this.id)
            return
        }

        if (message.isSignalRequestComplete)
            this._state = 'complete'

        if (this._onResponse)
            this._onResponse({
                content: message.body!,
                complete: this.hasCompleted,
                chunkId: message.chunkId ?? 0
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

    onMessage?: (message: TerminalRequest) => void;
    sendMessage: (message: TerminalRequest) => void;
}

export interface IMessageManager {
    init(): void;

    get activeRequestId(): string | undefined;

    dispose(): void;

    sendToolResponse(content: string, toolCallId: string, promptId: string): void

    sendPromptRequest(content: string, options: {
        onResponse: (response: WsResponseChunk) => void
    }): WsRequest;

}


export interface OllamaFunctionCall {
    name: string,
    index: number,
    arguments: Record<string, any>
}

export interface OllamaToolCall {
    id?: string
    type?: string
    function?: OllamaFunctionCall
}

export interface ToolCallEvent {
    message: TerminalRequest,
    messageManager: IMessageManager
}

class MessageManager implements IMessageManager {
    private readonly _inboxBuffer: TerminalRequest[] = []
    private readonly _outboxBuffer: TerminalRequest[] = []
    private _activeRequestId: string | undefined
    get activeRequestId() {
        return this._activeRequestId
    }

    private _currentPrompt: WsRequestWrapper | undefined;

    private loopHandle: number | undefined;
    private readonly loopTimeout: number = 100;

    constructor(private readonly _messageStream: IMessageStream) {
    }


    sendToolResponse(content: string, toolCallId: string, promptId: string) {
        if (!this._messageStream.canWrite) {
            throw new MessageManagerError("Message stream is not writable", 'STREAM_NOT_WRITABLE')
        }
        const message = new TerminalRequest()
        message.role = KnownRoles.tool
        message.body = content
        message.contentType = ContentType.json
        message.toolCallId = toolCallId
        message.promptId = promptId
        this._outboxBuffer.push(message)
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

        this._activeRequestId = uuidv4()
        this._currentPrompt = new WsRequestWrapper(this._activeRequestId, options.onResponse)

        const message = new TerminalRequest()
        message.promptId = this._activeRequestId
        message.role = KnownRoles.user
        message.body = content
        message.contentType = ContentType.text_plain
        this._outboxBuffer.push(message)

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
                console.log(`Processing message: ${inMessage.promptId} - ${inMessage.chunkId} - ${inMessage.body} - toolCallId: ${inMessage.toolCallId} - signalType: ${inMessage.signalType}`)

                if (inMessage.isSignalRequestComplete) {
                    this._activeRequestId = undefined
                }
                switch (inMessage.role) {
                    case KnownRoles.assistant:
                        if (this._currentPrompt) {
                            this._currentPrompt.onMessage(inMessage)
                            if (this._currentPrompt.hasCompleted)
                                this._currentPrompt = undefined
                        }
                        break
                    case KnownRoles.tool:
                        window.dispatchEvent(new CustomEvent('toolCall', {
                            detail: {
                                message: inMessage,
                                messageManager: this
                            } as ToolCallEvent
                        }))
                        break
                    default:
                        throw new Error("Unknown role: " + inMessage.role)
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