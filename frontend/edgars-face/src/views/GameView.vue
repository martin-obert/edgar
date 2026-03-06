<script setup lang="ts">
import {useBackendStore} from "../stores/backend.store.ts";
import EdgarsTerminal from "../components/EdgarsTerminal.vue";
import {createExitCommand, createPromptCommand} from "../commands.ts";
import {onMounted, onUnmounted} from "vue";
import {useRouter} from "vue-router";
import SessionConfiguration from "../components/SessionConfiguration.vue";

const {sessionId} = defineProps<{ sessionId: string }>()

const connectionStore = useBackendStore()
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
  <div class="flex flex-row gap-2 m-2">
  <div>
    <EdgarsTerminal :commands="gameCommands"/>
  </div>
    <SessionConfiguration :sessionId="sessionId"/>
  </div>
</template>

<style scoped>

</style>