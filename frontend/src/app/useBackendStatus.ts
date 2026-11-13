// Asks GET /api/stats once and reduces the answer to what the footer shows: loading, offline, or online with mode and scan count.
import { useEffect, useState } from 'react'
import { api } from '@/lib/api/client'
import type { ScanMode } from '@/lib/api/types'

type BackendStatus =
  | { state: 'loading' }
  | { state: 'offline' }
  | { state: 'online'; mode: ScanMode; totalScans: number }

export function useBackendStatus(): BackendStatus {
  const [status, setStatus] = useState<BackendStatus>({ state: 'loading' })

  useEffect(() => {
    // The request is tiny, so it is not aborted on cleanup: React's dev mode mounts effects twice, and an abort here
    // made the server see a cancelled request on every page load (a debugger stops on that). We just ignore a late answer.
    let ignored = false

    api
      .GET('/api/stats')
      .then(({ data }) => {
        if (ignored) return
        setStatus(data ? { state: 'online', mode: data.mode, totalScans: data.totalScans } : { state: 'offline' })
      })
      .catch(() => {
        if (!ignored) setStatus({ state: 'offline' }) // network error: backend not running
      })

    return () => {
      ignored = true
    }
  }, [])

  return status
}
