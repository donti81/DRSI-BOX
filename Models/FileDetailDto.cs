using System.Text.Json.Serialization;

namespace Starterkit.Models
{
    internal record FileDetailDto(
        [property: JsonPropertyName("id")]   long?  Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("size")] long   Size
    );
}
