import {customRef, type Ref} from "vue";

export interface TerminalOutputBuffer {
    write: (message: string | string[]) => void;
    clear: () => void;
    items: Ref<readonly string[]>
    length: Ref<number>
    pop: () => string | undefined
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
                return [...array];  // new reference each trigger
            },
            set() {
            },
        };
    });

    function smartSplit(text: string, maxLen = 100) {
        const lines = [];
        let remaining = text;

        while (remaining.length > maxLen) {
            // Find the last space within the limit
            let splitAt = remaining.lastIndexOf(' ', maxLen);

            // If no space found, force split at maxLen
            if (splitAt === -1) splitAt = maxLen;

            lines.push(remaining.slice(0, splitAt).trim());
            remaining = remaining.slice(splitAt).trim();
        }

        if (remaining) lines.push(remaining);
        return lines;
    }

    function write(message: string | string[]) {
        if (typeof message === 'string') {
            array.push(...smartSplit(message, lineWidth))
        } else {
            for (const m of message) {
                array.push(...smartSplit(m, lineWidth))
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