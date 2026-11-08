import { UserManager, WebStorageStateStore } from 'oidc-client-ts'

const authority = import.meta.env.VITE_AUTHORITY ?? 'https://localhost:7043'

export const userManager = new UserManager({
  authority,
  client_id: 'ownidentity-admin',
  redirect_uri: `${window.location.origin}/auth/callback`,
  post_logout_redirect_uri: `${window.location.origin}/auth/logout-callback`,
  response_type: 'code',
  scope: 'openid profile email roles identity.admin',
  userStore: new WebStorageStateStore({ store: window.sessionStorage }),
  stateStore: new WebStorageStateStore({ store: window.sessionStorage }),
  automaticSilentRenew: false,
  monitorSession: false,
})
