import type {Router} from "vue-router";
import {customRef, type Ref} from "vue";
import type {IWebSocketManager} from "./websocket-manager.ts";

export interface TerminalMessage {
    type: 'in' | 'out',
    value: string
}

export interface TerminalOutputBuffer {
    write: (message: string | string[]) => void;
    clear: () => void;
    items: Ref<readonly string[]>
    length: Ref<number>
    pop: () => string | undefined
}

export interface TerminalCommand {
    name: string;
    description: string;
    execute: (outBuffer: TerminalOutputBuffer) => Promise<void> | void;
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
        execute: (_) => {
            messages.value = []
        }
    } as TerminalCommand
}

export const createHelpCommand: (commands: TerminalCommand[]) => TerminalCommand = (commands: TerminalCommand[]) => {
    return {
        name: "help",
        description: "Show help",
        execute: (buffer) => {
            if (commands) {
                buffer.write(commands.map(c => `${c.name} - ${c.description}`))
            }
        }
    } as TerminalCommand
}

export const createConnectCommand = (m: IWebSocketManager) => {
    return {
        name: "connect",
        description: "Connect to the server",
        execute: (_) => {
        }
    } as TerminalCommand
}

export const createLoremCommand = () => {
    return {
        name: "lorem",
        description: "Lorem",
        execute: (buffer) => {
            buffer.write("Sed sit amet imperdiet sem. Duis mollis turpis nec efficitur ultrices. Nulla sit amet erat tin.")
        }
    } as TerminalCommand
}

export function useTerminalBuffer(lineWidth: number = 50): TerminalOutputBuffer {
    const array: string[] = []
    let triggerLength: () => void;
    let triggerItems: () => void;

    const items = customRef<readonly string[]>((track, trigger) => {
        triggerItems = trigger;
        return {
            get() {
                track();
                return array;
            },
            set() { /* read-only */
            },
        };
    });

    function chunkString(str: string, maxLen: number) {
        const chunks = [];
        for (let i = 0; i < str.length; i += maxLen) {
            chunks.push(str.substring(i, i + maxLen));
        }
        return chunks;
    }

    function write(message: string | string[]) {
        if (typeof message === 'string') {
            array.push(...chunkString(message, lineWidth))
        } else {
            for (const m of message) {
                array.push(...chunkString(m, lineWidth))
            }
        }
        triggerLength()
        triggerItems()
    }

    function clear() {
        array.splice(0, array.length)
        triggerLength()
        triggerItems()
    }

    const length = customRef<number>((track, trigger) => {
        triggerLength = trigger;
        return {
            get() {
                track();
                return array.length;
            },
            set() { /* read-only */
            },
        };
    })

    function pop() {
        const result = array.shift()
        triggerLength()
        triggerItems()
        return result
    }
    return {
        write,
        clear,
        length,
        items,
        pop
    }
}