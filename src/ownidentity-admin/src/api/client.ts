import { userManager } from '@/auth/userManager'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7043'

export interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails,
  ) {
    super(problem.title ?? problem.detail ?? `Request failed with status ${status}`)
  }
}

export function createApiClient(
  getAccessToken: () => Promise<string | undefined>,
  fetcher: typeof fetch = fetch,
) {
  return async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const token = await getAccessToken()
    const headers = new Headers(init.headers)
    headers.set('Accept', 'application/json')
    if (init.body) headers.set('Content-Type', 'application/json')
    if (token) headers.set('Authorization', `Bearer ${token}`)

    const response = await fetcher(`${apiBaseUrl}${path}`, { ...init, headers })
    if (!response.ok) {
      const problem = (await response.json().catch(() => ({}))) as ProblemDetails
      throw new ApiError(response.status, problem)
    }

    return (await response.json()) as T
  }
}

export const api = createApiClient(async () => (await userManager.getUser())?.access_token)

export interface User {
  id: string
  email: string
  enabled: boolean
  roles: string[]
}

export interface Role { id: string; name: string }
export interface Application {
  clientId: string
  displayName: string
  redirectUris: string[]
  postLogoutRedirectUris: string[]
}
export interface Scope { name: string; displayName: string; resources: string[] }
export interface IdentityServer4PreflightDiagnostic { code: string; path: string; message: string }
export interface IdentityServer4PreflightResult {
  isValid: boolean
  cutoverBlocked: boolean
  summary: {
    tables: number
    extensions: number
    credentials: number
    keys: number
    clients: number
    customGrants: number
    adrs: number
    ropcBlockers: number
  }
  diagnostics: IdentityServer4PreflightDiagnostic[]
  diagnosticsTruncated: boolean
}

export const adminApi = {
  listUsers: () => api<User[]>('/api/admin/users'),
  createUser: (email: string) => api<{ user: User; temporaryPassword: string }>('/api/admin/users', {
    method: 'POST', body: JSON.stringify({ email }),
  }),
  setUserEnabled: (id: string, enabled: boolean) => api<User>(`/api/admin/users/${id}/enabled`, {
    method: 'PUT', body: JSON.stringify({ enabled }),
  }),
  setUserRoles: (id: string, roles: string[]) => api<User>(`/api/admin/users/${id}/roles`, {
    method: 'PUT', body: JSON.stringify({ roles }),
  }),
  listRoles: () => api<Role[]>('/api/admin/roles'),
  createRole: (name: string) => api<Role>('/api/admin/roles', { method: 'POST', body: JSON.stringify({ name }) }),
  listApplications: () => api<Application[]>('/api/admin/applications'),
  createApplication: (application: Omit<Application, 'clientId'> & { clientId: string }) =>
    api<Application>('/api/admin/applications', { method: 'POST', body: JSON.stringify(application) }),
  listScopes: () => api<Scope[]>('/api/admin/scopes'),
  createScope: (scope: Scope) => api<Scope>('/api/admin/scopes', { method: 'POST', body: JSON.stringify(scope) }),
  preflightIdentityServer4: (manifest: File) => api<IdentityServer4PreflightResult>(
    '/api/admin/migrations/identityserver4/preflight',
    { method: 'POST', body: manifest },
  ),
}
