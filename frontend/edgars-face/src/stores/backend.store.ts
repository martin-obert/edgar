import {defineStore} from "pinia";
import {createWebSocketManager} from "../websocket-manager.ts";
import envVariables from "../environment.ts";
import {computed} from "vue";
import {createMessageManager} from "../message-manager.ts";
import {createRestApi} from "../rest.api.ts";

export const useBackendStore = defineStore('backend', () => {
    debugger
    const wsUrl = envVariables.WS_URL()
    const ws = createWebSocketManager(wsUrl)
    const connectionState = computed(() => {
        return ws.state;
    })
    const ms = createMessageManager(ws)
    const apiUrl = envVariables.API_URL()
    const rest= createRestApi(apiUrl)
    return {ws, connectionState, ms, rest}
})