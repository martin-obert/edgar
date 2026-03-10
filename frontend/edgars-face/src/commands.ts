import type {Router} from "vue-router";
import {type Ref} from "vue";
import {type IWebSocketManager, WebSocketState} from "./websocket-manager.ts";
import type {TerminalOutputBuffer} from "./terminalBuffer.ts";
import {type IMessageManager} from "./message-manager.ts";
import {v4 as uuid} from "uuid";

export interface TerminalMessage {
    type: 'in' | 'out',
    value: string
}

export interface TerminalCommandContext {
    buffer: TerminalOutputBuffer,
    cancellationToken: AbortSignal,
    command: string
}


export interface TerminalCommand {
    name: string;
    description: string;
    canProcess: (command: string) => boolean;
    execute: (context: TerminalCommandContext) => Promise<void> | void;
    abort?: () => void | Promise<void>;
}

export const createPlayCommand = (router: Router, ws: IWebSocketManager) => {
    return {
        name: "play",
        canProcess: (val) => val.startsWith('play'),
        description: "Play the game",
        execute: async ({buffer, cancellationToken}) => {
            const sessionId = uuid()

            buffer.write("Connecting to E.D.G.A.R. interface...")
            await connectProcedure(sessionId, ws, {buffer, cancellationToken, command: "connect"})
            await router.push(`/game/${sessionId}`)
        }
    } as TerminalCommand
}

export const createClearCommand = (messages: Ref<TerminalMessage[]>) => {
    return {
        name: "clear",
        canProcess: (val) => val.startsWith('clear'),
        description: "Clear the terminal",
        execute: (_) => {
            messages.value = []
        }
    } as TerminalCommand
}

export const createHelpCommand: (commands: TerminalCommand[]) => TerminalCommand = (commands: TerminalCommand[]) => {
    return {
        name: "help",
        canProcess: (val) => val.startsWith('help'),
        description: "Show help",
        execute: ({buffer}) => {
            if (commands) {
                for (const c of commands) {
                    buffer.write(`${c.name} - ${c.description}`)
                }
            }
        }
    } as TerminalCommand
}

export const createLoremCommand = () => {
    return {
        name: "lorem",
        canProcess: (val) => val.startsWith('lorem'),
        description: "Lorem",
        execute: ({buffer}) => {
            buffer.write("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla imperdiet fermentum sapien in feugiat. Cras molestie leo eget lacus efficitur, eget ultricies odio viverra. In hac habitasse platea dictumst. Curabitur velit erat, bibendum ut quam vel, elementum bibendum magna. Proin est elit, facilisis sed odio auctor, elementum viverra dui. Sed imperdiet, ante id tempor elementum, nunc orci posuere ipsum, auctor pharetra elit turpis eu ligula. Nulla vel purus eu ante feugiat dignissim. Cras auctor malesuada faucibus. Etiam non ornare lacus. Vestibulum scelerisque quam ut interdum semper. Ut nec felis ac velit rutrum porttitor. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Maecenas fermentum mattis pulvinar.")
        }
    } as TerminalCommand
}


export const createPromptCommand = (ms: IMessageManager, ws: IWebSocketManager, sessionId: string) => {
    return {
        name: ":[your-prompt-text]",
        canProcess: (val) => val.startsWith(':'),
        description: "Prompt E.D.G.A.R.s",
        execute: async ({command, buffer, cancellationToken}) => {
            if (ws.state === WebSocketState.CLOSED || ws.state === WebSocketState.CLOSING || ws.state === WebSocketState.UNSET) {
                const result = await connectProcedure(sessionId, ws, {buffer, cancellationToken, command})
                if (!result) {
                    buffer.write("Unable to process prompt, no connection to E.D.G.A.R. available. Please try again later.")
                    return
                }
            }
            try {
                const request = ms.sendPromptRequest(command.substring(1), {
                    onResponse: (r) => {
                        buffer.write(r.content)
                    }
                })

                await request.wait(10000)

                console.log("Sent prompt", request.id)
            } catch (e) {
                buffer.write("Error sending prompt: " + e)
                console.error(e)
            }
        }
    } as TerminalCommand
}

export const createExitCommand = (router: Router, route: string) => {
    return {
        name: "exit",
        canProcess: (val) => val === 'q' || val === 'exit',
        description: "Exit E.D.G.A.R.",
        execute: async (_) => {
            await router.push(route)
        }
    } as TerminalCommand
}

const connectProcedure = async (sessionId: string, ws: IWebSocketManager, {cancellationToken}: TerminalCommandContext) => {
    const timeout = 1500
    const reconnectionAttempts = 10
    console.log(`Connecting to session ... ${sessionId}`)

    for (let i = 0; i < reconnectionAttempts; i++) {
        if (cancellationToken.aborted) return
        try {
            await ws.connectAsync(sessionId, timeout, cancellationToken)
            console.log("Connected")
            return true
        } catch (e) {
            if (cancellationToken.aborted) {
                console.log("Connection aborted")
                break
            }
            console.log(`Reconnecting ${i + 1}/${reconnectionAttempts}...`)
            await new Promise(resolve => setTimeout(resolve, timeout))
        }
    }
    return false
}