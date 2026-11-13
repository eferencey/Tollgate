// React binding for the scan runner: starts a scan whenever the url changes, cancels the old one, exposes `retry`.
import { useEffect, useState } from 'react'
import { realScanDeps } from '../api/scanApi'
import { IDLE, runScan, SCANNING_AT_START, type ScanFlowState } from '../logic/scanRunner'

// Every reported state is stamped with the scan it belongs to, so a stale state (from the previous url or attempt)
// is never shown for the current one. That removes the need to reset state inside the effect.
interface StampedState {
  scanKey: string
  state: ScanFlowState
}

export function useScan(url: string | null): { state: ScanFlowState; retry: () => void } {
  const [attempt, setAttempt] = useState(0)
  const [stamped, setStamped] = useState<StampedState | null>(null)
  const scanKey = `${attempt}|${url}`

  useEffect(() => {
    if (!url) return

    const controller = new AbortController()
    const report = (state: ScanFlowState) => setStamped({ scanKey, state })
    void runScan(url, realScanDeps, report, controller.signal).catch(() => report({ phase: 'failed', failure: { kind: 'server' } }))
    return () => controller.abort()
  }, [url, scanKey])

  const state = !url ? IDLE : stamped?.scanKey === scanKey ? stamped.state : SCANNING_AT_START
  return { state, retry: () => setAttempt((count) => count + 1) }
}
