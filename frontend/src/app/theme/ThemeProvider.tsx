// Holds the theme preference in React state and keeps <html> in sync with it (including OS changes in "system" mode).
import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { ThemeContext } from './ThemeContext'
import { applyTheme, readPreference, resolveTheme, writePreference, type ThemePreference } from './theme'

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [preference, setPreferenceState] = useState<ThemePreference>(readPreference)

  useEffect(() => {
    applyTheme(resolveTheme(preference))
    if (preference !== 'system') return

    const query = window.matchMedia('(prefers-color-scheme: light)')
    const followSystem = () => applyTheme(resolveTheme('system'))
    query.addEventListener('change', followSystem)
    return () => query.removeEventListener('change', followSystem)
  }, [preference])

  const value = useMemo(
    () => ({
      preference,
      setPreference: (next: ThemePreference) => {
        writePreference(next)
        setPreferenceState(next)
      },
    }),
    [preference],
  )

  return <ThemeContext value={value}>{children}</ThemeContext>
}
