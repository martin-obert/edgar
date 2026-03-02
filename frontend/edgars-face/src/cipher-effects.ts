import {type MaybeRefOrGetter, onScopeDispose, ref, toValue} from "vue";

export interface CipherSentenceEffectOptions {
    completeCallback: () => void
}

export function useCipherSentenceEffect(sentence: string, options?: CipherSentenceEffectOptions) {
    const words = sentence.split(' ')
    const index = ref<number>(0)
    let isComplete = false

    function moveNext() {
        if (isComplete) {
            return
        }
        index.value++
        if (index.value >= words.length) {
            isComplete = true
            options?.completeCallback()
        }
    }

    return {
        words,
        index,
        moveNext
    }
}

export interface CipherWordEffectOptions {
    completeCallback: () => void,
    doDecipher: MaybeRefOrGetter<boolean>
}

export function useCipherWordEffect(word: string, options?: CipherWordEffectOptions) {
    const letters = [...word]
    const letterBuffer = [...word]

    const index = ref<number>(0)
    const startTime = new Date().getTime()
    let isComplete = false
    const renderedWord = ref<string>(Array.from(word, () => String.fromCharCode(97 + Math.random() * 26 | 0)).join(""))
    const loopSpeed = 50
    const decipherSpeed = loopSpeed + 50
    let lastTrigger = 0
    let loopTime = 0

    function doScramble() {
        if (isComplete) return

        for (let i = index.value; i < letterBuffer.length; i++) {
            letterBuffer[i] = String.fromCharCode(97 + Math.random() * 26 | 0)
        }
    }

    function doDecipherLoop() {
        if (isComplete) return
        if (lastTrigger + decipherSpeed > loopTime) return
        lastTrigger = loopTime
        if (options && !toValue(options.doDecipher)) return
        for (let i = 0; i < index.value; i++) {
            letterBuffer[i] = letters[i]!
        }
        index.value++
    }

    const intervalHandle = setInterval(() => {
        doScramble()
        doDecipherLoop()

        renderedWord.value = letterBuffer.join("")
        loopTime += loopSpeed
        if (index.value > letters.length) {
            clearInterval(intervalHandle)
            isComplete = true
            options?.completeCallback()
            return
        }
    }, loopSpeed)

    onScopeDispose(() => {
        clearInterval(intervalHandle)
    })


    return {
        letters,
        index,
        renderedWord,
        isComplete,
        startTime,
    }
}