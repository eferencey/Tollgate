// The only place the history page talks HTTP: the two reading endpoints and the two deleting ones, each reduced to
// data or a HistoryFailure.
import { api } from '@/lib/api/client'
import type { HistoryResponse, StatsResponse } from '@/lib/api/types'
import { historyFailureFromResponse, type HistoryFailure } from '../logic/historyFailure'

export type Outcome<T> = { ok: true; data: T } | { ok: false; failure: HistoryFailure }

async function settle<T>(request: Promise<{ data?: T; response: Response }>): Promise<Outcome<T>> {
  try {
    const { data, response } = await request
    if (data) return { ok: true, data }
    return { ok: false, failure: historyFailureFromResponse(response.status, response.headers.get('Retry-After')) }
  } catch {
    return { ok: false, failure: { kind: 'offline' } } // fetch throws only when the server cannot be reached at all
  }
}

/**
 * For the deletions, which answer 204 without a body. A 404 also counts as done: the scan is already gone
 * (deleted in another tab, or dropped by the retention period), which is what the user asked for.
 */
async function settleDeletion(request: Promise<{ response: Response }>): Promise<Outcome<null>> {
  try {
    const { response } = await request
    if (response.ok || response.status === 404) return { ok: true, data: null }
    return { ok: false, failure: historyFailureFromResponse(response.status, response.headers.get('Retry-After')) }
  } catch {
    return { ok: false, failure: { kind: 'offline' } }
  }
}

export const fetchStats = (): Promise<Outcome<StatsResponse>> => settle(api.GET('/api/stats'))

export const fetchHistory = (limit: number): Promise<Outcome<HistoryResponse>> =>
  settle(api.GET('/api/history', { params: { query: { limit } } }))

export const deleteScan = (id: number): Promise<Outcome<null>> =>
  settleDeletion(api.DELETE('/api/history/{id}', { params: { path: { id } } }))

export const clearHistory = (): Promise<Outcome<null>> => settleDeletion(api.DELETE('/api/history'))
