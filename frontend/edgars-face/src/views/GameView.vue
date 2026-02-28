<script setup lang="ts">

import {computed, onMounted, onUnmounted, ref, watch} from "vue";
import {useConnectionStore} from "../stores/connection.store.ts";
import {WebSocketState, WebSocketStateToString} from "../websocket-manager.ts";

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
    <h1>E.D.G.A.R.s</h1>
    <sub>{{ WebSocketStateToString(connectionStore.connectionState) }}</sub>
    <div v-if="connectionStore.connectionState === WebSocketState.UNSET">
      <Button label="Reconnect" @click="reconnect"/>
      <ToggleSwitch v-model="connectionStore.ws.autoReconnect"/>
    </div>
  </div>
</template>

<style scoped>

</style>