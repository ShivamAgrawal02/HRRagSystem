# HR RAG System

<p align="center">
  <strong>Document ingestion foundation for semantic search over HR PDFs</strong><br>
  Built with .NET 10, Google Gemini embeddings, SQLite, and Qdrant.
</p>

<p align="center">
  <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white">
  <img alt="ASP.NET Core" src="https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4?logo=dotnet&logoColor=white">
  <img alt="Qdrant" src="https://img.shields.io/badge/Vector_DB-Qdrant-DC244C">
  <img alt="SQLite" src="https://img.shields.io/badge/Metadata-SQLite-003B57?logo=sqlite&logoColor=white">
  <img alt="Google Gemini" src="https://img.shields.io/badge/Embeddings-Google_Gemini-4285F4?logo=google&logoColor=white">
</p>

Finding the right detail in a collection of HR policies starts with getting those documents into a form an application can work with. HR RAG System handles that first step: it scans a folder of PDFs, notices what has changed since the last run, extracts and prepares the text, and stores each chunk with a Google Gemini embedding in Qdrant. SQLite keeps the document record and its processing status.

The project is intentionally focused on ingestion today. It gives you a foundation for semantic retrieval, while the public retrieval and chat API still needs to be built.

> **Current scope:** The working API endpoint triggers document ingestion. A Qdrant similarity-search method exists in `VectorRepository`, but the retrieval controller and retrieval service are currently empty and are not registered in dependency injection. This repository does not yet expose a question-answering/chat API.

## At a glance

- Recursively scans a configured directory and uses SHA-256 hashes to spot new, changed, and removed PDFs.
- Extracts PDF text, tidies it, then splits it into overlapping chunks.
- Generates a Gemini embedding for each chunk and stores the vectors and chunk text in Qdrant.
- Saves document metadata and processing progress in SQLite, including a `Failed` status when processing throws an error.
- Starts ingestion through `POST /api/ingestion`.

## Architecture at a glance

The API is the entry point, but the ingestion service does the coordinating. SQLite answers “what did we process before?” while Qdrant holds the chunk vectors that can later support semantic search.

```mermaid
flowchart TB
	User[You or an API client] -->|POST /api/ingestion| API[ASP.NET Core API]
	API --> Ingest[IngestionService]

	subgraph Source[Document source]
		PDFs[HR policy PDFs]
	end

	subgraph Prep[Prepare each new or changed document]
		Reader[PdfReader<br/>extract text]
		Cleaner[TextCleaner<br/>normalize text]
		Chunker[ChunkerUsingTextChunker<br/>split into chunks]
		Embedder[EmbeddingGenerator]
		Gemini[Google Gemini<br/>embedding model]
		Reader --> Cleaner --> Chunker --> Embedder --> Gemini
		Gemini -->|vector| Embedder
	end

	subgraph Stores[Storage]
		Docs[DocumentRepository]
		SQLite[(SQLite<br/>document metadata + status)]
		Vectors[VectorRepository]
		Qdrant[(Qdrant<br/>embeddings + chunk payloads)]
	end

	Ingest --> Scanner[DocumentScanner<br/>find PDFs + hash files]
	PDFs --> Scanner
	Scanner -->|scanned files| Ingest
	Ingest --> Docs
	Docs <--> SQLite
	Docs -->|existing records| Ingest
	Ingest --> Detector[ChangeDetector<br/>compare paths + hashes]
	Detector -->|new, changed, removed| Ingest
	Ingest --> Reader
	Ingest -->|save status checkpoints| Docs
	Chunker --> Vectors
	Vectors --> Qdrant
	Ingest -->|delete vectors for changed/removed docs| Vectors

	classDef actor fill:#fff3bf,stroke:#e67700,color:#402100,stroke-width:2px;
	classDef api fill:#e5dbff,stroke:#7048e8,color:#2b1a59,stroke-width:2px;
	classDef service fill:#d3f9d8,stroke:#2b8a3e,color:#13351a,stroke-width:2px;
	classDef ai fill:#d0ebff,stroke:#1971c2,color:#102a43,stroke-width:2px;
	classDef storage fill:#ffe8cc,stroke:#d9480f,color:#421000,stroke-width:2px;
	class User,PDFs actor;
	class API api;
	class Ingest,Scanner,Detector,Reader,Cleaner,Chunker,Embedder,Docs,Vectors service;
	class Gemini ai;
	class SQLite,Qdrant storage;
```

