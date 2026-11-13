// The one typed HTTP client for the Tollgate backend; every request in the app goes through `api`.
import createClient from 'openapi-fetch'
import type { paths } from './schema'

// Relative base URL: in development Vite proxies /api to the backend (see vite.config.ts), so there is no CORS.
export const api = createClient<paths>({ baseUrl: '' })
