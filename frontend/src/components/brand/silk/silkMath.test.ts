// Tests for the silk background's colour parsing and coordinate maths.
import { describe, expect, it } from 'vitest'
import { backingSize, hexToRgb, toShaderSpace } from './silkMath'

describe('hexToRgb', () => {
  it('reads six and three digit hex, with or without a hash and spaces', () => {
    expect(hexToRgb('#ff0000')).toEqual([1, 0, 0])
    expect(hexToRgb(' #0f0 ')).toEqual([0, 1, 0])
    expect(hexToRgb('0000ff')).toEqual([0, 0, 1])
  })

  it('falls back to black for anything it cannot read', () => {
    expect(hexToRgb('')).toEqual([0, 0, 0])
    expect(hexToRgb('oklch(0.5 0 0)')).toEqual([0, 0, 0])
  })
})

describe('backingSize', () => {
  it('multiplies by the pixel ratio but never beyond 2x, and never returns less than 1', () => {
    expect(backingSize(400, 1)).toBe(400)
    expect(backingSize(400, 3)).toBe(800)
    expect(backingSize(400, 0)).toBe(400)
    expect(backingSize(0, 2)).toBe(1)
  })
})

describe('toShaderSpace', () => {
  it('maps the centre to the origin and the top edge to +0.5', () => {
    expect(toShaderSpace(200, 100, 400, 200)).toEqual([0, 0])
    expect(toShaderSpace(200, 0, 400, 200)).toEqual([0, 0.5])
  })

  it('returns the origin for an empty canvas', () => {
    expect(toShaderSpace(10, 10, 0, 0)).toEqual([0, 0])
  })
})
