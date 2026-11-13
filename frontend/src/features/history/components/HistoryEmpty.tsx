// The empty state: no scan is on record yet. Says why a scan may be missing, then points at the way to make one.
import { Link } from 'react-router'
import { buttonVariants } from '@/components/ui/button'
import { cn } from '@/lib/utils'

export function HistoryEmpty({ retentionDays }: { retentionDays: number }) {
  return (
    <div className="border border-border bg-card px-4 py-6">
      <p className="label-caps text-muted-foreground">Nothing on record</p>
      <p className="mt-2 text-2xl heading-type">No scans yet.</p>
      <p className="mt-3 max-w-prose text-muted-foreground">
        Every finished scan from the last {retentionDays} days appears here. A scan that no source could answer is not recorded, because “no
        data” is not the same as “safe”, and a link that is still being analysed is recorded once it finishes.
      </p>
      <Link to="/" className={cn(buttonVariants({ size: 'lg' }), 'mt-6')}>
        Scan a link
      </Link>
    </div>
  )
}
