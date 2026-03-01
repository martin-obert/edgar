import type {Router} from "vue-router";
import type {Ref} from "vue";
import type {IWebSocketManager} from "./websocket-manager.ts";

export interface TerminalMessage {
    type: 'in' | 'out',
    value: string
}

export interface TerminalCommand {
    name: string;
    description: string;
    execute: () => string[] | Promise<string[]> | Promise<void> | null;
    abort?: () => void | Promise<void>;
}

export const createPlayCommand = (router: Router) => {
    return {
        name: "play",
        description: "Play the game",
        execute: async () => {
            await router.push('/game')
        }
    } as TerminalCommand
}

export const createClearCommand = (messages: Ref<TerminalMessage[]>) => {
    return {
        name: "clear",
        description: "Clear the terminal",
        execute: () => {
            messages.value = []
        }
    } as TerminalCommand
}

export const createHelpCommand: (commands: TerminalCommand[]) => TerminalCommand = (commands: TerminalCommand[]) => {
    return ({
        name: "help",
        description: "Show help",
        execute: () => {
            if (commands) {
                return commands.map(c => `${c.name} - ${c.description}`)
            }
            return []
        }
    } as TerminalCommand)
}

export const createConnectCommand = (m: IWebSocketManager)=>{
    return {
        name: "connect",
        description: "Connect to the server",
        execute: () => {
            m.connect()
            return []
        }
    } as TerminalCommand
}