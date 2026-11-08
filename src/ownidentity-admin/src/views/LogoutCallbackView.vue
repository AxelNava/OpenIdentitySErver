<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()
const message = ref('Completing sign-out...')
onMounted(async () => {
  await auth.completeLogout()
  message.value = 'Signed out.'
  await router.replace('/login')
})
</script>

<template><main class="centered"><p>{{ message }}</p></main></template>
