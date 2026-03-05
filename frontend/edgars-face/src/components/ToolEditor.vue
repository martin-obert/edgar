<script setup lang="ts">
import {ref} from "vue";

const toolModel = defineModel('tool', {
  required: true
})
const editing = ref(false)
const definition = ref(JSON.stringify(toolModel.value, null, 2))
</script>

<template>
  <Panel>

    <Textarea v-model="definition" v-if="editing" class="w-full"></Textarea>
    <p v-else>
      {{definition }}}
    </p>
    <Button @click="editing = true" :label="editing ? 'Save' : 'Edit'" severity="secondary"/>
  </Panel>
</template>

<style scoped>

</style>