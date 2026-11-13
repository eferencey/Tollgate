// Vite build/dev-server configuration: React, Tailwind, the "@/" import alias and the dev proxy to the backend.
import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Backend address of `dotnet run --project src/Tollgate.Api` (see "Getting started" in README.md).
const backendUrl = 'http://localhost:5297'

export default defineConfig({
  base: '/tollgate/',
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { '@': path.resolve(import.meta.dirname, 'src') },
  },
  server: {
    // The backend has no CORS policy on purpose. In development the browser talks only to
    // this server and Vite forwards /api calls, so the page and the API share one origin.
    proxy: { '/api': backendUrl },
  },
})
