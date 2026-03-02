import {Message, MessageT} from "./generated/edgar.ts";
import * as flatbuffers from "flatbuffers";

export interface IWebSocketManager {
    get state(): WebSocketState

    connectAsync(timeout: number, cancellationToken: AbortSignal): Promise<void>

    disconnectAsync(code?: number, reason?: string): Promise<void>

    sendAsync(data: Uint8Array): Promise<void>
}

export enum WebSocketState {
    UNSET = -1,
    CONNECTING = 0,
    OPEN = 1,
    CLOSING = 2,
    CLOSED = 3
}

class WebSocketManager implements IWebSocketManager {
    private readonly _baseUrl: string;
    private _ws: WebSocket | null = null;

    private _stateInternal: WebSocketState = WebSocketState.UNSET

    get state(): WebSocketState {
        return this._stateInternal
    }

    constructor(baseUrl: string) {
        this._baseUrl = baseUrl;
    }

    sendAsync(_: Uint8Array): Promise<void> {
        throw new Error("Method not implemented.");
    }

    async disconnectAsync(code?: number, reason?: string): Promise<void> {
        if (this._ws) {
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
            await this.disconnectAsync()
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
            this._ws.onmessage = (e: MessageEvent<any>) => {
                console.log(`Received message: ${e.type}`)
                const array = new Uint8Array(e.data.arrayBuffer())
                const bb = new flatbuffers.ByteBuffer(array)
                const message = Message.getRootAsMessage(bb).unpack()
                console.log(new TextDecoder().decode(message.body!))
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

export const createWebSocketManager = (baseUrl: string): IWebSocketManager => new WebSocketManager(baseUrl)