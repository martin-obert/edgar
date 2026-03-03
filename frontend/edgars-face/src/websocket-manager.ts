import {Message, MessageT} from "./generated/edgar.ts";
import * as flatbuffers from "flatbuffers";
import type {IMessageStream} from "./message-manager.ts";
import {serializeMessage} from "./websocket-messaging.ts";

export enum WsError {
    UNINITIALIZED = 0,
    INVALID_STATE = 1,
}

export class WebSocketManagerError extends Error {
    private readonly _errorType: WsError;
    get errorType() {
        return this._errorType
    }

    constructor(message: string, errorType: WsError) {
        super(message);
        this.name = "WebSocketManagerError";
        this._errorType = errorType;
        Object.setPrototypeOf(this, WebSocketManagerError.prototype); // fix prototype chain
    }
}

export interface IWebSocketManager {
    get state(): WebSocketState

    connectAsync(timeout: number, cancellationToken: AbortSignal): Promise<void>

    disconnectAsync(code?: number, reason?: string): Promise<void>

}

export enum WebSocketState {
    UNSET = -1,
    CONNECTING = 0,
    OPEN = 1,
    CLOSING = 2,
    CLOSED = 3
}

class WebSocketManager implements IWebSocketManager, IMessageStream {
    private readonly _baseUrl: string;
    private _ws: WebSocket | null = null;

    private _stateInternal: WebSocketState = WebSocketState.UNSET

    get state(): WebSocketState {
        return this._stateInternal
    }

    constructor(baseUrl: string) {
        this._baseUrl = baseUrl;
    }

    get canWrite(): boolean {
        return this.state === WebSocketState.OPEN
    }

    get canRead(): boolean {
        return this.state === WebSocketState.OPEN
    }

    sendMessage(message: MessageT) {
        if (!this._ws)
            throw new WebSocketManagerError("WebSocket not set", WsError.UNINITIALIZED)

        if (this.state !== WebSocketState.OPEN)
            throw new WebSocketManagerError(`Invalid state: ${this.state}`, WsError.INVALID_STATE)


        const data = serializeMessage(message)
        console.log(`Sending data: ${data.length}`)
        this._ws.send(data)
    }

    onMessage?: (message: MessageT) => void;

    async disconnectAsync(code?: number, reason?: string): Promise<void> {
        if (this._ws) {
            console.log(`Disconnecting from: ${this._baseUrl}`)
            this._ws.close(code, reason)
        }
        this._ws = null

        this._updateState()
    }

    private _updateState() {

        this._stateInternal = this._ws?.readyState as WebSocketState ?? WebSocketState.UNSET
    }

    async connectAsync(timeout: number, cancellationSignal: AbortSignal): Promise<void> {
        await this.disconnectAsync()

        cancellationSignal.addEventListener('abort', async () => {
            // await this.disconnectAsync()
        })

        return new Promise<void>((resolve, reject) => {
            console.log(`Connecting to: ${this._baseUrl}`)

            if (this._ws) throw new Error("WebSocket not reset")

            this._updateState()
            this._ws = new WebSocket(this._baseUrl)
            this._updateState()

            this._ws.onopen = (e: Event) => {
                this._updateState()
                console.log(`Connection opened: ${e.type} - ${this._ws?.readyState}`)
                resolve()
            }
            this._ws.onerror = (e: Event) => {
                console.log(`Connection error: ${e.type}`)
            }
            this._ws.onclose = (e: CloseEvent) => {
                this._updateState()
                console.log(`Connection closed: CODE - ${e.code}, REASON - ${e.reason}, WAS_CLEAN - ${e.wasClean}`)
                reject(e)
            }

            this._ws.onmessage = async (e: MessageEvent<any>) => {
                const buffer = e.data instanceof Blob
                    ? await e.data.arrayBuffer()
                    : e.data;
                const uintArray = new Uint8Array(buffer)
                const bb = new flatbuffers.ByteBuffer(uintArray);
                const message = Message.getRootAsMessage(bb).unpack();
                if (this.onMessage) this.onMessage(message)
            }

            setTimeout(async () => {
                if (this.state === WebSocketState.OPEN || cancellationSignal.aborted) {
                    return
                }
                console.log("Connection timeout")
                await this.disconnectAsync()
                reject(new Error("Connection timeout"))
            }, timeout)
        })
    }

}

export const WebSocketStateToString = (state: WebSocketState) => {
    switch (state) {
        case WebSocketState.UNSET:
            return "UNSET"
        case WebSocketState.CONNECTING:
            return "CONNECTING"
        case WebSocketState.OPEN:
            return "OPEN"
        case WebSocketState.CLOSING:
            return "CLOSING"
        case WebSocketState.CLOSED:
            return "CLOSED"
        default:
            return `UNKNOWN ${state}`
    }
}

export const createWebSocketManager = (baseUrl: string): IWebSocketManager & IMessageStream => new WebSocketManager(baseUrl)