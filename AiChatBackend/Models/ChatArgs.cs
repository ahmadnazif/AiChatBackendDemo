namespace AiChatBackend.Models;

public class ChatArgs : ChatArgsBase
{
    public string? ConnectionId { get; set; }
}

public class ChatArgsBase
{
    public string? Message { get; set; }
}
