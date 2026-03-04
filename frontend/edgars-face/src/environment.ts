export default {
    API_URL: () => {
        const useTls = import.meta.env.VITE_USE_TLS === 'true'
        const url = import.meta.env.VITE_API_URL
        return (useTls ? 'https://' : 'http://') + url
    },
    WS_URL: () => {
        const useTls = import.meta.env.VITE_USE_TLS === 'true'
        const url = import.meta.env.VITE_API_URL
        return (useTls ? 'wss://' : 'ws://') + url + '/ws'
    }
}