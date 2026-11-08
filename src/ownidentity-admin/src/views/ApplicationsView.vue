<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { adminApi, type Application } from '@/api/client'
import PageHeader from '@/components/PageHeader.vue'
import InlineError from '@/components/InlineError.vue'

const applications = ref<Application[]>([])
const form = reactive({ clientId: '', displayName: '', redirectUri: '', logoutUri: '' })
const error = ref<string | null>(null)
async function load() { try { applications.value = await adminApi.listApplications() } catch (cause) { error.value = cause instanceof Error ? cause.message : 'Applications could not be loaded.' } }
async function createApplication() {
  try {
    applications.value.push(await adminApi.createApplication({ clientId: form.clientId, displayName: form.displayName, redirectUris: [form.redirectUri], postLogoutRedirectUris: form.logoutUri ? [form.logoutUri] : [] }))
    Object.assign(form, { clientId: '', displayName: '', redirectUri: '', logoutUri: '' })
  } catch (cause) { error.value = cause instanceof Error ? cause.message : 'Application could not be created.' }
}
onMounted(load)
</script>

<template>
  <PageHeader eyebrow="OpenID Connect" title="Applications" description="Register public authorization-code clients. PKCE is enforced for every client." />
  <InlineError :message="error" />
  <form class="form-grid" @submit.prevent="createApplication">
    <label>Client ID <input v-model="form.clientId" required /></label><label>Display name <input v-model="form.displayName" required /></label>
    <label>Redirect URI <input v-model="form.redirectUri" type="url" required /></label><label>Logout redirect URI <input v-model="form.logoutUri" type="url" /></label>
    <button class="primary">Register application</button>
  </form>
  <div class="data-list"><article v-for="application in applications" :key="application.clientId" class="data-card"><div><h2>{{ application.displayName }}</h2><code>{{ application.clientId }}</code></div><small v-for="uri in application.redirectUris" :key="uri">{{ uri }}</small></article></div>
</template>
