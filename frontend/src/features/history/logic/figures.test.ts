// Tests for the history summary figures: verdict slices, host bars and percentage wording.
import { describe, expect, it } from 'vitest'
import type { StatsResponse } from '@/lib/api/types'
import { describeVerdictSplit, formatPercent, hostBars, summaryFigures, verdictSegments } from './figures'

const stats = (overrides: Partial<StatsResponse> = {}): StatsResponse => ({
  mode: 'Demo',
  retentionDays: 90,
  totalScans: 8,
  averageScore: 62.5,
  dangerousRatio: 0.25,
  verdicts: { dangerous: 2, suspicious: 2, safe: 4 },
  topHosts: [],
  ...overrides,
})

describe('summaryFigures', () => {
  it('reads the four headline numbers', () => {
    expect(summaryFigures(stats())).toEqual([
      { label: 'Scans on record', value: '8', note: 'Finished scans only' },
      { label: 'Average score', value: '63', note: 'Out of 100' },
      { label: 'Dangerous', value: '25%', note: 'Share of all scans' },
      { label: 'Data', value: 'Demo', note: 'Kept for 90 days' },
    ])
  })

  it('shows a dash, not a zero, when there is nothing to average', () => {
    const figures = summaryFigures(stats({ totalScans: 0, averageScore: undefined, dangerousRatio: undefined }))
    expect(figures[1].value).toBe('—')
    expect(figures[2].value).toBe('—')
  })
})

describe('formatPercent', () => {
  it('rounds to whole percent', () => {
    expect(formatPercent(0.3333)).toBe('33%')
    expect(formatPercent(1)).toBe('100%')
    expect(formatPercent(0)).toBe('0%')
  })

  it('never calls a non-zero share zero', () => {
    expect(formatPercent(0.003)).toBe('<1%')
  })
})

describe('verdictSegments', () => {
  it('lists all three verdicts, most serious first, with exact shares', () => {
    const segments = verdictSegments({ dangerous: 1, suspicious: 1, safe: 2 })
    expect(segments.map((segment) => segment.verdict)).toEqual(['Dangerous', 'Suspicious', 'Safe'])
    expect(segments.map((segment) => segment.share)).toEqual([25, 25, 50])
    expect(segments.map((segment) => segment.percent)).toEqual(['25%', '25%', '50%'])
  })

  it('keeps a verdict with no scans in the legend, at width zero', () => {
    const segments = verdictSegments({ dangerous: 0, suspicious: 0, safe: 3 })
    expect(segments[0]).toMatchObject({ verdict: 'Dangerous', count: 0, share: 0 })
  })

  it('does not divide by zero when there are no scans', () => {
    expect(verdictSegments({ dangerous: 0, suspicious: 0, safe: 0 }).every((segment) => segment.share === 0)).toBe(true)
  })

  it('describes the split in words for screen readers', () => {
    expect(describeVerdictSplit(verdictSegments({ dangerous: 1, suspicious: 2, safe: 3 }))).toBe('Dangerous 1, Suspicious 2, Safe 3')
  })
})

describe('hostBars', () => {
  it('scales the bars against the busiest host', () => {
    const bars = hostBars([
      { host: 'a.com', scans: 4, averageScore: 100 },
      { host: 'b.com', scans: 1, averageScore: 12.4 },
    ])
    expect(bars.map((bar) => bar.share)).toEqual([100, 25])
    expect(bars[1].averageScore).toBe(12)
  })

  it('is empty for no hosts', () => {
    expect(hostBars([])).toEqual([])
  })
})
