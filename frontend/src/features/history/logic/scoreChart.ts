// Geometry of the "recent scores" chart: one bar per scan, newest on the right. Pure, so the layout is unit-tested
// and the component only draws rectangles. Units are SVG user units; the SVG scales to its container.
import type { ScanHistoryEntry, Verdict } from '@/lib/api/types'
import { formatWhen } from './when'

export const CHART = {
  /** Bars the chart holds. Fewer scans leave empty slots on the left, so the newest bar always sits at the right edge. */
  slots: 20,
  width: 600,
  height: 160,
  /** Space between two bars. */
  gap: 6,
  /** A score of 0 is the most important reading and must stay visible, so no bar is drawn flatter than this. */
  minBarHeight: 3,
} as const

interface ScoreBar {
  x: number
  y: number
  width: number
  height: number
  verdict: Verdict
  /** Hover text: the host, the score, the verdict and when. */
  label: string
}

/** `entries` are newest first, as the API returns them; the bars run oldest to newest, left to right. */
export function scoreBars(entries: ScanHistoryEntry[], now: number): ScoreBar[] {
  const shown = entries.slice(0, CHART.slots).reverse()
  const slotWidth = CHART.width / CHART.slots
  const firstSlot = CHART.slots - shown.length

  return shown.map((entry, index) => {
    const height = Math.max(CHART.minBarHeight, (entry.score / 100) * CHART.height)
    return {
      x: (firstSlot + index) * slotWidth + CHART.gap / 2,
      y: CHART.height - height,
      width: slotWidth - CHART.gap,
      height,
      verdict: entry.verdict,
      label: `${entry.host}: ${entry.score}, ${entry.verdict}, ${formatWhen(entry.scannedAt, now)}`,
    }
  })
}

/** Text alternative for the chart, read by screen readers: the same numbers the bars show. */
export function describeScores(entries: ScanHistoryEntry[]): string {
  const shown = entries.slice(0, CHART.slots).reverse()
  if (shown.length === 0) return 'No scores to show.'
  return `Scores of the last ${shown.length} ${shown.length === 1 ? 'scan' : 'scans'}, oldest to newest: ${shown.map((entry) => entry.score).join(', ')}.`
}
