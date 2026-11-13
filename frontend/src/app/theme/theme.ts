// Pure theme rules: which preferences exist, how one resolves to dark/light, and how it is stored and applied.

export type ThemePreference = 'system' | 'dark' | 'light'
type ResolvedTheme = 'dark' | 'light'

// The same key and rule are repeated in the inline script of index.html, which must run before first paint.
const THEME_STORAGE_KEY = 'tollgate-theme'

const PREFERENCES: readonly ThemePreference[] = ['system', 'dark', 'light']

export function readPreference(): ThemePreference {
  try {
    const stored = localStorage.getItem(THEME_STORAGE_KEY)
    return PREFERENCES.find((preference) => preference === stored) ?? 'system'
  } catch {
    return 'system' // storage can be blocked (private window); the app must still work
  }
}

export function writePreference(preference: ThemePreference): void {
  try {
    localStorage.setItem(THEME_STORAGE_KEY, preference)
  } catch {
    // Ignored on purpose: the choice then lasts only for this visit.
  }
}

// Dark is the designed-first look, so "system" only switches to light when the OS explicitly asks for it.
export function resolveTheme(preference: ThemePreference): ResolvedTheme {
  if (preference !== 'system') return preference
  return window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark'
}

export function applyTheme(theme: ResolvedTheme): void {
  const root = document.documentElement
  root.classList.toggle('dark', theme === 'dark')
  root.style.colorScheme = theme // native widgets (scrollbars, inputs) follow the theme
}
