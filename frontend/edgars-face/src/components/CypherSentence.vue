<script setup lang="ts">
import CypherWord from "./CypherWord.vue";
import {useCipherSentenceEffect} from "../cipher-effects.ts";

const $emit = defineEmits<{ (e: 'done', sentence: string): void }>()

const {sentence} = defineProps<{ sentence: string }>()

const {index, words, moveNext} = useCipherSentenceEffect(sentence, {
  completeCallback: () => $emit('done', sentence)
})

const isLast = (idx: number): boolean => idx === words.length - 1
</script>

<template>
  <span v-for="(w, idx) in words">
    <CypherWord :doLoop="idx <= index"
                :word="w"
                @done="moveNext"/>
    <span v-if="!isLast(idx)">&nbsp;</span>
  </span>
</template>

<style scoped>

</style>