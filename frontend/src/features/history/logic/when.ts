// How the list says when a scan happened: "5 min ago" for recent ones, a plain date for old ones. Pure; `now` is a parameter.
const MINUTE = 60_000
const HOUR = 60 * MINUTE
const DAY = 24 * HOUR
/** From this age on, a date reads better than "40 days ago". */
const RELATIVE_LIMIT_DAYS = 30

export function formatWhen(iso: string, now: number): string {
  const then = Date.parse(iso)
  if (Number.isNaN(then)) return '—'

  const age = now - then
  if (age < MINUTE) return 'just now' // also covers a small clock difference between server and browser
  if (age < HOUR) return `${Math.floor(age / MINUTE)} min ago`
  if (age < DAY) return `${Math.floor(age / HOUR)} h ago`
  if (age < RELATIVE_LIMIT_DAYS * DAY) return `${Math.floor(age / DAY)} d ago`
  return new Date(then).toISOString().slice(0, 10)
}

/**
 * The small caption of a list row. Confidence is named only when it is not High: most rows are High, so repeating it
 * would be noise, while a Low or Medium reading is exactly what a reader should notice.
 */
export function formatRowMeta(confidence: string, iso: string, now: number): string {
  const when = formatWhen(iso, now)
  return confidence === 'High' ? when : `${confidence} confidence · ${when}`
}

/** The exact moment in the viewer's own time zone, for the tooltip of the relative text. */
export function formatExact(iso: string): string {
  const then = new Date(iso)
  return Number.isNaN(then.getTime()) ? iso : then.toLocaleString()
}
