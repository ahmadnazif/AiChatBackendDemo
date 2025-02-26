namespace AiChatBackend.Models;

public class ChatHubBatchChatRequest
{
    public List<ChatRequest> Messages { get; set; }

}

public record ChatRequest(ChatSender Type, string Message);
