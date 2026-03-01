<script setup lang="ts">

import {onMounted, onUnmounted} from "vue";
import {useConnectionStore} from "../stores/connection.store.ts";
import {WebSocketState, WebSocketStateToString} from "../websocket-manager.ts";
import EdgarsTerminal from "../components/EdgarsTerminal.vue";
import {createLoremCommand} from "../commands.ts";

const connectionStore = useConnectionStore()

onMounted(async () => {
  await connectionStore.ws.connectAsync()
})

onUnmounted(async () => {
  await connectionStore.ws.disconnectAsync()
})

const reconnect = async () => {
  await connectionStore.ws.reset()
  await connectionStore.ws.connectAsync()
}


</script>

<template>
  <div>
    <EdgarsTerminal :commands="[createLoremCommand()]"/>
    <sub>{{ WebSocketStateToString(connectionStore.connectionState) }}</sub>
    <div v-if="connectionStore.connectionState === WebSocketState.UNSET || connectionStore.connectionState === WebSocketState.CLOSED">
      <Button label="Reconnect" @click="reconnect"/>
      <ToggleSwitch v-model="connectionStore.ws.autoReconnect"/>
    </div>
  </div>
</template>

<style scoped>

</style>