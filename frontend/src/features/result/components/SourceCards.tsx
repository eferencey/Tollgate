// One ruled card per threat-intelligence source: did it answer, what did it take off, or why it could not answer.
import { SOURCE_INFO, sortSources } from '@/features/scan/logic/sources'
import type { ScanResponse, SourceStatus } from '@/lib/api/types'
import { cn } from '@/lib/utils'
import { formatPoints, isQuotaExhausted, pointsFrom } from '../logic/reading'

export function SourceCards({ result }: { result: ScanResponse }) {
  return (
    <section aria-labelledby="sources-title">
      <h2 id="sources-title" className="text-3xl sm:text-4xl">
        The four sources
      </h2>
      <ul className="mt-6 grid gap-px border border-border bg-border sm:grid-cols-2 lg:grid-cols-4">
        {sortSources(result.sources).map((status) => (
          <SourceCard key={status.source} status={status} points={pointsFrom(status.source, result.findings)} />
        ))}
      </ul>
    </section>
  )
}

function stateCaption(status: SourceStatus): string {
  if (status.state === 'Completed') return 'Answered'
  if (status.state === 'Pending') return 'Still analysing'
  return isQuotaExhausted(status) ? 'Quota used up' : 'Unavailable'
}

function SourceCard({ status, points }: { status: SourceStatus; points: number }) {
  const info = SOURCE_INFO[status.source]
  const answered = status.state === 'Completed'

  return (
    <li className={cn('flex flex-col gap-3 bg-card p-4', !answered && 'text-muted-foreground')}>
      <div className="flex items-center justify-between gap-2">
        <h3 className="text-xl">{info.label}</h3>
        {/* A shape as well as words: filled square = answered, hollow = did not answer. Colour is reserved for verdicts. */}
        <span aria-hidden="true" className={cn('size-2.5 border border-current', answered && 'bg-current')} />
      </div>
      <p className="label-caps">{stateCaption(status)}</p>
      <p className="text-sm">{info.checks}</p>
      <p className="mt-auto border-t border-border pt-3 font-mono text-sm">
        {answered ? (points === 0 ? 'No deductions' : `${formatPoints(points)} points`) : (status.reason ?? 'No answer yet')}
      </p>
    </li>
  )
}
