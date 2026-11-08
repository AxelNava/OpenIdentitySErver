import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import type { User } from 'oidc-client-ts'
import { userManager } from '@/auth/userManager'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const initialized = ref(false)
  const isAuthenticated = computed(() => user.value !== null && !user.value.expired)

  async function initialize() {
    if (initialized.value) return
    user.value = await userManager.getUser()
    if (user.value?.expired) {
      await userManager.removeUser()
      user.value = null
    }
    initialized.value = true
  }

  async function login(returnPath = '/users') {
    await userManager.signinRedirect({ state: { returnPath } })
  }

  async function completeLogin() {
    user.value = await userManager.signinRedirectCallback()
    initialized.value = true
    return (user.value.state as { returnPath?: string } | undefined)?.returnPath ?? '/users'
  }

  async function logout() {
    await userManager.signoutRedirect()
  }

  async function completeLogout() {
    await userManager.signoutRedirectCallback().catch(() => undefined)
    await userManager.removeUser()
    user.value = null
    initialized.value = true
  }

  return { user, initialized, isAuthenticated, initialize, login, completeLogin, logout, completeLogout }
})
