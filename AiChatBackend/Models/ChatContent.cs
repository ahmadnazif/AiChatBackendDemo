namespace AiChatBackend.Models;

public class ChatContent
{
    public ChatMsg Message { get; set; }
    public List<ChatFile> Files { get; set; }
}
