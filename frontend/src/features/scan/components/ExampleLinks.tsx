// Three example links under the form, one per verdict, so a first-time visitor can see a reading without their own link.
// Each row is a link to "?url=…": it runs the scan in Tollgate and never opens the site itself.
import { Link } from 'react-router'
import { VerdictBadge } from '@/components/brand/VerdictBadge'
import { rescanPath } from '@/features/history/logic/links'
import { EXAMPLE_LINKS } from '../logic/examples'

/** The link as shown: no scheme and no trailing slash, which carry no information here. */
const shown = (url: string) => url.replace(/^https?:\/\//, '').replace(/\/$/, '')

export function ExampleLinks() {
  return (
    <section aria-labelledby="examples-title" className="w-full">
      <h2 id="examples-title" className="label-caps">
        Try an example
      </h2>
      <ul className="mt-3 border border-border bg-card">
        {EXAMPLE_LINKS.map((example) => (
          <li key={example.url} className="border-b border-border last:border-b-0">
            <Link
              to={rescanPath(example.url)}
              className="grid min-h-11 grid-cols-[7rem_1fr] items-baseline gap-x-3 gap-y-1 px-4 py-3 outline-none hover:bg-muted focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset"
            >
              <VerdictBadge verdict={example.expected} className="justify-self-start" />
              <span className="min-w-0 font-mono text-sm break-all">
                <span className="sr-only">Scan </span>
                {shown(example.url)}
              </span>
              <span className="col-start-2 text-sm text-muted-foreground">{example.note}</span>
            </Link>
          </li>
        ))}
      </ul>
      <p className="mt-2 text-sm">Opening an example scans it here; the site itself is never opened. In live mode real sources may know more, so scores can differ.</p>
    </section>
  )
}
