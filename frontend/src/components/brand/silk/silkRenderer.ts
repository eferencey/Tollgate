// The WebGL side of the silk background: owns one canvas, one shader program and the animation loop; knows nothing of React.
// Draws a single still frame (no loop) under prefers-reduced-motion, and pauses while off screen or in a hidden tab.
import { backingSize, hexToRgb, toShaderSpace, type Rgb } from './silkMath'
import { FRAGMENT_SHADER, VERTEX_SHADER } from './silkShader'

/** Shadow, body, fold and highlight colours, darkest to lightest. */
export type SilkPalette = [Rgb, Rgb, Rgb, Rgb]

interface SilkRenderer {
  /** Draws once more; used when the palette changed while the loop is not running. */
  redraw: () => void
  dispose: () => void
}

const INTRO_SECONDS = 1.6
const MAX_FRAME_SECONDS = 0.05 // a long pause (tab switch) must not make the cloth jump
const POINTER_EASE = 0.06
const HOVER_EASE = 0.05
const CLICK_NEVER = -100_000 // "a very long time ago", so no ripple is visible before the first click

export function readSilkPalette(style: CSSStyleDeclaration): SilkPalette {
  const read = (name: string) => hexToRgb(style.getPropertyValue(name))
  return [read('--silk-0'), read('--silk-1'), read('--silk-2'), read('--silk-3')]
}

function compile(gl: WebGLRenderingContext, type: number, source: string): WebGLShader | null {
  const shader = gl.createShader(type)
  if (!shader) return null
  gl.shaderSource(shader, source)
  gl.compileShader(shader)
  if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
    console.warn('silk shader:', gl.getShaderInfoLog(shader))
    gl.deleteShader(shader)
    return null
  }
  return shader
}

