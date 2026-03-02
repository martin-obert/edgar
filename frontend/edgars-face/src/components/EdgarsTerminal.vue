<script setup lang="ts">

// import {onMounted, onUnmounted, ref} from "vue";
// import {Request, RequestT} from "../generated/edgar.ts";
// import {Builder, ByteBuffer} from "flatbuffers";
//
// const received = ref<string | null>()
// const input = ref<string | null>()
// const ws = ref<WebSocket | null>()
// onMounted(() => {
//   const wSocket = new WebSocket("ws://127.0.0.1:8000/ws");
//   wSocket.onopen = () => {
//     console.log("Connection opened");
//   }
//   wSocket.onclose = () => {
//     console.log("Connection closed");
//   }
//   wSocket.onmessage = async (e: MessageEvent) => {
//     console.log(`Received message: ${e.type}`);
//     const arrayBuffer = await e.data.arrayBuffer();
//     const buf = new ByteBuffer(new Uint8Array(arrayBuffer));
//     const req = Request.getRootAsRequest(buf);
//     received.value = new TextDecoder().decode(req.bodyArray());
//   }
//   wSocket.onerror = (e: Event) => {
//     console.log(`Received message: ${e.type}`);
//   }
//
//   ws.value = wSocket
// })
//
// onUnmounted(() => {
//   if (ws.value) ws.value.close()
// })
//
// const send = () => {
//   const r = new RequestT();
//   const b = new Builder(256)
//   b.finish(r.pack(b))
//   ws.value!.send(b.asUint8Array())
// }

import {nextTick, onMounted, ref, watch} from "vue";
import CypherSentence from "../components/CypherSentence.vue";
import {
  createClearCommand,
  createHelpCommand,
  type TerminalCommand,
  type TerminalMessage,
} from "../commands.ts";
import {useTerminalBuffer} from "../terminalBuffer.ts";
import {onKeyStroke} from "@vueuse/core";

const {commands, startup} = defineProps<{ commands: TerminalCommand[], startup?: string }>()
const messages = ref<TerminalMessage[]>([])
const commandInput = ref<HTMLInputElement | null>(null)
const messageStackSize = 5
const lineLen = 100

onMounted(() => {
  if (commandInput.value) {
    commandInput.value.focus()
  }

  if (startup)
    executeCommand(startup, {push: false})
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
const executeCommand = async (value: string, options?: { push?: boolean }) => {
  if (options && options.push)
    pushMessage({value: value, type: 'in'})

  if (commandInput.value)
    commandInput.value.value = ""

  const commandSaturated = value.trim()
  if (commandSaturated.length === 0) return

  currentCommand.value = internalCommands.value.find(command => command.name === commandSaturated)
  if (currentCommand.value) {
    try {
      await currentCommand.value.execute({
        buffer: outBuffer,
        cancellationToken: abortController.value.signal,
      })
    } finally {
      currentCommand.value = undefined
    }
  } else {
    pushMessage({value: `Unknown command: ${commandSaturated}, /help`, type: 'out'})
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
  outBuffer.clear()
})
</script>

<template>
  <div class="flex flex-col items-center justify-center" @click="commandInput?.focus()"
       style="max-width: 50%; max-height: 50%;">
    <div class="bezel">
      <div :style="{width: `${lineLen}ch`}" class="crt monitor fx-scanlines fx-rgb fx-flicker fx-curve fx-glow fx-roll"
           id="green">
        <div class="content" :style="{margin: '1em', marginBottom: '1.5em'}">
          <div style="font-family: 'VT323', monospace" :style="{height: `${messageStackSize+1}lh`}">
            <p style="margin: 0;">
            <span v-for="(message, index) in messages" :key="index">
              {{ message.value }}<br></span>
              <CypherSentence v-if="cypher" :sentence="cypher" @done="popBuffer"/>
            </p>
            <input name="commandInput" v-if="!renderingBuffer && !currentCommand" ref="commandInput"
                   v-on:keyup.enter="executeCommand(($event.target as HTMLInputElement).value)"/>
          </div>
          <div v-if="renderingBuffer || currentCommand"><p style="margin: 0;">Processing ... (ESC)</p></div>
          <div v-else>&nbsp;</div>
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