## What happens during ingestion?

This second view follows one ingestion request from start to finish, including the status updates saved along the way.

```mermaid
flowchart TD
	Start([POST /api/ingestion]) --> Scan[Scan PDF folder<br/>calculate SHA-256 hashes]
	Scan --> Compare[Compare paths and hashes<br/>with SQLite records]
	Compare --> Changes{What changed?}

	Changes -->|No change| Skip[Leave existing record and vectors as-is]
	Changes -->|Removed| Delete[Delete document vectors<br/>and SQLite record]
	Changes -->|New or changed| Begin[Save status: NotProcessed]

	Begin --> Extract[Extract PDF text]
	Extract --> S1[Save status: Extracted]
	S1 --> Clean[Normalize text]
	Clean --> S2[Save status: Cleaned]
	S2 --> Chunk[Create overlapping chunks<br/>and generate embeddings]
	Chunk --> S3[Save status: Chunked]
	S3 --> IsUpdate{Was this document<br/>already indexed?}
	IsUpdate -->|Yes| RemoveOld[Remove its old Qdrant vectors]
	IsUpdate -->|No| Upsert
	RemoveOld --> Upsert[Upsert chunk vectors and payloads]
	Upsert --> Done[Save status: Stored<br/>set LastIngestedAtUtc]

	Extract -. processing error .-> Failed[Save status: Failed<br/>then rethrow the error]
	Clean -. processing error .-> Failed
	Chunk -. processing error .-> Failed
	RemoveOld -. processing error .-> Failed
	Upsert -. processing error .-> Failed

	classDef start fill:#fff3bf,stroke:#e67700,color:#402100,stroke-width:2px;
	classDef work fill:#d0ebff,stroke:#1971c2,color:#102a43,stroke-width:2px;
	classDef status fill:#d3f9d8,stroke:#2b8a3e,color:#13351a,stroke-width:2px;
	classDef choice fill:#f3d9fa,stroke:#9c36b5,color:#35113f,stroke-width:2px;
	classDef cleanup fill:#ffe8cc,stroke:#d9480f,color:#421000,stroke-width:2px;
	classDef failure fill:#ffe3e3,stroke:#c92a2a,color:#4a1010,stroke-width:2px;
	class Start start;
	class Scan,Compare,Extract,Clean,Chunk,Upsert work;
	class S1,S2,S3,Done status;
	class Changes,IsUpdate choice;
	class Skip,Delete,RemoveOld cleanup;
	class Failed failure;
```

The statuses in the diagram are values from `DocumentStatus`: `NotProcessed`, `Extracted`, `Cleaned`, `Chunked`, `Stored`, and `Failed`. The dashed red connections illustrate the shared failure path: an error anywhere in per-document processing is recorded as `Failed` and rethrown. Documents that are unchanged are not reprocessed; for changed documents, old Qdrant vectors are removed before the replacement chunks are written.

## Tech stack

| Area | Technology | Use in this project |
|---|---|---|
| Runtime and web framework | .NET 10, ASP.NET Core | HTTP API, dependency injection, configuration, and hosting |
| API description | ASP.NET Core OpenAPI, Swagger UI | OpenAPI document and development-time API UI |
| Metadata persistence | SQLite, Entity Framework Core | Document records, file hashes, and change tracking |
| Vector database | Qdrant .NET client | Collection management and chunk-vector storage; includes a similarity-query method |
| Embedding provider | Google Gemini through `Mscc.GenerativeAI.Microsoft` | Generates vectors for document chunks |
| Text chunking | `Microsoft.SemanticKernel` TextChunker | Splits cleaned document text into chunks with overlap |
| PDF extraction | `UglyToad.PdfPig` | Reads text from PDF pages |
| Additional package | `sqlite-vec` | Referenced by the project; the current ingestion implementation writes vectors to Qdrant |

