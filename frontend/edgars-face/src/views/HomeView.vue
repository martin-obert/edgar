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
import {ref} from "vue";
import {createLoremCommand, createPlayCommand, type TerminalCommand} from "../commands.ts";
import EdgarsTerminal from "../components/EdgarsTerminal.vue";
import {useBackendStore} from "../stores/backend.store.ts";

const connection = useBackendStore()
const commands = ref<TerminalCommand[]>([createPlayCommand(useRouter(), connection.ws), createLoremCommand()])
</script>

<template>
  <EdgarsTerminal :commands="commands"/>
</template>

<style scoped>
</style>