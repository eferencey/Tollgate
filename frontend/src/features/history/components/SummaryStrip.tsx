// The four headline numbers across the top: a ruled strip of cells, like the readouts of an instrument.
import { summaryFigures } from '../logic/figures'
import type { StatsResponse } from '@/lib/api/types'

export function SummaryStrip({ stats }: { stats: StatsResponse }) {
  return (
    <dl aria-label="Summary" className="grid grid-cols-2 gap-px border border-border bg-border lg:grid-cols-4">
      {summaryFigures(stats).map((figure) => (
        <div key={figure.label} className="bg-card p-4">
          <dt className="label-caps text-muted-foreground">{figure.label}</dt>
          <dd className="mt-2 font-mono text-4xl font-medium">{figure.value}</dd>
          <dd className="mt-1 text-sm text-muted-foreground">{figure.note}</dd>
        </div>
      ))}
    </dl>
  )
}
