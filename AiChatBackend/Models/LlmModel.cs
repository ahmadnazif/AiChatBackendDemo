namespace AiChatBackend.Models;

public class LlmModel
{
    public LlmModelType ModelType { get; set; }
    public string ModelTypeName => ModelType.ToString();
    public List<string> ModelIds { get; set; }
    public string? DefaultModelId { get; set; }
}
