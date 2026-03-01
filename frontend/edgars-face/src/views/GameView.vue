<script setup lang="ts">

import {computed, onMounted, onUnmounted, ref, watch} from "vue";
import {useConnectionStore} from "../stores/connection.store.ts";
import {WebSocketState, WebSocketStateToString} from "../websocket-manager.ts";
import EdgarsTerminal from "../components/EdgarsTerminal.vue";

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
    <EdgarsTerminal :commands="[]"/>
    <sub>{{ WebSocketStateToString(connectionStore.connectionState) }}</sub>
    <div v-if="connectionStore.connectionState === WebSocketState.UNSET || connectionStore.connectionState === WebSocketState.CLOSED">
      <Button label="Reconnect" @click="reconnect"/>
      <ToggleSwitch v-model="connectionStore.ws.autoReconnect"/>
    </div>
  </div>
</template>

<style scoped>

</style>