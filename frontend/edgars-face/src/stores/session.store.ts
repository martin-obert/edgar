import {defineStore} from "pinia";
import type {SessionConfiguration} from "../rest.api.ts";

export const useSessionStore = defineStore('session', () => {
    const sessionsJson = localStorage.getItem('sessions')
    const sessionsDb = JSON.parse(sessionsJson || '{}')

    function putSession(id: string, session: SessionConfiguration) {
        sessionsDb[id] = session
        localStorage.setItem('sessions', JSON.stringify(sessionsDb))
    }

    function getSession(id: string) {
        return sessionsDb[id]
    }

    function listSessions() {
        return sessionsDb as Record<string, SessionConfiguration>
    }

    function deleteSession(id: string) {
        delete sessionsDb[id]
        localStorage.setItem('sessions', JSON.stringify(sessionsDb))
    }

    return {putSession, getSession, listSessions, deleteSession}
})