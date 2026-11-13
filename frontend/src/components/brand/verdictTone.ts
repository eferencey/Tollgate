// The one place that maps a verdict to its colour classes. Colour means a verdict and nothing else.
import type { Verdict } from '@/lib/api/types'

export const VERDICT_TEXT: Record<Verdict, string> = {
  Safe: 'text-safe',
  Suspicious: 'text-suspicious',
  Dangerous: 'text-dangerous',
}

export const VERDICT_BORDER: Record<Verdict, string> = {
  Safe: 'border-safe',
  Suspicious: 'border-suspicious',
  Dangerous: 'border-dangerous',
}

// Solid fill for bars and swatches. Always paired with the verdict word, never the only carrier of meaning.
export const VERDICT_BG: Record<Verdict, string> = {
  Safe: 'bg-safe',
  Suspicious: 'bg-suspicious',
  Dangerous: 'bg-dangerous',
}
