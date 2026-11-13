// The React context object for the theme, kept apart from the provider component so hot reload stays reliable.
import { createContext } from 'react'
import type { ThemePreference } from './theme'

interface ThemeContextValue {
  preference: ThemePreference
  setPreference: (preference: ThemePreference) => void
}

export const ThemeContext = createContext<ThemeContextValue | null>(null)
