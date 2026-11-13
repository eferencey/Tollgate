// The /history route: headline, then whatever the dashboard state says (skeleton, failure, empty, or the numbers and the list).
// The headline sits directly on the silk background and uses full-strength text; every other block is an opaque card.
import { HistoryEmpty } from './HistoryEmpty'
import { HistoryFailureNotice } from './HistoryFailureNotice'
import { HistorySkeleton } from './HistorySkeleton'
import { RecentScans } from './RecentScans'
import { RecentScores } from './RecentScores'
import { SummaryStrip } from './SummaryStrip'
import { TopHosts } from './TopHosts'
import { useDashboard } from '../hooks/useDashboard'
import { VerdictSplit } from './VerdictSplit'

export function HistoryPage() {
  const { state, showMore, retry, remove, clearAll } = useDashboard()

  return (
    <section className="flex flex-col gap-8">
      <div className="flex flex-col items-start gap-4">
        <p className="label-caps">Logbook · Past readings</p>
        <h1 className="text-5xl sm:text-6xl">History and statistics</h1>
        <p className="max-w-prose text-lg">What this copy of Tollgate has checked so far, in numbers and in order.</p>
      </div>

      {state.phase === 'loading' && <HistorySkeleton />}
      {state.phase === 'failed' && <HistoryFailureNotice failure={state.failure} onRetry={retry} />}
      {state.phase === 'ready' && state.stats.totalScans === 0 && <HistoryEmpty retentionDays={state.stats.retentionDays} />}
      {state.phase === 'ready' && state.stats.totalScans > 0 && (
        <>
          <SummaryStrip stats={state.stats} />
          <div className="grid items-start gap-6 lg:grid-cols-2">
            <VerdictSplit counts={state.stats.verdicts} />
            <TopHosts hosts={state.stats.topHosts} />
          </div>
          <RecentScores entries={state.items} now={state.loadedAt} />
          <RecentScans
            items={state.items}
            total={state.stats.totalScans}
            retentionDays={state.stats.retentionDays}
            requested={state.requested}
            now={state.loadedAt}
            more={state.more}
            onShowMore={showMore}
            onDelete={remove}
            onClear={clearAll}
          />
        </>
      )}
    </section>
  )
}
