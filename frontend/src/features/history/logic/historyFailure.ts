// The ways loading the history page can fail, reduced to what the screen needs. Reuses the scan page's HTTP mapping,
// so a 429 is read the same way everywhere (one Retry-After rule).
import { failureFromResponse } from '@/features/scan/logic/scanFailure'

export type HistoryFailure = { kind: 'rate-limited'; retryAfterSeconds: number } | { kind: 'offline' } | { kind: 'server' }

export function historyFailureFromResponse(status: number, retryAfterHeader: string | null): HistoryFailure {
  const failure = failureFromResponse(status, null, retryAfterHeader)
  // These endpoints take no URL, so a 400 can only mean something is badly wrong on the server side.
  return failure.kind === 'invalid-url' ? { kind: 'server' } : failure
}
