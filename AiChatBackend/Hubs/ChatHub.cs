using AiChatBackend.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using System.Diagnostics;

namespace AiChatBackend.Hubs;

public class ChatHub(ILogger<ChatHub> logger, IChatClient chatClient, IHubUserService user) : Hub
{
    private readonly ILogger<ChatHub> logger = logger;
    private readonly IChatClient chatClient = chatClient;
    private readonly IHubUserService user = user;

    public async Task ReceiveMessageAsync(ChatHubChatRequest req)
    {
        var username = user.FindUsername(Context.ConnectionId);
        if (username == null)
            logger.LogWarning($"Username {username} not found in cache");
        else
        {
            logger.LogInformation($"Prompt: {req.Message}");
            List<ChatMessage> msg = [];
            msg.Add(new(ChatRole.User, req.Message));

            Stopwatch sw = Stopwatch.StartNew();
            var resp = await chatClient.GetResponseAsync(msg);
            sw.Stop();

            ChatHubChatResponse r = new()
            {
                Username = username,
                ConnectionId = Context.ConnectionId,
                RequestMessage = req.Message,
                ResponseMessage = resp.Message.Text,
                Duration = sw.Elapsed,
                ModelId = resp.ModelId
            };

            await Clients.User(username).SendAsync("OnReceived", r);
            logger.LogInformation($"Response generated & sent [{sw.Elapsed}]");
        }
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

        var connectionId = Context.ConnectionId;
        logger.LogInformation($"A client has been disconnected. Connection ID: {connectionId}");
        user.Remove(connectionId, UserSessionKeyType.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
    #endregion
}
