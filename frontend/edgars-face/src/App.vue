<script setup lang="ts">
import {useBackendStore} from "./stores/backend.store.ts";
import type {MenuItem} from "primevue/menuitem";
import {ref} from "vue";
import {WebSocketStateToString} from "./websocket-manager.ts";

const connectionStore = useBackendStore()

const items = ref<MenuItem[]>([
  {
    key: 'connection_state'
  }
])

</script>

<template>
  <div class="card">
    <Menubar :model="items">
      <template #start>
        E.D.G.A.R.s
      </template>
      <template #end>
          {{ WebSocketStateToString(connectionStore.connectionState)}}
      </template>
    </Menubar>
  </div>
  <RouterView>

  </RouterView>
  <Toast />
</template>

<style scoped>
</style>
