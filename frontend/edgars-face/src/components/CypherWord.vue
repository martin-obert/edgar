<script setup lang="ts">

import {useCipherWordEffect} from "../cipher-effects.ts";

const $emits = defineEmits<{ (e: 'done', word: string): void }>()
const {word, doLoop} = defineProps<{ word: string, doLoop: boolean }>()

const {renderedWord} = useCipherWordEffect(word, {
  completeCallback: () => {
    $emits('done', word)
  },
  doDecipher: () => doLoop
})

</script>

<template>
  {{ renderedWord }}
</template>

<style scoped>

</style>