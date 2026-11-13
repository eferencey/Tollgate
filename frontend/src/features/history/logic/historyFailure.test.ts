// Tests for turning a failed history request into a user-facing failure.
import { describe, expect, it } from 'vitest'
import { DEFAULT_RETRY_AFTER_SECONDS } from '@/features/scan/logic/scanFailure'
import { historyFailureFromResponse } from './historyFailure'

describe('historyFailureFromResponse', () => {
  it('reads Retry-After on 429', () => {
    expect(historyFailureFromResponse(429, '12')).toEqual({ kind: 'rate-limited', retryAfterSeconds: 12 })
  })

  it('falls back to the default wait when the header is missing', () => {
    expect(historyFailureFromResponse(429, null)).toEqual({ kind: 'rate-limited', retryAfterSeconds: DEFAULT_RETRY_AFTER_SECONDS })
  })

  it('treats server errors as a server failure', () => {
    expect(historyFailureFromResponse(500, null)).toEqual({ kind: 'server' })
  })

  it('never reports an invalid link: these endpoints take none', () => {
    expect(historyFailureFromResponse(400, null)).toEqual({ kind: 'server' })
  })
})
