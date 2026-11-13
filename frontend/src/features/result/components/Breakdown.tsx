// "Why this score?": the arithmetic behind the number, one row per deduction with the source that caused it.
import { SOURCE_INFO } from '@/features/scan/logic/sources'
import type { ScanResponse } from '@/lib/api/types'
import { explainCap, explainFinding, summarize } from '../logic/explain'
import { formatPoints, type Reading } from '../logic/reading'

export function Breakdown({ result, reading }: { result: ScanResponse; reading: Reading }) {
  return (
    <section id="why" aria-labelledby="why-title" className="scroll-mt-6">
      <h2 id="why-title" className="text-3xl sm:text-4xl">
        Why this score?
      </h2>
      <p className="mt-3 max-w-prose text-lg">{summarize(result, reading)}</p>
      <p className="mt-2 max-w-prose text-muted-foreground">
        Every score starts at 100. Each finding takes points off and names the source that reported it.
      </p>

      <table className="mt-6 w-full border-collapse border-y border-border text-left">
        <caption className="sr-only">Deductions from a starting score of 100</caption>
        <thead>
          {/* On a phone each row is stacked (source and points on top, text below), so the header row is read aloud only. */}
          <tr className="label-caps text-muted-foreground max-sm:sr-only">
            <th scope="col" className="w-28 py-2 pr-3 font-medium sm:w-40">
              Source
            </th>
            <th scope="col" className="py-2 pr-3 font-medium">
              Finding
            </th>
            <th scope="col" className="w-16 py-2 text-right font-medium">
              Points
            </th>
          </tr>
        </thead>
        <tbody>
          {result.findings.length === 0 && (
            <tr className="border-t border-border">
              <td colSpan={3} className="py-3 text-muted-foreground">
                {reading.noData
                  ? 'No source answered, so nothing could be checked.'
                  : 'No deductions. None of the sources that answered reported a problem.'}
              </td>
            </tr>
          )}
          {result.findings.map((finding) => (
            // Below sm the row becomes a two-column grid; the explicit roles keep the table semantics that display:grid drops.
            <tr
              key={`${finding.source}-${finding.description}`}
              role="row"
              className="border-t border-border align-top max-sm:grid max-sm:grid-cols-[1fr_auto] max-sm:gap-x-3"
            >
              <th scope="row" role="rowheader" className="label-caps py-3 pr-3 font-medium max-sm:col-start-1 max-sm:row-start-1 max-sm:pb-0">
                {SOURCE_INFO[finding.source].label}
              </th>
              <td role="cell" className="py-3 pr-3 max-sm:col-span-2 max-sm:row-start-2 max-sm:pt-1">
                {finding.description}
                {finding.scoreCap != null && (
                  <span className="label-caps ml-2 text-muted-foreground">Limit {finding.scoreCap}</span>
                )}
                <p className="mt-1 max-w-prose text-sm text-muted-foreground">{explainFinding(finding)}</p>
              </td>
              <td role="cell" className="py-3 text-right font-mono max-sm:col-start-2 max-sm:row-start-1 max-sm:pb-0">
                {formatPoints(finding.pointsDeducted)}
              </td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <Total label="Starting score" value="100" />
          <Total label="Total deducted" value={reading.totalDeducted === 0 ? '0' : formatPoints(reading.totalDeducted)} />
          {result.cappedBy && (
            <Total
              label={`Capped by ${SOURCE_INFO[result.cappedBy.source].label}`}
              note={result.cappedBy.description}
              value={`≤ ${result.cappedBy.scoreCap}`}
            />
          )}
          {/* With no data the backend's 100 is only the starting point, not a result, so it is not repeated as the score. */}
          <Total label="Final score" value={reading.noData ? '— (no data)' : String(result.score)} strong />
        </tfoot>
      </table>

      {result.cappedBy && (
        <p className="mt-3 max-w-prose text-sm text-muted-foreground">{explainCap(result.cappedBy)}</p>
      )}
    </section>
  )
}

function Total({ label, note, value, strong }: { label: string; note?: string; value: string; strong?: boolean }) {
  return (
    <tr role="row" className="border-t border-border max-sm:grid max-sm:grid-cols-[1fr_auto] max-sm:gap-x-3">
      <th scope="row" role="rowheader" colSpan={2} className="py-2 pr-3 text-left font-medium">
        <span className={strong ? 'label-caps text-foreground' : 'label-caps text-muted-foreground'}>{label}</span>
        {note && <span className="ml-3 text-sm font-normal text-muted-foreground">{note}</span>}
      </th>
      <td role="cell" className={strong ? 'py-2 text-right font-mono text-lg font-semibold whitespace-nowrap' : 'py-2 text-right font-mono whitespace-nowrap'}>
        {value}
      </td>
    </tr>
  )
}
