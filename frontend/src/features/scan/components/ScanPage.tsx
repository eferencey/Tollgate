// Home route: form -> progress -> result. The scanned link lives in the address bar (?url=), so back, reload and sharing work.
import { lazy, Suspense } from 'react'
import { useSearchParams } from 'react-router'
import { ScanFailureNotice } from './ScanFailureNotice'
import { EntryBand } from './EntryBand'
import { ExampleLinks } from './ExampleLinks'
import { ScanProgress } from './ScanProgress'
import { SourcesPrimer } from './SourcesPrimer'
import { useScan } from '../hooks/useScan'

// The result screen (with GSAP, the biggest dependency) is downloaded only when a result exists, not on the first visit.
const ResultView = lazy(() => import('@/features/result/components/ResultView').then((module) => ({ default: module.ResultView })))

export function ScanPage() {
  const [params, setParams] = useSearchParams()
  const url = params.get('url')?.trim() || null
  const { state, retry } = useScan(url)

  // Submitting the link that is already in the address bar changes no state, so it re-runs the scan explicitly.
  const startScan = (next: string) => (next === url ? retry() : setParams({ url: next }))
  const reset = () => {
    setParams({})
    window.scrollTo(0, 0)
  }

  if (state.phase === 'done') {
    return (
      <Suspense fallback={<p role="status" className="label-caps">Preparing the reading…</p>}>
        <ResultView key={state.result.scannedAt} result={state.result} onScanAnother={reset} />
      </Suspense>
    )
  }

  const invalidMessage = state.phase === 'failed' && state.failure.kind === 'invalid-url' ? state.failure.message : undefined

  return (
    <section className="flex flex-col items-start gap-6">
      <EntryBand
        key={url ?? ''}
        hero={state.phase !== 'scanning'}
        form={{ initialUrl: url ?? '', busy: state.phase === 'scanning', error: invalidMessage, onSubmit: startScan }}
      />

      {state.phase === 'scanning' && <ScanProgress sources={state.sources} nextCheckAt={state.nextCheckAt} />}
      {state.phase === 'failed' && state.failure.kind !== 'invalid-url' && <ScanFailureNotice failure={state.failure} onRetry={retry} />}
      {state.phase === 'idle' && (
        <>
          <ExampleLinks />
          <SourcesPrimer />
        </>
      )}
    </section>
  )
}
