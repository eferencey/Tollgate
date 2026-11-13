// Tests for how history links are shown (query string and credentials hidden) and scanned again.
import { describe, expect, it } from 'vitest'
import { displayLink, rescanPath } from './links'

describe('displayLink', () => {
  it('shows host and path', () => {
    expect(displayLink('https://example.com/a/b')).toEqual({ host: 'example.com', rest: '/a/b' })
  })

  it('shows a bare host without a trailing slash', () => {
    expect(displayLink('https://example.com/')).toEqual({ host: 'example.com', rest: '' })
  })

  it('hides the query string and marks the cut', () => {
    expect(displayLink('https://example.com/reset?token=SECRET')).toEqual({ host: 'example.com', rest: '/reset?…' })
    expect(displayLink('https://example.com/?token=SECRET')).toEqual({ host: 'example.com', rest: '/?…' })
  })

  it('hides credentials in front of the host and marks the cut', () => {
    expect(displayLink('http://user:hunter2@192.168.0.1/login')).toEqual({ host: '…@192.168.0.1', rest: '/login' })
  })

  it('keeps a non-default port, which is part of the host', () => {
    expect(displayLink('http://localhost:8080/x').host).toBe('localhost:8080')
  })

  it('never prints the query of something it cannot parse', () => {
    const shown = displayLink('not a url?token=SECRET')
    expect(JSON.stringify(shown)).not.toContain('SECRET')
  })
})

describe('rescanPath', () => {
  it('builds the home address with the full link as its url parameter', () => {
    expect(rescanPath('https://example.com/a?x=1&y=2')).toBe('/?url=https%3A%2F%2Fexample.com%2Fa%3Fx%3D1%26y%3D2')
  })
})
