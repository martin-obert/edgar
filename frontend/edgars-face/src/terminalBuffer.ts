import {customRef, type Ref} from "vue";

export interface TerminalOutputBuffer {
    write: (message: string, options?: { append: boolean }) => void;
    clear: () => void;
    items: Ref<readonly string[]>
    length: Ref<number>
    pop: () => string | undefined
}

export function useTerminalBuffer(lineWidth: number = 50): TerminalOutputBuffer {
    const lines: string[] = []
    let triggerLength: () => void;
    let triggerItems: () => void;

    const items = customRef<readonly string[]>((track, trigger) => {
        triggerItems = trigger;
        return {
            get() {
                track();
                return [...lines];  // new reference each trigger
            },
            set() {
            },
        };
    });


    function write(message: string) {
        if (message == null || message.length === 0) return

        if (lines.length === 0) {
            lines.push(message)
            triggerLength()
            triggerItems()
            return;
        }

        let currentLine = lines[lines.length - 1]!

        if(currentLine.length + message.length > lineWidth) {
            lines.push(message)
            triggerLength()
            triggerItems()
            return;
        }

        lines[lines.length - 1] = currentLine + message
        triggerLength()
        return;

    }

    function clear() {
        lines.splice(0, lines.length)
        triggerLength()
        triggerItems()
    }

    const length = customRef<number>((track, trigger) => {
        triggerLength = trigger;
        return {
            get() {
                track();
                return lines.length;
            },
            set() { /* read-only */
            },
        };
    })

    function pop() {
        if(lines.length === 0) return undefined
        const result = lines.shift()
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