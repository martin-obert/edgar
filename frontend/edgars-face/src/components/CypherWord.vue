<script setup lang="ts">
import {ref, watch} from "vue";
import {useIntervalFn} from "@vueuse/core";

const $emits = defineEmits<{ (e: 'done', word: string): void }>()
const {word, isDecypher} = defineProps<{ word: string, isDecypher: boolean }>()
let clearIndex = 0
const displayed = ref<string>("")

const decypherInterval = 100
const letterRndSpeed = 100
const decryptFunction = () => {
  const {pause} = useIntervalFn(() => {
    displayed.value = Array.from(word, (w, idx) => idx < clearIndex ? w : String.fromCharCode(97 + Math.random() * 26 | 0)).join("");
    if (clearIndex >= word.length) {
      pause()
      $emits('done', word)
      return
    }
  }, letterRndSpeed)
}

const {resume: decypherResume} = useIntervalFn(() => {
  if (clearIndex > word.length) {
    return
  }
  clearIndex++
}, decypherInterval, {immediate: false})

watch(() => isDecypher, (v) => {
  if (v) {
    decypherResume()
  }
}, {immediate: true})


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