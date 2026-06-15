import { createBrowserRouter, Navigate } from 'react-router-dom'
import { AppShell } from '@/layouts/AppShell'
import { EmailListPage } from '@/features/emails/EmailListPage'
import { EmailDetailPage } from '@/features/emails/EmailDetailPage'
import { SimulatorPage } from '@/features/simulator/SimulatorPage'
import { HumanReviewQueuePage } from '@/features/reviews/HumanReviewQueuePage'
import { TaxonomyQueuePage } from '@/features/taxonomy/TaxonomyQueuePage'
import { DashboardPage } from '@/features/dashboard/DashboardPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppShell />,
    children: [
      { index: true, element: <Navigate to="/inbox" replace /> },
      { path: 'inbox', element: <EmailListPage /> },
      { path: 'inbox/:id', element: <EmailDetailPage /> },
      { path: 'simulator', element: <SimulatorPage /> },
      { path: 'reviews', element: <HumanReviewQueuePage /> },
      { path: 'taxonomy', element: <TaxonomyQueuePage /> },
      { path: 'dashboard', element: <DashboardPage /> },
    ],
  },
])
