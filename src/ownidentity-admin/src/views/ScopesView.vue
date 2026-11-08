<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { adminApi, type Scope } from '@/api/client'
import PageHeader from '@/components/PageHeader.vue'
import InlineError from '@/components/InlineError.vue'

const scopes = ref<Scope[]>([])
const form = reactive({ name: '', displayName: '', resources: '' })
const error = ref<string | null>(null)
async function load() { try { scopes.value = await adminApi.listScopes() } catch (cause) { error.value = cause instanceof Error ? cause.message : 'Scopes could not be loaded.' } }
async function createScope() {
  try {
    scopes.value.push(await adminApi.createScope({ name: form.name, displayName: form.displayName, resources: form.resources.split(',').map((value) => value.trim()).filter(Boolean) }))
    Object.assign(form, { name: '', displayName: '', resources: '' })
  } catch (cause) { error.value = cause instanceof Error ? cause.message : 'Scope could not be created.' }
}
onMounted(load)
</script>

<template>
  <PageHeader eyebrow="Delegated access" title="Scopes" description="Name the permissions clients can request and APIs can enforce." />
  <InlineError :message="error" />
  <form class="form-grid" @submit.prevent="createScope"><label>Name <input v-model="form.name" required /></label><label>Display name <input v-model="form.displayName" required /></label><label>Resources <input v-model="form.resources" placeholder="api-one, api-two" /></label><button class="primary">Create scope</button></form>
  <div class="data-list"><article v-for="scope in scopes" :key="scope.name" class="data-card"><div><h2>{{ scope.displayName }}</h2><code>{{ scope.name }}</code></div><span>{{ scope.resources.join(', ') || 'No resources' }}</span></article></div>
</template>
