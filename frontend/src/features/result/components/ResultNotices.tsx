// Plain-language notes that qualify a result: partial (a source still analysing), degraded confidence, cached, disclaimer.
import type { ReactNode } from 'react'
import { SOURCE_INFO } from '@/features/scan/logic/sources'
import type { ScanResponse } from '@/lib/api/types'
import { cn } from '@/lib/utils'
import type { Reading } from '../logic/reading'

function Notice({ title, children, strong }: { title: string; children: ReactNode; strong?: boolean }) {
  return (
    <div role="note" className={cn('border px-4 py-3', strong ? 'border-foreground' : 'border-border')}>
      <p className="label-caps">{title}</p>
      <p className="mt-1 max-w-prose text-sm text-muted-foreground">{children}</p>
    </div>
  )
}

export function ResultNotices({ result, reading }: { result: ScanResponse; reading: Reading }) {
  const waiting = reading.stillAnalysing.map((source) => SOURCE_INFO[source.source].label).join(', ')
  const unanswered = reading.totalSources - reading.respondedSources - reading.stillAnalysing.length

  return (
    <div className="flex flex-col gap-3">
      {reading.stillAnalysing.length > 0 && (
        <Notice title="Partial result" strong>
          {waiting} {reading.stillAnalysing.length === 1 ? 'was' : 'were'} still analysing when we stopped waiting, so this score may
          change. Scan the link again in a few minutes.
        </Notice>
      )}
      {/* With no data there is no score, but the link's own shape was still checked: say what that found. */}
      {reading.noData && reading.localFindings.length > 0 && (
        <Notice title="Local checks only" strong>
          No source answered, but our own check of the link's text still found {reading.localFindings.length === 1 ? 'a problem' : 'problems'}:{' '}
          {reading.localFindings.map((finding) => finding.description).join('; ')}. Treat the link with care.
        </Notice>
      )}
      {!reading.noData && unanswered > 0 && (
        <Notice title="Lower confidence">
          {unanswered} of {reading.totalSources} sources could not be reached. The score only reflects the sources that answered.
        </Notice>
      )}
      {result.fromCache && (
        <Notice title="From cache">
          This link was checked recently, so the earlier reading is shown. No new requests were spent.
        </Notice>
      )}
      <Notice title="Sharing this result">
        A shared link does not save this reading: opening it scans the link again. Within an hour the cache gives the same result; later it may
        differ. Tollgate is not published online, so the link only opens where this copy of Tollgate can be reached.
      </Notice>
      <Notice title="A risk indicator, not a guarantee">
        A brand-new phishing page that is on no list yet can still look safe. Treat the score as one input to your own judgement.
      </Notice>
    </div>
  )
}
