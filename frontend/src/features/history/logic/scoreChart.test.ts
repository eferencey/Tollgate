// Tests for the recent-scores bar chart geometry and its screen-reader description.
import { describe, expect, it } from 'vitest'
import type { ScanHistoryEntry, Verdict } from '@/lib/api/types'
import { CHART, describeScores, scoreBars } from './scoreChart'

const NOW = Date.parse('2026-11-11T12:00:00Z')
const entry = (score: number, verdict: Verdict = 'Safe', host = 'example.com'): ScanHistoryEntry => ({
  id: score,
  url: `https://${host}/`,
  host,
  score,
  verdict,
  confidence: 'High',
  scannedAt: '2026-11-11T11:55:00Z',
})

describe('scoreBars', () => {
  it('runs oldest to newest from left to right (the API sends newest first)', () => {
    const bars = scoreBars([entry(10), entry(50), entry(90)], NOW) // newest = 10
    expect(bars.map((bar) => bar.x)).toEqual([...bars.map((bar) => bar.x)].sort((a, b) => a - b))
    expect(bars[0].height).toBeGreaterThan(bars[2].height) // oldest (90) is the tallest here, newest (10) the shortest
  })

  it('keeps the newest bar at the right edge even with few scans', () => {
    const bars = scoreBars([entry(80)], NOW)
    expect(bars).toHaveLength(1)
    expect(bars[0].x + bars[0].width + CHART.gap / 2).toBeCloseTo(CHART.width)
  })

  it('scales height with the score and rests every bar on the baseline', () => {
    const [full, half] = scoreBars([entry(50), entry(100)], NOW) // oldest first: 100, 50
    expect(full.height).toBe(CHART.height)
    expect(half.height).toBe(CHART.height / 2)
    expect(full.y + full.height).toBe(CHART.height)
  })

  it('draws a score of 0 as a thin visible bar, not nothing', () => {
    expect(scoreBars([entry(0, 'Dangerous')], NOW)[0].height).toBe(CHART.minBarHeight)
  })

  it('carries the verdict and a readable hover label', () => {
    const [bar] = scoreBars([entry(63, 'Suspicious', 'free-bonus.xyz')], NOW)
    expect(bar.verdict).toBe('Suspicious')
    expect(bar.label).toBe('free-bonus.xyz: 63, Suspicious, 5 min ago')
  })

  it('shows at most the slots it has, taking the newest', () => {
    const many = Array.from({ length: CHART.slots + 5 }, (_, index) => entry(index)) // index 0 = newest
    const bars = scoreBars(many, NOW)
    expect(bars).toHaveLength(CHART.slots)
    expect(bars.at(-1)?.label).toContain(': 0,') // the newest scan is last
  })
})

describe('describeScores', () => {
  it('lists the scores oldest to newest', () => {
    expect(describeScores([entry(10), entry(50), entry(90)])).toBe('Scores of the last 3 scans, oldest to newest: 90, 50, 10.')
  })

  it('has a sentence for no scans', () => {
    expect(describeScores([])).toBe('No scores to show.')
  })
})
