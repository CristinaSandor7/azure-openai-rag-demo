using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using RagDemo.Models;

namespace RagDemo.Services;

public class SearchIndexService
{
    private readonly SearchIndexClient _indexClient;
    private readonly string _indexName = "products-index";

    public SearchIndexService(IConfiguration config)
    {
        var endpoint = config["AzureSearch:Endpoint"]!;
        var apiKey = config["AzureSearch:ApiKey"]!;
        _indexClient = new SearchIndexClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task CreateIndexIfNotExistsAsync()
    {
        var vectorSearch = new VectorSearch
        {
            Profiles = { new VectorSearchProfile("default-profile", "default-algorithm") },
            Algorithms = { new HnswAlgorithmConfiguration("default-algorithm") }
        };

        var index = new SearchIndex(_indexName, new List<SearchField>
        {
            new SimpleField("Id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
            new SearchableField("Name"),
            new SearchableField("Description"),
            new VectorSearchField("Vector", 1536, "default-profile")
        })
        {
            VectorSearch = vectorSearch
        };

        await _indexClient.CreateOrUpdateIndexAsync(index);
    }

    public async Task<long> GetDocumentCountAsync()
    {
        var searchClient = GetSearchClient();
        var response = await searchClient.GetDocumentCountAsync();
        return response.Value;
    }

    public async Task UploadProductsAsync(List<SearchProduct> products)
    {
        var searchClient = GetSearchClient();
        await searchClient.UploadDocumentsAsync(products);
    }

    public SearchClient GetSearchClient()
    {
        return _indexClient.GetSearchClient(_indexName);
    }
}