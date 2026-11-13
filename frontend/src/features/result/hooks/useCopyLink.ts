// Copy-to-clipboard state for the share button: idle -> copied (fades back to idle) or denied (stays, so the fallback field stays).
import { useCallback, useEffect, useRef, useState } from 'react'
import { copyText } from '../logic/sharing'

const COPIED_VISIBLE_MS = 3000

type CopyStatus = 'idle' | 'copied' | 'denied'

export function useCopyLink(link: string) {
  const [status, setStatus] = useState<CopyStatus>('idle')
  const timer = useRef<number | undefined>(undefined)

  useEffect(() => () => window.clearTimeout(timer.current), [])

  const copy = useCallback(async () => {
    window.clearTimeout(timer.current)
    const outcome = await copyText(link, navigator.clipboard)
    setStatus(outcome)
    if (outcome === 'copied') timer.current = window.setTimeout(() => setStatus('idle'), COPIED_VISIBLE_MS)
  }, [link])

  return { status, copy }
}
