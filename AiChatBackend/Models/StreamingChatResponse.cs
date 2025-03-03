namespace AiChatBackend.Models;

public class StreamingChatResponse
{
    public bool HasFinished { get; set; }
    public string? Text { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
