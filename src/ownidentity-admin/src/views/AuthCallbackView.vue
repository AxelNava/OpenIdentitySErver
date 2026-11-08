<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()
const error = ref<string | null>(null)

onMounted(async () => {
  try {
    await router.replace(await auth.completeLogin())
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : 'Sign-in could not be completed.'
  }
})
</script>

<template><main class="centered"><p v-if="!error">Completing sign-in...</p><p v-else class="error">{{ error }}</p></main></template>
