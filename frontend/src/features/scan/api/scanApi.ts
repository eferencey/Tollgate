// The real dependencies of the scan runner: the typed API client (requests are never cancelled, see
// ScanRunnerDeps.fetchScan), an abortable timer and the clock.
import { api } from '@/lib/api/client'
import { failureFromResponse } from '../logic/scanFailure'
import type { FetchOutcome, ScanRunnerDeps } from '../logic/scanRunner'

async function fetchScan(url: string): Promise<FetchOutcome> {
  try {
    const { data, error, response } = await api.GET('/api/scan', { params: { query: { url } } })
    if (data) return { ok: true, result: data }
    return { ok: false, failure: failureFromResponse(response.status, error?.detail, response.headers.get('Retry-After')) }
  } catch {
    return { ok: false, failure: { kind: 'offline' } } // fetch throws only when the server cannot be reached at all
  }
}

function sleep(milliseconds: number, signal: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(resolve, milliseconds)
    signal.addEventListener(
      'abort',
      () => {
        clearTimeout(timer)
        reject(signal.reason)
      },
      { once: true },
    )
  })
}

export const realScanDeps: ScanRunnerDeps = { fetchScan, sleep, now: Date.now }
