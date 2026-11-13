// React binding for the history page: loads the statistics and the first page of scans together, grows the list on
// request, and deletes scans. All rules about what the screen shows live in dashboardState.ts; this file only talks to
// the API and dispatches.
import { useEffect, useReducer, useState } from 'react'
import { dashboardReducer, INITIAL_STATE, PAGE_STEP, type DashboardState } from '../logic/dashboardState'
import { clearHistory, deleteScan, fetchHistory, fetchStats, type Outcome } from '../api/historyApi'

interface Dashboard {
  state: DashboardState
  showMore: () => void
  retry: () => void
  /** Resolves to false when the deletion failed (the screen keeps the row and says so). */
  remove: (id: number) => Promise<boolean>
  clearAll: () => Promise<boolean>
}

export function useDashboard(): Dashboard {
  const [state, dispatch] = useReducer(dashboardReducer, INITIAL_STATE)
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    // Not aborted on cleanup: React's dev mode mounts effects twice and a cancelled request makes the server log noise
    // (see useBackendStatus). A late answer for a replaced attempt is simply ignored.
    let ignored = false

    void Promise.all([fetchStats(), fetchHistory(PAGE_STEP)]).then(([stats, history]) => {
      if (ignored) return
      if (!stats.ok) return dispatch({ type: 'failed', failure: stats.failure })
      if (!history.ok) return dispatch({ type: 'failed', failure: history.failure })
      dispatch({ type: 'loaded', stats: stats.data, items: history.data.items, at: Date.now() })
    })

    return () => {
      ignored = true
    }
  }, [attempt])

  const showMore = () => {
    if (state.phase !== 'ready' || state.more === 'loading') return
    const requested = state.requested + PAGE_STEP
    dispatch({ type: 'more-started' })
    void fetchHistory(requested).then((history) =>
      dispatch(history.ok ? { type: 'more-loaded', items: history.data.items, requested, at: Date.now() } : { type: 'more-failed' }),
    )
  }

  const retry = () => {
    dispatch({ type: 'start' })
    setAttempt((count) => count + 1)
  }

  // After a deletion the numbers and the rows are read again, as many rows as were open. The deletion itself already
  // succeeded, so if this reload fails the whole page reloads instead of showing rows that no longer exist.
  const deleteThenRefresh = async (deletion: Promise<Outcome<null>>): Promise<boolean> => {
    if (!(await deletion).ok) return false
    const requested = state.phase === 'ready' ? state.requested : PAGE_STEP
    const [stats, history] = await Promise.all([fetchStats(), fetchHistory(requested)])
    if (stats.ok && history.ok) dispatch({ type: 'refreshed', stats: stats.data, items: history.data.items, at: Date.now() })
    else retry()
    return true
  }

  return {
    state,
    showMore,
    retry,
    remove: (id) => deleteThenRefresh(deleteScan(id)),
    clearAll: () => deleteThenRefresh(clearHistory()),
  }
}
