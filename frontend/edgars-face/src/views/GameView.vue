<script setup lang="ts">
import {useConnectionStore} from "../stores/connection.store.ts";
import EdgarsTerminal from "../components/EdgarsTerminal.vue";
import {createExitCommand, createPromptCommand} from "../commands.ts";
import {onMounted, onUnmounted} from "vue";
import {useRouter} from "vue-router";

const {sessionId} = defineProps<{ sessionId: string }>()

const connectionStore = useConnectionStore()
const router = useRouter()
const gameCommands = [createPromptCommand(connectionStore.ms, connectionStore.ws, sessionId), createExitCommand(router, '/')]

onMounted(() => {
  connectionStore.ms.init()
})

onUnmounted(() => {
  connectionStore.ms.dispose()
})


</script>

<template>
  <div>
    <EdgarsTerminal :commands="gameCommands"/>
    <!--    <sub>{{ WebSocketStateToString(connectionStore.connectionState) }}</sub>-->
    <!--    <div v-if="connectionStore.connectionState === WebSocketState.UNSET || connectionStore.connectionState === WebSocketState.CLOSED">-->
    <!--      <Button label="Reconnect" @click="reconnect"/>-->
    <!--      <ToggleSwitch v-model="connectionStore.ws.autoReconnect"/>-->
    <!--    </div>-->
  </div>
</template>

<style scoped>

</style>