Package versions are maintained in [`HRRagSystem/HRRagSystem.csproj`](HRRagSystem/HRRagSystem.csproj).

## Service catalog

| Service | Responsibility |
|---|---|
| `IngestionService` (`IIngestionService`) | Orchestrates scanning and change detection, then processes new or changed PDFs. It saves each document's status as it moves through extraction, cleaning, chunking, and storage; processing errors are recorded as `Failed` and rethrown. |
| `DocumentScanner` (`IDocumentScanner`) | Recursively finds PDF files under the configured data directory and returns file metadata, relative paths, SHA-256 hashes, and an initial `NotProcessed` status. |
| `ChangeDetector` (`IChangeDetector`) | Compares scanned documents against database records by relative path and hash; returns new, updated, and deleted document sets. |
| `PdfReader` (`IPdfReader`) | Extracts text from each page of a PDF using PdfPig. |
| `TextCleaner` (`ITextCleaner`) | Normalizes line endings, repeated spaces/tabs, and excessive blank lines in extracted text. |
| `ChunkerUsingTextChunker` (`IChunker`) | Uses Semantic Kernel's `TextChunker` to split text into chunks, estimates token counts, attaches document metadata, and requests an embedding per chunk. Current limits are configured in the class (100 tokens per line, 200 tokens per paragraph, 20-token overlap). |
| `EmbeddingGenerator` (`IEmbeddingGenerator`) | Calls the configured Gemini embedding model and returns the generated `float[]` vector. |
| `DocumentRepository` (`IDocumentRepository`) | Reads, upserts, and deletes document records through the EF Core SQLite context. The ingestion service uses it to persist status checkpoints and the last-ingested timestamp. |
| `VectorRepository` (`IVectorRepository`) | Ensures the Qdrant collection exists, upserts chunk vectors and payloads, removes vectors by document ID, and provides a similarity-query method. That query is not currently exposed by an API endpoint. |
| `HRRagSystemSQLiteDbContext` | Defines the SQLite `Documents` set and document key/required-field/unique-relative-path rules. |

### Dependency injection lifetimes

`Program.cs` registers the document-processing and repository services as scoped. `EmbeddingGenerator` and `QdrantClient` are singletons; the EF Core DbContext is registered using `AddDbContext`.

## API

### Trigger document ingestion

```http
POST /api/ingestion
```

No request body is required. The endpoint awaits `PerformIngestAsync` and returns `200 OK` when ingestion completes. Cancellation is passed through the request pipeline.

There is no retrieval or chat route yet. Although `VectorRepository` has a similarity-query method, the retrieval controller and service are placeholders and are not registered in `Program.cs`.

In Development, the app maps the OpenAPI specification at `/openapi/v1.json` and enables Swagger UI. Check the URL printed by `dotnet run` for the local host and port.

## Configuration

The application reads configuration from its JSON settings and the standard .NET configuration providers. Relevant keys:

| Key | Purpose |
|---|---|
| `DataFolderPath` | Root directory to scan recursively for PDF files. |
| `ConnectionStrings:DefaultConnection` | SQLite connection string. |
| `Qdrant:Host` | Qdrant host name. |
| `Qdrant:GrpcPort` | Qdrant gRPC port. |
| `Qdrant:CollectionName` | Vector collection name. |
| `Qdrant:VectorSize` | Expected vector dimension; must match the configured Gemini embedding model. |
| `GeminiApiKey` | Gemini API credential. Supply as a secret, not a committed value. |
| `GeminiModel` | Embedding model identifier used by the embedding generator. |

