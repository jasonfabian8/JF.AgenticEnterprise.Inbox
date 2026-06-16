# Software Architecture Document (SAD)
## Agentic Enterprise Inbox
### Version 1.0 — Microsoft Agents League Hackathon

---

| Attribute | Value |
|-----------|-------|
| Project | Agentic Enterprise Inbox |
| Version | 1.0 |
| Status | Draft — Hackathon MVP |
| Date | 2026-06-14 |
| Category | Reasoning Agents |
| Event | Microsoft Agents League Hackathon |

---

## Document Purpose

This Software Architecture Document (SAD) serves as the primary architecture reference for the Agentic Enterprise Inbox platform. It is intended for:

- **Executive Review** — Business context, expected benefits, and strategic fit
- **Technical Review** — Architecture decisions, component design, and integration patterns
- **Development Onboarding** — Project structure, coding conventions, and implementation guidance
- **Hackathon Documentation** — Demo architecture, MVP scope, and delivery roadmap

---

## Section Index

| # | Section | File | Audience |
|---|---------|------|----------|
| 01 | Executive Summary | [01-executive-summary.md](01-executive-summary.md) | All |
| 02 | Architectural Drivers | [02-architectural-drivers.md](02-architectural-drivers.md) | Technical, Executive |
| 03 | Architectural Principles | [03-architectural-principles.md](03-architectural-principles.md) | Technical |
| 04 | Solution Overview | [04-solution-overview.md](04-solution-overview.md) | All |
| 05 | Logical Architecture | [05-logical-architecture.md](05-logical-architecture.md) | Technical |
| 06 | Multi-Agent Architecture | [06-multi-agent-architecture.md](06-multi-agent-architecture.md) | Technical |
| 07 | Domain Model | [07-domain-model.md](07-domain-model.md) | Technical |
| 08 | Backend Architecture | [08-backend-architecture.md](08-backend-architecture.md) | Development |
| 09 | Frontend Architecture | [09-frontend-architecture.md](09-frontend-architecture.md) | Development |
| 10 | API Architecture | [10-api-architecture.md](10-api-architecture.md) | Development, Technical |
| 11 | Real-Time Communication | [11-realtime-communication.md](11-realtime-communication.md) | Development |
| 12 | Data Architecture | [12-data-architecture.md](12-data-architecture.md) | Technical, Development |
| 13 | Security Architecture | [13-security-architecture.md](13-security-architecture.md) | Technical, Executive |
| 14 | Observability Architecture | [14-observability-architecture.md](14-observability-architecture.md) | Technical |
| 15 | Deployment Architecture | [15-deployment-architecture.md](15-deployment-architecture.md) | Technical |
| 16 | Architectural Decisions (ADR) | [16-architectural-decisions.md](16-architectural-decisions.md) | Technical |
| 17 | MVP Architecture | [17-mvp-architecture.md](17-mvp-architecture.md) | All |
| 18 | Development Roadmap | [18-development-roadmap.md](18-development-roadmap.md) | All |
| 19 | Risks and Mitigations | [19-risks-and-mitigations.md](19-risks-and-mitigations.md) | All |
| 20 | Final Architecture Recommendation | [20-final-recommendation.md](20-final-recommendation.md) | All |

---

## Technology Stack at a Glance

```
┌─────────────────────────────────────────────────────┐
│  Frontend                                           │
│  TypeScript · React · Vite · Tailwind CSS           │
│  shadcn/ui · React Flow                             │
├─────────────────────────────────────────────────────┤
│  API Layer                                          │
│  ASP.NET Core Minimal APIs (.NET 10)                │
│  SignalR · REST                                     │
├─────────────────────────────────────────────────────┤
│  Agent Layer                                        │
│  Microsoft Semantic Kernel · Azure OpenAI           │
│  7 Specialized Agents                               │
├─────────────────────────────────────────────────────┤
│  Persistence                                        │
│  SQLite (MVP) → PostgreSQL / SQL Server (Phase 2)  │
├─────────────────────────────────────────────────────┤
│  Observability                                      │
│  OpenTelemetry · Serilog · Application Insights     │
├─────────────────────────────────────────────────────┤
│  Infrastructure                                     │
│  Azure App Service · Azure OpenAI · Blob Storage    │
└─────────────────────────────────────────────────────┘
```

---

## Agent Workforce at a Glance

| Agent | Role | Invocation |
|-------|------|------------|
| Orchestrator | Workflow coordination, state management | Always first |
| Classification | Email type + confidence scoring | Every email |
| Document Understanding | Attachment analysis + routing | When attachments present |
| Invoice | Invoice extraction + validation | When invoice detected |
| Contract | Contract analysis + risk flagging | When contract detected |
| Taxonomy Evolution | New category detection + proposals | Background, always |
| Human Collaboration | Escalation, review tasks, approvals | When confidence low or flags raised |
