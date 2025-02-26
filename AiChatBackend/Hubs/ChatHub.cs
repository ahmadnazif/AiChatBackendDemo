using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;

namespace AiChatBackend.Hubs;

public class ChatHub(ILogger<ChatHub> logger, IChatClient chatClient) : Hub
{
    private readonly ILogger<ChatHub> logger = logger;
    private readonly IChatClient chatClient = chatClient;

    #region Override methods
    public override Task OnConnectedAsync()
    {
        var id = Context.ConnectionId;
        logger.LogInformation($"A client connected. Connection ID: {id}");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
            logger.LogError(exception.Message);

        return base.OnDisconnectedAsync(exception);
    }
    #endregion
}
