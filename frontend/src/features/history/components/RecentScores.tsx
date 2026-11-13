// A bar chart drawn by hand in SVG: one bar per recent scan, its height the score, its colour the verdict, newest on the right.
// The geometry comes from scoreChart.ts; this file only draws rectangles. No chart library is used: three simple charts do not justify a dependency.
import { VERDICT_TEXT } from '@/components/brand/verdictTone'
import type { ScanHistoryEntry } from '@/lib/api/types'
import { cn } from '@/lib/utils'
import { Panel } from './Panel'
import { CHART, describeScores, scoreBars } from '../logic/scoreChart'

export function RecentScores({ entries, now }: { entries: ScanHistoryEntry[]; now: number }) {
  const bars = scoreBars(entries, now)
  const caption = `Scores of the last ${bars.length} ${bars.length === 1 ? 'scan' : 'scans'}`

  return (
    <Panel id="scores-title" title="Recent scores" aside="Newest on the right">
      <div className="p-4">
        <svg
          role="img"
          aria-label={describeScores(entries)}
          viewBox={`0 0 ${CHART.width} ${CHART.height}`}
          className="block h-auto w-full"
        >
          {/* The two rules are the scale: the bottom is a score of 0, the top a score of 100. */}
          <line x1="0" x2={CHART.width} y1="0.5" y2="0.5" className="stroke-border" strokeDasharray="4 4" />
          <line x1="0" x2={CHART.width} y1={CHART.height - 0.5} y2={CHART.height - 0.5} className="stroke-muted-foreground" />
          {bars.map((bar) => (
            <rect
              key={bar.x}
              x={bar.x}
              y={bar.y}
              width={bar.width}
              height={bar.height}
              className={cn('fill-current', VERDICT_TEXT[bar.verdict])}
            >
              <title>{bar.label}</title>
            </rect>
          ))}
        </svg>
        <p className="mt-3 text-sm text-muted-foreground">
          {caption}. Bar height is the score from 0 (bottom) to 100 (top); colour is the verdict. Hover a bar for details.
        </p>
      </div>
    </Panel>
  )
}
