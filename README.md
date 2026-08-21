# EduPlatform

> **A web platform for managing and supporting the educational process**
> Diploma thesis project · Bringing students, teachers and parents together in one digital environment.

*(`EduPlatform` is a working name and can be changed before the first code commit.)*

---

## What this is

A modern educational platform designed as an improved alternative to the Bulgarian Ministry of Education's **Digital Backpack** (*Дигитална раница*). On top of the standard features — materials, homework, grades, timetable — the platform adds an **integrated AI assistant** that summarizes lessons, generates tests from the teaching material, and gives students personalized feedback.

**Key difference from existing solutions:** AI answers are grounded in **the materials uploaded by the student's own teacher** (Retrieval-Augmented Generation), not in the language model's general knowledge. A student gets an explanation of *their* lesson, not of some arbitrary source.

---

## Features

### 👨‍🎓 For students
- Registration and login, personal dashboard
- Weekly timetable and calendar with tests, exams and deadlines
- Access to learning materials by subject
- Homework submission (files + text) with status tracking
- Grades and performance tracking per subject over time
- Online tests with a **server-side timer** and **randomized ordering** of questions and answers
- Result statistics plus **AI feedback** on what to focus on
- AI assistant for explaining difficult topics and summarizing lessons
- Notifications (in-app, e-mail, push) and teacher remarks

### 👩‍🏫 For teachers
- Create classes and enroll students
- Upload and organize learning materials (folders, PDFs, links, video)
- Create homework assignments with deadlines and point values
- Review and grade submitted homework
- Build online tests from a question bank
- **AI question generation** from uploaded material (teacher reviews and approves)
- Record grades, absences and remarks
- Message an individual student or parent, or post to the class feed

### 👨‍👩‍👧 For parents
- Track their child's grades, absences and remarks
- Notifications on new grades, absences or teacher messages
- Direct communication with teachers
- AI helper: explain difficult topics, generate practice questions, summarize PDF files

---

## Technology stack

| Layer | Technology | Role |
|---|---|---|
| Frontend | **Angular 20** + Angular Material, NgRx Signals | SPA, state management |
| Backend | **ASP.NET Core 9** Web API (modular monolith) | REST API, business logic |
| ORM | **EF Core 9** + Npgsql | Data access, migrations |
| Database | **PostgreSQL 17** + `pgvector` | Relational data + vector search |
| Cache | **Redis** | Sessions, caching, SignalR backplane |
| Real-time | **SignalR** | Notifications, class feed, test synchronization |
| File storage | **MinIO** (S3-compatible) | Materials, submissions, attachments |
| Authentication | ASP.NET Core Identity + **JWT** (access + refresh) | Roles: Student / Teacher / Parent / Admin |
| Background jobs | **Hangfire** | Reminders, nightly AI summaries, reports |
| AI | **Claude API** (`claude-opus-5`, `claude-haiku-4-5`) | Summaries, test generation, feedback |
| Embeddings | `bge-m3` (self-hosted, multilingual) | Vectorizing materials for RAG |
| Push | Firebase Cloud Messaging | Out-of-app notifications |
| Observability | OpenTelemetry + Prometheus + Grafana + Serilog | Metrics, logs, tracing |
| Containerization | **Docker** / Docker Compose | Local environment |
| Orchestration | **Kubernetes** + Helm | Production deployment |
| CI/CD | GitHub Actions → GHCR | Build, test, images |
| Testing | xUnit + Testcontainers + Playwright | Unit, integration, E2E |

📄 Full rationale for every choice is in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#1-technology-choices--rationale).

---

## Architecture at a glance

A **modular monolith**, not microservices. One deployable API split into independent modules with explicit boundaries. Each module owns its PostgreSQL schema and talks to the others only through public contracts and domain events.

```
Angular SPA
     │ HTTPS / REST + WebSocket
     ▼
ASP.NET Core API ──► PostgreSQL 17 (+pgvector)
     │           ──► Redis
     │           ──► MinIO
     └──────────────► Claude API
```

Modules: `Identity` · `SchoolStructure` · `Schedule` · `Content` · `Assignments` · `Assessment` · `Gradebook` · `Communication` · `Intelligence` · `Analytics`

📄 Diagrams, module boundaries and the data model: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)

---

## Getting started

### Prerequisites
- [.NET SDK 9](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org) and `npm`
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (enable Kubernetes for the deployment stage)

### 1. Infrastructure

```bash
docker compose up -d
```

Starts PostgreSQL, Redis, MinIO, Mailpit and Seq.

### 2. Database

```bash
dotnet ef database update --project src/EduPlatform.Api
```

### 3. Backend

```bash
dotnet run --project src/EduPlatform.Api
```

API: `https://localhost:5001` · Scalar UI: `https://localhost:5001/scalar`

### 4. Frontend

```bash
npm --prefix src/web install && npm --prefix src/web start
```

App: `http://localhost:4200`

### Local endpoints

| Service | Address | Credentials |
|---|---|---|
| Angular | http://localhost:4200 | — |
| API | https://localhost:5001 | — |
| PostgreSQL | localhost:5432 | `eduplatform` / `dev` |
| MinIO console | http://localhost:9001 | `minioadmin` / `minioadmin` |
| Mailpit (e-mail) | http://localhost:8025 | — |
| Seq (logs) | http://localhost:5341 | — |

---

## Configuration

Copy `.env.example` to `.env` and fill in:

```
ConnectionStrings__Postgres=Host=localhost;Database=eduplatform;Username=eduplatform;Password=dev
ConnectionStrings__Redis=localhost:6379
Storage__Endpoint=localhost:9000
Storage__AccessKey=minioadmin
Storage__SecretKey=minioadmin
Jwt__Issuer=https://localhost:5001
Jwt__Key=<generated 256-bit key>
Anthropic__ApiKey=<key from console.anthropic.com>
```

> ⚠️ The `ANTHROPIC_API_KEY` **never** reaches the frontend. Angular only calls our own backend; the backend calls the Claude API.

---

## Repository layout

```
├── docs/                       Documentation
│   ├── ARCHITECTURE.md         Architecture, data model, API
│   └── ROADMAP.md              Phased development plan
├── src/
│   ├── EduPlatform.Api/        Host: DI, middleware, endpoints, SignalR hubs
│   ├── EduPlatform.Modules.*/  Business modules
│   ├── EduPlatform.BuildingBlocks.*/  Shared infrastructure
│   └── web/                    Angular application
├── tests/
│   ├── *.UnitTests/
│   ├── *.IntegrationTests/     Testcontainers (real PostgreSQL)
│   └── EduPlatform.E2E/        Playwright
├── deploy/
│   ├── helm/                   Helm chart
│   └── k8s/                    Kubernetes manifests
├── .github/workflows/          CI/CD
└── docker-compose.yml
```

---

## Testing

```bash
dotnet test
```

```bash
npm --prefix src/web test
```

Integration tests spin up a real PostgreSQL container via Testcontainers — no in-memory substitutes, so behaviour matches production.

---

## Development plan

The work is split into **10 phases plus the defense**, spanning the 2026/2027 academic year — from infrastructure to a finished product running on Kubernetes. See [docs/ROADMAP.md](docs/ROADMAP.md).

---

## License

[MIT](LICENSE)
