// Tests for mapping failed scan responses (invalid link, rate limit, server and network errors) to user-facing failures.
import { describe, expect, it } from 'vitest'
import { DEFAULT_RETRY_AFTER_SECONDS, failureFromResponse } from './scanFailure'

describe('failureFromResponse', () => {
  it('turns 400 into an invalid-url failure carrying the backend explanation', () => {
    expect(failureFromResponse(400, 'Only http and https URLs can be scanned.', null)).toEqual({
      kind: 'invalid-url',
      message: 'Only http and https URLs can be scanned.',
    })
  })

  it('has a fallback message when the backend sent none', () => {
    expect(failureFromResponse(400, undefined, null)).toMatchObject({ kind: 'invalid-url' })
  })

  it('reads the Retry-After header on 429', () => {
    expect(failureFromResponse(429, null, '37')).toEqual({ kind: 'rate-limited', retryAfterSeconds: 37 })
  })

  it('falls back to a default wait when Retry-After is missing or garbage', () => {
    expect(failureFromResponse(429, null, null)).toEqual({ kind: 'rate-limited', retryAfterSeconds: DEFAULT_RETRY_AFTER_SECONDS })
    expect(failureFromResponse(429, null, 'soon')).toEqual({ kind: 'rate-limited', retryAfterSeconds: DEFAULT_RETRY_AFTER_SECONDS })
  })

  it('treats everything else as a server failure', () => {
    expect(failureFromResponse(500, 'boom', null)).toEqual({ kind: 'server' })
  })
})
