import { afterEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { adminApi, type IdentityServer4PreflightResult } from '@/api/client'
import IdentityServer4MigrationView from '@/views/IdentityServer4MigrationView.vue'

const validResult: IdentityServer4PreflightResult = {
  isValid: true,
  cutoverBlocked: false,
  summary: {
    tables: 1,
    extensions: 2,
    credentials: 3,
    keys: 4,
    clients: 5,
    customGrants: 6,
    adrs: 7,
    ropcBlockers: 0,
  },
  diagnostics: [],
  diagnosticsTruncated: false,
}

afterEach(() => vi.restoreAllMocks())

describe('IdentityServer4 migration preflight view', () => {
  it('does not request preflight before a file is selected', () => {
    const preflight = vi.spyOn(adminApi, 'preflightIdentityServer4')
    const wrapper = mount(IdentityServer4MigrationView)

    expect(preflight).not.toHaveBeenCalled()
    expect(wrapper.get('button').attributes('disabled')).toBeDefined()
  })

  it('rejects an oversized file locally without making a request', async () => {
    const preflight = vi.spyOn(adminApi, 'preflightIdentityServer4')
    const wrapper = mount(IdentityServer4MigrationView)
    const file = new File([new Uint8Array(1_048_577)], 'oversized.json', { type: 'application/json' })

    await selectFile(wrapper, file)

    expect(wrapper.text()).toContain('exceeds the 1 MiB limit and was not sent')
    expect(wrapper.get('button').attributes('disabled')).toBeDefined()
    expect(preflight).not.toHaveBeenCalled()
  })

  it('rejects a non-json selection locally without making a request', async () => {
    const preflight = vi.spyOn(adminApi, 'preflightIdentityServer4')
    const wrapper = mount(IdentityServer4MigrationView)

    await selectFile(wrapper, new File(['not-json'], 'inventory.txt', { type: 'text/plain' }))

    expect(wrapper.text()).toContain('Choose a .json manifest file')
    expect(preflight).not.toHaveBeenCalled()
  })

  it('disables controls and announces progress while validating', async () => {
    let resolveRequest!: (result: IdentityServer4PreflightResult) => void
    vi.spyOn(adminApi, 'preflightIdentityServer4').mockImplementation(() => new Promise((resolve) => {
      resolveRequest = resolve
    }))
    const wrapper = mount(IdentityServer4MigrationView)
    await selectFile(wrapper, new File(['{}'], 'inventory.json', { type: 'application/json' }))

    await wrapper.get('button').trigger('click')

    expect(wrapper.get('[data-testid="manifest-input"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('button').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[aria-live="polite"]').text()).toBe('Validating manifest...')

    resolveRequest(validResult)
    await flushPromises()
  })

  it('shows the redacted summary and a prominent ROPC cutover blocker', async () => {
    vi.spyOn(adminApi, 'preflightIdentityServer4').mockResolvedValue({
      ...validResult,
      cutoverBlocked: true,
      summary: { ...validResult.summary, ropcBlockers: 2 },
    })
    const wrapper = mount(IdentityServer4MigrationView)
    await selectFile(wrapper, new File(['{}'], 'inventory.json', { type: 'application/json' }))

    await wrapper.get('button').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Manifest is valid')
    expect(wrapper.text()).toContain('Cutover blocked')
    expect(wrapper.text()).toContain('2 ROPC blocker(s)')
    expect(wrapper.text()).toContain('ROPC has no activation or exception path')
  })

  it('states the limited evidence when a valid manifest has no ROPC blocker', async () => {
    vi.spyOn(adminApi, 'preflightIdentityServer4').mockResolvedValue(validResult)
    const wrapper = mount(IdentityServer4MigrationView)
    await selectFile(wrapper, new File(['{}'], 'inventory.json', { type: 'application/json' }))

    await wrapper.get('button').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Source truth, migration readiness, and data application remain unproven')
  })

  it('renders bounded safe diagnostics without manifest content or client IDs', async () => {
    vi.spyOn(adminApi, 'preflightIdentityServer4').mockResolvedValue({
      ...validResult,
      isValid: false,
      diagnostics: [{ code: 'LIV122', path: '$.clients[0].owner', message: 'ROPC blocker owner is required.' }],
    })
    const wrapper = mount(IdentityServer4MigrationView)
    const sourceValue = 'source-client-id-must-not-render'
    await selectFile(wrapper, new File([`{"clientId":"${sourceValue}"}`], 'inventory.json', { type: 'application/json' }))

    await wrapper.get('button').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Manifest is invalid')
    expect(wrapper.text()).toContain('LIV122')
    expect(wrapper.text()).toContain('$.clients[0].owner')
    expect(wrapper.text()).not.toContain(sourceValue)
    expect(wrapper.html()).not.toContain(sourceValue)
  })

  it('clears a stale result when a replacement file is selected', async () => {
    vi.spyOn(adminApi, 'preflightIdentityServer4').mockResolvedValue(validResult)
    const wrapper = mount(IdentityServer4MigrationView)
    await selectFile(wrapper, new File(['{}'], 'first.json', { type: 'application/json' }))
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Manifest is valid')

    await selectFile(wrapper, new File(['{}'], 'replacement.json', { type: 'application/json' }))

    expect(wrapper.text()).not.toContain('Manifest is valid')
    expect(wrapper.text()).toContain('replacement.json')
  })

  it('retains the selected file so a failed API request can retry', async () => {
    const preflight = vi.spyOn(adminApi, 'preflightIdentityServer4')
      .mockRejectedValueOnce(new Error('server detail must not render'))
      .mockResolvedValueOnce(validResult)
    const wrapper = mount(IdentityServer4MigrationView)
    await selectFile(wrapper, new File(['{}'], 'inventory.json', { type: 'application/json' }))

    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Check your access and connection, then retry')
    expect(wrapper.text()).not.toContain('server detail must not render')
    expect(wrapper.get('button').text()).toBe('Retry preflight')

    await wrapper.get('button').trigger('click')
    await flushPromises()

    expect(preflight).toHaveBeenCalledTimes(2)
    expect(wrapper.text()).toContain('Manifest is valid')
  })
})

async function selectFile(wrapper: ReturnType<typeof mount>, file: File) {
  const input = wrapper.get('[data-testid="manifest-input"]')
  Object.defineProperty(input.element, 'files', { configurable: true, value: [file] })
  await input.trigger('change')
}
