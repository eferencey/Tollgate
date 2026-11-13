// The instrument panel that opens the result: ring gauge, verdict, confidence and one chip per finding.
// With motion allowed it is revealed by scrolling (useScoreReveal); otherwise it is just this panel, finished.
import { useRef } from 'react'
import { BarrierMark } from '@/components/brand/BarrierMark'
import { VerdictBadge } from '@/components/brand/VerdictBadge'
import { VERDICT_TEXT } from '@/components/brand/verdictTone'
import { Button } from '@/components/ui/button'
import { SOURCE_INFO } from '@/features/scan/logic/sources'
import type { ScanResponse } from '@/lib/api/types'
import { cn } from '@/lib/utils'
import { formatPoints, hostOf, type Reading } from '../logic/reading'
import { ScoreRing } from './ScoreRing'
import { ShareLink } from './ShareLink'
import { useScoreReveal } from '../hooks/useScoreReveal'

interface ResultStageProps {
  result: ScanResponse
  reading: Reading
  onScanAnother: () => void
}

export function ResultStage({ result, reading, onScanAnother }: ResultStageProps) {
  const root = useRef<HTMLElement>(null)
  const { animated, revealed, skip } = useScoreReveal(root, { enabled: !reading.noData, score: result.score })

  const verdictShown = !reading.noData && revealed
  // The barrier arm in the verdict row opens only for a revealed "Safe"; a lowered arm means "stop".
  const raised = verdictShown && result.verdict === 'Safe'

  const tone = verdictShown ? VERDICT_TEXT[result.verdict] : 'text-foreground'

  return (
    <section ref={root} aria-label="Score reading" className={cn('relative flex flex-col', animated && 'overflow-hidden')}>
      {animated && (
        <div className="flex h-10 shrink-0 items-center justify-between">
          <p className="label-caps">Scroll to read the meter</p>
          <Button variant="ghost" size="sm" onClick={skip}>
            Skip · Esc
          </Button>
        </div>
      )}

      <div className="relative flex-1">
        <div
          data-reveal="intro"
          className={cn('absolute inset-0 flex-col items-center justify-center gap-4 px-4 text-center', animated ? 'flex' : 'hidden')}
        >
          <p className="label-caps">Lane 01 · Reading taken</p>
          <h1 className="max-w-3xl text-5xl sm:text-7xl">
            Result for <span className="block font-mono text-[0.7em] font-semibold [overflow-wrap:anywhere]">{hostOf(result.url)}</span>
          </h1>
        </div>

        <div data-reveal="panel" className={cn('mx-auto flex w-full max-w-5xl flex-col border border-border bg-card', animated && 'h-full')}>
          <header className="flex flex-wrap items-center justify-between gap-x-4 gap-y-1 border-b border-border px-4 py-3">
            <p className="label-caps text-muted-foreground">
              Reading
              {result.mode === 'Demo' && <span className="ml-3 border border-border px-1.5 py-0.5">Demo · simulated data</span>}
            </p>
            {!animated && <h1 className="sr-only">Result for {hostOf(result.url)}</h1>}
            <p className="max-w-full truncate font-mono text-sm" title={result.url}>
              {result.url}
            </p>
          </header>

          <div className="flex flex-1 flex-col items-center justify-center gap-6 px-4 py-8">
            <ScoreRing score={reading.noData ? null : result.score} toneClass={tone} />

            {reading.noData ? <NoDataVerdict /> : <VerdictRow result={result} reading={reading} raised={raised} />}

            {!reading.noData && <FindingChips result={result} />}
          </div>

          <footer data-reveal="closing" className="flex flex-wrap items-center justify-end gap-3 border-t border-border px-4 py-3">
            <a href="#why" className="label-caps touch-link mr-auto">
              Why this score? ↓
            </a>
            <ShareLink scannedUrl={result.url} />
            <Button onClick={onScanAnother}>Scan another link</Button>
          </footer>
        </div>
      </div>
    </section>
  )
}

function VerdictRow({ result, reading, raised }: { result: ScanResponse; reading: Reading; raised: boolean }) {
  return (
    <div data-reveal="verdict" className="flex flex-wrap items-center justify-center gap-x-4 gap-y-2">
      <BarrierMark raised={raised} className={cn('size-10', VERDICT_TEXT[result.verdict])} />
      <VerdictBadge verdict={result.verdict} className="h-8 px-3 text-sm" />
      <p className="label-caps text-muted-foreground">
        Confidence {result.confidence} · {reading.respondedSources} of {reading.totalSources} sources answered
      </p>
    </div>
  )
}

function NoDataVerdict() {
  return (
    <div className="flex max-w-prose flex-col items-center gap-2 text-center">
      <span className="label-caps inline-flex h-8 items-center border border-border px-3 text-sm">No data</span>
      <p className="text-muted-foreground">
        None of the four sources answered, so there is nothing to score. This is <strong className="text-foreground">not</strong> a safe result: it
        means we could not check the link.
      </p>
    </div>
  )
}

function FindingChips({ result }: { result: ScanResponse }) {
  return (
    <ul className="flex max-w-3xl flex-wrap justify-center gap-2" aria-label="Deductions">
      {result.findings.length === 0 && (
        <li data-reveal="chip" className="label-caps border border-border px-2 py-1 text-muted-foreground">
          No deductions
        </li>
      )}
      {result.findings.map((finding) => (
        <li key={`${finding.source}-${finding.description}`} data-reveal="chip" className="label-caps flex gap-2 border border-border px-2 py-1">
          <span className="text-muted-foreground">{SOURCE_INFO[finding.source].label}</span>
          <span>{formatPoints(finding.pointsDeducted)}</span>
        </li>
      ))}
      {result.cappedBy && (
        <li data-reveal="chip" className="label-caps flex gap-2 border border-foreground px-2 py-1">
          <span className="text-muted-foreground">Capped at</span>
          <span>{result.cappedBy.scoreCap}</span>
        </li>
      )}
    </ul>
  )
}
