# RAG Demo - Medical Supply Product Assistant

A minimal Retrieval-Augmented Generation (RAG) API built with **.NET 8**, **Azure OpenAI**, and **Azure AI Search**. The API answers natural-language questions about a medical supply company's product catalog by retrieving the most semantically relevant products and using them as context for an LLM-generated answer.

## What this project demonstrates

- Integrating **Azure OpenAI** (chat completions + embeddings) into a .NET application
- Implementing a full **RAG pipeline**: embedding generation → vector similarity search → context-augmented prompting
- Using **Azure AI Search** as a persistent vector database (HNSW algorithm)
- Basic **admin API** for managing the product catalog (create/update/delete) without redeploying the app
- Simple **API key authentication** on admin endpoints via Minimal API endpoint filters
- Clean project structure with clear separation of concerns (Models / Services / Data / Endpoints)

## Architecture

```
Question
   │
   ▼
Azure OpenAI (text-embedding-3-small) → question vector
   │
   ▼
Azure AI Search (vector similarity search) → top N relevant products
   │
   ▼
Context (relevant products) + Question → Azure OpenAI (gpt-5-mini)
   │
   ▼
Final answer
```

## Tech stack

| Component | Technology |
|---|---|
| API | ASP.NET Core 8 (Minimal API) |
| LLM | Azure OpenAI - gpt-5-mini |
| Embeddings | Azure OpenAI - text-embedding-3-small |
| Vector store | Azure AI Search (Free tier) |
| Auth | Custom API key filter on admin routes |

## Project structure

```
RagDemo/
├── Models/          # Data records (Product, SearchProduct, request DTOs)
├── Services/         # Business logic (search indexing, RAG pipeline)
├── Data/             # Seed data for the initial product catalog
├── Endpoints/        # Minimal API route definitions
└── Program.cs        # App composition (DI, middleware, startup seeding)
```

## Getting started

### Prerequisites

- .NET 8 SDK
- An Azure subscription with:
  - An Azure OpenAI resource with two model deployments: a chat model (e.g. `gpt-5-mini`) and an embedding model (`text-embedding-3-small`)
  - An Azure AI Search resource (Free tier is sufficient for this demo)

### Configuration

Add your credentials via [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) (do not commit these to source control):

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-key>"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-5-mini"
dotnet user-secrets set "AzureOpenAI:EmbeddingDeploymentName" "text-embedding-3-small"
dotnet user-secrets set "AzureSearch:Endpoint" "https://<your-search-service>.search.windows.net"
dotnet user-secrets set "AzureSearch:ApiKey" "<your-key>"
dotnet user-secrets set "AdminApiKey" "<a-secret-value-of-your-choice>"
```

### Running

```bash
dotnet run
```

On first run, the app creates the Azure AI Search index and seeds it with the sample product catalog (`Data/ProductData.cs`). On subsequent runs, seeding is skipped if the index already contains documents.

Swagger UI is available at `/swagger` in development.

## API reference

### `POST /ask`

Ask a question about the product catalog. No authentication required.

**Request**
```json
{
  "question": "have anything for checking blood sugar?"
}
```

**Response**
```json
{
  "answer": "...",
  "usedProducts": ["Blood glucose meter"]
}
```

### `POST /admin/products`

Add a new product, or update an existing one by providing its `id`. Requires the `X-Admin-Api-Key` header.

**Request**
```json
{
  "name": "Blood glucose meter",
  "description": "Portable device for measuring blood sugar levels, includes 25 test strips."
}
```

**Response**
```json
{
  "message": "Product saved successfully",
  "id": "..."
}
```

### `DELETE /admin/products/{id}`

Delete a product by id. Requires the `X-Admin-Api-Key` header.

**Response**
```json
{
  "message": "Product deleted successfully"
}
```

## Notes on design decisions

- **Why Azure AI Search instead of in-memory vectors**: an in-memory list works for a handful of documents, but does not persist across restarts and does not scale. Azure AI Search stores embeddings persistently and uses an optimized algorithm (HNSW) for fast similarity search, even at large scale.
- **Why the seed step checks document count first**: recalculating embeddings for the entire catalog on every startup would be wasteful and would incur unnecessary API costs at scale. The app only seeds when the index is empty; product changes after that go through the admin endpoints.
- **Why Minimal API instead of Controllers**: for a small number of endpoints, Minimal API keeps the project lightweight, while route grouping (`MapGroup`) still allows shared concerns (like authentication) to be applied cleanly to a subset of routes.

## Possible next steps

- Load the seed catalog from an external file (JSON/CSV) instead of a hardcoded list
- Add hybrid search (keyword + vector) for improved retrieval quality
- Add integration tests for the RAG pipeline
