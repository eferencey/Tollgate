// Footer readout of the backend connection, shown like a lamp plus a mono caption.
import { Link } from 'react-router'
import { cn } from '@/lib/utils'
import { useBackendStatus } from './useBackendStatus'

export function StatusReadout() {
  const status = useBackendStatus()

  const caption =
    status.state === 'loading' ? (
      'Contacting API'
    ) : status.state === 'offline' ? (
      'API offline'
    ) : (
      <>
        API online · {status.mode} mode ·{' '}
        <Link to="/history" className="touch-link">
          {status.totalScans} {status.totalScans === 1 ? 'scan' : 'scans'} on record
        </Link>
      </>
    )

  return (
    <p className="label-caps flex items-center gap-2" role="status">
      <span
        aria-hidden="true"
        className={cn(
          'size-2 rounded-full',
          status.state === 'online' && 'bg-safe',
          status.state === 'offline' && 'bg-dangerous',
          status.state === 'loading' && 'bg-muted-foreground',
        )}
      />
      {caption}
    </p>
  )
}
