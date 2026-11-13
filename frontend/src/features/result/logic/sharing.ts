// The shareable link: the app's own address plus ?url=<scanned link>, plus copying it to the clipboard.
// There is no stored "result id": opening the link runs the scan again (the 60-minute cache usually returns the same reading).

/** `basePath` is the path the app is served under ("/" locally, "/tollgate/" on GitHub Pages). */
export function buildShareLink(origin: string, scannedUrl: string, basePath = '/'): string {
  const link = new URL(basePath, origin)
  link.searchParams.set('url', scannedUrl)
  return link.toString()
}

type CopyOutcome = 'copied' | 'denied'

/** The part of the browser clipboard API we use; passed in so the failure paths can be tested without a browser. */
interface ClipboardLike {
  writeText(text: string): Promise<void>
}

/**
 * Copies text. The clipboard can be missing (insecure context, old browser) or refuse (permission denied, document
 * not focused); both end as `denied`, and the caller then lets the person copy the text by hand.
 */
export async function copyText(text: string, clipboard: ClipboardLike | undefined): Promise<CopyOutcome> {
  if (!clipboard) return 'denied'
  try {
    await clipboard.writeText(text)
    return 'copied'
  } catch {
    return 'denied'
  }
}
