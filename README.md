# RAG Demo - Medical Supply Product Assistant

A Retrieval-Augmented Generation (RAG) system built with **.NET 8/9**, **Azure OpenAI**, **Azure AI Search**, **Azure Blob Storage**, **Azure Functions**, and **Azure App Service**. The system answers natural-language questions about a medical supply company's product catalog by retrieving the most semantically relevant products and using them as context for an LLM-generated answer. Product data is stored externally and re-indexed automatically whenever it changes, with no manual steps or redeployments required.

**Live demo:** https://ragdemo-api-cmb-acgzfkbxc5fmf4bx.swedencentral-01.azurewebsites.net/swagger

## What this project demonstrates

- Integrating **Azure OpenAI** (chat completions + embeddings) into .NET applications
- Implementing a full **RAG pipeline**: embedding generation → vector similarity search → context-augmented prompting
- Using **Azure AI Search** as a persistent vector database (HNSW algorithm)
- Reading source data from **Azure Blob Storage** instead of hardcoding it in the application
- An **event-driven indexing pipeline** using **Azure Functions** (Blob Trigger), so updating a file automatically re-indexes the catalog with no manual steps or app restarts
- Basic **admin API** for managing the product catalog (create/update/delete) without redeploying the app
- Simple **API key authentication** on admin endpoints via Minimal API endpoint filters
- **Live deployment** to Azure App Service
- Clean project structure with clear separation of concerns (Models / Services / Endpoints), split across two coordinated .NET projects in the same solution

## Architecture

```
products.json uploaded/updated in Azure Blob Storage
                    │
                    ▼ (Blob Trigger, automatic)
        Azure Function (RagDemo.Functions)
      generates embeddings for each product
                    │
                    ▼
          Azure AI Search (vector index, HNSW)
                    │
                    ▼
   Question ──► Azure OpenAI (text-embedding-3-small) ──► question vector
                    │
                    ▼
      Azure AI Search (vector similarity search) ──► top N relevant products
                    │
                    ▼
  Context (relevant products) + Question ──► Azure OpenAI (gpt-5-mini)
                    │
                    ▼
                Final answer
                    │
                    ▼
      Served live via Azure App Service (RagDemo web API)
```

## Tech stack

| Component | Technology |
|---|---|
| API | ASP.NET Core 8 (Minimal API) |
| Indexing pipeline | Azure Functions (.NET 9, isolated worker, Blob Trigger) |
| LLM | Azure OpenAI - gpt-5-mini |
| Embeddings | Azure OpenAI - text-embedding-3-small |
| Vector store | Azure AI Search (Free tier) |
| Source data storage | Azure Blob Storage |
| Hosting | Azure App Service (Windows) |
| Auth | Custom API key filter on admin routes |

## Solution structure

```
RagDemo/
├── RagDemo.sln
├── RagDemo/                    # Web API project
│   ├── Models/                 # Data records (Product, SearchProduct, request DTOs)
│   ├── Services/                # Business logic (search indexing, RAG pipeline, blob reading)
│   ├── Endpoints/               # Minimal API route definitions
│   └── Program.cs               # App composition (DI, middleware, startup seeding)
└── RagDemo.Functions/           # Azure Functions project
    └── ProductIndexerFunction.cs  # Blob Trigger: re-indexes the catalog on file change
```

## Getting started

### Prerequisites

- .NET 8 SDK (web API) and .NET 9 SDK (Functions project)
- Azure Functions Core Tools (for running the Functions project locally)
- An Azure subscription with:
  - An Azure OpenAI resource with two model deployments: a chat model (e.g. `gpt-5-mini`) and an embedding model (`text-embedding-3-small`)
  - An Azure AI Search resource (Free tier is sufficient for this demo)
  - An Azure Storage account with a `products` container holding a `products.json` file

### Configuration

**Web API project** - add your credentials via [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) (do not commit these to source control):

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-key>"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-5-mini"
dotnet user-secrets set "AzureOpenAI:EmbeddingDeploymentName" "text-embedding-3-small"
dotnet user-secrets set "AzureSearch:Endpoint" "https://<your-search-service>.search.windows.net"
dotnet user-secrets set "AzureSearch:ApiKey" "<your-key>"
dotnet user-secrets set "AzureStorage:ConnectionString" "<your-storage-connection-string>"
dotnet user-secrets set "AzureStorage:ContainerName" "products"
dotnet user-secrets set "AzureStorage:BlobName" "products.json"
dotnet user-secrets set "AdminApiKey" "<a-secret-value-of-your-choice>"
```

**Functions project** - add the same Azure OpenAI and Azure AI Search values to `local.settings.json`. The connection to Blob Storage uses Managed Identity via a Service Connector rather than a raw connection string.

In production (Azure App Service), the same settings are configured as Application Settings on the App Service resource instead of User Secrets.

### Running locally

Start the web API:
```bash
cd RagDemo
dotnet run
```

Start the Functions project (in a separate terminal, or as a second startup project in Visual Studio):
```bash
cd RagDemo.Functions
func start
```

On first run, if the Azure AI Search index is empty, it's populated automatically from the `products.json` file in Blob Storage. Uploading a new or updated `products.json` to the container automatically triggers re-indexing via the Azure Function - no restart needed.

Swagger UI is available at `/swagger`.

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
- **Why Blob Storage instead of a hardcoded product list**: keeps the source data decoupled from the application code, closer to a real-world scenario where a catalog is managed independently of the application that serves it.
- **Why a separate Azure Function instead of indexing at web API startup**: decouples the indexing workload from the web API's lifecycle. The catalog can be updated at any time - by uploading a new file - without needing to restart or redeploy the web application. This also means indexing (which can be slow for large catalogs) never blocks or delays the API from starting up.
- **Why the seed step checks document count first**: recalculating embeddings for the entire catalog unnecessarily would waste API calls. The web API only seeds on first run if the index is empty; ongoing updates flow through the Azure Function or the admin endpoints.
- **Why Minimal API instead of Controllers**: for a small number of endpoints, Minimal API keeps the project lightweight, while route grouping (`MapGroup`) still allows shared concerns (like authentication) to be applied cleanly to a subset of routes.

## Possible next steps

- Add hybrid search (keyword + vector) for improved retrieval quality
- Add integration tests for the RAG pipeline
- Move secrets to Azure Key Vault instead of App Settings
- Add CI/CD via GitHub Actions for automatic deployment on push