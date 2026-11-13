// How a stored link appears in the history list, and the address that scans it again. Pure.
// Privacy: history stores the full URL, which may hold a token in its query string or a password before the
// host. The list shows neither; it marks the cut with an ellipsis. The re-scan address still uses the full link.

interface DisplayLink {
  /** The host; prefixed with an ellipsis and "@" when credentials were cut off in front of it. */
  host: string
  /** The path, ending in "?…" when a query string was cut off. */
  rest: string
}

const HIDDEN = '…'

export function displayLink(url: string): DisplayLink {
  let parsed: URL
  try {
    parsed = new URL(url)
  } catch {
    // Not expected (the backend stores normalized URLs), but never print what we could not parse safely.
    const beforeQuery = url.split(/[?#]/)[0]
    return { host: beforeQuery, rest: url.length > beforeQuery.length ? `?${HIDDEN}` : '' }
  }

  const hasCredentials = parsed.username !== '' || parsed.password !== ''
  const hasQuery = parsed.search !== ''
  const path = parsed.pathname === '/' && !hasQuery ? '' : parsed.pathname

  return {
    host: hasCredentials ? `${HIDDEN}@${parsed.host}` : parsed.host,
    rest: hasQuery ? `${path}?${HIDDEN}` : path,
  }
}

/** The in-app address that scans this exact link again (the same `/?url=` address the share link uses). */
export function rescanPath(url: string): string {
  return `/?${new URLSearchParams({ url }).toString()}`
}
