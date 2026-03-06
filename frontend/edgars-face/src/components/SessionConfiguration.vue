<script setup lang="ts">

import type {FormResolverOptions, FormSubmitEvent} from "@primevue/forms";
import {useToast} from "primevue";
import {useBackendStore} from "../stores/backend.store.ts";
import type {SessionConfiguration} from "../rest.api.ts";
import {doorsToolDefinitions} from "../gameplay/door.tools.ts";
import ShipDoors from "../gameplay/ShipDoors.vue";
import {useSessionStore} from "../stores/session.store.ts";
import {computed, onMounted, onUnmounted, ref} from "vue";
import {OllamaModelOptions} from "../websocket-messaging.ts";
import LifeSupport from "../gameplay/LifeSupport.vue";
import {lifeSupportTools} from "../gameplay/life-support.tools.ts";
import {useRouter} from "vue-router";

const {sessionId} = defineProps<{ sessionId: string }>()
const backend = useBackendStore()

const sessionStore = useSessionStore()

const toast = useToast();

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
const defaultSessionConfig = {
  model: 'qwen2.5:7b',
  system_prompt: 'You are a helpful assistant.',
  all_tools: [
    ...doorsToolDefinitions,
    ...lifeSupportTools
  ],
  options: {...new OllamaModelOptions()}
}
const currentSession = ref<SessionConfiguration | undefined>()

onMounted(async () => {
  currentSession.value = sessionStore.getSession(sessionId)
  if (!currentSession.value) {
    currentSession.value = {...defaultSessionConfig}
  }
  await backend.rest.updateSessionConfiguration(sessionId, currentSession.value)
})

onUnmounted(() => {
  if (currentSession.value)
    sessionStore.putSession(sessionId, currentSession.value)
})


const onFormSubmit = async ({valid, values}: FormSubmitEvent) => {
  if (valid) {
    try {
      await backend.rest.updateSessionConfiguration(sessionId, {...currentSession.value!, ...values})
      sessionStore.putSession(sessionId, {...currentSession.value!, ...values})
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
const deleteSession = async () => {
  await backend.rest.deleteSession(sessionId)
  sessionStore.deleteSession(sessionId)
  currentSession.value = {...defaultSessionConfig}
  await backend.rest.updateSessionConfiguration(sessionId, currentSession.value)
  sessionStore.putSession(sessionId, currentSession.value!)
  window.location.reload()
}

const sessionList = computed(() => {
  const values = []
  for (const sessionId in sessionStore.listSessions()) {
    values.push(sessionId)
  }
  return values
})
const router = useRouter()
const changeTo = async (sessionId: string) => {
  await router.push('/game/' + sessionId)
  router.go(0)
}
</script>

<template>
  <div class="w-full">
    <div class="grid grid-cols-2 gap-2">
      <ShipDoors/>
      <LifeSupport/>
    </div>
    <Form v-if="currentSession" v-slot="$form" :initialValues="currentSession" :resolver @submit="onFormSubmit"
          class="w-full">
      <Panel toggleable :collapsed="false">
        <template #footer>
          <Button label="Reset Session" @click="deleteSession" severity="danger" icon="pi pi-trash"/>
        </template>
        <template #header>
          <div>
            Session <Select optionLabel="" :options="sessionList" :modelValue="sessionId"
                            @valueChange="(value:string) => changeTo(value)" name="model"></Select>
          </div>
        </template>
        <div class="grid grid-cols-2 gap-2 h-full">
          <Fieldset legend="System" :toggleable="true" :collapsed="false">
            <IftaLabel>
              <Select :options="validModels" name="model"></Select>
              <label for="model">Model</label>
              <Message v-if="$form.model?.invalid" severity="error" size="small" variant="simple">
                {{ $form.model.error?.message }}
              </Message>
            </IftaLabel>
            <IftaLabel>
              <Textarea name="system_prompt" class="w-full" style="height: 300px"/>
              <label for="system_prompt">System Prompt</label>
              <Message v-if="$form.system_prompt?.invalid" severity="error" size="small" variant="simple">
                {{ $form.system_prompt.error?.message }}
              </Message>
            </IftaLabel>
          </Fieldset>
          <Fieldset legend="Model Options" :toggleable="true" :collapsed="false">

            <FormField v-slot="$field" name="options.seed"
                       :initialValue="currentSession.options.seed">
              <IftaLabel>
                <InputNumber :modelValue="$field.value"
                             @update:modelValue="(val) => ($field as any).onChange(val)"
                             :min="0" :max="1000000000" :step="1"/>
                <label>seed</label>
              </IftaLabel>
            </FormField>
            <FormField v-slot="$field" name="options.num_ctx"
                       :initialValue="currentSession.options.num_ctx">
              <IftaLabel>
                <InputNumber :modelValue="$field.value"
                             @update:modelValue="(val) => ($field as any).onChange(val)"
                             :min="0" :max="1000000000" :step="1"/>
                <label>num_ctx</label>
              </IftaLabel>
            </FormField>
            <FormField v-slot="$field" name="options.num_predict"
                       :initialValue="currentSession.options.num_predict">
              <IftaLabel>
                <InputNumber :modelValue="$field.value"
                             @update:modelValue="(val) => ($field as any).onChange(val)"
                             :min="0" :max="1000000000" :step="1"/>
                <label>num_predict</label>
              </IftaLabel>
            </FormField>
            <FormField v-slot="$field" name="options.temperature"
                       :initialValue="currentSession.options.temperature">
              <IftaLabel>
                <InputNumber :modelValue="$field.value"
                             @update:modelValue="(val) => ($field as any).onChange(val)"
                             :min="0" :max="2" :step="0.1"
                             :minFractionDigits="1" :maxFractionDigits="2"/>
                <label>temperature</label>
              </IftaLabel>
            </FormField>
            <FormField v-slot="$field" name="options.top_k"
                       :initialValue="currentSession.options.top_k">
              <IftaLabel>
                <InputNumber :modelValue="$field.value"
                             @update:modelValue="(val) => ($field as any).onChange(val)"
                             :min="0" :max="200" :step="0.1"
                             :minFractionDigits="1" :maxFractionDigits="2"/>
                <label>top_k</label>
              </IftaLabel>
            </FormField>
            <FormField v-slot="$field" name="options.top_p"
                       :initialValue="currentSession.options.top_p">
              <IftaLabel>
                <InputNumber :modelValue="$field.value"
                             @update:modelValue="(val) => ($field as any).onChange(val)"
                             :min="0" :max="200" :step="0.1"
                             :minFractionDigits="1" :maxFractionDigits="2"/>
                <label>top_p</label>
              </IftaLabel>
            </FormField>
          </Fieldset>
        </div>
        <Button type="submit" severity="error" label="Apply configuration"/>
      </Panel>
    </Form>

  </div>
</template>

<style scoped>

</style>