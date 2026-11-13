// Tests for the scan polling state machine: when it asks again, when it stops and how failures end it.
import { describe, expect, it } from 'vitest'
import type { ScanResponse } from '@/lib/api/types'
import { MAX_RECHECKS, runScan, type FetchOutcome, type ScanFlowState, type ScanRunnerDeps } from './scanRunner'

function response(overrides: Partial<ScanResponse> = {}): ScanResponse {
  return {
    status: 'Complete',
    mode: 'Demo',
    url: 'https://example.com/',
    score: 100,
    verdict: 'Safe',
    confidence: 'High',
    findings: [],
    sources: [{ source: 'VirusTotal', state: 'Completed' }],
    fromCache: false,
    scannedAt: '2026-11-11T12:00:00Z',
    ...overrides,
  }
}

const partial = (retryAfterSeconds = 20) => response({ status: 'Partial', retryAfterSeconds, sources: [{ source: 'VirusTotal', state: 'Pending' }] })

/** Plays back scripted answers; records every wait instead of really waiting. */
function harness(script: FetchOutcome[]) {
  const states: ScanFlowState[] = []
  const waits: number[] = []
  let calls = 0
  const deps: ScanRunnerDeps = {
    fetchScan: async () => script[Math.min(calls++, script.length - 1)],
    sleep: async (milliseconds) => void waits.push(milliseconds),
    now: () => 1_000,
  }
  const run = (signal = new AbortController().signal) => runScan('example.com', deps, (state) => states.push(state), signal)
  return { run, states, waits, calls: () => calls }
}

describe('runScan', () => {
  it('reports done after a single complete answer', async () => {
    const complete = response()
    const { run, states, waits } = harness([{ ok: true, result: complete }])

    await run()

    expect(states).toEqual([{ phase: 'done', result: complete }])
    expect(waits).toEqual([])
  })

  it('waits retryAfterSeconds and asks again while the answer is partial', async () => {
    const complete = response()
    const { run, states, waits, calls } = harness([{ ok: true, result: partial(20) }, { ok: true, result: complete }])

    await run()

    expect(waits).toEqual([20_000])
    expect(calls()).toBe(2)
    expect(states).toEqual([
      { phase: 'scanning', sources: [{ source: 'VirusTotal', state: 'Pending' }], nextCheckAt: 21_000 },
      { phase: 'done', result: complete },
    ])
  })

  it('stops waiting after the maximum number of re-checks and shows the partial result', async () => {
    const stuck = partial()
    const { run, states, waits, calls } = harness([{ ok: true, result: stuck }])

    await run()

    expect(waits).toHaveLength(MAX_RECHECKS)
    expect(calls()).toBe(MAX_RECHECKS + 1)
    expect(states.at(-1)).toEqual({ phase: 'done', result: stuck })
  })

  it('treats a partial answer without retryAfterSeconds as final', async () => {
    const noHint = response({ status: 'Partial' })
    const { run, states, waits } = harness([{ ok: true, result: noHint }])

    await run()

    expect(waits).toEqual([])
    expect(states).toEqual([{ phase: 'done', result: noHint }])
  })

  it('reports a failure and does not retry on its own', async () => {
    const { run, states, calls } = harness([{ ok: false, failure: { kind: 'offline' } }])

    await run()

    expect(states).toEqual([{ phase: 'failed', failure: { kind: 'offline' } }])
    expect(calls()).toBe(1)
  })

  it('goes quiet once cancelled, so a superseded scan cannot overwrite the new one', async () => {
    const controller = new AbortController()
    const { run, states } = harness([{ ok: true, result: response() }])
    controller.abort()

    await run(controller.signal)

    expect(states).toEqual([])
  })

  it('ignores an answer that arrives after the scan was cancelled (the request itself is not cancelled)', async () => {
    const controller = new AbortController()
    const states: ScanFlowState[] = []
    const deps: ScanRunnerDeps = {
      fetchScan: async () => {
        controller.abort() // e.g. React dev mode cleaned the effect up while the request was on its way
        return { ok: true, result: response() }
      },
      sleep: async () => {},
      now: () => 1_000,
    }

    await runScan('example.com', deps, (state) => states.push(state), controller.signal)

    expect(states).toEqual([])
  })
})
