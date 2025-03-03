namespace AiChatBackend.Models;

public class ChainedChatRequest
{
    public List<ChatMsg> PreviousMessages { get; set; }
    public ChatMsg Prompt { get; set; }
}