<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
</script>

<template>
  <div class="admin-layout">
    <aside class="sidebar">
      <div>
        <p class="wordmark">OWN / ID</p>
        <p class="caption">Administration</p>
      </div>
      <nav aria-label="Administration">
        <RouterLink to="/users">Users</RouterLink>
        <RouterLink to="/roles">Roles</RouterLink>
        <RouterLink to="/applications">Applications</RouterLink>
        <RouterLink to="/scopes">Scopes</RouterLink>
        <RouterLink to="/migrations/identityserver4">Migrate from IdentityServer4</RouterLink>
      </nav>
      <button class="text-button" type="button" @click="auth.logout">Sign out</button>
    </aside>
    <main class="content">
      <header class="topbar">
        <span class="status-dot"></span>
        <span>{{ auth.user?.profile.email ?? auth.user?.profile.name ?? 'Administrator' }}</span>
      </header>
      <RouterView />
    </main>
  </div>
</template>
