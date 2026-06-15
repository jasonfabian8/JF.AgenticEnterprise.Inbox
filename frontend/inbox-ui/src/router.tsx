import { createBrowserRouter, Navigate } from 'react-router-dom'
import { AppShell } from '@/layouts/AppShell'
import { EmailListPage } from '@/features/emails/EmailListPage'
import { EmailDetailPage } from '@/features/emails/EmailDetailPage'
import { SimulatorPage } from '@/features/simulator/SimulatorPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppShell />,
    children: [
      { index: true, element: <Navigate to="/inbox" replace /> },
      { path: 'inbox', element: <EmailListPage /> },
      { path: 'inbox/:id', element: <EmailDetailPage /> },
      { path: 'simulator', element: <SimulatorPage /> },
      {
        path: 'dashboard',
        element: (
          <div className="flex items-center justify-center py-24 text-sm text-gray-400">
            Dashboard — coming in Sprint 1
          </div>
        ),
      },
    ],
  },
])