Example configuration shape (use your own paths and valid model settings):

```json
{
  "DataFolderPath": "./hr-documents",
  "ConnectionStrings": {
	"DefaultConnection": "Data Source=HRRagSystem.db"
  },
  "Qdrant": {
	"Host": "localhost",
	"GrpcPort": 6334,
	"CollectionName": "HRDocumentsChunks",
	"VectorSize": 768
  }
}
```

The Gemini settings are intentionally not included in this JSON sample. Configure them with User Secrets or environment variables. For example, from the `HRRagSystem` project directory:

```powershell
dotnet user-secrets set "GeminiApiKey" "YOUR_GEMINI_API_KEY"
dotnet user-secrets set "GeminiModel" "YOUR_EMBEDDING_MODEL"
```

The checked-in `appsettings.json` currently contains a machine-specific `DataFolderPath`; replace it with a path appropriate to your environment. Do not commit credentials or confidential HR files.

## Run locally

### Prerequisites

- .NET 10 SDK
- A Google Gemini API key and a supported embedding model
- Qdrant running and reachable at the configured gRPC host/port

### 1. Start Qdrant

For local development with Docker:

```powershell
docker run --name qdrant -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

### 2. Configure the application

Set `DataFolderPath`, the SQLite connection string, and Qdrant settings in local configuration. Configure `GeminiApiKey` and `GeminiModel` using User Secrets as shown above. Confirm `Qdrant:VectorSize` matches the chosen embedding model output dimensions.

### 3. Restore and run

From the repository root:

```powershell
dotnet restore
dotnet run --project .\HRRagSystem\HRRagSystem.csproj
```

### 4. Ingest documents

Call the API route using the base URL printed in the console:

```powershell
Invoke-RestMethod -Method Post -Uri "https://localhost:<port>/api/ingestion"
```

For local HTTPS development, trust the .NET development certificate if your client reports a certificate error.

## Data model overview

- **`DocumentInfo`** is the SQLite document record: ID, file name, relative path, hash, file size, timestamps, ingestion version, and status.
- **`DocumentChunk`** is the unit indexed in Qdrant: chunk ID, document ID, file name, chunk index, text content, estimated token count, embedding, and ingestion version.
- Qdrant payloads include the document ID, file name, chunk index, content, token count, and ingestion version, enabling results to be associated with their source document.

`DocumentStatus` tracks ingestion progress: `NotProcessed` → `Extracted` → `Cleaned` → `Chunked` → `Stored`. If processing a document fails, the service records `Failed` before rethrowing the error. A successfully stored document also gets a refreshed `LastIngestedAtUtc` timestamp.

## Project layout

```text
HRRagSystem/
├── HRRagSystem.slnx
├── README.md
└── HRRagSystem/
	├── Controllers/          # HTTP API controllers
	├── Database/             # EF Core SQLite DbContext
	├── Migrations/           # EF Core migrations and model snapshot
	├── Models/                # Document and chunk models
	├── Services/
	│   ├── Interfaces/       # Service contracts
	│   └── Implementation/   # Ingestion and persistence services
	├── Data/                  # Configured PDF input folder (keep private PDFs out of Git)
	├── Program.cs             # Dependency injection and API pipeline
	├── HRRagSystem.csproj
	└── appsettings*.json
```

## Current limitations and next steps

- The retrieval controller, retrieval service, and retrieval interface are placeholders. Similarity search exists at the vector repository layer but is not exposed as an HTTP endpoint.
- There is no LLM answer-generation/chat flow in the current API.
- The configured embedding size must be compatible with the selected Gemini embedding model and the Qdrant collection.
- Ingestion is started by an API request; no background schedule is configured.

## Security

- Keep Gemini credentials in .NET User Secrets or a secure environment-specific secret store.
- Avoid committing real employee/HR PDFs, database files, API keys, or machine-specific paths.
- Restrict access to the document directory and Qdrant instance appropriately for your deployment.
