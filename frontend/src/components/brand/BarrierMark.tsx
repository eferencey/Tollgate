// The Tollgate logo mark: a toll-barrier arm on a post. Raised means "pass", lowered means "stop".
import { cn } from '@/lib/utils'

// Alternating segments (full / faded ink) give the striped arm without a second colour, so the mark works on any background.
const ARM_SEGMENTS = [0, 1, 2, 3, 4]

export function BarrierMark({ raised = false, className }: { raised?: boolean; className?: string }) {
  return (
    <svg viewBox="0 0 32 32" fill="currentColor" aria-hidden="true" className={cn('size-8', className)}>
      <rect x="3" y="15" width="6" height="13" rx="1" />
      <g
        className="transition-transform duration-700 ease-instrument"
        style={{ transformOrigin: '6px 14px', transform: raised ? 'rotate(-38deg)' : 'none' }}
      >
        {ARM_SEGMENTS.map((index) => (
          <rect key={index} x={9 + index * 4.6} y="12" width="4.6" height="4" opacity={index % 2 === 0 ? 1 : 0.4} />
        ))}
      </g>
      <circle cx="6" cy="14" r="2.2" />
    </svg>
  )
}
