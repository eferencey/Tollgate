// Decorative silk-cloth page background (WebGL), coloured by the --silk-* theme tokens so it follows dark/light.
// Fixed behind the whole page (it does not scroll); content goes above it. Without WebGL the flat page background stays.
import { useEffect, useRef } from 'react'
import { createSilkRenderer, readSilkPalette, type SilkPalette } from './silk/silkRenderer'

export function SilkField() {
  const root = useRef<HTMLDivElement>(null)
  const holder = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const rootElement = root.current
    const holderElement = holder.current
    if (!rootElement || !holderElement) return

    const html = document.documentElement
    const palette: { current: SilkPalette } = { current: readSilkPalette(getComputedStyle(html)) }
    const renderer = createSilkRenderer(holderElement, rootElement, palette)

    // The theme switch toggles the `dark` class on <html>; re-read the tokens when it does.
    const themeWatcher = new MutationObserver(() => {
      palette.current = readSilkPalette(getComputedStyle(html))
      renderer?.redraw()
    })
    themeWatcher.observe(html, { attributes: true, attributeFilter: ['class'] })

    return () => {
      themeWatcher.disconnect()
      renderer?.dispose()
    }
  }, [])

  return (
    <div ref={root} aria-hidden="true" className="pointer-events-none fixed inset-0 -z-10 overflow-hidden bg-background">
      <div ref={holder} className="absolute inset-0" />
    </div>
  )
}
