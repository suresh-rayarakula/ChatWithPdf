# ChatWithPdf — RAG Application with .NET

A **Chat with PDF** application built from scratch to demonstrate a real Retrieval-Augmented Generation (RAG) pipeline. Upload a PDF, ask a question, and receive a grounded answer with source citations (page number and chunk).

This project is designed for hands-on learning: PDF extraction, chunking, embeddings, vector search, prompt construction, and LLM integration — all wired together in a layered .NET solution.

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Solution Structure](#solution-structure)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
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

1. **Upload** a text-based PDF document
2. **Extract** text page by page
3. **Split** text into overlapping chunks
4. **Generate** vector embeddings via OpenAI
5. **Store** chunks and embeddings in PostgreSQL with pgvector
6. **Search** for the most relevant chunks when a user asks a question
7. **Generate** a grounded answer using an LLM with retrieved context
8. **Return** the answer along with source citations (file, page, excerpt, similarity score)

### What makes this "real" RAG

The LLM never sees the full PDF. It only receives the **top-K most semantically similar chunks** retrieved from the vector database. Answers are constrained by a system prompt to stay within that context.

---

## Architecture

### High-level flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        PHASE 1: INDEXING (Upload)                       │
├─────────────────────────────────────────────────────────────────────────┤
│  PDF Upload → Extract Text → Chunk Text → Embed Chunks → Store in DB   │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                        PHASE 2: QUERY (Chat)                              │
├─────────────────────────────────────────────────────────────────────────┤
│  Question → Embed Question → Vector Search → Build Prompt → LLM Answer   │
│                                    ↓                                    │
│                          Return Answer + Sources                        │
└─────────────────────────────────────────────────────────────────────────┘
```

### Layered design (Clean Architecture)

```
┌──────────────────────────────────────────────────────────┐
│  ChatWithPdf.Api                                         │
│  Controllers, HTTP contracts, startup                    │
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
│  EF Core, OpenAI, PdfPig, pgvector, service implementations│
└──────────────────────────────────────────────────────────┘
```

**Dependency rule:** API → Application → Domain. Infrastructure implements Application contracts and is registered at startup.

---

## Solution Structure

```
ChatWithPdf/
├── ChatWithPdf.sln
├── docker-compose.yml
├── README.md
│
├── ChatWithPdf.Api/                    # Web API (entry point)
│   ├── Controllers/
│   │   ├── DocumentsController.cs      # Upload + list documents
│   │   └── ChatController.cs           # RAG Q&A
│   ├── Contracts/
│   │   └── Requests/
│   │       └── ChatRequest.cs
│   ├── Extensions/
│   │   └── WebApplicationExtensions.cs # DB migration on startup
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
    ├── Ingestion/
    │   └── DocumentIngestionService.cs
    ├── OpenAI/
    │   └── OpenAIEmbeddingService.cs
    ├── Pdf/
    │   └── PdfPigTextExtractor.cs
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
| Web framework | ASP.NET Core Web API |
| ORM | Entity Framework Core 10 |
| Database | PostgreSQL 17 |
| Vector search | pgvector + HNSW index |
| PDF parsing | UglyToad.PdfPig |
| Embeddings & chat | OpenAI API (`text-embedding-3-small`, `gpt-4o-mini`) |
| Containerization | Docker Compose |

### Key NuGet packages

| Package | Project | Purpose |
|---------|---------|---------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Infrastructure | PostgreSQL provider |
| `Pgvector.EntityFrameworkCore` | Infrastructure | Vector column + cosine distance |
| `OpenAI` | Infrastructure | Embeddings and chat completions |
| `UglyToad.PdfPig` | Infrastructure | PDF text extraction |
| `Microsoft.EntityFrameworkCore.Design` | Api, Infrastructure | EF Core migrations |

---

## Prerequisites

Install the following before running the project:

1. **[.NET 10 SDK](https://dotnet.microsoft.com/download)** — verify with:
   ```powershell
   dotnet --version
   ```

2. **[Docker Desktop](https://www.docker.com/products/docker-desktop/)** — for PostgreSQL with pgvector

3. **OpenAI API key** — from [platform.openai.com](https://platform.openai.com/)

4. **EF Core CLI tools** (optional, for manual migrations):
   ```powershell
   dotnet tool install --global dotnet-ef
   ```

---

## Getting Started

### 1. Clone or open the project

```powershell
cd "D:\Projects\AI RAG"
```

### 2. Start PostgreSQL with pgvector

```powershell
docker compose up -d
```

Verify the container is running:

```powershell
docker ps
```

Expected container: `chatwithpdf-postgres` on port `5432`.

### 3. Configure OpenAI API key

**Recommended:** use .NET User Secrets (keeps the key out of source control).

```powershell
cd ChatWithPdf.Api
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-openai-api-key-here"
```

Alternatively, edit `ChatWithPdf.Api/appsettings.json` (do not commit real keys).

### 4. Build the solution

```powershell
cd ..
dotnet build
```

### 5. Run the API

```powershell
dotnet run --project ChatWithPdf.Api
```

On startup, the application automatically applies EF Core migrations to the database.

Default URLs (from `launchSettings.json`):

| Profile | URL |
|---------|-----|
| HTTPS | `https://localhost:7094` |
| HTTP | `http://localhost:5271` |

---

## Configuration

All settings live in `ChatWithPdf.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=chatwithpdf;Username=postgres;Password=postgres"
  },
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY"
  },
  "Rag": {
    "ChunkSize": 800,
    "ChunkOverlap": 150,
    "TopK": 5,
    "EmbeddingModel": "text-embedding-3-small",
    "ChatModel": "gpt-4o-mini",
    "EmbeddingDimensions": 1536
  }
}
```

### Configuration reference

| Setting | Default | Description |
|---------|---------|-------------|
| `Rag:ChunkSize` | `800` | Maximum characters per chunk |
| `Rag:ChunkOverlap` | `150` | Overlap between consecutive chunks |
| `Rag:TopK` | `5` | Number of chunks retrieved per question |
| `Rag:EmbeddingModel` | `text-embedding-3-small` | OpenAI embedding model |
| `Rag:ChatModel` | `gpt-4o-mini` | OpenAI chat model |
| `Rag:EmbeddingDimensions` | `1536` | Vector size — **must match** DB column `vector(1536)` |

> **Important:** If you change `EmbeddingDimensions` or `EmbeddingModel`, you must recreate the database migration and re-index all documents.

---

## Running the Application

### Quick start (full sequence)

```powershell
# Terminal 1 — database
docker compose up -d

# Terminal 2 — API
cd "D:\Projects\AI RAG"
dotnet run --project ChatWithPdf.Api
```

### Test with the included HTTP file

Open `ChatWithPdf.Api/ChatWithPdf.Api.http` in Visual Studio or Rider and run the requests.

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
| `500` | No extractable text, OpenAI failure, DB error |

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
| 1 | `PdfPigTextExtractor` | Reads each PDF page and extracts plain text |
| 2 | `SlidingWindowChunkingService` | Splits page text into ~800-char chunks with 150-char overlap |
| 3 | `OpenAIEmbeddingService` | Sends all chunk texts to OpenAI in a batch embedding call |
| 4 | `DocumentIngestionService` | Saves `Document` + `DocumentChunk` rows with `vector(1536)` embeddings |
| 5 | PostgreSQL + pgvector | Stores vectors; HNSW index enables fast cosine similarity search |

### Phase 2: Query (triggered by chat)

| Step | Service | What happens |
|------|---------|--------------|
| 1 | `OpenAIEmbeddingService` | Embeds the user's question into a 1536-dim vector |
| 2 | `PgVectorSearchService` | Finds top-K chunks by cosine distance (optionally filtered by `documentId`) |
| 3 | `RagChatService` | Builds a prompt with retrieved context + system instructions |
| 4 | OpenAI Chat API | Generates a grounded answer |
| 5 | API response | Returns answer + source citations with page numbers and similarity scores |

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
| `PageCount` | `int` | Number of pages with text |
| `UploadedAt` | `timestamptz` | Upload timestamp |

**`Chunks`**

| Column | Type | Description |
|--------|------|-------------|
| `Id` | `uuid` | Primary key |
| `DocumentId` | `uuid` | FK → Documents (cascade delete) |
| `PageNumber` | `int` | Source page in PDF |
| `ChunkIndex` | `int` | Chunk order within page |
| `Content` | `text` | Chunk text |
| `Embedding` | `vector(1536)` | OpenAI embedding |
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

```powershell
docker compose down -v
docker compose up -d
dotnet run --project ChatWithPdf.Api
```

---

## Development Workflow

### Typical development loop

1. Make code changes
2. `dotnet build`
3. If entities changed → add migration
4. `dotnet run --project ChatWithPdf.Api`
5. Test via `.http` file or curl

### Service registration

All infrastructure services are registered in `ChatWithPdf.Infrastructure/DependencyInjection.cs`:

```csharp
services.AddScoped<IPdfTextExtractor, PdfPigTextExtractor>();
services.AddScoped<IChunkingService, SlidingWindowChunkingService>();
services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();
services.AddScoped<IVectorSearchService, PgVectorSearchService>();
services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
services.AddScoped<IDocumentQueryService, DocumentQueryService>();
services.AddScoped<IRagChatService, RagChatService>();
```

### Adding a new feature (example: chat history)

1. Add entities in `ChatWithPdf.Domain`
2. Add interface + DTOs in `ChatWithPdf.Application`
3. Implement service in `ChatWithPdf.Infrastructure`
4. Register in `DependencyInjection.cs`
5. Add controller endpoint in `ChatWithPdf.Api`
6. Create and apply EF Core migration

---

## Troubleshooting

### Database connection failed

```
Failed to connect to 127.0.0.1:5432
```

- Ensure Docker is running: `docker compose up -d`
- Check container status: `docker ps`
- Verify connection string in `appsettings.json`

### OpenAI API key missing

```
OpenAI:ApiKey is missing.
```

- Set via user secrets: `dotnet user-secrets set "OpenAI:ApiKey" "sk-..."`
- Or update `appsettings.json`

### No extractable text in PDF

```
No extractable text was found in the PDF.
```

- The PDF may be scanned (image-only). PdfPig only extracts embedded text.
- Solution: use OCR (e.g. Azure Document Intelligence, Tesseract) — not yet implemented.

### Vector dimension mismatch

```
ERROR: expected 1536 dimensions, not N
```

- `Rag:EmbeddingDimensions` must match the DB column and the embedding model output.
- Default: `1536` for `text-embedding-3-small`.
- Fix: align config, recreate migration, re-upload documents.

### InvalidCastException writing Vector

```
Writing values of 'Pgvector.Vector' is not supported
```

- Ensure `UseVector()` is called in `UseNpgsql()` configuration.
- If using a custom `NpgsqlDataSourceBuilder`, call `dataSourceBuilder.UseVector()` before `Build()`.

### Upload works but chat returns no results

- Confirm documents appear in `GET /api/documents` with `chunkCount > 0`
- Check that `documentId` in chat request matches an uploaded document (if scoped)
- Try lowering `TopK` or re-uploading with a text-rich PDF

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
| 1 | Blazor or React UI | File upload UX, streaming responses |
| 2 | Chat history | Stateful conversations |
| 3 | Background ingestion | Large PDFs via queue (Hangfire) |
| 4 | Hybrid search | BM25 + vector retrieval |
| 5 | Re-ranking | Improve retrieval precision |
| 6 | OCR support | Scanned PDF handling |
| 7 | Evaluation harness | Measure answer quality objectively |

---

## License

This project is for educational purposes.
