namespace AiChatBackend.Models;

public class ChainedChatResponse
{
    public string? Username { get; set; }
    public string? ConnectionId { get; set; }
    public List<ChatMsg> PreviousMessages { get; set; }
    public ChatMsg ResponseMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ModelId { get; set; }
}
