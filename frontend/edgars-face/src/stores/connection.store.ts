import {defineStore} from "pinia";
import {createWebSocketManager} from "../websocket-manager.ts";
import envVariables from "../environment.ts";
import {computed} from "vue";
import {createMessageManager} from "../message-manager.ts";

export const useConnectionStore = defineStore('connection', () => {
    const ws = createWebSocketManager(envVariables.WS_URL)
    const connectionState = computed(() => {
        return ws.state;
    })
    const ms = createMessageManager(ws)
    return {ws, connectionState, ms}
})