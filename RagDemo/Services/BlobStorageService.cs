using System.Text.Json;
using Azure.Storage.Blobs;
using RagDemo.Models;

namespace RagDemo.Services;

public class BlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly string _blobName;

    public BlobStorageService(IConfiguration config)
    {
        var connectionString = config["AzureStorage:ConnectionString"]!;
        var containerName = config["AzureStorage:ContainerName"]!;
        _blobName = config["AzureStorage:BlobName"]!;

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    }

    public async Task<List<Product>> LoadProductsAsync()
    {
        var blobClient = _containerClient.GetBlobClient(_blobName);

        var response = await blobClient.DownloadContentAsync();
        var json = response.Value.Content.ToString();

        var products = JsonSerializer.Deserialize<List<Product>>(json);
        return products ?? new List<Product>();
    }
}