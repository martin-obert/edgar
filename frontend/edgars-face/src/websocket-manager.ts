export interface IWebSocketManager {
    get state(): WebSocketState

    connectAsync(reset?: boolean): Promise<void>

    disconnectAsync(code?: number, reason?: string): Promise<void>

    sendAsync(data: Uint8Array): Promise<void>

    autoReconnect: boolean

    reset(): Promise<void>
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
    private _reconnectFncHandle: number | null = null;
    private _reconnectAttemptsBudget: number = 0;

    private _stateInternal: WebSocketState = WebSocketState.UNSET

    get state(): WebSocketState {
        return this._stateInternal
    }

    constructor(baseUrl: string, private readonly options: { timeout?: number, retryAttempts?: number } = {
        timeout: 1000,
        retryAttempts: 3
    }) {
        this._baseUrl = baseUrl;
        this._reconnectAttemptsBudget = options.retryAttempts ?? 3;
    }

    autoReconnect: boolean = false

    async reset(): Promise<void> {
        this._reconnectAttemptsBudget = this.options.retryAttempts ?? 3;
    }

    sendAsync(data: Uint8Array): Promise<void> {
        throw new Error("Method not implemented.");
    }

    async disconnectAsync(code?: number, reason?: string): Promise<void> {
        if (this._ws) {
            this._ws.close(code, reason)
        }
        this._ws = null

        this._updateState()
    }

    private stopReconnectFnc() {
        if (this._reconnectFncHandle) {
            window.clearTimeout(this._reconnectFncHandle)
            this._reconnectFncHandle = null
        }
    }

    private _updateState() {

        this._stateInternal = this._ws?.readyState as WebSocketState ?? WebSocketState.UNSET
    }

    async connectAsync(reset: boolean = false): Promise<void> {
        await this.disconnectAsync()

        if (reset) await this.reset()

        if (this._ws) throw new Error("WebSocket not reset")

        this._updateState()
        this._ws = new WebSocket(this._baseUrl)

        this._reconnectFncHandle = window.setTimeout(async () => {
            this._reconnectAttemptsBudget--
            await this.connectAsync()
            if (this._reconnectAttemptsBudget <= 0) {
                this._reconnectAttemptsBudget = 0
                if (this.autoReconnect) return
                await this.disconnectAsync()
                this.stopReconnectFnc()
            }
        }, this.options.timeout)

        this._ws.onopen = (e: Event) => {
            this.stopReconnectFnc()
            this._updateState()
            console.log(`Connection opened: ${e.type} - ${this._ws?.readyState}`)
        }
        this._ws.onerror = (e: Event) => {

            console.log(`Connection error: ${e.type}`)
        }
        this._ws.onclose = (e: CloseEvent) => {
            this._updateState()
            console.log(`Connection closed: CODE - ${e.code}, REASON - ${e.reason}, WAS_CLEAN - ${e.wasClean}`)

        }
        this._ws.onmessage = (e: MessageEvent<any>) => {
            console.log(`Received message: ${e.type}`)
            this._stateInternal = WebSocketState.CLOSED
        }
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
    }
}

export const createWebSocketManager = (baseUrl: string): IWebSocketManager => new WebSocketManager(baseUrl)