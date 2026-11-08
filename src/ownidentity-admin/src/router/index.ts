import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import AdminShell from '@/components/AdminShell.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/login', component: () => import('@/views/LoginView.vue'), meta: { public: true } },
    { path: '/auth/callback', component: () => import('@/views/AuthCallbackView.vue'), meta: { public: true } },
    { path: '/auth/logout-callback', component: () => import('@/views/LogoutCallbackView.vue'), meta: { public: true } },
    {
      path: '/',
      component: AdminShell,
      children: [
        { path: '', redirect: '/users' },
        { path: 'users', component: () => import('@/views/UsersView.vue') },
        { path: 'roles', component: () => import('@/views/RolesView.vue') },
        { path: 'applications', component: () => import('@/views/ApplicationsView.vue') },
        { path: 'scopes', component: () => import('@/views/ScopesView.vue') },
        { path: 'migrations/identityserver4', component: () => import('@/views/IdentityServer4MigrationView.vue') },
      ],
    },
  ],
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  await auth.initialize()
  if (!to.meta.public && !auth.isAuthenticated) {
    return { path: '/login', query: { returnPath: to.fullPath } }
  }
  if (to.path === '/login' && auth.isAuthenticated) return '/users'
})

export default router
