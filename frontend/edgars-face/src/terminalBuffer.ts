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

        // Get the last line
        const appendedLine = lines[lines.length - 1] ?? ''

        if (appendedLine.length + message.length > lineWidth) {
            lines.push(message)
            triggerLength()
            triggerItems()
            return
        }

        // Append word and check if the current line is overflowing
        const multiLines = appendedLine + message

        if (lines.length === 0) {
            lines.push(multiLines)
            triggerLength()
            triggerItems()
            return
        }

        const changedLine = multiLines
        if (changedLine)
            lines[lines.length - 1] = changedLine
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