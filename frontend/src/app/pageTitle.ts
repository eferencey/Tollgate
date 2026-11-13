// Pure rule for the browser-tab title of each route, so a tab, the history list and a screen reader say where you are.
import { hostOf } from '@/features/result/logic/reading'

const APP_NAME = 'Tollgate'

const ROUTE_TITLES: Readonly<Record<string, string>> = {
  '/history': 'History and statistics',
  '/design': 'Design system',
}

/** Home with a `?url=` is a scan of that link; any other unknown path is the 404 page. */
export function pageTitle(pathname: string, scannedUrl: string | null): string {
  if (pathname === '/') {
    const link = scannedUrl?.trim()
    return link ? `${hostOf(toAbsolute(link))} · ${APP_NAME}` : `${APP_NAME} · Check a link before you pass`
  }
  return `${ROUTE_TITLES[pathname] ?? 'Page not found'} · ${APP_NAME}`
}

// The address bar may hold "example.com/x" without a scheme; `new URL` needs one to find the host.
function toAbsolute(link: string): string {
  return /^[a-z][a-z0-9+.-]*:\/\//i.test(link) ? link : `https://${link}`
}
