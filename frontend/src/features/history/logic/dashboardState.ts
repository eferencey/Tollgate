// What the history page is showing at any moment, as a pure state machine (a reducer): loading -> ready | failed,
// and inside "ready" the list can grow page by page. No React and no network here, so the rules are unit-tested.
import type { ScanHistoryEntry, StatsResponse } from '@/lib/api/types'
import type { HistoryFailure } from './historyFailure'

/** Rows in the first request and the rows each "Show more" adds. A presentation choice; the server only caps it. */
export const PAGE_STEP = 20

export type MoreState = 'idle' | 'loading' | 'failed'

export type DashboardState =
  | { phase: 'loading' }
  | { phase: 'failed'; failure: HistoryFailure }
  | {
      phase: 'ready'
      stats: StatsResponse
      items: ScanHistoryEntry[]
      requested: number
      more: MoreState
      /** When the data arrived. Relative times ("5 min ago") are measured from here, so rendering stays pure. */
      loadedAt: number
    }

type DashboardAction =
  | { type: 'start' }
  | { type: 'loaded'; stats: StatsResponse; items: ScanHistoryEntry[]; at: number }
  | { type: 'failed'; failure: HistoryFailure }
  | { type: 'more-started' }
  | { type: 'more-loaded'; items: ScanHistoryEntry[]; requested: number; at: number }
  | { type: 'more-failed' }
  /** Fresh numbers and rows after a deletion; keeps how many rows the user had opened. */
  | { type: 'refreshed'; stats: StatsResponse; items: ScanHistoryEntry[]; at: number }

export const INITIAL_STATE: DashboardState = { phase: 'loading' }

export function dashboardReducer(state: DashboardState, action: DashboardAction): DashboardState {
  switch (action.type) {
    case 'start':
      return INITIAL_STATE
    case 'loaded':
      return { phase: 'ready', stats: action.stats, items: action.items, requested: PAGE_STEP, more: 'idle', loadedAt: action.at }
    case 'failed':
      return { phase: 'failed', failure: action.failure }
    // The "more" actions only mean something while a list is on screen; a late answer after a reload is ignored.
    case 'more-started':
      return state.phase === 'ready' ? { ...state, more: 'loading' } : state
    case 'more-loaded':
      return state.phase === 'ready' ? { ...state, items: action.items, requested: action.requested, more: 'idle', loadedAt: action.at } : state
    case 'more-failed':
      return state.phase === 'ready' ? { ...state, more: 'failed' } : state
    case 'refreshed':
      return state.phase === 'ready' ? { ...state, stats: action.stats, items: action.items, more: 'idle', loadedAt: action.at } : state
  }
}

type ListEnd =
  /** Every retained scan is on screen. */
  | 'complete'
  /** The server returned fewer rows than asked for although more exist: it caps how many it sends at once. */
  | 'capped'
  | 'more'

/** Decides what sits under the list: nothing more to load, a "the server stops here" note, or a "Show more" button. */
export function listEnd(shown: number, total: number, requested: number): ListEnd {
  if (shown >= total) return 'complete'
  return shown < requested ? 'capped' : 'more'
}
