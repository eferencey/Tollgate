// Turns the statistics response into the numbers and bar widths the dashboard draws. Pure: no React, no network.
import type { HostStatistics, StatsResponse, Verdict, VerdictCounts } from '@/lib/api/types'

interface Figure {
  label: string
  value: string
  note: string
}

/** Shown instead of a number while there is nothing to average: "no data" must not look like a zero. */
const NO_VALUE = '—'

export function formatPercent(ratio: number): string {
  const rounded = Math.round(ratio * 100)
  return ratio > 0 && rounded === 0 ? '<1%' : `${rounded}%` // a single dangerous scan among hundreds is still not "0%"
}

export function summaryFigures(stats: StatsResponse): Figure[] {
  return [
    { label: 'Scans on record', value: String(stats.totalScans), note: 'Finished scans only' },
    {
      label: 'Average score',
      value: stats.averageScore == null ? NO_VALUE : String(Math.round(stats.averageScore)),
      note: 'Out of 100',
    },
    {
      label: 'Dangerous',
      value: stats.dangerousRatio == null ? NO_VALUE : formatPercent(stats.dangerousRatio),
      note: 'Share of all scans',
    },
    { label: 'Data', value: stats.mode, note: `Kept for ${stats.retentionDays} days` },
  ]
}

interface VerdictSegment {
  verdict: Verdict
  count: number
  /** Exact share of the bar (0 to 100), used for the segment width. */
  share: number
  /** The same share rounded for the legend text. */
  percent: string
}

// Most serious first, the same order the API lists the counts in.
const VERDICT_ORDER: Verdict[] = ['Dangerous', 'Suspicious', 'Safe']

const COUNT_KEY: Record<Verdict, keyof VerdictCounts> = {
  Dangerous: 'dangerous',
  Suspicious: 'suspicious',
  Safe: 'safe',
}

/** One entry per verdict, always all three, so the legend never silently drops a verdict that has zero scans. */
export function verdictSegments(counts: VerdictCounts): VerdictSegment[] {
  const total = counts.dangerous + counts.suspicious + counts.safe
  return VERDICT_ORDER.map((verdict) => {
    const count = counts[COUNT_KEY[verdict]]
    const ratio = total === 0 ? 0 : count / total
    return { verdict, count, share: ratio * 100, percent: formatPercent(ratio) }
  })
}

export function describeVerdictSplit(segments: VerdictSegment[]): string {
  return segments.map((segment) => `${segment.verdict} ${segment.count}`).join(', ')
}

interface HostBar {
  host: string
  scans: number
  /** Bar width (0 to 100) relative to the busiest host. */
  share: number
  averageScore: number
}

export function hostBars(hosts: HostStatistics[]): HostBar[] {
  const busiest = Math.max(1, ...hosts.map((host) => host.scans))
  return hosts.map((host) => ({
    host: host.host,
    scans: host.scans,
    share: (host.scans / busiest) * 100,
    averageScore: Math.round(host.averageScore),
  }))
}
