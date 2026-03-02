<script setup lang="ts">
import {ref, watch} from "vue";
import {useIntervalFn, useTimeoutFn} from "@vueuse/core";

const $emits = defineEmits<{ (e: 'done', word: string): void }>()
const {word} = defineProps<{ word: string }>()
let clearIndex = 0
const displayed = ref<string>("")

const decypherTimeout = 500
const decypherInterval = 100
const letterRndSpeed = 100
const decryptFunction = () => {
  useTimeoutFn(() => {
    const {pause} = useIntervalFn(() => {
      if (clearIndex > word.length) {
        pause()
        return
      }
      clearIndex++
    }, decypherInterval)
  }, decypherTimeout)

  const {pause} = useIntervalFn(() => {
    displayed.value = Array.from(word, (w, idx) => idx < clearIndex ? w : String.fromCharCode(97 + Math.random() * 26 | 0)).join("");
    if (clearIndex >= word.length) {
      pause()
      $emits('done', word)
      return
    }
  }, letterRndSpeed)
}

watch(() => word, () => {
  displayed.value = "";
  clearIndex = 0
  decryptFunction()
}, {immediate: true})
</script>

<template>
  {{ displayed }}
</template>

<style scoped>

</style>