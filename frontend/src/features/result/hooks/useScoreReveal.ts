// The score reveal, scrubbed by scrolling (GSAP + ScrollTrigger): intro text, the panel rises and grows to fill the
// screen, the ring fills while the numeral counts up, the verdict lands, finding chips pop in one by one, the panel settles.
// Choreography after the 21st.dev "Cinematic landing hero"; the look lives in ResultStage. Users who prefer reduced
// motion never enter the animated branch: they get the finished result as plain content.
import { useLayoutEffect, useRef, useState, type RefObject } from 'react'
import gsap from 'gsap'
import { ScrollTrigger } from 'gsap/ScrollTrigger'

gsap.registerPlugin(ScrollTrigger)

// Scroll distance the reveal is pinned for, in screen heights. The original pins for ~7000 px; this is about a third of that.
const PIN_SCREENS = 2.8
const SCRUB_SMOOTHING_SECONDS = 0.6
const PANEL_START_SCALE = 0.55
const PANEL_SETTLE_SCALE = 0.96

// Positions on the timeline (arbitrary units; scrolling maps the whole timeline onto PIN_SCREENS).
const AT = { panelRises: 0.3, panelGrows: 1.5, ringFills: 2.7, verdict: 4.4, chips: 4.6, settle: 6.2, closing: 6.4 } as const

interface Reveal {
  /** True while the animated, pinned version is active (false for reduced motion and for results with no data). */
  animated: boolean
  /** True once the timeline has passed the moment the verdict appears; always true when not animated. */
  revealed: boolean
  /** Jumps to the end of the reveal (the "Skip" button and Esc). */
  skip: () => void
}

export function useScoreReveal(root: RefObject<HTMLElement | null>, options: { enabled: boolean; score: number }): Reveal {
  const { enabled, score } = options
  const [animated, setAnimated] = useState(false)
  const [revealed, setRevealed] = useState(true)
  const trigger = useRef<ScrollTrigger | null>(null)

  useLayoutEffect(() => {
    const rootElement = root.current
    if (!enabled || !rootElement) return

    const media = gsap.matchMedia()
    media.add('(prefers-reduced-motion: no-preference)', () => {
      const find = gsap.utils.selector(rootElement)
      const panel = find('[data-reveal="panel"]')
      const intro = find('[data-reveal="intro"]')
      const arc = rootElement.querySelector<SVGCircleElement>('[data-reveal="arc"]')!
      const numeral = find('[data-reveal="numeral"]')[0]
      const verdict = find('[data-reveal="verdict"]')
      const chips = find('[data-reveal="chip"]')
      const closing = find('[data-reveal="closing"]')

      const circumference = Number(arc.dataset.circumference)
      const finalOffset = Number(arc.dataset.finalOffset)
      const counter = { value: 0 }

      window.scrollTo(0, 0) // the reveal always starts from its first frame
      setAnimated(true)
      setRevealed(false)

      gsap.set(rootElement, { height: '100svh' })
      gsap.set(panel, { yPercent: 110, scale: PANEL_START_SCALE })
      gsap.set(arc, { strokeDashoffset: circumference })
      gsap.set([verdict, closing], { autoAlpha: 0, y: 12 })
      gsap.set(chips, { autoAlpha: 0, scale: 0.8, y: 12 })
      numeral.textContent = '0'

      const timeline: gsap.core.Timeline = gsap
        .timeline({
          defaults: { ease: 'none' },
          scrollTrigger: {
            trigger: rootElement,
            start: 'top top',
            end: () => `+=${window.innerHeight * PIN_SCREENS}`,
            pin: true,
            scrub: SCRUB_SMOOTHING_SECONDS,
            invalidateOnRefresh: true,
            anticipatePin: 1,
          },
          onUpdate: () => setRevealed(timeline.time() >= AT.verdict),
        })
        .to(intro, { autoAlpha: 0, y: -48, duration: 1.2 }, AT.panelRises)
        .to(panel, { yPercent: 0, duration: 1.2, ease: 'power2.out' }, AT.panelRises)
        .to(panel, { scale: 1, duration: 1, ease: 'power2.inOut' }, AT.panelGrows)
        .to(arc, { strokeDashoffset: finalOffset, duration: 1.6 }, AT.ringFills)
        .to(
          counter,
          {
            value: score,
            duration: 1.6,
            onUpdate: () => {
              numeral.textContent = String(Math.round(counter.value))
            },
          },
          AT.ringFills,
        )
        .to(verdict, { autoAlpha: 1, y: 0, duration: 0.4 }, AT.verdict)
        .to(chips, { autoAlpha: 1, scale: 1, y: 0, duration: 0.4, ease: 'back.out(2)', stagger: 0.25 }, AT.chips)
        .to(panel, { scale: PANEL_SETTLE_SCALE, duration: 0.8, ease: 'power2.inOut' }, AT.settle)
        .to(closing, { autoAlpha: 1, y: 0, duration: 0.5 }, AT.closing)

      trigger.current = timeline.scrollTrigger ?? null

      return () => {
        trigger.current = null
        setAnimated(false)
        setRevealed(true)
      }
    })

    return () => media.revert() // removes the pin spacing, the inline styles and the scroll listeners
  }, [root, enabled, score])

  const skip = () => {
    if (trigger.current) window.scrollTo({ top: trigger.current.end })
  }

  // Esc is the keyboard way out of the reveal, next to the visible Skip button.
  useLayoutEffect(() => {
    if (!animated) return
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') skip()
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [animated])

  return { animated, revealed, skip }
}
