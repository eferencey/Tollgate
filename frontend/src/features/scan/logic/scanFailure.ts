// The ways a scan request can fail, reduced to what the screen needs to show; no HTTP details leak into components.

export type ScanFailure =
  | { kind: 'invalid-url'; message: string }
  | { kind: 'rate-limited'; retryAfterSeconds: number }
  | { kind: 'offline' }
  | { kind: 'server' }

// Used when the 429 response carries no usable Retry-After header (the backend always sends one).
export const DEFAULT_RETRY_AFTER_SECONDS = 60

export function failureFromResponse(status: number, detail: string | null | undefined, retryAfterHeader: string | null): ScanFailure {
  if (status === 400) return { kind: 'invalid-url', message: detail || 'That does not look like a link we can check.' }
  if (status === 429) return { kind: 'rate-limited', retryAfterSeconds: parseRetryAfter(retryAfterHeader) }
  return { kind: 'server' }
}

function parseRetryAfter(header: string | null): number {
  const seconds = Number(header)
  return Number.isInteger(seconds) && seconds > 0 ? seconds : DEFAULT_RETRY_AFTER_SECONDS
}
