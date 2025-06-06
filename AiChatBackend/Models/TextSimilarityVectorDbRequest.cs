namespace AiChatBackend.Models;

public class TextSimilarityVectorDbRequest
{
    public string? Prompt { get; set; }
    public int Top { get; set; }
}
