# Section 09 — Frontend Architecture

---

## Technology Stack

| Technology | Role |
|------------|------|
| TypeScript | Language — type safety across the entire frontend |
| React 18 | UI framework — component model, hooks, concurrent features |
| Vite | Build tool — fast HMR, optimized production bundling |
| Tailwind CSS | Utility-first styling — no custom CSS files |
| shadcn/ui | Accessible component library built on Radix UI + Tailwind |
| React Flow | Agent workflow graph visualization |
| React Query (TanStack Query v5) | Server state — API calls, caching, background refresh |
| Zustand | Client state — real-time UI state, SignalR event stream |
| @microsoft/signalr | SignalR client — real-time agent event subscription |
| React Router v6 | Client-side routing |
| Zod | Schema validation for API request payloads |
| date-fns | Date formatting |

---

## Application Structure

```
frontend/
│
├── public/
│   └── favicon.svg
│
├── src/
│   ├── main.tsx                        ← Vite entry point
│   ├── App.tsx                         ← Router root
│   │
│   ├── features/                       ← Feature-based organization
│   │   ├── inbox/                      ← Email list and submission
│   │   │   ├── components/
│   │   │   │   ├── EmailList.tsx
│   │   │   │   ├── EmailListItem.tsx
│   │   │   │   ├── EmailSubmitForm.tsx
│   │   │   │   └── EmailStatusBadge.tsx
│   │   │   ├── hooks/
│   │   │   │   ├── useEmails.ts        ← React Query: GET /emails
│   │   │   │   └── useIngestEmail.ts   ← React Query mutation: POST /emails/ingest
│   │   │   ├── types/
│   │   │   │   └── email.types.ts
│   │   │   └── index.ts
│   │   │
│   │   ├── workflow/                   ← Agent execution visualization
│   │   │   ├── components/
│   │   │   │   ├── WorkflowGraph.tsx   ← React Flow wrapper
│   │   │   │   ├── AgentNode.tsx       ← Custom React Flow node
│   │   │   │   ├── AgentEdge.tsx       ← Animated edge component
│   │   │   │   ├── ReasoningPanel.tsx  ← Per-agent reasoning display
│   │   │   │   ├── ConflictBadge.tsx
│   │   │   │   └── ConfidenceGauge.tsx
│   │   │   ├── hooks/
│   │   │   │   └── useWorkflowGraph.ts ← Builds React Flow nodes/edges from events
│   │   │   ├── types/
│   │   │   │   └── workflow.types.ts
│   │   │   └── index.ts
│   │   │
│   │   ├── review/                     ← Human review queue
│   │   │   ├── components/
│   │   │   │   ├── ReviewQueue.tsx
│   │   │   │   ├── ReviewQueueItem.tsx
│   │   │   │   ├── ReviewDetail.tsx
│   │   │   │   ├── InvoiceReviewForm.tsx
│   │   │   │   ├── ContractReviewPanel.tsx
│   │   │   │   ├── RiskFlagList.tsx
│   │   │   │   ├── FieldEditor.tsx     ← Inline field correction with confidence color
│   │   │   │   └── DecisionPanel.tsx   ← Approve/Reject/Escalate actions
│   │   │   ├── hooks/
│   │   │   │   ├── useReviewQueue.ts
│   │   │   │   └── useSubmitDecision.ts
│   │   │   ├── types/
│   │   │   │   └── review.types.ts
│   │   │   └── index.ts
│   │   │
│   │   ├── taxonomy/                   ← Taxonomy management
│   │   │   ├── components/
│   │   │   │   ├── TaxonomyBrowser.tsx
│   │   │   │   ├── CategoryCard.tsx
│   │   │   │   ├── ProposalCard.tsx
│   │   │   │   └── ProposalApprovalModal.tsx
│   │   │   ├── hooks/
│   │   │   │   ├── useTaxonomyCategories.ts
│   │   │   │   ├── useTaxonomyProposals.ts
│   │   │   │   └── useApproveProposal.ts
│   │   │   └── index.ts
│   │   │
│   │   └── dashboard/                  ← Real-time metrics overview
│   │       ├── components/
│   │       │   ├── Dashboard.tsx
│   │       │   ├── MetricsBar.tsx
│   │       │   ├── CategoryDistributionChart.tsx
│   │       │   ├── ActiveAgentsFeed.tsx
│   │       │   └── RecentEmailsFeed.tsx
│   │       ├── hooks/
│   │       │   └── useDashboardSummary.ts
│   │       └── index.ts
│   │
│   ├── lib/
│   │   ├── api/
│   │   │   ├── client.ts               ← Axios instance with base URL, interceptors
│   │   │   ├── emails.api.ts
│   │   │   ├── reviews.api.ts
│   │   │   ├── taxonomy.api.ts
│   │   │   └── dashboard.api.ts
│   │   ├── signalr/
│   │   │   ├── signalr-client.ts       ← HubConnection setup + reconnection
│   │   │   └── AgentEventContext.tsx   ← React context provider for SignalR
│   │   └── utils/
│   │       ├── confidence.ts           ← Confidence → color/label helpers
│   │       ├── formatters.ts           ← Date, currency, status formatters
│   │       └── ulid.ts
│   │
│   ├── store/
│   │   ├── useWorkflowStore.ts         ← Zustand: active workflow state + RT events
│   │   ├── useReviewStore.ts           ← Zustand: review queue badge count
│   │   └── useConnectionStore.ts       ← Zustand: SignalR connection status
│   │
│   ├── components/                     ← Shared UI components
│   │   ├── layout/
│   │   │   ├── AppShell.tsx            ← Shell with sidebar navigation
│   │   │   ├── Sidebar.tsx
│   │   │   └── TopBar.tsx
│   │   ├── ui/                         ← shadcn/ui re-exports + custom atoms
│   │   │   ├── ConfidenceBadge.tsx
│   │   │   ├── AgentTypeBadge.tsx
│   │   │   ├── StatusPill.tsx
│   │   │   └── LoadingSpinner.tsx
│   │   └── errors/
│   │       ├── ErrorBoundary.tsx
│   │       └── ApiErrorAlert.tsx
│   │
│   ├── pages/
│   │   ├── DashboardPage.tsx
│   │   ├── InboxPage.tsx
│   │   ├── EmailDetailPage.tsx
│   │   ├── ReviewQueuePage.tsx
│   │   ├── ReviewDetailPage.tsx
│   │   └── TaxonomyPage.tsx
│   │
│   ├── router/
│   │   └── routes.tsx                  ← React Router route definitions
│   │
│   └── types/
│       └── api.types.ts                ← Shared type definitions matching API contracts
│
├── index.html
├── vite.config.ts
├── tailwind.config.ts
├── tsconfig.json
└── package.json
```

