// The whole result screen: the revealed panel first, then the explanation below it.
import type { ScanResponse } from '@/lib/api/types'
import { Breakdown } from './Breakdown'
import { readResult } from '../logic/reading'
import { ResultNotices } from './ResultNotices'
import { ResultStage } from './ResultStage'
import { SourceCards } from './SourceCards'

export function ResultView({ result, onScanAnother }: { result: ScanResponse; onScanAnother: () => void }) {
  const reading = readResult(result)

  return (
    <div>
      <ResultStage result={result} reading={reading} onScanAnother={onScanAnother} />
      {/* The explanation is a solid sheet under the panel: its text is small and must not sit directly on the moving silk. */}
      <div className="mt-8 flex flex-col gap-16 border border-border bg-card p-5 sm:mt-16 sm:p-10">
        <ResultNotices result={result} reading={reading} />
        <Breakdown result={result} reading={reading} />
        <SourceCards result={result} />
        <p className="label-caps text-muted-foreground">
          Scanned {new Date(result.scannedAt).toLocaleString()} · {result.mode} mode
        </p>
      </div>
    </div>
  )
}
