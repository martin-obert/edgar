<script setup lang="ts">
import {ref} from "vue";
import {getFunctionFromBody} from "../websocket-messaging.ts";
import type {ToolCallEvent} from "../message-manager.ts";
import {useEventListener} from "@vueuse/core";
import type {DoorStatus} from "./door.tools.ts";


interface ShipDoor {
  id: string,
  state: DoorStatus,
}

const doors = ref<ShipDoor[]>([
  {id: 'left', state: 'closed'},
  {id: 'right', state: 'open'},
  {id: 'front', state: 'jammed'},
  {id: 'back', state: 'locked'},
])

function toolCallHandler({detail}: CustomEvent<ToolCallEvent>) {
  const func = getFunctionFromBody(detail.message.body!)
  switch (func.name) {
    case 'set_door_state':
      console.log(func.name)
      const id = func.arguments.id
      const door = doors.value.find(door => door.id === id)
      if (!door) {
        detail.messageManager.sendToolResponse(`Error ${id} not found`, detail.message.toolCallId!, detail.message.promptId!)
        return
      }
      door.state = func.arguments.state
      detail.messageManager.sendToolResponse(JSON.stringify(door), detail.message.toolCallId!, detail.message.promptId!)
      break
    case 'list_doors':
      console.log(func.name)
      detail.messageManager.sendToolResponse(JSON.stringify(doors.value), detail.message.toolCallId!, detail.message.promptId!)
      break
  }
}

useEventListener(window, 'toolCall', toolCallHandler)

</script>

<template>
  <Panel header="Doors">
    <div class="grid grid-cols-2 gap-2">
      <div v-for="door in doors" class="flex flex-row justify-items-start gap-2">
      <span :style="{color: door.state ? 'green' : 'red'}">
        <span>{{ door.id }} </span><i class="pi" :class="!door.state ? 'pi-lock' : 'pi-lock-open'"></i>
      </span>

      </div>
    </div>
  </Panel>
</template>

<style scoped>

</style>