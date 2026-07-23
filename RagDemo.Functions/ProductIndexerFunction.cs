using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RagDemo.Models;
using RagDemo.Services;

namespace RagDemo.Functions;

public class ProductIndexerFunction
{
    private readonly ILogger<ProductIndexerFunction> _logger;
    private readonly IConfiguration _config;

    public ProductIndexerFunction(ILogger<ProductIndexerFunction> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    [Function(nameof(ProductIndexerFunction))]
    public async Task Run([BlobTrigger("products/{name}", Connection = "ProductsStorageConnection")] Stream stream, string name)
    {
        _logger.LogInformation($"Blob trigger fired for file: {name}");

        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var products = JsonSerializer.Deserialize<List<Product>>(json);
        if (products == null || products.Count == 0)
        {
            _logger.LogWarning("No products found in the uploaded file.");
            return;
        }

        var endpoint = _config["AzureOpenAI:Endpoint"]!;
        var apiKey = _config["AzureOpenAI:ApiKey"]!;
        var embeddingDeploymentName = _config["AzureOpenAI:EmbeddingDeploymentName"]!;

        var openAiClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        var embeddingClient = openAiClient.GetEmbeddingClient(embeddingDeploymentName);

        var searchIndexService = new SearchIndexService(_config);

        await searchIndexService.CreateIndexIfNotExistsAsync();
        await searchIndexService.DeleteAllDocumentsAsync();

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

        await searchIndexService.UploadProductsAsync(productsToUpload);

        _logger.LogInformation($"Successfully indexed {productsToUpload.Count} products from {name}.");
    }
}