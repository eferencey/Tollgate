// Display facts about where findings come from: the four threat-intelligence sources (fixed order, name, what each
// one tells us) and our own local analysis of the link's shape, which is not one of the queried sources.
import type { SourceStatus, ThreatSource } from '@/lib/api/types'

export const SOURCE_INFO: Record<ThreatSource, { label: string; checks: string }> = {
  VirusTotal: { label: 'VirusTotal', checks: 'How many antivirus engines flag the link as malicious or suspicious.' },
  SafeBrowsing: { label: 'Safe Browsing', checks: "Whether Google's blacklist of dangerous sites lists the link." },
  UrlHaus: { label: 'URLhaus', checks: 'Whether the link or its domain has been used to distribute malware.' },
  Rdap: { label: 'RDAP', checks: 'How old the domain registration is; brand-new domains are riskier.' },
  LocalAnalysis: {
    label: 'Local analysis',
    checks: 'Whether the link itself looks like an imitation: look-alike letters, typos of well-known brands, abused domain endings, raw IP addresses.',
  },
}

// The four queried sources in one fixed order everywhere (progress list, cards, tables) so the eye learns where each sits.
// Local analysis is deliberately absent: it needs no request, cannot fail and never counts towards confidence.
export const SOURCE_ORDER: ThreatSource[] = ['VirusTotal', 'SafeBrowsing', 'UrlHaus', 'Rdap']

export function sortSources(sources: SourceStatus[]): SourceStatus[] {
  return [...sources].sort((a, b) => SOURCE_ORDER.indexOf(a.source) - SOURCE_ORDER.indexOf(b.source))
}
