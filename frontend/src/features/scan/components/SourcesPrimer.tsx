// The empty state of the home page: before any scan, say plainly which four sources will be asked and what each contributes,
// plus the one check we do ourselves without asking anyone.
import { SOURCE_INFO, SOURCE_ORDER } from '../logic/sources'

export function SourcesPrimer() {
  return (
    <section aria-label="The four sources and our own check" className="w-full">
      <p className="label-caps">Four sources, asked at once, and one check of our own</p>
      <dl className="mt-3 grid gap-px border border-border bg-border sm:grid-cols-2">
        {[...SOURCE_ORDER, 'LocalAnalysis' as const].map((source) => (
          <div key={source} className={source === 'LocalAnalysis' ? 'bg-card p-4 sm:col-span-2' : 'bg-card p-4'}>
            <dt className="label-caps">{SOURCE_INFO[source].label}</dt>
            <dd className="mt-1 text-sm text-muted-foreground">{SOURCE_INFO[source].checks}</dd>
          </div>
        ))}
      </dl>
    </section>
  )
}
