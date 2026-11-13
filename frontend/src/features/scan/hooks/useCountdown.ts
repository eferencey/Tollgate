// Whole seconds left until a moment in time, re-rendering once a second; null when there is no target.
import { useEffect, useState } from 'react'

export function useCountdown(targetMs: number | undefined): number | null {
  const [now, setNow] = useState(Date.now)

  useEffect(() => {
    if (targetMs === undefined) return
    const tick = () => setNow(Date.now())
    const first = setTimeout(tick, 0) // catch up at once when the target changes, instead of showing a stale second
    const timer = setInterval(tick, 1000)
    return () => {
      clearTimeout(first)
      clearInterval(timer)
    }
  }, [targetMs])

  return targetMs === undefined ? null : Math.max(0, Math.ceil((targetMs - now) / 1000))
}
