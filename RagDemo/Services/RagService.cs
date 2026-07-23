using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using RagDemo.Models;

namespace RagDemo.Services;

public class RagService
{
    private readonly AzureOpenAIClient _openAiClient;
    private readonly SearchIndexService _searchIndexService;
    private readonly string _chatDeploymentName;
    private readonly string _embeddingDeploymentName;
    private readonly BlobStorageService _blobStorageService;

    public RagService(AzureOpenAIClient openAiClient, SearchIndexService searchIndexService, BlobStorageService blobStorageService, IConfiguration config)
    {
        _openAiClient = openAiClient;
        _searchIndexService = searchIndexService;
        _blobStorageService = blobStorageService;
        _chatDeploymentName = config["AzureOpenAI:DeploymentName"]!;
        _embeddingDeploymentName = config["AzureOpenAI:EmbeddingDeploymentName"]!;
    }

    public async Task SeedProductsIfEmptyAsync()
    {
        await _searchIndexService.CreateIndexIfNotExistsAsync();

        var existingDocumentCount = await _searchIndexService.GetDocumentCountAsync();
        if (existingDocumentCount > 0)
        {
            return;
        }

        var products = await _blobStorageService.LoadProductsAsync();

        var embeddingClient = _openAiClient.GetEmbeddingClient(_embeddingDeploymentName);
        var productsToUpload = new List<SearchProduct>();

        foreach (var product in products)
        {
            var text = $"{product.Name}: {product.Description}";
            var embeddingResponse = await embeddingClient.GenerateEmbeddingAsync(text);
            var vector = embeddingResponse.Value.ToFloats().ToArray();

            productsToUpload.Add(new SearchProduct
            {
                Id = Guid.NewGuid().ToString(),
                Name = product.Name,
                Description = product.Description,
                Vector = vector
            });
        }

        await _searchIndexService.UploadProductsAsync(productsToUpload);
    }

    public async Task<(string Answer, List<string> UsedProducts)> AskAsync(string question)
    {
        var embeddingClient = _openAiClient.GetEmbeddingClient(_embeddingDeploymentName);
        var questionEmbeddingResponse = await embeddingClient.GenerateEmbeddingAsync(question);
        var questionVector = questionEmbeddingResponse.Value.ToFloats().ToArray();

        var searchClient = _searchIndexService.GetSearchClient();
        var searchOptions = new SearchOptions
        {
            VectorSearch = new()
            {
                Queries = { new VectorizedQuery(questionVector) { KNearestNeighborsCount = 3, Fields = { "Vector" } } }
            }
        };

        var searchResults = await searchClient.SearchAsync<SearchProduct>("*", searchOptions);
        var relevantProducts = new List<SearchProduct>();
        await foreach (var result in searchResults.Value.GetResultsAsync())
        {
            relevantProducts.Add(result.Document);
        }

        var context = string.Join("\n", relevantProducts.Select(p => $"- {p.Name}: {p.Description}"));

        var prompt = $"""
            You are an assistant for a Medical Supply company. Answer the question using ONLY the information in the context below.
            If you cannot find the answer in the context, say you don't have that information.

            Context (relevant products):
            {context}

            Question: {question}
            """;

        var chatClient = _openAiClient.GetChatClient(_chatDeploymentName);
        var response = await chatClient.CompleteChatAsync(prompt);

        return (response.Value.Content[0].Text, relevantProducts.Select(p => p.Name).ToList());
    }

    public async Task<string> AddOrUpdateProductAsync(AdminProductRequest request)
    {
        var embeddingClient = _openAiClient.GetEmbeddingClient(_embeddingDeploymentName);
        var text = $"{request.Name}: {request.Description}";
        var embeddingResponse = await embeddingClient.GenerateEmbeddingAsync(text);
        var vector = embeddingResponse.Value.ToFloats().ToArray();

        var searchProduct = new SearchProduct
        {
            Id = string.IsNullOrEmpty(request.Id) ? Guid.NewGuid().ToString() : request.Id,
            Name = request.Name,
            Description = request.Description,
            Vector = vector
        };

        await _searchIndexService.UploadProductsAsync(new List<SearchProduct> { searchProduct });

        return searchProduct.Id;
    }

    public async Task DeleteProductAsync(string id)
    {
        var searchClient = _searchIndexService.GetSearchClient();
        await searchClient.DeleteDocumentsAsync("Id", new[] { id });
    }
}