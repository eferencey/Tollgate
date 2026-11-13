// Living style guide at /design: every design token and brand piece rendered from the real CSS tokens.
import { useState, type ReactNode } from 'react'
import { BarrierMark } from '@/components/brand/BarrierMark'
import { Logo } from '@/components/brand/Logo'
import { VerdictBadge } from '@/components/brand/VerdictBadge'
import { Button } from '@/components/ui/button'
import type { Verdict } from '@/lib/api/types'

// Swatch classes are written out in full (not built from strings) so Tailwind can see them.
const SWATCHES = [
  { name: 'background', className: 'bg-background' },
  { name: 'card', className: 'bg-card' },
  { name: 'foreground', className: 'bg-foreground' },
  { name: 'muted-foreground', className: 'bg-muted-foreground' },
  { name: 'border', className: 'bg-border' },
  { name: 'safe', className: 'bg-safe' },
  { name: 'suspicious', className: 'bg-suspicious' },
  { name: 'dangerous', className: 'bg-dangerous' },
]

const VERDICTS: readonly Verdict[] = ['Safe', 'Suspicious', 'Dangerous']

// Each block is an opaque card (like the history panels), so secondary text never sits directly on the silk background.
function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="border border-border bg-card p-4 sm:p-6">
      <h2 className="label-caps mb-4 font-mono text-muted-foreground" style={{ fontStretch: '100%' }}>
        {title}
      </h2>
      {children}
    </section>
  )
}

export function DesignPage() {
  const [raised, setRaised] = useState(false)

  return (
    <div className="flex flex-col gap-6">
      <header className="pb-4">
        {/* Full-strength text: this header sits directly on the silk, where the muted grey is too faint. */}
        <p className="label-caps">Design system · Instrument panel</p>
        <h1 className="mt-2 text-5xl">Tokens in use</h1>
      </header>

      <Section title="Wordmark · and the verdict barrier">
        <div className="flex flex-wrap items-center gap-8">
          <Logo />
          <BarrierMark raised={raised} className="size-16 text-muted-foreground" />
          <Button variant="outline" onClick={() => setRaised((value) => !value)} aria-pressed={raised}>
            {raised ? 'Lower the arm' : 'Raise the arm'}
          </Button>
        </div>
        <p className="mt-3 max-w-prose text-sm text-muted-foreground">
          The wordmark is set in the heading type. The barrier arm now appears only in the verdict row of the result panel:
          raised = pass (Safe), lowered = stop.
        </p>
      </Section>

      <Section title="Colour · neutral surfaces, colour only for verdicts">
        <ul className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          {SWATCHES.map(({ name, className }) => (
            <li key={name}>
              <div className={`h-14 border border-border ${className}`} />
              <p className="label-caps mt-1.5 text-muted-foreground">{name}</p>
            </li>
          ))}
        </ul>
      </Section>

      <Section title="Typography · Archivo (condensed headings) + JetBrains Mono">
        <p className="font-heading text-6xl font-extrabold" style={{ fontStretch: '75%' }}>
          Heading 63
        </p>
        <p className="mt-3 max-w-prose">
          Body text is Archivo at normal width. It carries sentences: explanations, reasons, warnings.
        </p>
        <p className="mt-3 font-mono text-sm">
          VT_MALICIOUS_ENGINES · -12 pts · https://free-bonus.xyz/claim
        </p>
        <p className="label-caps mt-3 text-muted-foreground">Label caps · Source · Confidence</p>
      </Section>

      <Section title="Verdict badges">
        <div className="flex flex-wrap gap-3">
          {VERDICTS.map((verdict) => (
            <VerdictBadge key={verdict} verdict={verdict} />
          ))}
        </div>
      </Section>

      <Section title="Buttons">
        <div className="flex flex-wrap gap-3">
          <Button>Scan link</Button>
          <Button variant="secondary">Secondary</Button>
          <Button variant="outline">Outline</Button>
          <Button variant="ghost">Ghost</Button>
          <Button variant="destructive">Destructive</Button>
          <Button disabled>Disabled</Button>
        </div>
      </Section>
    </div>
  )
}
