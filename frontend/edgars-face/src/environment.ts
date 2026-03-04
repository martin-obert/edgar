export default {
    API_URL: () => {
        const useTls = import.meta.env.VITE_USE_TLS === 'true'
        return useTls ? 'https://' : 'http://' + import.meta.env.VITE_API_URL
    },
    WS_URL: () => {
        const useTls = import.meta.env.VITE_USE_TLS === 'true'
        return useTls ? 'wss://' : 'ws://' + import.meta.env.VITE_API_URL + '/ws'
    }
}