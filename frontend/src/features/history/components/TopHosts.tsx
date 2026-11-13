// The most scanned hosts as horizontal bars. The bars are neutral on purpose: colour is reserved for verdicts, and a
// host's average score is not a verdict (the thresholds live in backend configuration), so it is printed as a number.
import type { HostStatistics } from '@/lib/api/types'
import { hostBars } from '../logic/figures'
import { Panel } from './Panel'

export function TopHosts({ hosts }: { hosts: HostStatistics[] }) {
  const bars = hostBars(hosts)

  return (
    <Panel id="hosts-title" title="Most scanned hosts" aside={`Top ${bars.length}`}>
      <ol className="flex flex-col gap-3 p-4">
        {bars.map((bar) => (
          <li key={bar.host}>
            <p className="flex items-baseline justify-between gap-3">
              <span className="min-w-0 truncate font-mono text-sm">{bar.host}</span>
              <span className="label-caps shrink-0 text-muted-foreground">
                {bar.scans} {bar.scans === 1 ? 'scan' : 'scans'} · avg {bar.averageScore}
              </span>
            </p>
            <div aria-hidden="true" className="mt-1 h-2 bg-muted">
              <div className="h-full bg-foreground" style={{ width: `${bar.share}%` }} />
            </div>
          </li>
        ))}
      </ol>
    </Panel>
  )
}
