<script setup lang="ts">
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const auth = useAuthStore()
const signIn = () => auth.login(typeof route.query.returnPath === 'string' ? route.query.returnPath : '/users')
</script>

<template>
  <main class="entry-page">
    <section class="entry-card">
      <p class="wordmark">OWN / ID</p>
      <p class="eyebrow">Secure administration</p>
      <h1>Control the identity boundary.</h1>
      <p>Sign in through the identity service. Credentials are never entered into this administration client.</p>
      <button class="primary" type="button" @click="signIn">Sign in with OwnIdentity</button>
    </section>
  </main>
</template>
