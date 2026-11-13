// Three-way theme switch (System / Dark / Light) styled as an instrument selector.
import { cn } from '@/lib/utils'
import type { ThemePreference } from './theme'
import { useTheme } from './useTheme'

const OPTIONS: readonly { value: ThemePreference; label: string }[] = [
  { value: 'system', label: 'Auto' },
  { value: 'dark', label: 'Dark' },
  { value: 'light', label: 'Light' },
]

export function ThemeToggle() {
  const { preference, setPreference } = useTheme()

  return (
    <div role="radiogroup" aria-label="Colour theme" className="inline-flex border border-border">
      {OPTIONS.map(({ value, label }) => (
        <button
          key={value}
          type="button"
          role="radio"
          aria-checked={preference === value}
          onClick={() => setPreference(value)}
          className={cn(
            'label-caps h-11 px-3 sm:h-8 sm:px-2.5transition-colors outline-none focus-visible:ring-2 focus-visible:ring-ring',
            preference === value ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:text-foreground',
          )}
        >
          {label}
        </button>
      ))}
    </div>
  )
}
