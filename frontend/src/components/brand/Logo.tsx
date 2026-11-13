// The wordmark: just the name, set in exactly the same type as the page headings (the `heading-type` utility).
import { cn } from '@/lib/utils'

export function Logo({ className }: { className?: string }) {
  return <span className={cn('heading-type text-3xl', className)}>Tollgate</span>
}
