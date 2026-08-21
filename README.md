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

### For students
- Registration and login, personal dashboard
- Weekly timetable and calendar with tests, exams and deadlines
- Access to learning materials by subject
- Homework submission (files + text) with status tracking
- Grades and performance tracking per subject over time
- Online tests with a **server-side timer** and **randomized ordering** of questions and answers
- Result statistics plus **AI feedback** on what to focus on
- AI assistant for explaining difficult topics and summarizing lessons
- Notifications (in-app, e-mail, push) and teacher remarks

### For teachers
- Create classes and enroll students
- Upload and organize learning materials (folders, PDFs, links, video)
- Create homework assignments with deadlines and point values
- Review and grade submitted homework
- Build online tests from a question bank
- **AI question generation** from uploaded material (teacher reviews and approves)
- Record grades, absences and remarks
- Message an individual student or parent, or post to the class feed

### For parents
- Track their child's grades, absences and remarks
- Notifications on new grades, absences or teacher messages
- Direct communication with teachers
- AI helper: explain difficult topics, generate practice questions, summarize PDF files

---

## Technology stack

| Layer | Technology | Role |
|---|---|---|
| Frontend | **Angular 22** + Angular Material, NgRx Signals | SPA, state management |
| Backend | **ASP.NET Core 10** Web API (modular monolith) | REST API, business logic |
| ORM | **EF Core 10** + Npgsql | Data access, migrations |
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
| Testing | xUnit + Shouldly + Testcontainers + Playwright | Unit, integration, E2E |

Full rationale for every choice is in [docs-architecture/ARCHITECTURE.md](docs-architecture/ARCHITECTURE.md#1-technology-choices--rationale).

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

📄 Diagrams, module boundaries and the data model: [docs-architecture/ARCHITECTURE.md](docs-architecture/ARCHITECTURE.md)

---

## Getting started

### Prerequisites
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org) and `npm`
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (enable Kubernetes for the deployment stage)

### 1. Infrastructure

```bash
docker compose up -d
```

Starts PostgreSQL, Redis, MinIO, Mailpit and Seq. Wait until `docker compose ps` reports every
service as `healthy` before continuing.

> There is no database migration step yet. The modules define no entities before Phase 1, so there
> is nothing to migrate — the schema is created by the extension script in
> `deploy/docker/postgres/` when the volume is first initialised.

### 2. Backend

```bash
dotnet run --project src/EduPlatform.Api
```

Listens on `https://localhost:5001` and `http://localhost:5000`. Scalar UI opens automatically at
`https://localhost:5001/scalar`.

### 3. Frontend

```bash
npm --prefix src/web install
```

```bash
npm --prefix src/web start
```

App: `http://localhost:4200`. The dev server proxies `/api` and `/health` to `http://localhost:5000`,
so the browser sees a single origin.

### 4. Development data (optional)

```bash
dotnet run --project src/EduPlatform.Api -- --seed
```

Populates the development data set and exits without serving traffic. Seeders are idempotent, so
this is safe to re-run. No module registers a seeder before Phase 1, so today it reports
`No data seeders are registered` and exits — the mechanism is wired, there is simply nothing to
populate yet.

### Local endpoints

| Service | Address | Credentials |
|---|---|---|
| Angular | http://localhost:4200 | — |
| API | https://localhost:5001 · http://localhost:5000 | — |
| API docs (Scalar) | https://localhost:5001/scalar | — |
| Health (readiness) | http://localhost:5000/health/ready | — |
| PostgreSQL | localhost:**5433** | `eduplatform` / `dev` |
| MinIO console | http://localhost:9001 | `minioadmin` / `minioadmin` |
| Mailpit (e-mail) | http://localhost:8025 | — |
| Seq (logs) | http://localhost:5341 | — |

> PostgreSQL is published on **5433**, not the default 5432, because a locally installed PostgreSQL
> service commonly already owns 5432. Change `POSTGRES_PORT` in `.env` if that does not apply to you.

