namespace AiChatBackend.Models;

public class ChatHubChatResponse
{
    public string? RequestMessage { get; set; }
    public string? ResponseMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ModelId { get; set; }
}
