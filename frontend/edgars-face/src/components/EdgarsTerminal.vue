<script setup lang="ts">
import {nextTick, onMounted, ref, watch} from "vue";
import CypherSentence from "../components/CypherSentence.vue";
import {createClearCommand, createHelpCommand, type TerminalCommand, type TerminalMessage,} from "../commands.ts";
import {useTerminalBuffer} from "../terminalBuffer.ts";
import {onKeyStroke} from "@vueuse/core";

const {commands} = defineProps<{ commands: TerminalCommand[] }>()
const messages = ref<TerminalMessage[]>([])
const commandInput = ref<HTMLInputElement | null>(null)
const messageStackSize = 5

onMounted(() => {
  if (commandInput.value) {
    commandInput.value.focus()
  }

})

const c = [
  ...commands,
  createClearCommand(messages)
]
c.push(createHelpCommand(c))

const internalCommands = ref<TerminalCommand[]>(c)
const pushMessage = (message: TerminalMessage) => {
  if (messages.value.length >= messageStackSize) {
    messages.value.shift()
  }
  messages.value.push(message)
}
const outBuffer = useTerminalBuffer()
const abortController = ref<AbortController>(new AbortController())
const currentCommand = ref<TerminalCommand | undefined>()
const renderingBuffer = ref(false)
const executeCommand = async (value: string) => {
  pushMessage({value: value, type: 'in'})

  if (commandInput.value)
    commandInput.value.value = ""

  const commandSaturated = value.trim()
  if (commandSaturated.length === 0) return

  currentCommand.value = internalCommands.value.find(command => command.canProcess(commandSaturated))
  if (currentCommand.value) {
    try {
      await currentCommand.value.execute({
        buffer: outBuffer,
        cancellationToken: abortController.value.signal,
        command: commandSaturated
      })
    } finally {
      currentCommand.value = undefined
    }
  } else {
    pushMessage({value: `Unknown command: ${commandSaturated}, help`, type: 'out'})
  }
}


const popBuffer = () => {
  if (outBuffer.length.value > 0) {
    pushMessage({value: outBuffer.pop()!, type: 'out'})
  }
}

const cypher = ref<string | undefined>(undefined)

watch(outBuffer.items, async (items) => {
  if (items.length > 0) {
    if (items[0] !== cypher.value) {
      cypher.value = undefined
      await nextTick(() => {
        if (items.length > 0) {
          renderingBuffer.value = true
          cypher.value = items[0]
        } else {
          if (renderingBuffer.value)
            renderingBuffer.value = false
          commandInput.value?.focus()
          cypher.value = undefined
        }
      })
    }
  } else {
    if (renderingBuffer.value)
      renderingBuffer.value = false
    cypher.value = undefined
    commandInput.value?.focus()
  }

})

onKeyStroke('Escape', (e) => {
  e.preventDefault()
  abortController.value.abort()
  abortController.value = new AbortController()
  if (cypher.value)
    messages.value.push({value: cypher.value, type: 'out'})
  outBuffer.clear()
})
</script>

<template>
  <div class="flex flex-col items-center justify-center" @click="commandInput?.focus()">
    <div class="bezel" style="width: 100ch">
      <div class="crt monitor fx-scanlines fx-rgb fx-flicker fx-curve fx-glow fx-roll"
           id="green">
        <div class="content" :style="{margin: '1em', marginBottom: '1.5em'}">
          <div style="font-family: 'VT323', monospace" :style="{height: `${messageStackSize+1}lh`}">
            <p style="margin: 0;">
            <span v-for="(message, index) in messages" :key="index">
              {{ message.value }}<br></span>
              <CypherSentence v-if="cypher" :sentence="cypher" @done="popBuffer"/>
            </p>
            <i style="margin: 0 1ch 0 -2ch;display:inline-block;position:absolute">&gt;</i>
            <input name="commandInput"
                   :placeholder="messages.length === 0 ? 'help' : (renderingBuffer || currentCommand !== undefined) ? 'Processing ... (ESC)' : 'Enter command'"
                   class="w-full"
                   :disabled="renderingBuffer || currentCommand !== undefined"
                   ref="commandInput"
                   v-on:keyup.enter="executeCommand(($event.target as HTMLInputElement).value)"/>
          </div>
        </div>
        <div class="vignette"></div>
        <div class="rolling-bar"></div>
      </div>
    </div>
  </div>
</template>

<style scoped>
@import url('../monitor-effect.css');

.content input, .content input:focus-visible {
  background-color: transparent;
  border: none;
  outline: none;
  cursor: none;
  caret-shape: block;
  caret-color: #33ff33;
  font-family: 'VT323', monospace;
  font-size: 18px;
  line-height: 1;
  color: #33ff33;
  text-shadow: 0 0 8px rgba(51, 255, 51, 0.5);
  vertical-align: middle;
  padding: 0;
  margin: 0;
}

</style>