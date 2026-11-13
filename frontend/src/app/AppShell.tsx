// Page frame shared by every route: header with logo and theme switch, the routed page, and the footer with status and disclaimer.
import { useEffect } from 'react'
import { Link, Outlet, useLocation, useSearchParams } from 'react-router'
import { Logo } from '@/components/brand/Logo'
import { SilkField } from '@/components/brand/SilkField'
import { pageTitle } from './pageTitle'
import { StatusReadout } from './StatusReadout'
import { ThemeToggle } from './theme/ThemeToggle'

// The silk is the page background of every real screen (scanning, result, history, design system). Only the 404 page stays flat.
const SILK_PATHS = ['/', '/history', '/design']

export function AppShell() {
  const { pathname } = useLocation()
  const [params] = useSearchParams()
  const onSilkPage = SILK_PATHS.includes(pathname)
  const title = pageTitle(pathname, params.get('url'))

  useEffect(() => {
    document.title = title
  }, [title])

  return (
    <div className="mx-auto flex min-h-svh w-full max-w-5xl flex-col px-4 sm:px-6">
      {onSilkPage && <SilkField />}
      {/* First Tab stop: lets keyboard users jump past the header controls. Visible only while focused. */}
      <a
        href="#main"
        className="label-caps sr-only z-10 bg-primary px-3 py-2 text-primary-foreground focus:not-sr-only focus:absolute focus:left-4 focus:top-2 focus-visible:ring-2 focus-visible:ring-ring"
      >
        Skip to content
      </a>
      <header className="flex items-center justify-between border-b border-border py-2">
        <Link to="/" aria-label="Tollgate home" className="inline-flex min-h-11 items-center outline-none focus-visible:ring-2 focus-visible:ring-ring">
          <Logo />
        </Link>
        <ThemeToggle />
      </header>

      <main id="main" tabIndex={-1} className="flex-1 py-10 outline-none sm:py-16">
        <Outlet />
      </main>

      <footer className="flex flex-col gap-1 border-t border-border py-2 sm:flex-row sm:items-center sm:justify-between">
        <StatusReadout />
        <div className="flex flex-wrap items-center gap-x-4 text-sm">
          <p>A risk indicator, not a safety guarantee.</p>
          <nav aria-label="Footer" className="flex gap-x-4">
            <Link to="/history" className="touch-link">
              History
            </Link>
            <Link to="/design" className="touch-link">
              Design system
            </Link>
          </nav>
        </div>
      </footer>
    </div>
  )
}