/** Returns null when WebGL is unavailable; the caller then keeps its flat fallback background. */
export function createSilkRenderer(
  holder: HTMLElement,
  visibilityTarget: HTMLElement,
  palette: { current: SilkPalette },
): SilkRenderer | null {
  // A fresh canvas per mount: a lost context cannot be reused, and React StrictMode mounts twice in development.
  const canvas = document.createElement('canvas')
  canvas.className = 'absolute inset-0 size-full'
  holder.appendChild(canvas)

  const gl = canvas.getContext('webgl', { antialias: false, alpha: false, powerPreference: 'high-performance' })
  const vertex = gl && compile(gl, gl.VERTEX_SHADER, VERTEX_SHADER)
  const fragment = gl && compile(gl, gl.FRAGMENT_SHADER, FRAGMENT_SHADER)
  const program = gl?.createProgram()
  if (!gl || !vertex || !fragment || !program) {
    canvas.remove()
    return null
  }

  gl.attachShader(program, vertex)
  gl.attachShader(program, fragment)
  gl.linkProgram(program)
  gl.useProgram(program)

  // One oversized triangle covers the whole canvas.
  const buffer = gl.createBuffer()
  gl.bindBuffer(gl.ARRAY_BUFFER, buffer)
  gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 3, -1, -1, 3]), gl.STATIC_DRAW)
  const position = gl.getAttribLocation(program, 'p')
  gl.enableVertexAttribArray(position)
  gl.vertexAttribPointer(position, 2, gl.FLOAT, false, 0, 0)

  const uniform = (name: string) => gl.getUniformLocation(program, name)
  const U = {
    res: uniform('uRes'), time: uniform('uTime'), mouse: uniform('uMouse'), hover: uniform('uHover'), intro: uniform('uIntro'),
    c0: uniform('uC0'), c1: uniform('uC1'), c2: uniform('uC2'), c3: uniform('uC3'),
    click: uniform('uClick'), clickAge: uniform('uClickAge'),
  }

  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)')
  // Measured on resize only; the frame loop just reads these numbers.
  const box = { width: 1, height: 1, left: 0, top: 0 }
  const pointer = { x: 0, y: 0, targetX: 0, targetY: 0, hover: 0, targetHover: 0 }
  const click = { x: 9, y: 9, at: CLICK_NEVER }
  const startedAt = performance.now()
  let frame = 0
  let running = false
  let onScreen = true
  let time = 0
  let lastFrameAt = startedAt

  const draw = (now: number) => {
    const reduced = reduceMotion.matches
    const delta = Math.min(MAX_FRAME_SECONDS, (now - lastFrameAt) / 1000)
    lastFrameAt = now
    if (!reduced) time += delta

    const intro = reduced ? 1 : Math.min(1, (now - startedAt) / 1000 / INTRO_SECONDS)
    pointer.x += (pointer.targetX - pointer.x) * POINTER_EASE
    pointer.y += (pointer.targetY - pointer.y) * POINTER_EASE
    pointer.hover += (pointer.targetHover - pointer.hover) * HOVER_EASE

    const [c0, c1, c2, c3] = palette.current
    gl.uniform2f(U.res, canvas.width, canvas.height)
    gl.uniform1f(U.time, time + 11) // the offset only picks a nicer starting fold
    gl.uniform2f(U.mouse, pointer.x, pointer.y)
    gl.uniform1f(U.hover, reduced ? 0 : pointer.hover)
    gl.uniform1f(U.intro, 1 - Math.pow(1 - intro, 3))
    gl.uniform3f(U.c0, ...c0)
    gl.uniform3f(U.c1, ...c1)
    gl.uniform3f(U.c2, ...c2)
    gl.uniform3f(U.c3, ...c3)
    gl.uniform2f(U.click, click.x, click.y)
    gl.uniform1f(U.clickAge, (now - click.at) / 1000)
    gl.drawArrays(gl.TRIANGLES, 0, 3)
  }

  const loop = (now: number) => {
    draw(now)
    frame = running ? requestAnimationFrame(loop) : 0
  }

  const syncLoop = () => {
    const shouldRun = onScreen && !document.hidden && !reduceMotion.matches
    if (shouldRun && !running) {
      running = true
      lastFrameAt = performance.now()
      frame = requestAnimationFrame(loop)
    } else if (!shouldRun && running) {
      running = false
      cancelAnimationFrame(frame)
      draw(performance.now())
    }
  }

  const measure = () => {
    const rect = canvas.getBoundingClientRect()
    Object.assign(box, { width: rect.width, height: rect.height, left: rect.left, top: rect.top })
    canvas.width = backingSize(rect.width, window.devicePixelRatio)
    canvas.height = backingSize(rect.height, window.devicePixelRatio)
    gl.viewport(0, 0, canvas.width, canvas.height)
    if (!running) draw(performance.now())
  }

  const locate = (event: PointerEvent) => toShaderSpace(event.clientX - box.left, event.clientY - box.top, box.width, box.height)
  const onMove = (event: PointerEvent) => {
    const [x, y] = locate(event)
    pointer.targetHover = 1
    pointer.targetX = x
    pointer.targetY = y
  }
  const onLeave = () => {
    pointer.targetHover = 0
  }
  const onDown = (event: PointerEvent) => {
    const [x, y] = locate(event)
    click.x = x
    click.y = y
    click.at = performance.now()
  }

  const resizeObserver = new ResizeObserver(measure)
  resizeObserver.observe(canvas)
  const visibilityObserver = new IntersectionObserver(([entry]) => {
    onScreen = entry.isIntersecting
    syncLoop()
  })
  visibilityObserver.observe(visibilityTarget)
  document.addEventListener('visibilitychange', syncLoop)
  reduceMotion.addEventListener('change', syncLoop)
  // The cloth sits behind the whole page, so the page content (not the canvas) receives the pointer: listen on the window.
  window.addEventListener('pointermove', onMove)
  window.addEventListener('pointerdown', onDown)
  document.documentElement.addEventListener('pointerleave', onLeave)
  measure()
  syncLoop()

  return {
    redraw: () => {
      if (!running) draw(performance.now())
    },
    dispose: () => {
      running = false
      cancelAnimationFrame(frame)
      resizeObserver.disconnect()
      visibilityObserver.disconnect()
      document.removeEventListener('visibilitychange', syncLoop)
      reduceMotion.removeEventListener('change', syncLoop)
      window.removeEventListener('pointermove', onMove)
      window.removeEventListener('pointerdown', onDown)
      document.documentElement.removeEventListener('pointerleave', onLeave)
      gl.deleteBuffer(buffer)
      gl.deleteProgram(program)
      gl.deleteShader(vertex)
      gl.deleteShader(fragment)
      gl.getExtension('WEBGL_lose_context')?.loseContext()
      canvas.remove()
    },
  }
}
