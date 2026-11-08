<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { adminApi, type Role } from '@/api/client'
import PageHeader from '@/components/PageHeader.vue'
import InlineError from '@/components/InlineError.vue'

const roles = ref<Role[]>([])
const name = ref('')
const error = ref<string | null>(null)
async function load() { try { roles.value = await adminApi.listRoles() } catch (cause) { error.value = cause instanceof Error ? cause.message : 'Roles could not be loaded.' } }
async function createRole() {
  try { roles.value.push(await adminApi.createRole(name.value)); name.value = '' }
  catch (cause) { error.value = cause instanceof Error ? cause.message : 'Role could not be created.' }
}
onMounted(load)
</script>

<template>
  <PageHeader eyebrow="Authorization" title="Roles" description="Define durable responsibility groups for identity accounts." />
  <InlineError :message="error" />
  <form class="create-row" @submit.prevent="createRole"><label>Name <input v-model="name" required /></label><button class="primary">Create role</button></form>
  <div class="data-list"><article v-for="role in roles" :key="role.id" class="data-card compact"><h2>{{ role.name }}</h2><code>{{ role.id }}</code></article></div>
</template>
