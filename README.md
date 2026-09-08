# ChatWithPdf — RAG Application with .NET

A **Chat with PDF** application built from scratch to demonstrate a real Retrieval-Augmented Generation (RAG) pipeline. Upload a PDF (including scanned or handwritten notes), ask a question, and receive a grounded answer with source citations (page number and chunk).

This project is designed for hands-on learning: PDF extraction, OCR fallback, chunking, embeddings, vector search, prompt construction, and LLM integration — all wired together in a layered .NET solution with a Blazor Server UI.

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Solution Structure](#solution-structure)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Database Setup (PostgreSQL + pgvector)](#database-setup-postgresql--pgvector)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [Using the Blazor UI](#using-the-blazor-ui)
- [API Reference](#api-reference)
- [RAG Pipeline Explained](#rag-pipeline-explained)
- [Database Schema](#database-schema)
- [EF Core Migrations](#ef-core-migrations)
- [Development Workflow](#development-workflow)
- [Troubleshooting](#troubleshooting)
- [Roadmap](#roadmap)

---

## Overview

### What this application does

1. **Upload** a PDF (text-based, scanned, or handwritten)
2. **Extract** text page by page (PdfPig first; Gemini OCR if no text is found)
3. **Split** text into overlapping chunks
4. **Generate** vector embeddings via Google Gemini (`gemini-embedding-001`)
5. **Store** chunks and embeddings in PostgreSQL with pgvector (`vector(768)`)
6. **Search** for the most relevant chunks when a user asks a question
7. **Generate** a grounded answer using Gemini (`gemini-3.6-flash`) with retrieved context
8. **Return** the answer along with source citations (file, page, excerpt, similarity score)
9. **Chat in the browser** via an integrated Blazor Server UI

### What makes this "real" RAG

The LLM never sees the full PDF at chat time. It only receives the **top-K most semantically similar chunks** retrieved from the vector database. Answers are constrained by a system prompt to stay within that context.

---

## Architecture

### High-level flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        PHASE 1: INDEXING (Upload)                       │
├─────────────────────────────────────────────────────────────────────────┤
│  PDF Upload → Extract / OCR → Chunk Text → Embed Chunks → Store in DB  │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                        PHASE 2: QUERY (Chat)                            │
├─────────────────────────────────────────────────────────────────────────┤
│  Question → Embed Question → Vector Search → Build Prompt → LLM Answer  │
│                                    ↓                                    │
│                          Return Answer + Sources                        │
└─────────────────────────────────────────────────────────────────────────┘
```

### Layered design (Clean Architecture)

```
┌──────────────────────────────────────────────────────────┐
│  ChatWithPdf.Api                                         │
│  Controllers, Blazor UI, HTTP contracts, startup         │
└──────────────────────────┬───────────────────────────────┘
                           │ depends on
┌──────────────────────────▼───────────────────────────────┐
│  ChatWithPdf.Application                                 │
│  Interfaces, DTOs, options (use cases)                   │
└──────────────────────────┬───────────────────────────────┘
                           │ depends on
┌──────────────────────────▼───────────────────────────────┐
│  ChatWithPdf.Domain                                      │
│  Entities (Document, DocumentChunk)                      │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│  ChatWithPdf.Infrastructure                              │
│  EF Core, Google.GenAI, PdfPig, pgvector, services       │
└──────────────────────────────────────────────────────────┘
```

**Dependency rule:** API → Application → Domain. Infrastructure implements Application contracts and is registered at startup.

---

## Solution Structure

```
ChatWithPdf/
├── ChatWithPdf.slnx
├── README.md
│
├── ChatWithPdf.Api/                    # Web API + Blazor UI (entry point)
│   ├── Components/
│   │   ├── App.razor
│   │   ├── Routes.razor
│   │   ├── Layout/
│   │   │   └── MainLayout.razor
│   │   └── Pages/
│   │       └── Home.razor              # Upload + chat UI
│   ├── Controllers/
│   │   ├── DocumentsController.cs      # Upload + list documents
│   │   └── ChatController.cs           # RAG Q&A
│   ├── Contracts/
│   │   └── Requests/
│   │       └── ChatRequest.cs
│   ├── Services/
│   │   └── ChatApiClient.cs            # HttpClient used by Blazor UI
│   ├── Extensions/
│   │   └── WebApplicationExtensions.cs # DB migration on startup
│   ├── wwwroot/
│   │   └── app.css
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Program.cs
│   ├── appsettings.json
│   └── ChatWithPdf.Api.http            # HTTP test file
│
├── ChatWithPdf.Application/            # Use case contracts
│   ├── Contracts/
│   │   ├── IPdfTextExtractor.cs
│   │   ├── IChunkingService.cs
│   │   ├── IEmbeddingService.cs
│   │   ├── IVectorSearchService.cs
│   │   ├── IDocumentIngestionService.cs
│   │   ├── IDocumentQueryService.cs
│   │   └── IRagChatService.cs
│   └── Options/
│       └── RagOptions.cs
│
├── ChatWithPdf.Domain/                 # Core entities
│   └── Entities/
│       ├── Document.cs
│       └── DocumentChunk.cs
│
└── ChatWithPdf.Infrastructure/         # Implementations
    ├── Chunking/
    │   └── SlidingWindowChunkingService.cs
    ├── Documents/
    │   └── DocumentQueryService.cs
    ├── Gemini/
    │   └── GeminiEmbeddingService.cs
    ├── Ingestion/
    │   └── DocumentIngestionService.cs
    ├── Pdf/
    │   ├── PdfPigTextExtractor.cs
    │   └── FallbackPdfTextExtractor.cs # PdfPig → Gemini OCR fallback
    ├── Persistence/
    │   ├── AppDbContext.cs
    │   └── Migrations/
    ├── Rag/
    │   └── RagChatService.cs
    ├── Search/
    │   └── PgVectorSearchService.cs
    └── DependencyInjection.cs
```

---

## Technology Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 10 |
| Web framework | ASP.NET Core Web API + Blazor Server |
| ORM | Entity Framework Core 10 |
| Database | PostgreSQL 17 |
| Vector search | pgvector + HNSW index |
| PDF parsing | UglyToad.PdfPig |
| OCR fallback | Gemini multimodal PDF understanding |
| Embeddings & chat | Google Gemini API (`gemini-embedding-001`, `gemini-3.6-flash`) |

### Key NuGet packages

| Package | Project | Purpose |
|---------|---------|---------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Infrastructure | PostgreSQL provider |
| `Pgvector.EntityFrameworkCore` | Infrastructure | Vector column + cosine distance |
| `Google.GenAI` | Infrastructure | Embeddings, chat, OCR |
| `UglyToad.PdfPig` | Infrastructure | Native PDF text extraction |
| `Microsoft.EntityFrameworkCore.Design` | Api, Infrastructure | EF Core migrations |

---

## Prerequisites

Install the following before running the project:

1. **[.NET 10 SDK](https://dotnet.microsoft.com/download)** — verify with:
   ```powershell
   dotnet --version
   ```

2. **PostgreSQL 15+ with pgvector** — see [Database Setup](#database-setup-postgresql--pgvector) below

3. **Gemini API key** — from [Google AI Studio](https://aistudio.google.com/apikey)

4. **EF Core CLI tools** (optional, for manual migrations):
   ```powershell
   dotnet tool install --global dotnet-ef
   ```

---

## Database Setup (PostgreSQL + pgvector)

The application requires **PostgreSQL** with the **pgvector** extension enabled. Docker is not required — install PostgreSQL directly on your machine or use a cloud provider.

### Option A: Local PostgreSQL on Windows (recommended)

#### Step 1 — Install PostgreSQL

1. Download the installer from [postgresql.org/download/windows](https://www.postgresql.org/download/windows/)
2. Run the installer (PostgreSQL 16 or 17 recommended)
3. Note the **port** (default `5432`), **username** (default `postgres`), and **password** you set during installation
4. Ensure the PostgreSQL service is running:
   ```powershell
   Get-Service -Name postgresql*
   ```

#### Step 2 — Install pgvector extension

pgvector must be installed separately for your PostgreSQL version.

**Using pre-built binaries (easiest on Windows):**

1. Download the pgvector release matching your PostgreSQL version from [github.com/pgvector/pgvector/releases](https://github.com/pgvector/pgvector/releases)
2. Copy the files into your PostgreSQL installation directory:
   - `vector.dll` → `C:\Program Files\PostgreSQL\17\lib\`
   - `vector.control` and `vector--*.sql` → `C:\Program Files\PostgreSQL\17\share\extension\`

**Or build from source** — follow the [pgvector installation guide](https://github.com/pgvector/pgvector#installation).

#### Step 3 — Create the database

Open **pgAdmin** (installed with PostgreSQL) or **psql** and run:

```sql
CREATE DATABASE chatwithpdf;

\c chatwithpdf

CREATE EXTENSION IF NOT EXISTS vector;
```

Using psql from PowerShell:

```powershell
psql -U postgres -c "CREATE DATABASE chatwithpdf;"
psql -U postgres -d chatwithpdf -c "CREATE EXTENSION IF NOT EXISTS vector;"
```

Verify the extension is enabled:

```sql
SELECT * FROM pg_extension WHERE extname = 'vector';
```

#### Step 4 — Update the connection string

Edit `ChatWithPdf.Api/appsettings.json` with your credentials:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=chatwithpdf;Username=postgres;Password=YOUR_PASSWORD"
}
```

---

### Option B: Cloud PostgreSQL (no local install)

Use any hosted PostgreSQL that supports pgvector:

| Provider | Notes |
|----------|-------|
| [Supabase](https://supabase.com/) | pgvector enabled by default |
| [Neon](https://neon.tech/) | Enable pgvector in SQL editor |
| Azure Database for PostgreSQL | Enable `vector` extension via portal |

1. Create a database on your chosen provider
2. Enable the extension (if not already on):
   ```sql
   CREATE EXTENSION IF NOT EXISTS vector;
   ```
3. Copy the provider's connection string into `appsettings.json` or user secrets:
   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Database=...;Username=...;Password=..."
   ```

---

### Option C: Existing PostgreSQL server

If you already have PostgreSQL running (on a VM, NAS, or company server):

1. Install pgvector on that instance
2. Create the `chatwithpdf` database
3. Run `CREATE EXTENSION vector;`
4. Point the connection string at that server

---

### What the app does with the database

- On startup, EF Core **automatically applies migrations** (creates tables, indexes, and the `vector` extension annotation)
- You only need to ensure PostgreSQL is running and the `vector` extension is available before starting the API

---

## Getting Started

### 1. Open the project

```powershell
cd "D:\Projects\AI RAG"
```

### 2. Set up PostgreSQL

Complete the [Database Setup](#database-setup-postgresql--pgvector) steps above before continuing.

### 3. Configure Gemini API key

**Recommended:** use .NET User Secrets (keeps the key out of source control).

```powershell
cd ChatWithPdf.Api
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "your-gemini-api-key-here"
```

Alternatively, set `Gemini:ApiKey` in `ChatWithPdf.Api/appsettings.Development.json` (do not commit real keys).

### 4. Build the solution

```powershell
cd ..
dotnet build
```

### 5. Run the app

```powershell
dotnet run --project ChatWithPdf.Api
```

On startup, the application automatically applies EF Core migrations to the database.

Default URLs (from `launchSettings.json`):

| Profile | URL | Purpose |
|---------|-----|---------|
| HTTPS | `https://localhost:7094` | Blazor UI + API |
| HTTP | `http://localhost:5271` | Blazor UI + API |

Open **https://localhost:7094** in a browser for the UI, or call `/api/...` from Postman/curl.

---

## Configuration

All settings live in `ChatWithPdf.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=chatwithpdf;Username=postgres;Password=YOUR_PASSWORD"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY"
  },
  "Rag": {
    "ChunkSize": 800,
    "ChunkOverlap": 150,
    "TopK": 5,
    "EmbeddingModel": "gemini-embedding-001",
    "ChatModel": "gemini-3.6-flash",
    "EmbeddingDimensions": 768
  }
}
```

### Configuration reference

| Setting | Default | Description |
|---------|---------|-------------|
| `Gemini:ApiKey` | — | Google Gemini Developer API key |
| `Rag:ChunkSize` | `800` | Maximum characters per chunk |
| `Rag:ChunkOverlap` | `150` | Overlap between consecutive chunks |
| `Rag:TopK` | `5` | Number of chunks retrieved per question |
| `Rag:EmbeddingModel` | `gemini-embedding-001` | Gemini embedding model |
| `Rag:ChatModel` | `gemini-3.6-flash` | Gemini chat / OCR model |
| `Rag:EmbeddingDimensions` | `768` | Vector size — **must match** DB column `vector(768)` |

> **Important:** If you change `EmbeddingDimensions` or `EmbeddingModel`, you must update the database migration and **re-upload all documents**. Existing OpenAI (`1536`) or older Gemini embeddings are not compatible.

---

## Running the Application

### Quick start (full sequence)

```powershell
# 1. Ensure PostgreSQL is running (Windows service)
Get-Service -Name postgresql*

# 2. Build and run the API + UI
cd "D:\Projects\AI RAG"
dotnet build
dotnet run --project ChatWithPdf.Api
```

On first run, EF Core creates all tables and indexes automatically.

### Test with the Blazor UI

1. Open https://localhost:7094
2. Upload a PDF from the left panel
3. Select the document
4. Ask a question in the chat panel

### Test with the included HTTP file

Open `ChatWithPdf.Api/ChatWithPdf.Api.http` in Visual Studio, Rider, or Cursor and run the requests.

### Test with curl (PowerShell)

**Upload a PDF:**

```powershell
curl.exe -k -X POST "https://localhost:7094/api/documents/upload" `
  -F "file=@C:\path\to\your\document.pdf"
```

**List uploaded documents:**

```powershell
curl.exe -k "https://localhost:7094/api/documents"
```

**Ask a question:**

```powershell
curl.exe -k -X POST "https://localhost:7094/api/chat" `
  -H "Content-Type: application/json" `
  -d "{\"question\": \"What is this document about?\"}"
```

**Ask about a specific document:**

```powershell
curl.exe -k -X POST "https://localhost:7094/api/chat" `
  -H "Content-Type: application/json" `
  -d "{\"question\": \"Summarize the refund policy\", \"documentId\": \"YOUR-DOCUMENT-GUID\"}"
```

> Tip: Handwritten or scanned PDFs may take **30–120 seconds** to upload because Gemini OCR runs before embeddings.

---

## Using the Blazor UI

The Blazor Server UI is hosted inside `ChatWithPdf.Api` (same process and origin as the API — no CORS setup needed).

| Area | Features |
|------|----------|
| Left sidebar | Upload PDF, list documents, select scope, search-all toggle |
| Chat panel | Conversation thread, source citations, question composer |
| Upload | Supports PDFs up to 100 MB; shows progress messaging for OCR/indexing |

### Typical UI flow

1. Click **Upload PDF**
2. Wait until indexing finishes (`chunkCount > 0`)
3. Select the document (or enable **Search all documents**)
4. Ask a question and expand **sources** under the answer

---

## API Reference

### `POST /api/documents/upload`

Upload and index a PDF file.

| | |
|---|---|
| **Content-Type** | `multipart/form-data` |
| **Body** | `file` — PDF file |

**Success response (200):**

```json
{
  "documentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "report.pdf",
  "pageCount": 12,
  "chunkCount": 34
}
```

**Error responses:**

| Status | Reason |
|--------|--------|
| `400` | Empty file or non-PDF file |
| `500` | No extractable text, Gemini failure, DB error |

---

### `GET /api/documents`

List all uploaded documents.

**Success response (200):**

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fileName": "report.pdf",
    "pageCount": 12,
    "uploadedAt": "2026-09-01T17:55:00Z",
    "chunkCount": 34
  }
]
```

---

### `POST /api/chat`

Ask a question against indexed documents.

**Request body:**

```json
{
  "question": "What are the main findings?",
  "documentId": null
}
```

| Field | Required | Description |
|-------|----------|-------------|
| `question` | Yes | User question |
| `documentId` | No | Scope search to one document; `null` searches all |

**Success response (200):**

```json
{
  "answer": "The main findings indicate that...",
  "sources": [
    {
      "documentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "fileName": "report.pdf",
      "pageNumber": 5,
      "chunkIndex": 2,
      "excerpt": "The study found significant improvements in...",
      "similarity": 0.87
    }
  ]
}
```

---

## RAG Pipeline Explained

### Phase 1: Indexing (triggered by upload)

| Step | Service | What happens |
|------|---------|--------------|
| 1 | `FallbackPdfTextExtractor` | Tries PdfPig first; if no text, sends the PDF to Gemini for OCR |
| 2 | `SlidingWindowChunkingService` | Splits page text into ~800-char chunks with 150-char overlap |
| 3 | `GeminiEmbeddingService` | Embeds each chunk with `gemini-embedding-001` (`RETRIEVAL_DOCUMENT`) |
| 4 | `DocumentIngestionService` | Saves `Document` + `DocumentChunk` rows with `vector(768)` embeddings |
| 5 | PostgreSQL + pgvector | Stores vectors; HNSW index enables fast cosine similarity search |

### Phase 2: Query (triggered by chat)

| Step | Service | What happens |
|------|---------|--------------|
| 1 | `GeminiEmbeddingService` | Embeds the user's question (`RETRIEVAL_QUERY`) into a 768-dim vector |
| 2 | `PgVectorSearchService` | Finds top-K chunks by cosine distance (optionally filtered by `documentId`) |
| 3 | `RagChatService` | Builds a prompt with retrieved context + system instructions |
| 4 | Gemini Chat (`gemini-3.6-flash`) | Generates a grounded answer |
| 5 | API / Blazor UI | Returns answer + source citations with page numbers and similarity scores |

### Text extraction strategy

1. **PdfPig** extracts embedded text from digital PDFs (fast, free).
2. If no text is found (scanned/handwritten PDFs), **Gemini OCR** reads the PDF as multimodal input and returns page-marked text.
3. Chunking and embedding proceed the same way for both paths.

### Chunking strategy

- Chunks are created **per page** so page numbers are preserved for citations.
- Text is normalized (whitespace collapsed).
- Splits prefer sentence boundaries (`. `) or word boundaries (space).
- Overlap ensures content at chunk boundaries is not lost.

### Vector search

- Distance metric: **cosine distance** (`vector_cosine_ops`)
- Index type: **HNSW** (approximate nearest neighbor — fast at scale)
- Similarity score returned to client: `1 - cosine_distance`

---

## Database Schema

### Tables

**`Documents`**

| Column | Type | Description |
|--------|------|-------------|
| `Id` | `uuid` | Primary key |
| `FileName` | `varchar(500)` | Original file name |
| `ContentType` | `varchar(200)` | MIME type |
| `FileSizeBytes` | `bigint` | File size |
| `PageCount` | `int` | Number of pages with extracted text |
| `UploadedAt` | `timestamptz` | Upload timestamp |

**`Chunks`**

| Column | Type | Description |
|--------|------|-------------|
| `Id` | `uuid` | Primary key |
| `DocumentId` | `uuid` | FK → Documents (cascade delete) |
| `PageNumber` | `int` | Source page in PDF |
| `ChunkIndex` | `int` | Chunk order within page |
| `Content` | `text` | Chunk text |
| `Embedding` | `vector(768)` | Gemini embedding |
| `TokenEstimate` | `int` | Rough token count |

### Indexes

| Index | Type | Purpose |
|-------|------|---------|
| `IX_Chunks_Embedding` | HNSW (`vector_cosine_ops`) | Fast similarity search |
| `IX_Chunks_DocumentId_PageNumber_ChunkIndex` | Unique | Prevent duplicate chunks |
| `IX_Documents_UploadedAt` | B-tree | List documents by date |

### PostgreSQL extension

```sql
CREATE EXTENSION IF NOT EXISTS vector;
```

Applied automatically via EF Core migration.

### Migrations of note

| Migration | Purpose |
|-----------|---------|
| `20260901175559_InitialCreate` | Creates `Documents` / `Chunks` with `vector(1536)` (original OpenAI layout) |
| `20260903020000_UpdateEmbeddingDimensionsForGemini` | Clears embeddings and switches column to `vector(768)` for Gemini |

After switching providers or dimensions, **re-upload PDFs** so chunks are re-embedded.

---

## EF Core Migrations

Migrations live in `ChatWithPdf.Infrastructure/Persistence/Migrations/`.

### Apply migrations automatically

Migrations run on application startup via `WebApplicationExtensions.MigrateDatabaseAsync()`.

### Apply migrations manually

```powershell
dotnet ef database update `
  --project ChatWithPdf.Infrastructure `
  --startup-project ChatWithPdf.Api
```

### Create a new migration (after entity changes)

```powershell
dotnet ef migrations add YourMigrationName `
  --project ChatWithPdf.Infrastructure `
  --startup-project ChatWithPdf.Api `
  --output-dir Persistence/Migrations
```

### Reset the database (development only)

Drop and recreate the database in psql:

```sql
DROP DATABASE IF EXISTS chatwithpdf;
CREATE DATABASE chatwithpdf;
\c chatwithpdf
CREATE EXTENSION IF NOT EXISTS vector;
```

Then restart the API — migrations will recreate all tables:

```powershell
dotnet run --project ChatWithPdf.Api
```

---

## Development Workflow

### Typical development loop

1. Make code changes
2. `dotnet build`
3. If entities changed → add migration
4. `dotnet run --project ChatWithPdf.Api`
5. Test via Blazor UI, `.http` file, Postman, or curl

### Service registration

All infrastructure services are registered in `ChatWithPdf.Infrastructure/DependencyInjection.cs`:

```csharp
services.AddScoped<IPdfTextExtractor, FallbackPdfTextExtractor>();
services.AddScoped<IChunkingService, SlidingWindowChunkingService>();
services.AddScoped<IEmbeddingService, GeminiEmbeddingService>();
services.AddScoped<IVectorSearchService, PgVectorSearchService>();
services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
services.AddScoped<IDocumentQueryService, DocumentQueryService>();
services.AddScoped<IRagChatService, RagChatService>();
```

Blazor registration (in `Program.cs`):

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient<ChatApiClient>();
```

### Adding a new feature (example: chat history)

1. Add entities in `ChatWithPdf.Domain`
2. Add interface + DTOs in `ChatWithPdf.Application`
3. Implement service in `ChatWithPdf.Infrastructure`
4. Register in `DependencyInjection.cs`
5. Add controller endpoint and/or Blazor UI in `ChatWithPdf.Api`
6. Create and apply EF Core migration

---

## Troubleshooting

### Database connection failed

```
Failed to connect to 127.0.0.1:5432
```

- Ensure the PostgreSQL Windows service is running:
  ```powershell
  Get-Service -Name postgresql*
  Start-Service postgresql-x64-17   # adjust version number if needed
  ```
- Verify the connection string in `appsettings.json` (host, port, username, password)
- Test connectivity with psql:
  ```powershell
  psql -U postgres -d chatwithpdf -c "SELECT 1;"
  ```

### pgvector extension not found

```
ERROR: extension "vector" is not available
```

- pgvector is not installed on your PostgreSQL instance
- Follow [Step 2 in Database Setup](#step-2--install-pgvector-extension)
- Then run manually: `CREATE EXTENSION IF NOT EXISTS vector;`

### Gemini API key missing

```
Gemini:ApiKey is missing.
```

- Set via user secrets: `dotnet user-secrets set "Gemini:ApiKey" "your-key"`
- Or update `appsettings.Development.json` / `appsettings.json`
- Restart the app after changing the key

### Gemini model unavailable / not found

```
This model models/gemini-2.0-flash is no longer available
models/text-embedding-004 is not found ... for embedContent
```

- Use the current models in config:
  - Chat / OCR: `gemini-3.6-flash`
  - Embeddings: `gemini-embedding-001`
- Model names change over time; check [Gemini API docs](https://ai.google.dev/gemini-api/docs/models) if errors persist

### No extractable text in PDF

```
No extractable text was found in the PDF.
```

- PdfPig found no embedded text **and** Gemini OCR returned empty content
- Try a clearer scan, better lighting, or a text-based PDF
- Confirm the Gemini key works and the chat model supports document input

### Vector dimension mismatch

```
ERROR: expected 768 dimensions, not N
```

- `Rag:EmbeddingDimensions` must match the DB column and the embedding model output
- Default: `768` for `gemini-embedding-001`
- Fix: align config, ensure the Gemini dimensions migration ran, re-upload documents

### InvalidCastException writing Vector

```
Writing values of 'Pgvector.Vector' is not supported
```

- Ensure `UseVector()` is called in `UseNpgsql()` configuration
- If using a custom `NpgsqlDataSourceBuilder`, call `dataSourceBuilder.UseVector()` before `Build()`

### Upload works but chat returns no results

- Confirm documents appear in `GET /api/documents` with `chunkCount > 0`
- Check that `documentId` in chat request matches an uploaded document (if scoped)
- Re-upload documents after switching from OpenAI to Gemini (old embeddings were cleared)

### UI loads but API calls fail / file lock on build

- Stop any already-running `ChatWithPdf.Api` process before rebuilding
- Blazor UI and API share the same host — use https://localhost:7094 for both

### EF Core tools version warning

```
The Entity Framework tools version '9.x' is older than that of the runtime '10.x'
```

```powershell
dotnet tool update --global dotnet-ef
```

---

## Roadmap

Suggested next steps for extending the project:

| Phase | Feature | Learning goal |
|-------|---------|---------------|
| 1 | Chat history | Stateful conversations across sessions |
| 2 | Streaming answers | Token-by-token Blazor UI updates |
| 3 | Background ingestion | Large PDFs via queue (Hangfire) |
| 4 | Hybrid search | BM25 + vector retrieval |
| 5 | Re-ranking | Improve retrieval precision |
| 6 | Evaluation harness | Measure answer quality objectively |
| 7 | Auth / multi-user | Protect uploads and conversations |

---

## License

This project is for educational purposes.
