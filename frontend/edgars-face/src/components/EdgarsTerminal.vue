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

import {useRouter} from "vue-router";
import {onMounted, ref} from "vue";
import CypherSentence from "../components/CypherSentence.vue";
import {createClearCommand, createHelpCommand, type TerminalCommand, type TerminalMessage} from "../commands.ts";

const {commands} = defineProps<{ commands: TerminalCommand[] }>()

const router = useRouter()

const messages = ref<TerminalMessage[]>([])
const command = ref<string>("")
const commandInput = ref<HTMLInputElement | null>(null)


onMounted(() => {
  if (commandInput.value) {
    commandInput.value.focus()
  }
  // setInterval(() => {
  //   if (i >= originalSentence[0]!.length) return
  //   i++
  // }, 130)
  //
  // setInterval(() => {
  //
  //   nextWord.value = Array.from(originalSentence[0]!).map((l, idx) => idx < i ? l : randomLetter()).join("")
  // }, 50)
  // setInterval(() => {
  //   if (index >= originalSentence.length) return
  //   currentSentence.value += originalSentence[index] + (index + 1 >= originalSentence.length ? "" : " ")
  //   index++
  //
  // }, 1000)
})

if (!commands.some(x => x.name === "help")) {
  commands.push(createHelpCommand(commands))
}
if (!commands.some(x => x.name === 'clear')) {
  commands.push(createClearCommand(messages))
}
const outBuffer = ref<string[]>([])

const enterCommand = async () => {
  messages.value.push({value: command.value, type: 'in'})
  const commandSaturated = command.value.trim()
  const handler = commands.find(command => command.name === commandSaturated)
  if (handler) {
    const result = await handler.execute()
    if (result != undefined) outBuffer.value.push(...result)
  } else {
    messages.value.push({value: `Unknown command: ${commandSaturated}, /help`, type: 'out'})
  }
  command.value = ""
}
const popBuffer = () => {
  if (outBuffer.value.length > 0) {
    messages.value.push({value: outBuffer.value.shift()!, type: 'out'})
  }
}

</script>

<template>
  <div class="flex flex-col items-center justify-center" style="max-width: 50%; max-height: 50%;">
    <div class="bezel">
      <div class="crt monitor fx-scanlines fx-rgb fx-flicker fx-curve fx-glow fx-roll" id="green">
        <div class="content">
          <p>
            <span v-for="(message, index) in messages" :key="index">
              {{ message.value}}<br></span>
            <CypherSentence v-if="outBuffer.length > 0" :sentence="outBuffer[0]!" @done="popBuffer"/>
            <input ref="commandInput" v-model="command" v-on:keyup.enter="enterCommand"/>
          </p>
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