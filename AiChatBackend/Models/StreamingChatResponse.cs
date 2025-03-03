namespace AiChatBackend.Models;

public class StreamingChatResponse
{
    public string StreamingId { get; set; }
    public bool HasFinished { get; set; }
    public ChatMsg Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
