// Explains a failed scan in plain words and offers the one sensible next step for each kind of failure.
// An invalid link is not shown here: its message sits under the input field, where the user fixes it.
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import type { ScanFailure } from '../logic/scanFailure'
import { useCountdown } from '../hooks/useCountdown'

type ReportedFailure = Exclude<ScanFailure, { kind: 'invalid-url' }>

export function ScanFailureNotice({ failure, onRetry }: { failure: ReportedFailure; onRetry: () => void }) {
  return (
    <div role="alert" className="w-full border border-destructive bg-card px-4 py-4">
      {failure.kind === 'rate-limited' ? <RateLimited seconds={failure.retryAfterSeconds} onRetry={onRetry} /> : <Plain failure={failure} onRetry={onRetry} />}
    </div>
  )
}

function Plain({ failure, onRetry }: { failure: Exclude<ReportedFailure, { kind: 'rate-limited' }>; onRetry: () => void }) {
  const offline = failure.kind === 'offline'
  return (
    <>
      <p className="label-caps text-destructive">{offline ? 'Backend unreachable' : 'Something went wrong'}</p>
      <p className="mt-2 max-w-prose">
        {offline
          ? 'The page could not reach the Tollgate backend. Start it with “dotnet run --project src/Tollgate.Api” and try again.'
          : 'The backend failed while checking this link. Nothing was scored. Try again in a moment.'}
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
        Tollgate allows 20 checks per minute to stay within the free quotas of its sources. {left > 0 ? `You can check again in ${left}s.` : 'You can check again now.'}
      </p>
      <Button className="mt-4" variant="outline" onClick={onRetry} disabled={left > 0}>
        {left > 0 ? `Wait ${left}s` : 'Try again'}
      </Button>
    </>
  )
}
