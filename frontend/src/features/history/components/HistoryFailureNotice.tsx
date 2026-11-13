// Explains why the history could not be loaded and offers the one sensible next step. Mirrors ScanFailureNotice's look.
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { useCountdown } from '@/features/scan/hooks/useCountdown'
import type { HistoryFailure } from '../logic/historyFailure'

export function HistoryFailureNotice({ failure, onRetry }: { failure: HistoryFailure; onRetry: () => void }) {
  return (
    <div role="alert" className="border border-destructive bg-card px-4 py-4">
      {failure.kind === 'rate-limited' ? <RateLimited seconds={failure.retryAfterSeconds} onRetry={onRetry} /> : <Plain failure={failure} onRetry={onRetry} />}
    </div>
  )
}

function Plain({ failure, onRetry }: { failure: Exclude<HistoryFailure, { kind: 'rate-limited' }>; onRetry: () => void }) {
  const offline = failure.kind === 'offline'
  return (
    <>
      <p className="label-caps text-destructive">{offline ? 'Backend unreachable' : 'Something went wrong'}</p>
      <p className="mt-2 max-w-prose">
        {offline
          ? 'The page could not reach the Tollgate backend, so the history cannot be shown. Start it with “dotnet run --project src/Tollgate.Api” and try again.'
          : 'The backend failed while reading the history. Your scans are not lost. Try again in a moment.'}
      </p>
      <Button className="mt-4" variant="outline" onClick={onRetry}>
        Try again
      </Button>
    </>
  )
}

function RateLimited({ seconds, onRetry }: { seconds: number; onRetry: () => void }) {
  // The wait starts when this notice appears; state keeps the target fixed across the countdown's re-renders.
  const [target] = useState(() => Date.now() + seconds * 1000)
  const left = useCountdown(target) ?? 0

  return (
    <>
      <p className="label-caps text-destructive">Request limit reached</p>
      <p className="mt-2 max-w-prose">
        Tollgate allows 20 requests per minute. {left > 0 ? `You can load the history again in ${left}s.` : 'You can load the history again now.'}
      </p>
      <Button className="mt-4" variant="outline" onClick={onRetry} disabled={left > 0}>
        {left > 0 ? `Wait ${left}s` : 'Try again'}
      </Button>
    </>
  )
}
