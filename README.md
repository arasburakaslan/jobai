# 🧠 JobRag Platform

**Production-grade RAG (Retrieval-Augmented Generation) Job Intelligence Platform for the German job market — architected for multi-country expansion.**

Built as a modular monolith using Clean Architecture, this platform crawls job listings, generates vector embeddings via OpenAI, and provides hybrid search (vector similarity + full-text BM25) through a REST API backed by PostgreSQL + pgvector. An AI Agents layer powers intelligent features like smart search, cover letter generation, interview prep, and job matching.

> **Current Status:** Scaffold complete, compiles with zero errors. Ready for database migration, real crawler implementation, and frontend integration.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Quick Start with Docker](#quick-start-with-docker)
  - [Local Development (without Docker)](#local-development-without-docker)
- [Configuration](#configuration)
- [API Reference](#api-reference)
- [How It Works](#how-it-works)
  - [Crawling Pipeline](#crawling-pipeline)
  - [Embedding Pipeline](#embedding-pipeline)
  - [Hybrid Search](#hybrid-search)
  - [AI Agents](#ai-agents)
  - [Country Rules Engine](#country-rules-engine)
- [Database Schema](#database-schema)
- [Next Steps / Roadmap](#next-steps--roadmap)
- [Contributing](#contributing)
- [License](#license)

---

## Architecture Overview

```
┌──────────────────────────────────────────────────────────────────┐
│                        Clean Architecture                        │
│                                                                  │
│  ┌────────────┐   ┌──────────────┐   ┌────────────────────────┐ │
│  │   Domain    │◄──│  Application │◄──│    Infrastructure      │ │
│  │  (Entities) │   │ (Interfaces, │   │  (EF Core, pgvector,   │ │
│  │             │   │    DTOs)     │   │   OpenAI, Crawlers,    │ │
│  └────────────┘   └──────────────┘   │   Search)              │ │
│                                      └───────────┬────────────┘ │
│                                                   │              │
│       ┌──────────────┐       ┌────────────────────┼──────┐       │
│       │   Agents     │       │                    │      │       │
│       │ (AI Agents,  │  ┌────┴─────┐       ┌──────┴───┐  │       │
│       │  Workflows,  │  │   API    │       │ Workers  │  │       │
│       │  Guardrails) │  │ (REST)   │       │(Background│  │       │
│       └──────────────┘  │ Port 5000│       │ Services)│  │       │
│                         └────┬─────┘       └──────┬───┘  │       │
│                              │                    │      │       │
│                              └────────┬───────────┘      │       │
│                                       │                  │       │
│                              ┌────────┴────────┐         │       │
│                              │   PostgreSQL    │         │       │
│                              │   + pgvector    │         │       │
│                              │   Port 5432     │         │       │
│                              └─────────────────┘         │       │
│                                                          │       │
└──────────────────────────────────────────────────────────┘       │
                                                                   │
                                         ┌─────────────────────────┘
                                         │
                                    ┌────┴──────────┐
                                    │  OpenAI API   │
                                    │ (Embeddings   │
                                    │  + Chat)      │
                                    └───────────────┘
```

**Design Decisions:**
- **Modular Monolith** — No premature microservices; split later when needed
- **Clean Architecture** — Domain has zero dependencies; Infrastructure depends inward
- **Germany-First** — Country rules engine supports DE now, NL scaffold ready, extensible to any country
- **AI-Native** — Dedicated Agents layer with guardrails for safe, composable AI features

---

## Tech Stack

| Layer | Technology | Purpose |
|---|---|---|
| Runtime | .NET 10.0 | Latest LTS framework |
| API | ASP.NET Core Web API | REST endpoints |
| Database | PostgreSQL 16 + pgvector | Relational + vector storage |
| ORM | Entity Framework Core 10.0 | Data access + migrations |
| Vector Search | pgvector (cosine similarity) | Semantic similarity search |
| Full-Text Search | PostgreSQL `ts_rank` / `tsvector` | BM25-style keyword matching |
| Embeddings | OpenAI `text-embedding-3-small` | 1536-dimensional vectors |
| AI Agents | OpenAI `gpt-4o-mini` | Query rewriting, cover letters, matching |
| Logging | Serilog | Structured logging to console |
| Scheduling | Quartz.NET | Background job scheduling (packages installed) |
| Scraping | HtmlAgilityPack | HTML DOM parsing for crawlers |
| Auth | JWT Bearer | Token-based authentication (scaffolded) |
| Containers | Docker Compose | Local development environment |

---

## Project Structure

```
JobRagPlatform/
├── docker-compose.yml              # PostgreSQL + pgvector, API, Workers
├── .env.example                    # Environment variable template
├── .gitignore                      # Standard .NET ignores
├── NuGet.Config                    # NuGet package source configuration
├── JobRagPlatform.sln              # Solution file
│
└── src/
    ├── Domain/                     # 🏛️  Domain Layer (zero dependencies)
    │   ├── Common/
    │   │   └── BaseEntity.cs       #   Base class: Id (Guid), CreatedAt
    │   ├── Jobs/
    │   │   ├── Job.cs              #   Core job listing entity
    │   │   └── JobEmbedding.cs     #   Vector embedding (1536-dim)
    │   └── Users/
    │       ├── User.cs             #   User entity
    │       ├── UserProfile.cs      #   CV text, CV embedding, preferences
    │       ├── UserSavedJob.cs     #   Bookmarked jobs with match scores
    │       └── JobApplication.cs   #   Job applications with cover letters
    │
    ├── Application/                # 📋  Application Layer (interfaces + DTOs)
    │   ├── Abstractions/
    │   │   ├── IEmbeddingService.cs #   Text → Vector embedding
    │   │   ├── IJobCrawler.cs       #   Crawl job source → RawJob[]
    │   │   ├── IJobNormalizer.cs     #   RawJob → normalized Job
    │   │   ├── IJobDeduplicator.cs   #   URL + hash deduplication
    │   │   ├── IJobMetadataExtractor.cs  # Industry, language, visa, salary
    │   │   ├── IHybridSearchService.cs   # Vector + text search
    │   │   ├── IJobRepository.cs    #   Data access interface
    │   │   └── ICountryRulesService.cs   # Country-specific normalization
    │   └── Features/
    │       ├── Jobs/DTOs/
    │       │   └── RawJob.cs        #   Raw crawled job data
    │       └── Search/DTOs/
    │           ├── SearchRequest.cs  #   Query + filters + pagination
    │           └── SearchResult.cs   #   Results with relevance scores
    │
    ├── Infrastructure/             # 🔧  Infrastructure Layer (implementations)
    │   ├── DependencyInjection.cs   #   Service registration (single entry point)
    │   ├── Persistence/
    │   │   ├── ApplicationDbContext.cs  # EF Core DbContext
    │   │   ├── Configurations/      #   Fluent API entity configurations
    │   │   │   ├── UserConfiguration.cs
    │   │   │   ├── UserProfileConfiguration.cs
    │   │   │   ├── JobConfiguration.cs
    │   │   │   ├── JobEmbeddingConfiguration.cs
    │   │   │   ├── UserSavedJobConfiguration.cs
    │   │   │   └── JobApplicationConfiguration.cs
    │   │   └── Repositories/
    │   │       └── JobRepository.cs #   EF Core job data access
    │   ├── Crawlers/
    │   │   ├── SampleRssCrawler.cs  #   Template RSS feed crawler
    │   │   ├── SampleHtmlCrawler.cs #   Template HTML scraper
    │   │   ├── JobNormalizer.cs     #   Field trimming, country resolution, SHA256
    │   │   ├── JobDeduplicator.cs   #   URL + description hash dedup
    │   │   └── JobMetadataExtractor.cs  # Rule-based metadata extraction
    │   ├── Embeddings/
    │   │   └── OpenAIEmbeddingService.cs  # OpenAI v1/embeddings integration
    │   ├── Search/
    │   │   └── HybridSearchService.cs     # Raw SQL hybrid search engine
    │   └── CountryRules/
    │       ├── GermanyRulesService.cs      # DE city normalization, language
    │       └── NetherlandsRulesService.cs  # NL city normalization
    │
    ├── JobRag.Agents/              # 🤖  AI Agents Layer
    │   ├── DependencyInjection.cs   #   Agent/guardrail/workflow DI registration
    │   ├── Abstractions/
    │   │   ├── IAgent.cs            #   Base agent interface + AgentResult<T>
    │   │   ├── IGuardrail.cs        #   Guardrail interface + GuardrailResult<T>
    │   │   ├── IWorkflow.cs         #   Workflow orchestration interface
    │   │   └── ILlmClient.cs        #   LLM provider abstraction
    │   ├── Agents/
    │   │   ├── QueryRewriteAgent.cs     # NL query → structured search filters
    │   │   ├── CoverLetterAgent.cs      # Job + CV → tailored cover letter
    │   │   ├── InterviewPrepAgent.cs    # Job → likely interview questions
    │   │   ├── JobMatchAgent.cs         # Job + CV → match score breakdown
    │   │   └── JobSummaryAgent.cs       # Long description → structured summary
    │   ├── Guardrails/
    │   │   ├── PiiGuardrail.cs          # Detects/blocks PII in output
    │   │   ├── HallucinationGuardrail.cs # Catches fabricated qualifications
    │   │   ├── ContentLengthGuardrail.cs # Enforces word count bounds
    │   │   └── CostBudgetGuardrail.cs   # Rate limiting + token budgets
    │   └── Workflows/
    │       ├── JobApplicationWorkflow.cs # Match → CoverLetter → Guardrails
    │       └── SmartSearchWorkflow.cs    # QueryRewrite → Search → Summarise
    │
    ├── Api/                        # 🌐  API Layer (HTTP entry point)
    │   ├── Program.cs              #   App bootstrap, Serilog, CORS, pipeline
    │   ├── Controllers/
    │   │   ├── SearchController.cs  #   POST /api/search
    │   │   ├── JobsController.cs    #   GET /api/jobs/{id}
    │   │   └── HealthController.cs  #   GET /health
    │   ├── appsettings.json         #   Configuration
    │   └── Dockerfile               #   Multi-stage Docker build
    │
    └── Workers/                    # ⚙️  Background Workers
        ├── Program.cs              #   Worker host bootstrap
        ├── EmbeddingWorker.cs      #   Polls every 5 min, batch-embeds jobs
        ├── CrawlingWorker.cs       #   Runs every 6 hours, orchestrates crawlers
        ├── appsettings.json        #   Configuration
        └── Dockerfile              #   Multi-stage Docker build
```

---

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL + pgvector)
- [OpenAI API Key](https://platform.openai.com/api-keys) (for embeddings + agents)
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

### Quick Start with Docker

```bash
# 1. Clone the repository
git clone <your-repo-url>
cd JobRagPlatform

# 2. Set up environment variables
cp .env.example .env
# Edit .env and add your OPENAI_API_KEY

# 3. Start everything (PostgreSQL + API + Workers)
docker compose up -d

# 4. Check health
curl http://localhost:5000/health
```

### Local Development (without Docker for app, Docker for DB)

```bash
# 1. Start only PostgreSQL with pgvector
docker compose up -d postgres

# 2. Set up environment variables
cp .env.example .env
# Edit .env and add your OPENAI_API_KEY

# 3. Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# 4. Create and apply the initial database migration
dotnet ef migrations add InitialCreate -p src/Infrastructure -s src/Api
dotnet ef database update -p src/Infrastructure -s src/Api

# 5. Run the API (in terminal 1)
dotnet run --project src/Api

# 6. Run the Workers (in terminal 2)
dotnet run --project src/Workers

# 7. Build the solution (verify everything compiles)
dotnet build
```

---

## Configuration

All configuration is in `appsettings.json` (overridable via environment variables):

| Setting | Default | Description |
|---|---|---|
| `ConnectionStrings:Default` | `Host=localhost;Port=5432;Database=jobragdb;Username=jobrag;Password=jobrag` | PostgreSQL connection string |
| `OpenAI:ApiKey` | *(empty)* | Your OpenAI API key |
| `OpenAI:BaseUrl` | `https://api.openai.com/` | OpenAI API base URL (change for Azure OpenAI) |
| `OpenAI:EmbeddingModel` | `text-embedding-3-small` | Embedding model (1536 dimensions) |
| `Cors:AllowedOrigins` | `["http://localhost:3000"]` | Allowed CORS origins for frontend |
| `Serilog:MinimumLevel:Default` | `Information` | Log level |

### Environment Variables for Docker

Set in `.env` file at project root:

```env
OPENAI_API_KEY=sk-your-key-here
```

Docker Compose automatically maps these via the `${OPENAI_API_KEY}` syntax in `docker-compose.yml`.

---

## API Reference

### Search Jobs

```http
POST /api/search
Content-Type: application/json

{
  "query": "senior backend developer C# remote",
  "countryCode": "DE",
  "industry": "Technology",
  "minSalary": 60000,
  "visaSponsorship": true,
  "remote": true,
  "vectorWeight": 0.6,
  "textWeight": 0.4,
  "topN": 20,
  "offset": 0
}
```

**Response:**

```json
{
  "query": "senior backend developer C# remote",
  "totalCount": 15,
  "jobs": [
    {
      "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "Senior .NET Developer",
      "company": "TechCorp GmbH",
      "location": "Berlin, Germany",
      "countryCode": "DE",
      "industry": "Technology",
      "salaryMin": 65000,
      "salaryMax": 85000,
      "currency": "EUR",
      "remote": true,
      "visaSponsorship": true,
      "languageRequired": "English",
      "relevanceScore": 0.87,
      "vectorScore": 0.91,
      "textScore": 0.82,
      "descriptionSnippet": "We are looking for a Senior .NET Developer..."
    }
  ]
}
```

### Get Job by ID

```http
GET /api/jobs/{id}
```

### Get Jobs Pending Embedding

```http
GET /api/jobs/pending-embedding?count=50
```

### Health Check

```http
GET /health
```

---

## How It Works

### Crawling Pipeline

The `CrawlingWorker` runs every **6 hours** and orchestrates the full ingestion pipeline:

```
Job Board → Crawler → Normalizer → Deduplicator → Metadata Extractor → Database
```

1. **Crawlers** (`IJobCrawler`) fetch raw job data from sources (RSS feeds, HTML pages)
2. **Normalizer** (`IJobNormalizer`) trims fields, resolves country codes, computes SHA256 description hash
3. **Deduplicator** (`IJobDeduplicator`) checks for existing URL or description hash to avoid duplicates
4. **Metadata Extractor** (`IJobMetadataExtractor`) applies rule-based extraction:
   - **Industry classification** — maps keywords to Technology, Finance, Healthcare, etc.
   - **Language detection** — identifies "German required", "Deutsch", "B2 level" patterns
   - **Visa sponsorship** — detects "visa sponsorship", "work permit" mentions
   - **Remote detection** — identifies "remote", "work from home", "hybrid" patterns
   - **Salary parsing** — extracts salary ranges via regex (e.g., "€60,000 - €80,000")

### Embedding Pipeline

The `EmbeddingWorker` runs every **5 minutes** and batch-processes unembedded jobs:

```
Jobs (EmbeddingPending=true) → Truncate to 2000 chars → OpenAI API → Store Vector
```

1. Fetches up to **50 jobs** per cycle where `EmbeddingPending = true`
2. Combines `Title + Company + Description` into a single text
3. Truncates to **2000 characters** for cost optimization
4. Calls OpenAI `text-embedding-3-small` to generate **1536-dimensional** vectors
5. Stores the vector in the `JobEmbeddings` table and sets `EmbeddingPending = false`

### Hybrid Search

The search engine combines two ranking signals via raw SQL for maximum performance:

$$\text{FinalScore} = w_v \times \text{VectorScore} + w_t \times \text{TextScore}$$

Where:
- $w_v$ = Vector weight (default **0.6**)
- $w_t$ = Text weight (default **0.4**)
- $\text{VectorScore} = 1 - \text{cosine\_distance}(\text{query\_embedding}, \text{job\_embedding})$
- $\text{TextScore} = \text{ts\_rank}(\text{tsvector}(\text{title} \| \text{description}), \text{plainto\_tsquery}(\text{query}))$

**Supported Filters:**
- Country code, Industry, Minimum salary
- Visa sponsorship (boolean), Remote (boolean)
- Pagination via `topN` + `offset`

### AI Agents

The platform includes a dedicated **Agents** layer (`JobRag.Agents`) with composable AI agents, safety guardrails, and orchestration workflows.

#### Agents

| Agent | Input | Output | Purpose |
|---|---|---|---|
| **QueryRewriteAgent** | Natural language query | Structured filters + optimised keywords | Converts "find me remote C# jobs in Berlin" into structured search parameters |
| **CoverLetterAgent** | Job description + CV | Tailored cover letter | Country-aware cover letter generation (formal for DE, adapted tone per market) |
| **InterviewPrepAgent** | Job description (+CV) | Categorised questions | Technical, behavioural, situational, and company-specific interview questions |
| **JobMatchAgent** | Job description + CV | Multi-dimensional score | Skill/experience/location/salary match with strengths, gaps, and recommendation |
| **JobSummaryAgent** | Job description | Structured summary | TL;DR, must-haves, nice-to-haves, tech stack, red flags, seniority estimate |

#### Guardrails

| Guardrail | What it checks | Action |
|---|---|---|
| **PiiGuardrail** | Email, phone, IBAN, German SSN patterns in output | Block |
| **HallucinationGuardrail** | Skills in cover letter not found in CV | Warn / Block |
| **ContentLengthGuardrail** | Word count within acceptable range | Warn / Block |
| **CostBudgetGuardrail** | Token usage and request rate per user | Warn / Block |

#### Workflows

| Workflow | Steps | Use Case |
|---|---|---|
| **JobApplicationWorkflow** | Match → CoverLetter → PII check → Hallucination check → Length check | One-click job application with safety checks |
| **SmartSearchWorkflow** | QueryRewrite → (caller runs search) → Summarise results | Conversational job search |

All agents communicate through a shared `ILlmClient` abstraction, making it easy to swap between OpenAI, Azure OpenAI, Anthropic, or local models.

### Country Rules Engine

Each supported country gets an `ICountryRulesService` implementation:

| Country | Service | Features |
|---|---|---|
| 🇩🇪 Germany | `GermanyRulesService` | City normalization (Berlin, München→Munich, Hamburg, Frankfurt, etc.), German/English language detection |
| 🇳🇱 Netherlands | `NetherlandsRulesService` | City normalization (Amsterdam, Rotterdam, Den Haag→The Hague, Utrecht, Eindhoven) |

Adding a new country is as simple as implementing `ICountryRulesService` and registering it as a singleton in `DependencyInjection.cs`.

---

## Database Schema

```
┌──────────────┐     ┌──────────────────┐
│    Users     │     │  UserProfiles    │
├──────────────┤     ├──────────────────┤
│ Id (PK)      │────►│ UserId (PK, FK)  │
│ Email (uniq) │     │ CountryCode      │
│ PasswordHash │     │ CVText           │
│ Role         │     │ CVEmbedding(1536)│
│ CreatedAt    │     │ PreferencesJson  │
└──────┬───────┘     │ UpdatedAt        │
       │             └──────────────────┘
       │
       ├─────────────────┐
       │                 │
┌──────┴───────┐  ┌──────┴──────────┐
│ UserSavedJobs│  │JobApplications  │
├──────────────┤  ├─────────────────┤
│ Id (PK)      │  │ Id (PK)         │
│ UserId (FK)  │  │ UserId (FK)     │
│ JobId (FK)   │  │ JobId (FK)      │
│ Status       │  │ AppliedAt       │
│ MatchScore   │  │ Status          │
│ CreatedAt    │  │ Notes           │
└──────┬───────┘  │ CoverLetterText │
       │          │ CreatedAt       │
       │          └───┬─────────────┘
       │              │
       └──────┬───────┘
              │
     ┌────────┴──────┐    ┌─────────────────┐
     │     Jobs      │    │  JobEmbeddings   │
     ├───────────────┤    ├─────────────────┤
     │ Id (PK)       │◄───│ JobId (PK, FK)  │
     │ CountryCode   │    │ Embedding (1536)│
     │ Industry      │    │ CreatedAt       │
     │ Title         │    └─────────────────┘
     │ Company       │
     │ Location      │       Indexes:
     │ Description   │       • IVFFlat on Embedding
     │ Url (unique)  │         (vector_cosine_ops)
     │ Source        │       • Unique on Url
     │ SalaryMin/Max │       • Composite on
     │ Currency      │         CountryCode + Industry
     │ LanguageReq   │       • Filtered on
     │ VisaSponsorship│        EmbeddingPending
     │ Remote        │       • On DescriptionHash
     │ EmbeddingPending│
     │ DescriptionHash│
     │ PostedDate    │
     │ CreatedAt     │
     └───────────────┘
```

**Key Indexes:**
- `JobEmbeddings.Embedding` — IVFFlat index with `vector_cosine_ops` for fast similarity search
- `Jobs.Url` — Unique index for deduplication
- `Jobs.CountryCode + Industry` — Composite index for filtered searches
- `Jobs.EmbeddingPending` — Filtered index for the embedding worker queue
- `Jobs.DescriptionHash` — Index for content-based deduplication
- `Users.Email` — Unique index for email uniqueness

---

## Next Steps / Roadmap

### 🔴 Phase 1: Get It Running (Immediate)

- [ ] **Create initial EF migration**
  ```bash
  dotnet ef migrations add InitialCreate -p src/Infrastructure -s src/Api
  dotnet ef database update -p src/Infrastructure -s src/Api
  ```
- [ ] **Add full-text search tsvector generated column** — Add a SQL migration to create a generated `tsvector` column on the `Jobs` table for better `ts_rank` performance:
  ```sql
  ALTER TABLE "Jobs" ADD COLUMN "SearchVector" tsvector
      GENERATED ALWAYS AS (
          to_tsvector('english', coalesce("Title", '') || ' ' || coalesce("Description", ''))
      ) STORED;
  CREATE INDEX idx_jobs_search_vector ON "Jobs" USING GIN ("SearchVector");
  ```
- [ ] **Set your OpenAI API key** — In `.env` or `appsettings.json`
- [ ] **Implement ILlmClient** — Wire up the OpenAI chat completions API for the Agents layer
- [ ] **Test the full pipeline end-to-end** — Insert a test job, run embedding worker, execute a search

### 🟡 Phase 2: Real Crawlers (Next Sprint)

- [ ] **Implement real German job board crawlers:**
  - Indeed.de crawler (HTML scraping)
  - StepStone.de crawler (API or HTML)
  - LinkedIn Germany crawler (API integration)
  - Arbeitsagentur (Federal Employment Agency) crawler
  - Xing Jobs crawler
- [ ] **Add rate limiting and politeness delays** to crawlers
- [ ] **Add retry logic with exponential backoff** for transient failures
- [ ] **Implement crawler health monitoring** — Track success rates, last-run timestamps, job counts per source
- [ ] **Switch to Quartz.NET scheduled jobs** — Replace `BackgroundService` timer loops with proper cron expressions

### 🟢 Phase 3: Authentication & User Features

- [ ] **Implement JWT authentication** — The bearer scheme is registered; add key/issuer config, token generation endpoint, refresh tokens
- [ ] **User registration and login endpoints** — `/api/auth/register`, `/api/auth/login`
- [ ] **CV upload and embedding** — Parse PDF/DOCX CVs, extract text, generate CV embedding for personalized job matching
- [ ] **Saved jobs API** — CRUD endpoints for `UserSavedJob`
- [ ] **Job application tracking** — Submit applications, track status, store cover letters
- [ ] **Personalized recommendations** — Use CV embedding + user preferences to find matching jobs proactively

### 🤖 Phase 3.5: AI Agent Features

- [ ] **Smart search endpoint** — `POST /api/search/smart` — Accepts natural language, uses SmartSearchWorkflow
- [ ] **Cover letter generation endpoint** — `POST /api/jobs/{id}/cover-letter`
- [ ] **Interview prep endpoint** — `POST /api/jobs/{id}/interview-prep`
- [ ] **Job match scoring** — Auto-score saved jobs against user CV
- [ ] **Implement ILlmClient** with OpenAI chat completions + structured output
- [ ] **Add agent observability** — Log token usage, latency, guardrail triggers per agent call
- [ ] **Expand guardrails** — Toxicity detection, prompt injection protection, output format validation

### 🔵 Phase 4: Search Enhancements

- [ ] **Add German-language full-text search** — Install and configure `pg_catalog.german` text search configuration alongside English
- [ ] **Implement query expansion** — Use LLM to expand search queries with synonyms and related terms
- [ ] **Add search result caching** — Redis or in-memory cache for frequent queries
- [ ] **A/B test vector vs. text weights** — Log search interactions to optimize the default 0.6/0.4 ratio
- [ ] **Add faceted search** — Return aggregated counts by country, industry, salary range, remote status
- [ ] **Implement "More Like This"** — Given a job ID, find similar jobs using its embedding

### 🟣 Phase 5: Multi-Country Expansion

- [ ] **Implement Netherlands crawlers** — Indeed.nl, LinkedIn NL, Nationale Vacaturebank
- [ ] **Add Austria rules service** — `AustriaRulesService` for AT-specific normalization
- [ ] **Add Switzerland rules service** — Handle CHF currency, multi-language (DE/FR/IT/EN)
- [ ] **Currency normalization** — Convert salaries to EUR for cross-country comparison
- [ ] **Multi-language embeddings** — Evaluate multilingual embedding models (e.g., `text-embedding-3-large` or open-source alternatives)

### ⚫ Phase 6: Production Readiness

- [ ] **Add OpenTelemetry tracing** — Distributed tracing across API → Workers → Database
- [ ] **Add Prometheus metrics** — Request latency, search response times, crawler success rates, embedding queue depth
- [ ] **Health check improvements** — Deep health checks (DB connectivity, OpenAI API reachability, queue depth)
- [ ] **Add integration tests** — TestContainers with PostgreSQL + pgvector for realistic testing
- [ ] **Add unit tests** — Domain logic, normalizer, deduplicator, metadata extractor, agent guardrails
- [ ] **CI/CD pipeline** — GitHub Actions with build → test → Docker build → deploy
- [ ] **Database connection pooling** — Configure Npgsql connection pool limits
- [ ] **Add Swagger/OpenAPI** — `builder.Services.AddEndpointsApiExplorer()` + `AddSwaggerGen()`
- [ ] **Structured error responses** — Consistent `ProblemDetails` format for all error responses
- [ ] **Rate limiting** — ASP.NET Core rate limiting middleware on API endpoints

### 🌟 Phase 7: Frontend & Chat

- [ ] **Chat-based job search** — LLM-powered conversational interface ("Find me remote C# jobs in Berlin with visa sponsorship")
- [ ] **Frontend (Next.js or Blazor)** — Search UI, job cards, saved jobs dashboard, application tracker
- [ ] **Email notifications** — New job alerts matching user preferences
- [ ] **Multi-tenancy** — Add back when B2B SaaS is needed (EF Global Query Filters pattern ready to re-introduce)

---

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -am 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

---

## License

This project is private. All rights reserved.

---

<p align="center">
  Built with ❤️ for the German job market<br/>
  <em>Architecture: Modular Monolith · Clean Architecture · AI-Native</em>
</p>
# jobai
# jobai
