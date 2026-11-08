import { describe, expect, it } from 'vitest'
import { ApiError, createApiClient } from '@/api/client'

describe('API client', () => {
  it('sends the access token as a bearer credential', async () => {
    let capturedInit: RequestInit | undefined
    const fetcher: typeof fetch = async (_input, init) => {
      capturedInit = init
      return new Response(JSON.stringify({ value: 42 }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    }
    const request = createApiClient(async () => 'access-token', fetcher)

    await expect(request<{ value: number }>('/test')).resolves.toEqual({ value: 42 })
    const headers = new Headers(capturedInit?.headers)
    expect(headers.get('Authorization')).toBe('Bearer access-token')
  })

  it('preserves problem details from failed responses', async () => {
    const fetcher: typeof fetch = async () => new Response(JSON.stringify({ title: 'Invalid role' }), {
      status: 400,
      headers: { 'Content-Type': 'application/problem+json' },
    })
    const request = createApiClient(async () => undefined, fetcher)

    const error = await request('/test').catch((cause: unknown) => cause)
    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 400, problem: { title: 'Invalid role' } })
  })

  it('sends the original File as an application/json request body', async () => {
    let capturedInit: RequestInit | undefined
    const fetcher: typeof fetch = async (_input, init) => {
      capturedInit = init
      return new Response(JSON.stringify({ isValid: true }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    }
    const request = createApiClient(async () => 'access-token', fetcher)
    const file = new File(['do-not-parse-this-content'], 'inventory.json', { type: 'application/json' })

    await request('/api/admin/migrations/identityserver4/preflight', { method: 'POST', body: file })

    expect(capturedInit?.body).toBe(file)
    expect(new Headers(capturedInit?.headers).get('Content-Type')).toBe('application/json')
  })
})
