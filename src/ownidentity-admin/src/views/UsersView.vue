<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { adminApi, type Role, type User } from '@/api/client'
import PageHeader from '@/components/PageHeader.vue'
import InlineError from '@/components/InlineError.vue'

const users = ref<User[]>([])
const roles = ref<Role[]>([])
const email = ref('')
const temporaryPassword = ref<string | null>(null)
const error = ref<string | null>(null)

async function load() {
  try {
    ;[users.value, roles.value] = await Promise.all([adminApi.listUsers(), adminApi.listRoles()])
  } catch (cause) { error.value = cause instanceof Error ? cause.message : 'Users could not be loaded.' }
}

async function createUser() {
  error.value = null
  try {
    const created = await adminApi.createUser(email.value)
    users.value.push(created.user)
    temporaryPassword.value = created.temporaryPassword
    email.value = ''
  } catch (cause) { error.value = cause instanceof Error ? cause.message : 'User could not be created.' }
}

async function toggle(user: User) {
  try { Object.assign(user, await adminApi.setUserEnabled(user.id, !user.enabled)) }
  catch (cause) { error.value = cause instanceof Error ? cause.message : 'User state could not be changed.' }
}

async function setRole(user: User, role: string, selected: boolean) {
  const next = selected ? [...user.roles, role] : user.roles.filter((name) => name !== role)
  try { Object.assign(user, await adminApi.setUserRoles(user.id, next)) }
  catch (cause) { error.value = cause instanceof Error ? cause.message : 'Roles could not be changed.' }
}

onMounted(load)
</script>

<template>
  <PageHeader eyebrow="Directory" title="Users" description="Create accounts, control access, and assign operational roles." />
  <InlineError :message="error" />
  <form class="create-row" @submit.prevent="createUser">
    <label>Email <input v-model="email" type="email" required placeholder="person@example.com" /></label>
    <button class="primary" type="submit">Create user</button>
  </form>
  <aside v-if="temporaryPassword" class="notice" role="status">
    <strong>Temporary password</strong>
    <code>{{ temporaryPassword }}</code>
    <span>Shown once. Transfer it securely and clear this message.</span>
    <button type="button" @click="temporaryPassword = null">Clear</button>
  </aside>
  <div class="data-list">
    <article v-for="user in users" :key="user.id" class="data-card">
      <div><h2>{{ user.email }}</h2><span class="badge" :class="{ muted: !user.enabled }">{{ user.enabled ? 'Enabled' : 'Disabled' }}</span></div>
      <div class="role-grid">
        <label v-for="role in roles" :key="role.id">
          <input type="checkbox" :checked="user.roles.includes(role.name)" @change="setRole(user, role.name, ($event.target as HTMLInputElement).checked)" />
          {{ role.name }}
        </label>
      </div>
      <button class="secondary" type="button" @click="toggle(user)">{{ user.enabled ? 'Disable' : 'Enable' }}</button>
    </article>
  </div>
</template>
