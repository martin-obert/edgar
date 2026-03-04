<script setup lang="ts">

import {reactive} from "vue";
import type {FormResolverOptions, FormSubmitEvent} from "@primevue/forms";
import {useToast} from "primevue";

const {sessionId} = defineProps<{ sessionId: string }>()

const toast = useToast();

interface FormFields {
  systemPrompt: string
}

const initialValues = reactive<FormFields>({
  systemPrompt: 'Test prompt'
});

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

const onFormSubmit = ({valid}: FormSubmitEvent) => {
  if (valid) {
    toast.add({
      severity: 'success',
      summary: 'Form is submitted.',
      life: 3000
    });
  }
};
</script>

<template>
  <Form v-slot="$form" :initialValues :resolver @submit="onFormSubmit" class="flex flex-col gap-4 w-full sm:w-56">
    <Card>
      <template #title>
        Configuration
      </template>
      <template #content>
        <div class="flex flex-col gap-1">
          <Fieldset legend="Session" :toggleable="true" :collapsed="false">
            <IftaLabel >
              <Textarea name="systemPrompt"/>
              <label for="systemPrompt">System Prompt</label>
              <Message v-if="$form.systemPrompt?.invalid" severity="error" size="small" variant="simple">{{ $form.systemPrompt.error?.message }}</Message>
            </IftaLabel>
          </Fieldset>
        </div>
        <Button type="submit" severity="secondary" label="Submit"/>
      </template>
    </Card>
  </Form>
</template>

<style scoped>

</style>