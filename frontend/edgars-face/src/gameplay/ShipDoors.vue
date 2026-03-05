<script setup lang="ts">
import {onMounted, onUnmounted, ref} from "vue";
import {getFunctionFromBody} from "../websocket-messaging.ts";
import type {ToolCallEvent} from "../message-manager.ts";

interface ShipDoor {
  name: string,
  open: boolean,
}

const doors = ref<ShipDoor[]>([
  {name: 'left', open: false},
  {name: 'right', open: false},
  {name: 'front', open: false},
  {name: 'back', open: false},
])

function toolCallHandler({detail}: CustomEvent<ToolCallEvent>) {
  const func = getFunctionFromBody(detail.message.body)
  console.log(func.name)
  if (func.name === 'list_doors') {
    detail.messageManager.sendToolResponse(JSON.stringify(doors.value), detail.message.toolCallId!, detail.message.promptId!)
  }
  if (func.name === 'open_door' || func.name === 'close_door') {
    const doorName = func.arguments.door_name
    const door = doors.value.find(door => door.name === doorName)
    if (door) {
      door.open = func.name === 'open_door'
      detail.messageManager.sendToolResponse(JSON.stringify(door), detail.message.toolCallId!, detail.message.promptId!)
    }
    detail.messageManager.sendToolResponse(`Doors ${doorName} not found`, detail.message.toolCallId!, detail.message.promptId!)

  }
}

onMounted(() => {
  //@ts-ignore
  window.addEventListener('toolCall', toolCallHandler);
})

onUnmounted(() => {
  window.removeEventListener('toolCall', toolCallHandler);
})
</script>

<template>
  <div v-for="door in doors">
    name: {{ door.name }} - open: {{ door.open }}
  </div>
</template>

<style scoped>

</style>