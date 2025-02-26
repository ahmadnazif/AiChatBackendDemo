namespace AiChatBackend.Models;

public class UserSession
{
    public string Username { get; set; }
    public string ConnectionId { get; set; }
    public DateTime StartTime { get; set; }
}
