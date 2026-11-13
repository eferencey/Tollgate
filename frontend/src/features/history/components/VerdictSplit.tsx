// How the scans divide into Dangerous / Suspicious / Safe: one stacked bar plus a legend that repeats every number in words,
// so the colours are never the only carrier of meaning.
import { VERDICT_BG } from '@/components/brand/verdictTone'
import type { VerdictCounts } from '@/lib/api/types'
import { cn } from '@/lib/utils'
import { describeVerdictSplit, verdictSegments } from '../logic/figures'
import { Panel } from './Panel'

export function VerdictSplit({ counts }: { counts: VerdictCounts }) {
  const segments = verdictSegments(counts)

  return (
    <Panel id="split-title" title="Verdict split" aside="Share of scans">
      <div className="p-4">
        <div role="img" aria-label={describeVerdictSplit(segments)} className="flex h-6 w-full border border-border bg-muted">
          {segments
            .filter((segment) => segment.count > 0)
            .map((segment) => (
              <div key={segment.verdict} className={cn('h-full', VERDICT_BG[segment.verdict])} style={{ width: `${segment.share}%` }} />
            ))}
        </div>

        <ul className="mt-4 flex flex-col gap-2">
          {segments.map((segment) => (
            <li key={segment.verdict} className="flex items-center gap-3">
              <span aria-hidden="true" className={cn('size-3 shrink-0', VERDICT_BG[segment.verdict])} />
              <span className="label-caps flex-1">{segment.verdict}</span>
              <span className="font-mono">{segment.count}</span>
              <span className="w-12 text-right font-mono text-muted-foreground">{segment.percent}</span>
            </li>
          ))}
        </ul>
      </div>
    </Panel>
  )
}
