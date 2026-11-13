// The URL entry form. It only collects text; the backend is the single validator, so its explanation is shown as-is.
import { useState, type FormEvent } from 'react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

export interface ScanFormProps {
  initialUrl: string
  busy: boolean
  /** Message from the backend when the last url was rejected. */
  error?: string
  onSubmit: (url: string) => void
}

export function ScanForm({ initialUrl, busy, error, onSubmit }: ScanFormProps) {
  const [value, setValue] = useState(initialUrl)

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const trimmed = value.trim()
    if (trimmed) onSubmit(trimmed)
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="w-full border border-border bg-card">
      <label htmlFor="scan-url" className="label-caps block border-b border-border px-4 py-2 text-muted-foreground">
        Link to check
      </label>
      <div className="flex flex-col gap-3 p-4 sm:flex-row">
        <input
          id="scan-url"
          name="url"
          type="text"
          inputMode="url"
          autoComplete="off"
          autoCapitalize="off"
          spellCheck={false}
          placeholder="example.com/some/page"
          value={value}
          onChange={(event) => setValue(event.target.value)}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? 'scan-url-error' : undefined}
          className={cn(
            'h-12 w-full min-w-0 border sm:w-auto sm:flex-1 border-input bg-background px-3 font-mono text-base text-foreground outline-none',
            'placeholder:text-muted-foreground focus-visible:ring-3 focus-visible:ring-ring/50',
            error && 'border-destructive',
          )}
        />
        <Button type="submit" size="lg" disabled={busy || !value.trim()}>
          {busy ? 'Checking…' : 'Check link'}
        </Button>
      </div>
      {error && (
        <p id="scan-url-error" role="alert" className="border-t border-border px-4 py-3 text-sm text-destructive">
          {error}
        </p>
      )}
    </form>
  )
}
