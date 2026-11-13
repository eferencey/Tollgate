// The scan state machine: ask once, and while the backend says "partial" wait retryAfterSeconds and ask again.
// Pure of React and of fetch (both are injected), so the polling rules are unit-tested without a network.
import type { ScanResponse, SourceStatus } from '@/lib/api/types'
import type { ScanFailure } from './scanFailure'

export type ScanFlowState =
  | { phase: 'idle' }
  | { phase: 'scanning'; sources: SourceStatus[]; nextCheckAt?: number }
  | { phase: 'done'; result: ScanResponse }
  | { phase: 'failed'; failure: ScanFailure }

export type FetchOutcome = { ok: true; result: ScanResponse } | { ok: false; failure: ScanFailure }

export interface ScanRunnerDeps {
  /**
   * Not cancellable on purpose: React's dev mode starts every effect twice, and a request cancelled in flight makes the
   * server's database call throw, which stops a debugging session. A late answer is simply ignored (see runScan).
   */
  fetchScan(url: string): Promise<FetchOutcome>
  /** Resolves after the delay; rejects with an AbortError if the signal fires first. */
  sleep(milliseconds: number, signal: AbortSignal): Promise<void>
  now(): number
}

// Gives up waiting after about 3 minutes (9 x 20 s): a source that is still analysing by then is shown as such.
export const MAX_RECHECKS = 9

export const IDLE: ScanFlowState = { phase: 'idle' }
export const SCANNING_AT_START: ScanFlowState = { phase: 'scanning', sources: [] }

export async function runScan(
  url: string,
  deps: ScanRunnerDeps,
  report: (state: ScanFlowState) => void,
  signal: AbortSignal,
): Promise<void> {
  try {
    for (let recheck = 0; ; recheck++) {
      const outcome = await deps.fetchScan(url)
      if (signal.aborted) return

      if (!outcome.ok) return report({ phase: 'failed', failure: outcome.failure })

      const { result } = outcome
      const waitSeconds = result.retryAfterSeconds
      const mustWait = result.status === 'Partial' && waitSeconds != null && recheck < MAX_RECHECKS
      if (!mustWait) return report({ phase: 'done', result })

      report({ phase: 'scanning', sources: result.sources, nextCheckAt: deps.now() + waitSeconds * 1000 })
      await deps.sleep(waitSeconds * 1000, signal)
    }
  } catch (error) {
    if (signal.aborted) return // the user started another scan or left the page; nobody is waiting for this one
    throw error
  }
}
