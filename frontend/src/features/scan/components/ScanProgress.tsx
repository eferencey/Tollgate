// What the user sees while a scan runs: the four sources with their live state, and the countdown to the next re-check.
import type { SourceStatus, ThreatSource } from '@/lib/api/types'
import { cn } from '@/lib/utils'
import { isQuotaExhausted } from '@/features/result/logic/reading'
import { SOURCE_INFO, SOURCE_ORDER } from '../logic/sources'
import { useCountdown } from '../hooks/useCountdown'

interface ScanProgressProps {
  sources: SourceStatus[]
  nextCheckAt?: number
}

function describe(status: SourceStatus | undefined): { caption: string; settled: boolean } {
  if (!status) return { caption: 'Asking…', settled: false }
  if (status.state === 'Completed') return { caption: 'Answered', settled: true }
  if (status.state === 'Pending') return { caption: 'Analysing the link', settled: false }
  return { caption: isQuotaExhausted(status) ? 'Quota used up' : 'Unavailable', settled: true }
}

export function ScanProgress({ sources, nextCheckAt }: ScanProgressProps) {
  const secondsLeft = useCountdown(nextCheckAt)
  const byName = new Map<ThreatSource, SourceStatus>(sources.map((status) => [status.source, status]))

  return (
    <section aria-labelledby="progress-title" className="w-full border border-border bg-card" aria-busy="true">
      <div className="flex items-center justify-between border-b border-border px-4 py-2">
        <h2 id="progress-title" className="text-xl">
          Taking a reading
        </h2>
        <p className="label-caps text-muted-foreground" role="status">
          {secondsLeft === null ? 'Querying four sources' : `Checking again in ${secondsLeft}s`}
        </p>
      </div>

      <ul>
        {SOURCE_ORDER.map((source) => {
          const status = byName.get(source)
          const { caption, settled } = describe(status)
          return (
            <li key={source} className="flex items-start gap-3 border-b border-border px-4 py-3 last:border-b-0">
              <span
                aria-hidden="true"
                className={cn('mt-1.5 size-2.5 shrink-0 border border-current', settled ? 'bg-current' : 'motion-safe:animate-pulse')}
              />
              <div className="min-w-0 flex-1">
                <p className="flex flex-wrap items-baseline justify-between gap-x-4">
                  <span className="label-caps">{SOURCE_INFO[source].label}</span>
                  <span className="label-caps text-muted-foreground">{caption}</span>
                </p>
                {status?.reason && <p className="mt-1 text-sm text-muted-foreground">{status.reason}</p>}
              </div>
            </li>
          )
        })}
      </ul>

      {secondsLeft !== null && (
        <p className="border-t border-border px-4 py-3 text-sm text-muted-foreground">
          VirusTotal has not seen this link before, so it is analysing it now. This page asks again by itself; you do not need to
          reload.
        </p>
      )}
    </section>
  )
}