---

## Feature Organization

The frontend follows **Feature-Sliced Design** principles adapted for a smaller codebase. Each feature folder is self-contained:

- `components/` — React components specific to this feature
- `hooks/` — React Query hooks and custom hooks for the feature
- `types/` — TypeScript types for the feature's domain objects
- `index.ts` — Public API of the feature (only exported items are used outside)

Cross-feature shared code lives in `lib/` (API clients, SignalR) and `components/` (shared UI atoms).

---

## State Management Strategy

The application uses a two-tier state model:

```
┌─────────────────────────────────────────────────┐
│  Server State — React Query (TanStack Query v5)  │
│                                                   │
│  Manages:                                         │
│  • Email list and email detail (GET /emails)     │
│  • Review queue (GET /reviews/queue)             │
│  • Taxonomy categories and proposals             │
│  • Dashboard summary                             │
│                                                   │
│  Features:                                        │
│  • Automatic background refetch                  │
│  • Cache invalidation on mutations               │
│  • Loading / error / success states              │
│  • Optimistic updates on review decisions        │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│  Client State — Zustand                          │
│                                                   │
│  useWorkflowStore                                │
│  • activeWorkflowId                              │
│  • agentEvents[]   ← populated by SignalR        │
│  • graphNodes[]    ← derived from agentEvents    │
│  • graphEdges[]    ← derived from agentEvents    │
│  • conflictEvent                                 │
│                                                   │
│  useReviewStore                                  │
│  • pendingReviewCount ← updated by SignalR       │
│                                                   │
│  useConnectionStore                              │
│  • signalRStatus (connected/reconnecting/failed) │
└─────────────────────────────────────────────────┘
```

---

## Component Hierarchy

### WorkflowGraph (React Flow)

The agent visualization graph is the centrepiece of the UI. It renders the agent collaboration as a live directed graph.

```
WorkflowGraph
├── ReactFlow (provider)
│   ├── AgentNode (x7 — one per agent type)
│   │   ├── AgentIcon
│   │   ├── AgentLabel
│   │   ├── StatusIndicator  (idle/active/completed/failed)
│   │   └── ConfidenceGauge  (shown when completed)
│   ├── AgentEdge (animated, directional)
│   │   └── EdgeLabel (message summary)
│   └── ConflictOverlay (shown when conflict detected)
│
└── ReasoningPanel (side panel, expands on node click)
    ├── AgentName
    ├── ConfidenceBadge
    ├── ReasoningText
    └── ExecutionTimestamp
```

**React Flow Node Layout:** The 7 agents are arranged in a fixed layout that mirrors the orchestration hierarchy:

```
[Orchestrator]
      │
   ┌──┴──┐
[Class] [DocUnd]
             │
          ┌──┴──┐
        [Inv]  [Con]
             │
         [TaxEvo]
             │
         [HumanCollab]
```

Edges animate (dashed flow animation) when an agent is actively being invoked. Completed nodes turn green; failed nodes turn red.

---

## Real-Time Event Handling

SignalR events flow from the server into the Zustand store and trigger React re-renders. The event handling pipeline:

```
SignalR Server Message
        ↓
AgentEventContext (React Context)
  → hub.on("agent.started", handler)
  → hub.on("agent.completed", handler)
  → hub.on("conflict.detected", handler)
  → hub.on("review.required", handler)
  → hub.on("workflow.completed", handler)
        ↓
Zustand Store dispatch
  useWorkflowStore.addAgentEvent(event)
        ↓
Derived state computed
  graphNodes = buildNodes(agentEvents)
  graphEdges = buildEdges(agentEvents)
        ↓
React Flow re-renders
  Node status indicator updates
  Edge animation starts/stops
  Confidence gauge populates
```

---

## TypeScript Type Safety Strategy

All API response shapes are defined as TypeScript interfaces in `src/types/api.types.ts`. These are derived from the OpenAPI specification generated by the .NET backend (Swagger). The goal is a single source of truth:

```
Backend: C# response contract
    ↓ (Scalar or NSwag)
OpenAPI spec (swagger.json)
    ↓ (manual types for MVP; generation in Phase 2)
src/types/api.types.ts
    ↓
React Query hooks (typed generics)
    ↓
Component props (typed)
```

This chain ensures that a backend contract change that breaks the API manifests as a TypeScript compilation error in the frontend, not a runtime error.
