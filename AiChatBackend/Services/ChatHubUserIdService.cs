using Microsoft.AspNetCore.SignalR;

namespace AiChatBackend.Services;

public class ChatHubUserIdService : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.GetHttpContext().Request.Query["username"];
    }
}
