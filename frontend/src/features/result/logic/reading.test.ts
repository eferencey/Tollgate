// Tests for how a scan response is read for display: the "no data" case, points, quota wording and host name.
import { describe, expect, it } from 'vitest'
import type { ScanResponse, SourceStatus } from '@/lib/api/types'
import { formatPoints, hostOf, isQuotaExhausted, pointsFrom, readResult } from './reading'

const completed = (source: SourceStatus['source']): SourceStatus => ({ source, state: 'Completed' })
const unavailable = (source: SourceStatus['source'], reason?: string): SourceStatus => ({ source, state: 'Unavailable', reason })

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

describe('readResult', () => {
  it('counts responding sources and sums every deduction', () => {
    const reading = readResult(
      response({
        findings: [
          { source: 'VirusTotal', kind: 'EnginesMalicious', description: 'a', pointsDeducted: 12 },
          { source: 'Rdap', kind: 'DomainRecentlyRegistered', description: 'b', pointsDeducted: 5 },
        ],
        sources: [completed('VirusTotal'), unavailable('SafeBrowsing'), completed('UrlHaus'), unavailable('Rdap')],
      }),
    )

    expect(reading.respondedSources).toBe(2)
    expect(reading.totalSources).toBe(4)
    expect(reading.totalDeducted).toBe(17)
    expect(reading.noData).toBe(false)
  })

  it('reports "no data" when no source answered, even though the backend scored 100', () => {
    const reading = readResult(
      response({ confidence: 'Low', sources: [unavailable('VirusTotal'), unavailable('SafeBrowsing'), unavailable('UrlHaus'), unavailable('Rdap')] }),
    )

    expect(reading.noData).toBe(true)
    expect(reading.respondedSources).toBe(0)
  })

  it('keeps findings from our own local analysis apart, and they never count as an answering source', () => {
    const local = { source: 'LocalAnalysis' as const, kind: 'BrandLookalikeCharacters' as const, description: 'looks like paypal.com', pointsDeducted: 45 }
    const reading = readResult(
      response({
        findings: [local],
        confidence: 'Low',
        sources: [unavailable('VirusTotal'), unavailable('SafeBrowsing'), unavailable('UrlHaus'), unavailable('Rdap')],
      }),
    )

    expect(reading.localFindings).toEqual([local])
    expect(reading.noData).toBe(true) // the local check found something, yet no source answered
    expect(reading.respondedSources).toBe(0)
    expect(reading.totalDeducted).toBe(45)
  })

  it('lists sources that are still analysing', () => {
    const pending: SourceStatus = { source: 'VirusTotal', state: 'Pending' }
    const reading = readResult(response({ status: 'Partial', sources: [pending, completed('Rdap')] }))

    expect(reading.stillAnalysing).toEqual([pending])
    expect(reading.noData).toBe(false)
  })
})

describe('pointsFrom', () => {
  it('adds up only the findings of the asked source', () => {
    const findings = [
      { source: 'VirusTotal' as const, kind: 'EnginesMalicious' as const, description: 'a', pointsDeducted: 12 },
      { source: 'VirusTotal' as const, kind: 'EnginesSuspicious' as const, description: 'b', pointsDeducted: 10 },
      { source: 'UrlHaus' as const, kind: 'DomainHasMalwareRecords' as const, description: 'c', pointsDeducted: 30 },
    ]

    expect(pointsFrom('VirusTotal', findings)).toBe(22)
    expect(pointsFrom('Rdap', findings)).toBe(0)
  })
})

describe('isQuotaExhausted', () => {
  it('recognises the used-up VirusTotal quota message', () => {
    const reason = 'VirusTotal daily quota (500 requests) is used up; it resets at 00:00 UTC'
    expect(isQuotaExhausted(unavailable('VirusTotal', reason))).toBe(true)
  })

  it('does not treat an ordinary outage as a quota problem', () => {
    expect(isQuotaExhausted(unavailable('VirusTotal', 'Demo: simulated outage'))).toBe(false)
    expect(isQuotaExhausted(unavailable('VirusTotal'))).toBe(false)
  })

  it('ignores the word quota on a source that answered', () => {
    expect(isQuotaExhausted({ source: 'VirusTotal', state: 'Completed', reason: 'quota' })).toBe(false)
  })
})

describe('formatting', () => {
  it('shows deductions with a minus sign', () => {
    expect(formatPoints(12)).toBe('−12')
  })

  it('shortens a url to its host and survives garbage', () => {
    expect(hostOf('https://free-bonus.xyz/win?x=1')).toBe('free-bonus.xyz')
    expect(hostOf('not a url')).toBe('not a url')
  })
})
