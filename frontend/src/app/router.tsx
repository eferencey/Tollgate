// Route table: every page of the app is declared here, all inside the shared AppShell.
import { createBrowserRouter } from 'react-router'
import { DesignPage } from '@/features/design/DesignPage'
import { HistoryPage } from '@/features/history/components/HistoryPage'
import { ScanPage } from '@/features/scan/components/ScanPage'
import { AppShell } from './AppShell'
import { NotFoundPage } from './NotFoundPage'

export const router = createBrowserRouter([
  {
    element: <AppShell />,
    children: [
      { index: true, element: <ScanPage /> },
      { path: 'history', element: <HistoryPage /> },
      { path: 'design', element: <DesignPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
  // The app may be served under a sub-path (GitHub Pages: /tollgate/); Vite's base is the single source for it.
], { basename: import.meta.env.BASE_URL })
