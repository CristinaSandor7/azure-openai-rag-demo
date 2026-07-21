using Azure.Search.Documents.Indexes;

namespace RagDemo.Models;

public class SearchProduct
{
    [SimpleField(IsKey = true, IsFilterable = true)]
    public string Id { get; set; } = "";

    [SearchableField]
    public string Name { get; set; } = "";

    [SearchableField]
    public string Description { get; set; } = "";

    [VectorSearchField(VectorSearchDimensions = 1536, VectorSearchProfileName = "default-profile")]
    public float[] Vector { get; set; } = Array.Empty<float>();
}