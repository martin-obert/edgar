<script setup lang="ts">
import {ref} from "vue";
import type {ToolCallEvent} from "../message-manager.ts";
import {getFunctionFromBody} from "../websocket-messaging.ts";
import {useEventListener} from "@vueuse/core";
import type {LifeSupportSystem} from "./life-support.tools.ts";


const systems = ref<LifeSupportSystem[]>([
  {id: 'oxygen', value: 100, unit: 'percent', allowed_units: ['percent']},
  {id: 'temperature', value: 20, unit: 'celsius', allowed_units: ['celsius', 'fahrenheit']},
])

const formatSystemValue = (system: LifeSupportSystem): string => {
  switch (system.unit) {
    case 'percent':
      return `${system.value}%`
    case 'celsius':
      return `${system.value}°C`
    case 'fahrenheit':
      return `${system.value}°F`
    default:
      return `${system.value}`
  }
}

function toolCallHandler({detail}: CustomEvent<ToolCallEvent>) {
  const func = getFunctionFromBody(detail.message.body!)
  console.log(func.name)
  if (func.name === 'get_life_support_status') {
    detail.messageManager.sendToolResponse(JSON.stringify(systems.value), detail.message.toolCallId!, detail.message.promptId!)
    return
  }
  if (func.name === 'set_life_support_system') {
    const systemId = func.arguments.system_id
    const value = func.arguments.value
    for (const system of systems.value) {
      if (system.id === systemId) {
        system.value = value
      }
    }
    detail.messageManager.sendToolResponse(JSON.stringify(systems.value), detail.message.toolCallId!, detail.message.promptId!)
  }
}

useEventListener(window, 'toolCall', toolCallHandler)

</script>

<template>
  <Panel header="Life Support">
    <div class="grid grid-cols-2 gap-2" v-for="system in systems" :key="system.id">
      {{system.id}}
      <ProgressBar :value="system.value">
        <template #default>
          {{ formatSystemValue(system) }}
        </template>
      </ProgressBar>
    </div>
  </Panel>
</template>

<style scoped>

</style>