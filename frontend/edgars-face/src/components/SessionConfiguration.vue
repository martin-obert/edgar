<script setup lang="ts">

import type {FormResolverOptions, FormSubmitEvent} from "@primevue/forms";
import {useToast} from "primevue";
import {useAsyncState} from "@vueuse/core";
import {useBackendStore} from "../stores/backend.store.ts";
import type {SessionConfiguration} from "../rest.api.ts";
import ToolEditor from "./ToolEditor.vue";

const {sessionId} = defineProps<{ sessionId: string }>()
const backend = useBackendStore()
const {
  state: initialValues,
  isReady
} = useAsyncState<SessionConfiguration>(
    () => backend.rest.getSessionConfiguration(sessionId),
    {} as SessionConfiguration,
    {immediate: true, resetOnExecute: false, shallow: false}
)

const toast = useToast();
const defaultTemplate = "{ \"type\": \"function\", \"function\": { \"name\": \"search\", \"description\": \"Search the web\", \"parameters\": { \"type\": \"object\", \"properties\": { \"query\": { \"type\": \"string\", \"description\": \"The search query\" } }, \"required\": [ \"query\" ] } } }"

const resolver = ({values}: FormResolverOptions): Record<string, any> | Promise<Record<string, any>> | undefined => {
  const errors: any = {};

  if (!values.systemPrompt) {
    errors.systemPrompt = [{message: 'System prompt is required.', key: 'required'}];
  }

  return {
    values, // (Optional) Used to pass current form values to submit event.
    errors
  };
};

const onFormSubmit = async ({valid, values}: FormSubmitEvent) => {
  if (valid) {
    try {
      const tools = initialValues.value.all_tools
      await backend.rest.updateSessionConfiguration(sessionId, {...values, all_tools: tools} as SessionConfiguration)
      toast.add({
        severity: 'success',
        summary: 'Updated',
        life: 3000
      });
    } catch (e: Error | any) {
      toast.add({
        severity: 'error',
        summary: 'Update failed',
        detail: e.message,
        life: 3000
      });
    }
  }
};
const validModels = ['qwen3:4b', 'qwen2.5:7b', 'qwen2.5:3b']

</script>

<template>
  <Form v-if="isReady" v-slot="$form" :initialValues :resolver @submit="onFormSubmit" class="w-full m-1 my-3">
    <Card>
      <template #title>
        Configuration
      </template>
      <template #content>
        <div class="flex flex-col gap-1">
          <Fieldset legend="System" :toggleable="true" :collapsed="false">
            <IftaLabel>
              <Select :options="validModels" name="model"></Select>
              <label for="model">Model</label>
              <Message v-if="$form.model?.invalid" severity="error" size="small" variant="simple">
                {{ $form.model.error?.message }}
              </Message>
            </IftaLabel>
            <IftaLabel>
              <Textarea name="system_prompt" class="w-full"/>
              <label for="system_prompt">System Prompt</label>
              <Message v-if="$form.system_prompt?.invalid" severity="error" size="small" variant="simple">
                {{ $form.system_prompt.error?.message }}
              </Message>
            </IftaLabel>
          </Fieldset>
          <Fieldset legend="Tools" :toggleable="true" :collapsed="true">
            <div v-for="(tool, index) in initialValues.all_tools" :key="tool.function.name">
              <ToolEditor v-model:tool="initialValues.all_tools[index]!" class="mb-2"/>
              <Button label="Remove" @click="initialValues.all_tools.splice(initialValues.all_tools.indexOf(tool), 1)"/>
            </div>
            <Button label="Add"
                    @click="initialValues.all_tools.push(JSON.parse(defaultTemplate))"/>
          </Fieldset>
        </div>
      </template>
      <template #footer>
        <Button type="submit" severity="secondary" :disabled="!$form.valid" label="Submit"/>
      </template>
    </Card>
  </Form>

</template>

<style scoped>

</style>