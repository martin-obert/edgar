<script setup lang="ts">
import {computed, ref} from "vue";
import JsonEditorVue from 'json-editor-vue'
import type {OllamaToolDefinition} from "../rest.api.ts";

const toolModel = defineModel<OllamaToolDefinition>('tool', {
  required: true
})
const editing = ref(false)
const definition = computed(() => JSON.stringify(toolModel.value, null, 2))
const v = computed({
  get: () => toolModel.value,
  set: (value: string) => toolModel.value = JSON.parse(value)
})
</script>

<template>
  <Panel>
    <JsonEditorVue v-if="editing"
        v-model="v"
        mode="text"
        :navigation-bar="false"
    />
    <p v-else>
      {{ definition }}}
    </p>
    <Button @click="editing = !editing" :label="editing ? 'Save' : 'Edit'" severity="secondary"/>
  </Panel>
</template>

<style scoped>

</style>