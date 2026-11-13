// The home page's entry area: headline, short pitch and the form. It sits directly on the page-wide silk background
// (see AppShell), so its text uses full-strength colour: secondary text would dip below 4.5:1 where a fold catches the light.
// Once a scan runs the area shrinks to the form so the progress below gets the screen.
import { cn } from '@/lib/utils'
import { ScanForm, type ScanFormProps } from './ScanForm'

interface EntryBandProps {
  /** True while the page is in its entry state (no scan running): the headline and pitch are shown large. */
  hero: boolean
  form: ScanFormProps
}

export function EntryBand({ hero, form }: EntryBandProps) {
  return (
    <div className={cn('flex w-full flex-col items-start gap-6', hero && 'min-h-[24rem] justify-end sm:min-h-[28rem]')}>
      <p className="label-caps">Lane 01 · Pre-crossing check</p>
      <h1 className="max-w-3xl text-5xl sm:text-7xl">Check the link before you pass through it.</h1>
      <p className="max-w-prose text-lg">
        Tollgate asks four threat-intelligence sources at once and gives one score from 0 to 100, with every point taken off traced
        back to the source that caused it.
      </p>
      <div className="mt-4 w-full">
        <ScanForm {...form} />
      </div>
    </div>
  )
}
