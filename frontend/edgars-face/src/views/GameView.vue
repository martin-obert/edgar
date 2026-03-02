<script setup lang="ts">
import {useConnectionStore} from "../stores/connection.store.ts";
import EdgarsTerminal from "../components/EdgarsTerminal.vue";
import {createConnectCommand} from "../commands.ts";

const connectionStore = useConnectionStore()


</script>

<template>
  <div>
    <EdgarsTerminal :commands="[createConnectCommand(connectionStore.ws)]" startup="connect"/>
<!--    <sub>{{ WebSocketStateToString(connectionStore.connectionState) }}</sub>-->
<!--    <div v-if="connectionStore.connectionState === WebSocketState.UNSET || connectionStore.connectionState === WebSocketState.CLOSED">-->
<!--      <Button label="Reconnect" @click="reconnect"/>-->
<!--      <ToggleSwitch v-model="connectionStore.ws.autoReconnect"/>-->
<!--    </div>-->
  </div>
</template>

<style scoped>

</style>