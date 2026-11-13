// Tests for the plain-language wording of findings, caps and the one-sentence summary.
import { describe, expect, it } from 'vitest'
import type { Finding, FindingKind, ScanResponse, SourceStatus } from '@/lib/api/types'
import { explainCap, explainFinding, summarize } from './explain'
import { readResult } from './reading'

const ALL_KINDS: FindingKind[] = [
  'EnginesMalicious',
  'EnginesSuspicious',
  'SafeBrowsingListed',
  'UrlActivelyDistributingMalware',
  'UrlDistributedMalwareInPast',
  'DomainHasMalwareRecords',
  'DomainRecentlyRegistered',
  'MixedAlphabetDomain',
  'NonLatinAlphabetDomain',
  'BrandLookalikeCharacters',
  'BrandTypo',
  'SuspiciousTld',
  'RawIpAddressHost',
  'HiddenBehindUserInfo',
]

const finding = (kind: FindingKind, pointsDeducted: number, scoreCap?: number): Finding => ({
  source: 'VirusTotal', // the wording depends on the kind only
  kind,
  description: 'technical text that must not influence the explanation',
  pointsDeducted,
  scoreCap,
})

const completed = (source: SourceStatus['source']): SourceStatus => ({ source, state: 'Completed' })
const unavailable = (source: SourceStatus['source']): SourceStatus => ({ source, state: 'Unavailable' })

function response(overrides: Partial<ScanResponse>): ScanResponse {
  return {
    status: 'Complete',
    mode: 'Demo',
    url: 'https://example.com/',
    score: 100,
    verdict: 'Safe',
    confidence: 'High',
    findings: [],
    sources: [completed('VirusTotal'), completed('SafeBrowsing'), completed('UrlHaus'), completed('Rdap')],
    fromCache: false,
    scannedAt: '2026-11-11T12:00:00Z',
    ...overrides,
  }
}

const summaryOf = (result: ScanResponse) => summarize(result, readResult(result))

describe('explainFinding', () => {
  it.each(ALL_KINDS)('has a plain sentence for %s', (kind) => {
    const sentence = explainFinding(finding(kind, 10))

    expect(sentence.length).toBeGreaterThan(20)
    expect(sentence.endsWith('.')).toBe(true)
  })

  it('gives every kind its own sentence', () => {
    const sentences = ALL_KINDS.map((kind) => explainFinding(finding(kind, 10)))

    expect(new Set(sentences).size).toBe(ALL_KINDS.length)
  })

  it('keeps jargon out of the sentences', () => {
    const jargon = /punycode|homoglyph|typosquat|\bTLD\b|\bRDAP\b|\bIP address\b|heuristic|payload/i

    for (const kind of ALL_KINDS) expect(explainFinding(finding(kind, 10))).not.toMatch(jargon)
  })

  it('ignores the description text', () => {
    expect(explainFinding(finding('SafeBrowsingListed', 60))).toBe(explainFinding({ ...finding('SafeBrowsingListed', 60), description: 'reworded' }))
  })
})

describe('explainCap', () => {
  it('names the reason and the ceiling in plain words', () => {
    expect(explainCap(finding('SafeBrowsingListed', 60, 20))).toBe(
      'Because Google lists it as a dangerous website, the score cannot be higher than 20, however clean the other sources are.',
    )
  })
})

describe('summarize', () => {
  it('reports a clean link with all sources answering, and says it is not a guarantee', () => {
    expect(summaryOf(response({}))).toBe('All 4 sources checked this link and none reported a problem, so it keeps all 100 points. That is a good sign, not a guarantee.')
  })

  it('counts only the sources that answered when some did not', () => {
    const summary = summaryOf(response({ sources: [completed('VirusTotal'), completed('Rdap'), unavailable('SafeBrowsing'), unavailable('UrlHaus')] }))

    expect(summary).toContain('2 of 4 sources answered and none reported a problem')
  })

  it('names the biggest deduction as the main reason and counts the rest', () => {
    const summary = summaryOf(
      response({
        score: 63,
        findings: [finding('DomainRecentlyRegistered', 5), finding('EnginesMalicious', 12), finding('EnginesSuspicious', 10)],
      }),
    )

    expect(summary).toBe('This link lost 37 points, mainly because security scanners flagged it as harmful, plus 2 other findings.')
  })

  it('uses the singular for one other finding and nothing for a single finding', () => {
    const one = finding('EnginesMalicious', 12)

    expect(summaryOf(response({ score: 88, findings: [one] }))).toBe('This link lost 12 points, mainly because security scanners flagged it as harmful.')
    expect(summaryOf(response({ score: 83, findings: [one, finding('DomainRecentlyRegistered', 5)] }))).toContain('plus 1 other finding.')
  })

  it('counts the points the cap took, not only the penalties', () => {
    const capped = finding('SafeBrowsingListed', 60, 20)

    expect(summaryOf(response({ score: 20, verdict: 'Dangerous', findings: [capped, finding('EnginesSuspicious', 5)], cappedBy: capped }))).toContain('lost 80 points')
  })

  it('never calls a link with no answering source safe, but mentions the local finding', () => {
    const none = [unavailable('VirusTotal'), unavailable('SafeBrowsing'), unavailable('UrlHaus'), unavailable('Rdap')]

    const bare = summaryOf(response({ confidence: 'Low', sources: none }))
    expect(bare).toContain('could not be checked')
    expect(bare).toContain('not the same as safe')
    expect(bare).not.toContain('keeps all 100')

    const withLocal = summaryOf(response({ confidence: 'Low', sources: none, findings: [{ ...finding('BrandTypo', 30), source: 'LocalAnalysis' }] }))
    expect(withLocal).toContain('our own check of the link')
  })
})
