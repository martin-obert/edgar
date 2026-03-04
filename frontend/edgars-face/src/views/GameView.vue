<script setup lang="ts">
import {useConnectionStore} from "../stores/connection.store.ts";
import EdgarsTerminal from "../components/EdgarsTerminal.vue";
import {createExitCommand, createPromptCommand} from "../commands.ts";
import {onMounted, onUnmounted} from "vue";
import {useRouter} from "vue-router";
import SessionConfiguration from "../components/SessionConfiguration.vue";

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
    <SessionConfiguration :sessionId="sessionId"/>
  </div>
</template>

<style scoped>

</style>