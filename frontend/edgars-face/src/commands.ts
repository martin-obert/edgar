import type {Router} from "vue-router";
import {type Ref} from "vue";
import type {IWebSocketManager} from "./websocket-manager.ts";
import type {TerminalOutputBuffer} from "./terminalBuffer.ts";

export interface TerminalMessage {
    type: 'in' | 'out',
    value: string
}

export interface TerminalCommandContext {
    buffer: TerminalOutputBuffer,
    cancellationToken: AbortSignal
}


export interface TerminalCommand {
    name: string;
    description: string;
    execute: (context: TerminalCommandContext) => Promise<void> | void;
    abort?: () => void | Promise<void>;
}

export const createPlayCommand = (router: Router) => {
    return {
        name: "play",
        description: "Play the game",
        execute: async (_) => {
            await router.push('/game')
        }
    } as TerminalCommand
}

export const createClearCommand = (messages: Ref<TerminalMessage[]>) => {
    return {
        name: "clear",
        description: "Clear the terminal",
        execute: (_) => {
            messages.value = []
        }
    } as TerminalCommand
}

export const createHelpCommand: (commands: TerminalCommand[]) => TerminalCommand = (commands: TerminalCommand[]) => {
    return {
        name: "help",
        description: "Show help",
        execute: ({buffer}) => {
            if (commands) {
                buffer.write(commands.map(c => `${c.name} - ${c.description}`))
            }
        }
    } as TerminalCommand
}

export const createConnectCommand = (ws: IWebSocketManager) => {
    return {
        name: "connect",
        description: "Connect to the server",
        execute: async ({buffer, cancellationToken}) => {
            const timeout = 1500
            const reconnectionAttempts = 10
            buffer.write("Connecting...")
            for (let i = 0; i < reconnectionAttempts; i++) {
                if(cancellationToken.aborted) return
                try {
                    await ws.connectAsync(timeout, cancellationToken)
                    buffer.write("Connected")
                    return
                } catch (e) {
                    if(cancellationToken.aborted){
                        buffer.write("Connection aborted")
                        return
                    }
                    buffer.write(`Reconnecting ${i+1}/${reconnectionAttempts}...`)
                    await new Promise(resolve => setTimeout(resolve, timeout))
                }
            }
        }
    } as TerminalCommand
}

export const createLoremCommand = () => {
    return {
        name: "lorem",
        description: "Lorem",
        execute: ({buffer}) => {
            buffer.write("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla imperdiet fermentum sapien in feugiat. Cras molestie leo eget lacus efficitur, eget ultricies odio viverra. In hac habitasse platea dictumst. Curabitur velit erat, bibendum ut quam vel, elementum bibendum magna. Proin est elit, facilisis sed odio auctor, elementum viverra dui. Sed imperdiet, ante id tempor elementum, nunc orci posuere ipsum, auctor pharetra elit turpis eu ligula. Nulla vel purus eu ante feugiat dignissim. Cras auctor malesuada faucibus. Etiam non ornare lacus. Vestibulum scelerisque quam ut interdum semper. Ut nec felis ac velit rutrum porttitor. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Maecenas fermentum mattis pulvinar.")
        }
    } as TerminalCommand
}
