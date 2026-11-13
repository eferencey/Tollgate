// Tests for building the share link and copying it to the clipboard.
import { describe, expect, it } from 'vitest'
import { buildShareLink, copyText } from './sharing'

describe('buildShareLink', () => {
  it('points at the app root with the scanned link in ?url=', () => {
    expect(buildShareLink('http://localhost:5173', 'https://free-bonus.xyz/claim')).toBe('http://localhost:5173/?url=https%3A%2F%2Ffree-bonus.xyz%2Fclaim')
  })

  it('round-trips awkward links through the query string', () => {
    const scanned = 'https://example.com/a b?x=1&y=2#frag'
    const link = new URL(buildShareLink('https://tollgate.example', scanned))

    expect(link.searchParams.get('url')).toBe(scanned)
  })

  it('drops whatever path the page happened to be on', () => {
    expect(buildShareLink('http://localhost:5173/design', 'https://a.test/')).toBe('http://localhost:5173/?url=https%3A%2F%2Fa.test%2F')
  })

  it('keeps the sub-path the app is served under', () => {
    expect(buildShareLink('https://user.github.io', 'https://a.test/', '/tollgate/')).toBe('https://user.github.io/tollgate/?url=https%3A%2F%2Fa.test%2F')
  })
})

describe('copyText', () => {
  it('reports "copied" when the clipboard accepts the text', async () => {
    const written: string[] = []

    const outcome = await copyText('hello', { writeText: async (text) => void written.push(text) })

    expect(outcome).toBe('copied')
    expect(written).toEqual(['hello'])
  })

  it('reports "denied" when the browser refuses', async () => {
    const outcome = await copyText('hello', {
      writeText: () => Promise.reject(new DOMException('Write permission denied.', 'NotAllowedError')),
    })

    expect(outcome).toBe('denied')
  })

  it('reports "denied" when there is no clipboard at all', async () => {
    expect(await copyText('hello', undefined)).toBe('denied')
  })
})
