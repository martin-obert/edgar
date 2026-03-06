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
const getDoorColor = (state:string)=>{
  switch (state) {
    case 'open':
      return 'green'
    case 'closed':
      return 'orange'
    case 'jammed':
      return 'red'
    case 'locked':
      return 'gray'
  }
}
</script>

<template>
  <Panel header="Doors">
    <div class="grid grid-cols-2 gap-2 justify-center">
      <div v-for="door in doors" class="flex flex-row justify-items-start gap-2 justify-center">
        <span>{{ door.id }} </span><img src="/assets/door.png" style="max-height: 2rem;"  alt="Door"/>
        <span :style="{color: getDoorColor(door.state)}" >{{door.state}}</span>
      </div>
    </div>
  </Panel>
</template>

<style scoped>

</style>