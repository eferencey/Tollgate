// Tests for the relative times shown in the history list.
import { describe, expect, it } from 'vitest'
import { formatRowMeta, formatWhen } from './when'

const NOW = Date.parse('2026-11-11T12:00:00Z')
const ago = (milliseconds: number) => new Date(NOW - milliseconds).toISOString()

describe('formatWhen', () => {
  it('says "just now" under a minute, and for a clock slightly ahead of ours', () => {
    expect(formatWhen(ago(20_000), NOW)).toBe('just now')
    expect(formatWhen(ago(-5_000), NOW)).toBe('just now')
  })

  it('counts minutes, hours and days', () => {
    expect(formatWhen(ago(5 * 60_000), NOW)).toBe('5 min ago')
    expect(formatWhen(ago(3 * 3_600_000), NOW)).toBe('3 h ago')
    expect(formatWhen(ago(2 * 86_400_000 + 3_600_000), NOW)).toBe('2 d ago')
  })

  it('switches to a date after thirty days', () => {
    expect(formatWhen('2026-09-01T10:00:00Z', NOW)).toBe('2026-09-01')
  })

  it('shows a dash for an unreadable time', () => {
    expect(formatWhen('not a date', NOW)).toBe('—')
  })
})

describe('formatRowMeta', () => {
  it('leaves High confidence unsaid', () => {
    expect(formatRowMeta('High', ago(5 * 60_000), NOW)).toBe('5 min ago')
  })

  it('names a Medium or Low confidence', () => {
    expect(formatRowMeta('Medium', ago(5 * 60_000), NOW)).toBe('Medium confidence · 5 min ago')
    expect(formatRowMeta('Low', ago(3 * 3_600_000), NOW)).toBe('Low confidence · 3 h ago')
  })
})