---

## Configuration

Two separate files, with different jobs:

**`.env`** — read by Docker Compose only. Copy it from the template; the defaults work as-is.

```bash
cp .env.example .env
```

**`src/EduPlatform.Api/appsettings.json`** — read by the API. It already points at the containers
above, so no edit is needed to run locally:

```
ConnectionStrings:Postgres   Host=localhost;Port=5433;Database=eduplatform;…
ConnectionStrings:Redis      localhost:6379
Storage:Endpoint             localhost:9000
Cors:AllowedOrigins          [ "http://localhost:4200" ]
Serilog:WriteTo              Console + Seq at http://localhost:5341
```

Secrets arrive in later phases and belong in .NET User Secrets, never in `appsettings.json`:

```bash
dotnet user-secrets set "Jwt:Key" "<generated 256-bit key>" --project src/EduPlatform.Api
```

| Secret | Needed from | Purpose |
|---|---|---|
| `Jwt:Key` | Phase 1 | Signing key for access tokens |
| `Anthropic:ApiKey` | Phase 8 | Claude API access |

> The Claude API key **never** reaches the frontend. Angular only calls our own backend; the backend calls the Claude API.

---

## Repository layout

```
├── docs-architecture/              Documentation
│   ├── ARCHITECTURE.md             Architecture, data model, API
│   └── phase-0-report.pdf          What Phase 0 delivered and how
├── src/
│   ├── EduPlatform.Api/            Host: DI, middleware, endpoints, health probes
│   ├── BuildingBlocks/             Shared kernel
│   │   ├── …Domain/                Entity, AggregateRoot, ValueObject, domain events
│   │   ├── …Application/           CQRS dispatcher, validation & logging behaviours
│   │   ├── …Events/                Domain event dispatch
│   │   └── …Infrastructure/        ModuleDbContext, EF interceptors, clock
│   ├── Modules/
│   │   └── Identity/               Domain · Application · Infrastructure · Contracts (Phase 1)
│   └── web/                        Angular application
├── tests/
│   ├── EduPlatform.ArchitectureTests/    NetArchTest — module boundary rules
│   └── EduPlatform.Api.IntegrationTests/ API pipeline + PostgreSQL via Testcontainers
├── deploy/
│   └── docker/postgres/            Extension bootstrap script
├── .github/workflows/ci.yml        Build, test, dependency audit
├── docker-compose.yml
├── Directory.Build.props           Shared build settings
└── Directory.Packages.props        Central package versions
```

`deploy/helm/` and `deploy/k8s/` arrive in Phase 9; there is no Playwright project before Phase 9 either.

---

## Testing

```bash
dotnet test EduPlatform.slnx
```

```bash
npm --prefix src/web test -- --watch=false
```

| Suite | Count | What it covers | Needs Docker |
|---|:--:|---|:--:|
| Architecture | 5 | Module boundaries enforced mechanically with NetArchTest — a violation fails the build | no |
| API pipeline | 5 | The real request pipeline through `WebApplicationFactory`: routing, correlation id, Problem Details, liveness probe | no |
| Database | 4 | A real PostgreSQL 17 server started by Testcontainers: connectivity, `pgvector` distance operator, `pg_trgm`, per-module schema isolation | **yes** |
| Seeding | 4 | Seeder discovery through DI, execution order, and that an empty registration is valid | no |
| Frontend | 1 | Application bootstraps (Vitest, jsdom) | no |

The database tests use the same `pgvector/pgvector:pg17` image as `docker-compose.yml`, so they
exercise the extensions the application actually depends on. No in-memory database substitute is used.

---

## Development plan

The work is split into **10 phases plus the defense**, spanning the 2026/2027 academic year — from infrastructure to a finished product running on Kubernetes. The phase breakdown is tracked separately.

---

## License

[MIT](LICENSE)
