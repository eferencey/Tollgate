// Placeholder shown while the history loads: grey blocks in the shape of the real page, so nothing jumps when data arrives.
// Deliberately still (no shimmer): there is little to see and nothing here should draw the eye.
function Block({ className }: { className: string }) {
  return <div aria-hidden="true" className={`bg-muted ${className}`} />
}

export function HistorySkeleton() {
  return (
    <div role="status" aria-busy="true" className="flex flex-col gap-6">
      <span className="sr-only">Loading history</span>
      <div className="grid grid-cols-2 gap-px border border-border bg-border lg:grid-cols-4">
        {[0, 1, 2, 3].map((cell) => (
          <div key={cell} className="flex flex-col gap-3 bg-card p-4">
            <Block className="h-3 w-24" />
            <Block className="h-9 w-16" />
            <Block className="h-3 w-28" />
          </div>
        ))}
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {[0, 1].map((panel) => (
          <div key={panel} className="flex flex-col gap-4 border border-border bg-card p-4">
            <Block className="h-5 w-40" />
            <Block className="h-24 w-full" />
          </div>
        ))}
      </div>

      <div className="flex flex-col gap-3 border border-border bg-card p-4">
        {[0, 1, 2, 3, 4].map((row) => (
          <Block key={row} className="h-10 w-full" />
        ))}
      </div>
    </div>
  )
}
