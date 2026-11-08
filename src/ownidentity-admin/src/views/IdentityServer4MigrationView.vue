<script setup lang="ts">
import { ref } from 'vue'
import { adminApi, type IdentityServer4PreflightResult } from '@/api/client'
import PageHeader from '@/components/PageHeader.vue'

const maximumBytes = 1_048_576
const selectedFile = ref<File | null>(null)
const selectedName = ref<string | null>(null)
const selectedSize = ref<number | null>(null)
const localError = ref<string | null>(null)
const requestError = ref<string | null>(null)
const result = ref<IdentityServer4PreflightResult | null>(null)
const validating = ref(false)

function selectFile(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0] ?? null
  selectedFile.value = file
  selectedName.value = file?.name ?? null
  selectedSize.value = file?.size ?? null
  if (file && !file.name.toLowerCase().endsWith('.json')) {
    localError.value = 'Choose a .json manifest file.'
  } else if (file && file.size > maximumBytes) {
    localError.value = 'The selected file exceeds the 1 MiB limit and was not sent.'
  } else {
    localError.value = null
  }
  requestError.value = null
  result.value = null
}

async function validateManifest() {
  if (!selectedFile.value || localError.value || validating.value) return

  validating.value = true
  requestError.value = null
  result.value = null
  try {
    result.value = await adminApi.preflightIdentityServer4(selectedFile.value)
  } catch {
    requestError.value = 'Preflight could not be completed. Check your access and connection, then retry.'
  } finally {
    validating.value = false
  }
}

function formatBytes(bytes: number) {
  return `${new Intl.NumberFormat('en-US').format(bytes)} bytes`
}
</script>

<template>
  <PageHeader
    eyebrow="Side-by-side migration"
    title="Migrate from IdentityServer4"
    description="Validate a sanitized schema-v1 inventory manifest before migration design. This preflight does not connect to the source, migrate, or apply data."
  />

  <section class="migration-panel" aria-labelledby="manifest-heading">
    <div>
      <p class="eyebrow">Manifest preflight</p>
      <h2 id="manifest-heading">Sanitized schema-v1 JSON only</h2>
      <p>Choose one <code>.json</code> file up to 1 MiB. Its content is sent directly for validation and is never previewed or persisted.</p>
    </div>
    <label>
      Inventory manifest
      <input
        data-testid="manifest-input"
        type="file"
        accept=".json,application/json"
        :disabled="validating"
        @change="selectFile"
      />
    </label>

    <p v-if="selectedName && selectedSize !== null" class="file-fact">
      Selected: <strong>{{ selectedName }}</strong> ({{ formatBytes(selectedSize) }})
    </p>
    <p v-if="localError" class="error" role="alert">{{ localError }}</p>
    <p v-if="requestError" class="error" role="alert">
      {{ requestError }}
    </p>

    <button
      class="primary"
      type="button"
      :disabled="!selectedFile || !!localError || validating"
      @click="validateManifest"
    >
      {{ requestError ? 'Retry preflight' : 'Validate manifest' }}
    </button>
    <p class="live-status" aria-live="polite">{{ validating ? 'Validating manifest...' : '' }}</p>
  </section>

  <section v-if="result" class="preflight-result" aria-labelledby="result-heading">
    <div :class="['result-banner', result.isValid ? 'valid' : 'invalid']">
      <p class="eyebrow">Preflight result</p>
      <h2 id="result-heading">{{ result.isValid ? 'Manifest is valid' : 'Manifest is invalid' }}</h2>
    </div>

    <div class="summary-grid" aria-label="Redacted inventory summary">
      <div><strong>{{ result.summary.tables }}</strong><span>Tables</span></div>
      <div><strong>{{ result.summary.extensions }}</strong><span>Extensions</span></div>
      <div><strong>{{ result.summary.credentials }}</strong><span>Credentials</span></div>
      <div><strong>{{ result.summary.keys }}</strong><span>Keys</span></div>
      <div><strong>{{ result.summary.clients }}</strong><span>Clients</span></div>
      <div><strong>{{ result.summary.customGrants }}</strong><span>Custom grants</span></div>
      <div><strong>{{ result.summary.adrs }}</strong><span>ADRs</span></div>
      <div><strong>{{ result.summary.ropcBlockers }}</strong><span>ROPC blockers</span></div>
    </div>

    <div v-if="result.isValid && result.cutoverBlocked" class="cutover-warning" role="alert">
      <strong>Cutover blocked.</strong> {{ result.summary.ropcBlockers }} ROPC blocker(s) must be eliminated. ROPC has no activation or exception path.
    </div>
    <p v-else-if="result.isValid" class="result-note">
      This proves only that the sanitized manifest matches the validation contract. Source truth, migration readiness, and data application remain unproven.
    </p>

    <div v-if="!result.isValid" class="diagnostics">
      <h3>Safe diagnostics</h3>
      <ul>
        <li v-for="diagnostic in result.diagnostics" :key="`${diagnostic.code}-${diagnostic.path}`">
          <code>{{ diagnostic.code }}</code> <code>{{ diagnostic.path }}</code> {{ diagnostic.message }}
        </li>
      </ul>
      <p v-if="result.diagnosticsTruncated">Only the first 100 diagnostics are shown.</p>
    </div>
  </section>
</template>
