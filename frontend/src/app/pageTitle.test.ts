// Tests for the per-route browser tab titles.
import { describe, expect, it } from 'vitest'
import { pageTitle } from './pageTitle'

describe('pageTitle', () => {
  it('names the app on the empty home page', () => {
    expect(pageTitle('/', null)).toBe('Tollgate · Check a link before you pass')
  })

  it('names the scanned host when a link is in the address bar', () => {
    expect(pageTitle('/', 'https://example.com/a?b=1')).toBe('example.com · Tollgate')
  })

  it('finds the host of a link typed without a scheme', () => {
    expect(pageTitle('/', 'free-bonus.xyz/win')).toBe('free-bonus.xyz · Tollgate')
  })

  it('ignores a blank link parameter', () => {
    expect(pageTitle('/', '   ')).toBe('Tollgate · Check a link before you pass')
  })

  it('titles the other routes and the 404 page', () => {
    expect(pageTitle('/history', null)).toBe('History and statistics · Tollgate')
    expect(pageTitle('/design', null)).toBe('Design system · Tollgate')
    expect(pageTitle('/nope', null)).toBe('Page not found · Tollgate')
  })
})
