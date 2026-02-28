<script setup lang="ts">
import {useConnectionStore} from "./stores/connection.store.ts";
import type {MenuItem} from "primevue/menuitem";
import {ref} from "vue";
import {WebSocketStateToString} from "./websocket-manager.ts";

const connectionStore = useConnectionStore()

const items = ref<MenuItem[]>([
  {
    key: 'connection_state'
  }
])

</script>

<template>
  <div class="card">
    <Menubar :model="items">
      <template #end>
          {{ WebSocketStateToString(connectionStore.connectionState)}}
      </template>
    </Menubar>
  </div>
  <RouterView>

  </RouterView>
</template>

<style scoped>
</style>
