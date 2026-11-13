// Small pure helpers for the silk background: colour parsing and the coordinate maths the shader expects.

export type Rgb = [number, number, number]

/** "#rrggbb" or "#rgb" to 0-1 RGB; anything unreadable becomes black. */
export function hexToRgb(hex: string): Rgb {
  let digits = hex.replace('#', '').trim()
  if (digits.length === 3) digits = digits[0] + digits[0] + digits[1] + digits[1] + digits[2] + digits[2]
  const value = parseInt(digits.slice(0, 6), 16)
  if (Number.isNaN(value) || digits.length < 6) return [0, 0, 0]
  return [((value >> 16) & 255) / 255, ((value >> 8) & 255) / 255, (value & 255) / 255]
}

/** Canvas backing size: CSS size times the device pixel ratio, capped at 2 so big screens do not cost 4x the pixels. */
export function backingSize(cssSize: number, devicePixelRatio: number): number {
  return Math.max(1, Math.round(cssSize * Math.min(2, Math.max(1, devicePixelRatio || 1))))
}

/** Pointer position in shader space: centred, y pointing up, the canvas height spanning -0.5..0.5. */
export function toShaderSpace(x: number, y: number, width: number, height: number): [number, number] {
  if (width <= 0 || height <= 0) return [0, 0]
  return [(x - width / 2) / height, (height / 2 - y) / height]
}
