using AiChatBackend.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;

namespace AiChatBackend.Hubs;

public class ChatHub(ILogger<ChatHub> logger, IChatClient chatClient, IHubUserService user) : Hub
{
    private readonly ILogger<ChatHub> logger = logger;
    private readonly IChatClient chatClient = chatClient;
    private readonly IHubUserService user = user;

    public async Task SendAsync(ChatArgsBase args)
    {
        var username = user.FindUsername(Context.ConnectionId);
        //await Clients.User(username).SendAsync("Receive", )
    }

    #region Override methods
    public override Task OnConnectedAsync()
    {
        user.Add(Context);
        logger.LogInformation($"A client connected. Connection ID: {Context.ConnectionId}");
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
