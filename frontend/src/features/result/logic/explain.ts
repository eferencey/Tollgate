// Plain-language wording for the "Why this score?" section: one sentence per kind of finding, one for a score cap
// and a one-sentence summary. Pure text logic, no React. Keyed on the finding's stable `kind` code, never on the
// backend's description, which is prose for the table and may be reworded.
import type { Finding, FindingKind, ScanResponse } from '@/lib/api/types'
import type { Reading } from './reading'

interface FindingWording {
  /** A sentence a non-technical reader understands; shown under the finding's row. */
  explanation: string
  /** A lower-case clause that fits "mainly because ...", used by the summary and the cap sentence. */
  reason: string
}

// `Record<FindingKind, ...>` makes the compiler refuse to build when the backend gains a kind that has no wording.
const WORDING: Record<FindingKind, FindingWording> = {
  EnginesMalicious: {
    explanation: 'Security scanners that check websites say this link is harmful, and every scanner that agrees costs more points.',
    reason: 'security scanners flagged it as harmful',
  },
  EnginesSuspicious: {
    explanation: 'Some security scanners are not sure about this link and call it suspicious, which costs fewer points than a firm "harmful".',
    reason: 'security scanners think it looks suspicious',
  },
  SafeBrowsingListed: {
    explanation: 'Google keeps a list of dangerous websites, and this link is on it.',
    reason: 'Google lists it as a dangerous website',
  },
  UrlActivelyDistributingMalware: {
    explanation: 'This exact link is being used right now to spread harmful software.',
    reason: 'it is spreading harmful software right now',
  },
  UrlDistributedMalwareInPast: {
    explanation: 'This exact link was used to spread harmful software before, even if it may be clean today.',
    reason: 'it has spread harmful software before',
  },
  DomainHasMalwareRecords: {
    explanation: 'Other pages on this website have been used to spread harmful software, so the site has a bad track record.',
    reason: 'the website has a history of spreading harmful software',
  },
  DomainRecentlyRegistered: {
    explanation: "This website's name was registered very recently, and scam sites are often brand new.",
    reason: "the website's name is very new",
  },
  MixedAlphabetDomain: {
    explanation: 'The website name mixes letters from different alphabets that look alike, a trick used to pass as a well-known site.',
    reason: 'its name mixes look-alike alphabets',
  },
  NonLatinAlphabetDomain: {
    explanation: 'The website name uses a non-Latin alphabet, which is often harmless but can hide letters that look like ordinary ones.',
    reason: 'its name uses a non-Latin alphabet',
  },
  BrandLookalikeCharacters: {
    explanation: 'The website name copies a well-known brand by swapping in look-alike characters, such as a number 1 for the letter l.',
    reason: 'its name copies a well-known brand with look-alike characters',
  },
  BrandTypo: {
    explanation: 'The website name is one typing slip away from a well-known brand, which scammers count on.',
    reason: 'its name is a typo away from a well-known brand',
  },
  SuspiciousTld: {
    explanation: 'The ending of the web address (such as .top) is used for scams far more often than usual, though honest sites use it too.',
    reason: 'its web address ends in a type often used for scams',
  },
  RawIpAddressHost: {
    explanation: 'The link points to a string of numbers instead of a website name, which real businesses rarely do.',
    reason: 'it points to a string of numbers instead of a website name',
  },
  HiddenBehindUserInfo: {
    explanation: 'The part of the link before the @ sign is only there to look like another site; the page is really on what comes after it.',
    reason: 'it hides its real website behind an @ sign',
  },
}

export function explainFinding(finding: Finding): string {
  return WORDING[finding.kind].explanation
}

/** Only called for a cap that really lowered the score (the backend reports `cappedBy` only then). */
export function explainCap(cappedBy: Finding): string {
  const reason = WORDING[cappedBy.kind].reason
  return `Because ${reason}, the score cannot be higher than ${cappedBy.scoreCap}, however clean the other sources are.`
}

/** The one sentence above the table. Never reads as "safe" when nothing could be checked. */
export function summarize(result: ScanResponse, reading: Reading): string {
  if (reading.noData) {
    return reading.localFindings.length > 0
      ? "None of the sources answered, so this link could not be checked. That is not the same as safe, and our own check of the link's spelling did find something."
      : 'None of the sources answered, so this link could not be checked. That is not the same as safe.'
  }

  if (result.findings.length === 0) {
    const verdictOfSources =
      reading.respondedSources === reading.totalSources
        ? `All ${reading.totalSources} sources checked this link and none reported a problem`
        : `${reading.respondedSources} of ${reading.totalSources} sources answered and none reported a problem`
    return `${verdictOfSources}, so it keeps all 100 points. That is a good sign, not a guarantee.`
  }

  const [biggest, ...others] = [...result.findings].sort((a, b) => b.pointsDeducted - a.pointsDeducted)
  const alsoFound = others.length === 0 ? '' : `, plus ${others.length} other ${others.length === 1 ? 'finding' : 'findings'}`
  return `This link lost ${100 - result.score} points, mainly because ${WORDING[biggest.kind].reason}${alsoFound}.`
}
