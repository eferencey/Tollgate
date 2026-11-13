// Tests for the history dashboard's loading / ready / failed state machine and its "Show more" paging.
import { describe, expect, it } from 'vitest'
import type { ScanHistoryEntry, StatsResponse } from '@/lib/api/types'
import { dashboardReducer, INITIAL_STATE, listEnd, PAGE_STEP, type DashboardState } from './dashboardState'

const stats: StatsResponse = {
  mode: 'Demo',
  retentionDays: 90,
  totalScans: 2,
  averageScore: 50,
  dangerousRatio: 0.5,
  verdicts: { dangerous: 1, suspicious: 0, safe: 1 },
  topHosts: [],
}
const entry = (host: string): ScanHistoryEntry => ({
  id: host.length,
  url: `https://${host}/`,
  host,
  score: 100,
  verdict: 'Safe',
  confidence: 'High',
  scannedAt: '2026-11-11T10:00:00Z',
})

const ready = (): DashboardState => dashboardReducer(INITIAL_STATE, { type: 'loaded', stats, items: [entry('a.com')], at: 1000 })

describe('dashboardReducer', () => {
  it('starts in the loading phase', () => {
    expect(INITIAL_STATE).toEqual({ phase: 'loading' })
  })

  it('becomes ready with the first page requested', () => {
    expect(ready()).toEqual({ phase: 'ready', stats, items: [entry('a.com')], requested: PAGE_STEP, more: 'idle', loadedAt: 1000 })
  })

  it('keeps the failure so the screen can say what happened', () => {
    expect(dashboardReducer(INITIAL_STATE, { type: 'failed', failure: { kind: 'offline' } })).toEqual({
      phase: 'failed',
      failure: { kind: 'offline' },
    })
  })

  it('goes back to loading on a retry', () => {
    expect(dashboardReducer(ready(), { type: 'start' })).toEqual({ phase: 'loading' })
  })

  it('keeps the rows on screen while more are loading and after they arrive', () => {
    const loading = dashboardReducer(ready(), { type: 'more-started' })
    expect(loading).toMatchObject({ more: 'loading', items: [entry('a.com')] })

    const loaded = dashboardReducer(loading, { type: 'more-loaded', items: [entry('a.com'), entry('b.com')], requested: 2 * PAGE_STEP, at: 2000 })
    expect(loaded).toMatchObject({ more: 'idle', requested: 2 * PAGE_STEP, loadedAt: 2000 })
    expect(loaded.phase === 'ready' && loaded.items).toHaveLength(2)
  })

  it('keeps the rows when loading more fails', () => {
    const failed = dashboardReducer(dashboardReducer(ready(), { type: 'more-started' }), { type: 'more-failed' })
    expect(failed).toMatchObject({ more: 'failed', items: [entry('a.com')] })
  })

  it('ignores a late "more" answer once the page was reloaded', () => {
    expect(dashboardReducer(INITIAL_STATE, { type: 'more-loaded', items: [], requested: 40, at: 1 })).toEqual(INITIAL_STATE)
    expect(dashboardReducer(INITIAL_STATE, { type: 'more-failed' })).toEqual(INITIAL_STATE)
    expect(dashboardReducer(INITIAL_STATE, { type: 'more-started' })).toEqual(INITIAL_STATE)
  })

  it('takes fresh numbers and rows after a deletion but keeps how many rows were opened', () => {
    const opened = dashboardReducer(ready(), { type: 'more-loaded', items: [entry('a.com'), entry('bb.com')], requested: 2 * PAGE_STEP, at: 2000 })
    const fewer = { ...stats, totalScans: 1 }

    const refreshed = dashboardReducer(opened, { type: 'refreshed', stats: fewer, items: [entry('bb.com')], at: 3000 })

    expect(refreshed).toEqual({ phase: 'ready', stats: fewer, items: [entry('bb.com')], requested: 2 * PAGE_STEP, more: 'idle', loadedAt: 3000 })
  })

  it('ignores a refresh that arrives after the page was reloaded', () => {
    expect(dashboardReducer(INITIAL_STATE, { type: 'refreshed', stats, items: [], at: 1 })).toEqual(INITIAL_STATE)
  })
})

describe('listEnd', () => {
  it('is complete when every retained scan is shown', () => {
    expect(listEnd(7, 7, 20)).toBe('complete')
  })

  it('offers more while the server filled the whole request and more exist', () => {
    expect(listEnd(20, 57, 20)).toBe('more')
  })

  it('reports a server cap when fewer rows came back than were asked for although more exist', () => {
    expect(listEnd(100, 150, 120)).toBe('capped')
  })
})
