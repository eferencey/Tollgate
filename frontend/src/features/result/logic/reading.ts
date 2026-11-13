// Turns a scan response into the few facts the result screen needs. Pure; this is where "no data is not safe" is decided.
import type { Finding, ScanResponse, SourceStatus, ThreatSource } from '@/lib/api/types'

export interface Reading {
  /** No source answered. The backend still returns score 100 / Low then, which must never be shown as "safe". */
  noData: boolean
  respondedSources: number
  totalSources: number
  /** Sum of every deduction, before the 0..100 limits and the cap are applied. */
  totalDeducted: number
  /** Sources the backend was still waiting on when it stopped asking (the score may still change). */
  stillAnalysing: SourceStatus[]
  /** Findings from our own check of the link's shape. They exist even when no source answered. */
  localFindings: Finding[]
}

export function readResult(result: ScanResponse): Reading {
  const responded = result.sources.filter((source) => source.state === 'Completed').length
  return {
    noData: responded === 0,
    respondedSources: responded,
    totalSources: result.sources.length,
    totalDeducted: result.findings.reduce((sum, finding) => sum + finding.pointsDeducted, 0),
    stillAnalysing: result.sources.filter((source) => source.state === 'Pending'),
    localFindings: result.findings.filter((finding) => finding.source === 'LocalAnalysis'),
  }
}

export function pointsFrom(source: ThreatSource, findings: Finding[]): number {
  return findings.filter((finding) => finding.source === source).reduce((sum, finding) => sum + finding.pointsDeducted, 0)
}

// The API reports a used-up quota only through the human-readable reason text, so we look for the word.
// If the backend ever gets a machine-readable reason code, switch to it and delete this guess.
export function isQuotaExhausted(source: SourceStatus): boolean {
  return source.state === 'Unavailable' && /quota/i.test(source.reason ?? '')
}

export function hostOf(url: string): string {
  try {
    return new URL(url).host
  } catch {
    return url
  }
}

export function formatPoints(points: number): string {
  return `−${points}` // a real minus sign, as wide as the digits in a mono font
}
