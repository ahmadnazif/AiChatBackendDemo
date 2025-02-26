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

    public async Task SendAsync(ChatHubChatRequest req)
    {
        var username = user.FindUsername(Context.ConnectionId);
        if (username == null)
            logger.LogWarning($"Username {username} not found in cache");
        else
        {
            List<ChatMessage> msg = [];
            msg.Add(new(ChatRole.User, req.Message));

            Stopwatch sw = Stopwatch.StartNew();
            //var cresp = await chatClient.GetResponseAsync(msg);
            sw.Stop();

            ChatHubChatResponse resp = new()
            {
                RequestMessage = req.Message,
                ResponseMessage = DateTime.Now.ToString(), //cresp.Message.Text,
                Duration = sw.Elapsed
            };

            await Clients.User(username).SendAsync("Receive", resp);
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

        user.Remove(Context.ConnectionId, UserSessionKeyType.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
    #endregion
}
