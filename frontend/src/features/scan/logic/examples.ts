// The "Try an example" links on the home page: one link per verdict, picked so demo and live mode agree on the verdict.
// Data only. The backend's demo-mode tests lock these exact links to these verdicts.
import type { Verdict } from '@/lib/api/types'

interface ExampleLink {
  url: string
  /** The verdict the scan is expected to give; shown before the scan runs. */
  expected: Verdict
  note: string
}

export const EXAMPLE_LINKS: readonly ExampleLink[] = [
  {
    url: 'https://wikipedia.org/',
    expected: 'Safe',
    note: 'An established encyclopedia that no source flags.',
  },
  {
    url: 'https://paypa1.com/',
    expected: 'Suspicious',
    note: 'A "1" stands in for the "l" of paypal: our own look-alike check objects.',
  },
  {
    url: 'https://testsafebrowsing.appspot.com/s/malware.html',
    expected: 'Dangerous',
    note: "Google's own test page, listed as malware on purpose. Harmless to scan.",
  },
]
