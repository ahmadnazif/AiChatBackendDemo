using System.Text.Json.Serialization;

namespace AiChatBackend.Models;


public class OllamaTagsResponse
{
    public List<OllamaModelBase> Models { get; set; } = [];
}

public class OllamaPsResponse
{
    public List<OllamaPsModel> Models { get; set; } = [];
}

public class OllamaPsModel : OllamaModelBase
{
    [JsonPropertyName("expires_at")] public DateTime ExpiresAt { get; set; }
    [JsonPropertyName("size_vram")] public long SizeVram { get; set; }
}

public class OllamaModelBase
{
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Digest { get; set; } = string.Empty;
    public OllamaModelDetails Details { get; set; } = new();
}

public class OllamaModelDetails
{
    [JsonPropertyName("parent_model")] public string ParentModel { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public List<string> Families { get; set; } = [];
    [JsonPropertyName("parameter_size")] public string ParameterSize { get; set; } = string.Empty;
    [JsonPropertyName("quantization_level")] public string QuantizationLevel { get; set; } = string.Empty;
}

