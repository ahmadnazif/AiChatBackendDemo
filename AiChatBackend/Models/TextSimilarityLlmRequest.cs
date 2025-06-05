namespace AiChatBackend.Models;

public class TextSimilarityLlmRequest
{
    public string? OriginalPrompt { get; set; }
    public List<string> Results { get; set; }
    public string? ModelId { get; set; }
}
