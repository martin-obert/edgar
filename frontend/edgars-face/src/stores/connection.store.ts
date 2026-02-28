import {defineStore} from "pinia";
import {createWebSocketManager} from "../websocket-manager.ts";
import envVariables from "../environment.ts";

export const useConnectionStore = defineStore('connection', {
    state: () => ({
        ws: createWebSocketManager(envVariables.WS_URL)
    }),
    getters: {

        connectionState(state) {
            return state.ws.state;
        }
    }
})