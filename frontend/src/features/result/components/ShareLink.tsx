// "Copy link" button for the result panel. Renders two siblings for the panel footer's wrapping flex row:
// the button, and a full-width feedback line below it (confirmation, or a field to copy by hand if the browser refuses).
import { useEffect, useId, useRef } from 'react'
import { Button } from '@/components/ui/button'
import { buildShareLink } from '../logic/sharing'
import { useCopyLink } from '../hooks/useCopyLink'

export function ShareLink({ scannedUrl }: { scannedUrl: string }) {
  const link = buildShareLink(window.location.origin, scannedUrl, import.meta.env.BASE_URL)
  const { status, copy } = useCopyLink(link)
  const field = useRef<HTMLInputElement>(null)
  const fieldId = useId()

  // When the browser refuses, move focus to the link, already selected: one Ctrl+C finishes the job.
  useEffect(() => {
    if (status === 'denied') {
      field.current?.focus()
      field.current?.select()
    }
  }, [status])

  return (
    <>
      <Button variant="outline" onClick={copy}>
        {status === 'copied' ? 'Link copied' : 'Copy link'}
      </Button>

      {/* The region exists from the start so screen readers announce the text that appears later. */}
      <div role="status" className="order-last basis-full text-sm text-muted-foreground empty:hidden">
        {status === 'copied' && <p>Link copied. Opening it scans the link again.</p>}
        {status === 'denied' && (
          <div className="flex flex-col gap-1">
            <label htmlFor={fieldId} className="label-caps">
              Your browser blocked copying. Copy the link by hand (Ctrl+C):
            </label>
            <input
              ref={field}
              id={fieldId}
              readOnly
              value={link}
              onFocus={(event) => event.currentTarget.select()}
              className="h-10 w-full border border-border bg-background px-3 font-mono text-sm text-foreground outline-none focus-visible:ring-2 focus-visible:ring-ring"
            />
          </div>
        )}
      </div>
    </>
  )
}
