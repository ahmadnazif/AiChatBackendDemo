namespace AiChatBackend.Models;

public class EmbeddingQueryRequest
{
    public string? CollectionName { get; set; }
    public string? Prompt { get; set; }
    public int Top { get; set; } = 1;
}
