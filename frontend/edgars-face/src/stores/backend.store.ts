import {defineStore} from "pinia";
import {createWebSocketManager} from "../websocket-manager.ts";
import envVariables from "../environment.ts";
import {computed} from "vue";
import {createMessageManager} from "../message-manager.ts";
import {createRestApi} from "../rest.api.ts";

export const useBackendStore = defineStore('backend', () => {
    const ws = createWebSocketManager(envVariables.WS_URL())
    const connectionState = computed(() => {
        return ws.state;
    })
    const ms = createMessageManager(ws)
    const rest= createRestApi(envVariables.API_URL())
    return {ws, connectionState, ms, rest}
})