using System.Text.Json.Serialization;

namespace RagDemo.Models;

public record Product(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description
);