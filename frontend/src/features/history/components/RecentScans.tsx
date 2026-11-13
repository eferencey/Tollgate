// The list of recent scans. Every row is an ordinary link that scans the same link again (so right-click, new tab and
// keyboard Enter all work). Rows show host and path only: the query string and any credentials stay hidden (see links.ts).
// Each row has its own delete button beside the link (a button cannot sit inside a link), and the list can be cleared.
import { Link } from 'react-router'
import { VerdictBadge } from '@/components/brand/VerdictBadge'
import { Button } from '@/components/ui/button'
import type { ScanHistoryEntry } from '@/lib/api/types'
import { listEnd, type MoreState } from '../logic/dashboardState'
import { displayLink, rescanPath } from '../logic/links'
import { ConfirmAction } from './ConfirmAction'
import { Panel } from './Panel'
import { formatExact, formatRowMeta } from '../logic/when'

const TITLE_ID = 'scans-title'

interface RecentScansProps {
  items: ScanHistoryEntry[]
  total: number
  retentionDays: number
  requested: number
  /** The moment the data was loaded; times in the list are relative to it. */
  now: number
  more: MoreState
  onShowMore: () => void
  onDelete: (id: number) => Promise<boolean>
  onClear: () => Promise<boolean>
}

export function RecentScans({ items, total, retentionDays, requested, now, more, onShowMore, onDelete, onClear }: RecentScansProps) {
  const end = listEnd(items.length, total, requested)

  // The deleted row disappears together with its focused button; the list title takes the focus instead of the page top.
  const deleteRow = async (id: number) => {
    const done = await onDelete(id)
    if (done) document.getElementById(TITLE_ID)?.focus()
    return done
  }

  return (
    <Panel id={TITLE_ID} title="Recent scans" aside={`${items.length} of ${total}`}>
      <div className="flex flex-col items-start gap-3 border-b border-border px-4 py-3">
        <p className="max-w-prose text-sm text-muted-foreground">
          Opening a row scans the link again; the result may differ from the one saved here. Links are stored in full for {retentionDays}{' '}
          days on this machine. This list hides everything after “?” and any password in front of the host. Deleting a scan also
          forgets its cached result.
        </p>
        <ConfirmAction
          trigger="Clear history"
          question={`Delete all ${total} scans and their cached results? This cannot be undone.`}
          confirmLabel="Delete all"
          failureText="The history could not be cleared. Nothing was deleted; try again."
          onConfirm={onClear}
        />
      </div>

      <ul>
        {items.map((item) => {
          const link = displayLink(item.url)
          return (
            <li key={item.id} className="flex flex-wrap items-center gap-x-2 border-b border-border pr-3 last:border-b-0">
              <Link
                to={rescanPath(item.url)}
                className="min-w-0 flex-1 grid grid-cols-[3.5rem_1fr] items-center gap-x-3 gap-y-1 px-4 py-3 outline-none hover:bg-muted focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset sm:grid-cols-[3.5rem_6.5rem_1fr_auto]"
              >
                <span className="row-span-2 font-mono text-2xl sm:row-span-1">
                  <span className="sr-only">Scan again, score </span>
                  {item.score}
                </span>

                <span className="col-start-2 row-start-1 min-w-0 truncate font-mono text-sm sm:col-start-3">
                  <span>{link.host}</span>
                  <span className="text-muted-foreground">{link.rest}</span>
                </span>

                {/* On a phone the badge and the time share the second line; from `sm` up they become grid columns of their own. */}
                <span className="col-start-2 row-start-2 flex flex-wrap items-center gap-x-3 gap-y-1 sm:contents">
                  <VerdictBadge verdict={item.verdict} className="sm:col-start-2 sm:row-start-1 sm:justify-self-start" />
                  <time
                    dateTime={item.scannedAt}
                    title={formatExact(item.scannedAt)}
                    className="label-caps text-muted-foreground sm:col-start-4 sm:row-start-1 sm:text-right"
                  >
                    {formatRowMeta(item.confidence, item.scannedAt, now)}
                  </time>
                </span>
              </Link>
              <ConfirmAction
                trigger="Delete"
                triggerLabel={`Delete ${link.host} from history`}
                question="Delete this scan from history?"
                confirmLabel="Delete"
                failureText="This scan could not be deleted; try again."
                onConfirm={() => deleteRow(item.id)}
              />
            </li>
          )
        })}
      </ul>

      <ListFooter end={end} shown={items.length} total={total} more={more} onShowMore={onShowMore} />
    </Panel>
  )
}

function ListFooter({ end, shown, total, more, onShowMore }: { end: ReturnType<typeof listEnd>; shown: number; total: number; more: MoreState; onShowMore: () => void }) {
  if (end === 'complete') return null

  return (
    <div className="flex flex-col items-start gap-3 border-t border-border px-4 py-3">
      {end === 'capped' ? (
        <p className="text-sm text-muted-foreground">
          Showing the newest {shown} of {total} scans, the most the server sends at once.
        </p>
      ) : (
        <Button variant="outline" onClick={onShowMore} disabled={more === 'loading'}>
          {more === 'loading' ? 'Loading…' : 'Show more'}
        </Button>
      )}
      {more === 'failed' && (
        <p role="alert" className="text-sm text-destructive">
          More scans could not be loaded. The ones above are still valid; try again.
        </p>
      )}
    </div>
  )
}
