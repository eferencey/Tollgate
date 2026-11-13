// The score gauge: a ring that fills clockwise from the top, with the numeral in the middle and a ruled scale around it.
// It renders the finished state; the reveal animation (useScoreReveal) only rewinds and replays the same elements.
import { cn } from '@/lib/utils'

const SIZE = 240
const STROKE = 10
const RADIUS = (SIZE - STROKE) / 2
const RING_CIRCUMFERENCE = 2 * Math.PI * RADIUS

// A decorative scale like a dial: a tick every 10 points, longer at 0/25/50/75. It marks no verdict thresholds on purpose
// (those live in the backend configuration, and the UI must not keep a second copy).
const TICKS = Array.from({ length: 40 }, (_, index) => index)
const TICK_OUTER = SIZE / 2 - 1
const TICK_SHORT = 4
const TICK_LONG = 8

function tickLine(index: number) {
  const angle = (index / TICKS.length) * 2 * Math.PI - Math.PI / 2
  const length = index % 10 === 0 ? TICK_LONG : TICK_SHORT
  const inner = TICK_OUTER - length
  return {
    x1: SIZE / 2 + Math.cos(angle) * inner,
    y1: SIZE / 2 + Math.sin(angle) * inner,
    x2: SIZE / 2 + Math.cos(angle) * TICK_OUTER,
    y2: SIZE / 2 + Math.sin(angle) * TICK_OUTER,
  }
}

interface ScoreRingProps {
  /** 0..100, or null when there is nothing to show (no source answered): the ring stays empty. */
  score: number | null
  /** Tailwind text colour for the arc and numeral (a verdict colour once revealed). */
  toneClass: string
  className?: string
}

export function ScoreRing({ score, toneClass, className }: ScoreRingProps) {
  const finalOffset = RING_CIRCUMFERENCE * (1 - (score ?? 0) / 100)

  return (
    <div className={cn('relative aspect-square w-full max-w-60', className)}>
      <svg viewBox={`0 0 ${SIZE} ${SIZE}`} className="size-full" aria-hidden="true">
        <g className="text-border" stroke="currentColor" strokeWidth="1">
          {TICKS.map((index) => (
            <line key={index} {...tickLine(index)} />
          ))}
        </g>
        <circle cx={SIZE / 2} cy={SIZE / 2} r={RADIUS - 8} fill="none" stroke="currentColor" strokeWidth={STROKE} className="text-border" />
        <circle
          data-reveal="arc"
          data-circumference={RING_CIRCUMFERENCE}
          data-final-offset={finalOffset}
          cx={SIZE / 2}
          cy={SIZE / 2}
          r={RADIUS - 8}
          fill="none"
          stroke="currentColor"
          strokeWidth={STROKE}
          strokeDasharray={RING_CIRCUMFERENCE}
          strokeDashoffset={finalOffset}
          transform={`rotate(-90 ${SIZE / 2} ${SIZE / 2})`}
          className={cn('transition-colors duration-500', toneClass)}
        />
      </svg>

      {/* The colour sits on this wrapper, not on the numeral: tailwind-merge would take `text-display` (a size) and
          `text-safe` (a colour) for the same utility group and drop one of them. */}
      <div className={cn('absolute inset-0 flex flex-col items-center justify-center transition-colors duration-500', toneClass)}>
        {/* The animated numeral is hidden from screen readers; the stable text below is what they hear. */}
        <span data-reveal="numeral" aria-hidden="true" className="font-mono text-display leading-none font-semibold tabular-nums">
          {score ?? '—'}
        </span>
        <span className="sr-only">{score === null ? 'No score: no source answered' : `Score ${score} out of 100`}</span>
        <span aria-hidden="true" className="label-caps mt-1 text-muted-foreground">
          {score === null ? 'No data' : 'of 100'}
        </span>
      </div>
    </div>
  )
}
