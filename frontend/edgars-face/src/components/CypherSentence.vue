<script setup lang="ts">
import {computed, ref, watch} from "vue";
import CypherWord from "./CypherWord.vue";

const $emit = defineEmits<{ (e: 'done', sentence: string): void }>()

const {sentence} = defineProps<{ sentence: string }>()

const words = computed(() => sentence.split(" "))
console.log('start - ' + new Date())

watch(() => sentence, () => {
  index.value = 0
  console.log('start - ' + new Date())
})

const index = ref<number>(0)

const onWordDone = () => {
  index.value++
  if (index.value >= words.value.length) {
    console.log('done - ' + sentence)
    $emit('done', sentence)
  }
}

</script>

<template>
  <CypherWord v-for="w in words" :word="w + (index < words.length - 1 ? ' ' : '')" @done="onWordDone"/>
</template>

<style scoped>

</style>