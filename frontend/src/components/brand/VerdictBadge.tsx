// Outlined badge for a verdict. Colour comes from the verdict tokens; the text label keeps it readable without colour.
import type { Verdict } from '@/lib/api/types'
import { cn } from '@/lib/utils'
import { VERDICT_BORDER, VERDICT_TEXT } from './verdictTone'

export function VerdictBadge({ verdict, className }: { verdict: Verdict; className?: string }) {
  return (
    <span className={cn('label-caps inline-flex h-6 items-center border px-2', VERDICT_BORDER[verdict], VERDICT_TEXT[verdict], className)}>
      {verdict}
    </span>
  )
}
