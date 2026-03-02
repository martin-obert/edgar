<script setup lang="ts">
import {computed, ref, watch} from "vue";
import CypherWord from "./CypherWord.vue";

const $emit = defineEmits<{ (e: 'done', sentence: string): void }>()

const {sentence} = defineProps<{ sentence: string }>()

const words = computed(() => sentence.split(" "))

const index = ref<number>(0)

watch(() => sentence, () => {
  index.value = 0
  console.log(new Date() + ' - ' + sentence)
}, {immediate: true})


const onWordDone = () => {
  index.value++
  if (index.value >= words.value.length) {
    console.log('done - ' + sentence)
    $emit('done', sentence)
  }
}
const isFirst = (idx: number): boolean => idx === 0
const isLast = (idx: number): boolean => idx === words.value.length - 1
</script>

<template>
  <span v-for="(w, idx) in words">
    <CypherWord :isDecypher="idx >= index" :word="w" @done="onWordDone"/><span
      v-if="!isLast(idx) && !isFirst(idx)">&nbsp;</span>
  </span> (C)
</template>

<style scoped>

</style>