using AiChatBackend.Caches;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AiChatBackend.Hubs;

public class ChatHub(ILogger<ChatHub> logger, IChatClient client, IHubUserCache cache) : Hub
{
    private readonly ILogger<ChatHub> logger = logger;
    private readonly IChatClient client = client;
    private readonly IHubUserCache cache = cache;

    private static ChatRole GetChatRole(ChatSender sender) => sender switch
    {
        ChatSender.User => ChatRole.User,
        ChatSender.Assistant => ChatRole.Assistant,
        _ => ChatRole.User,
    };

    public async Task ReceiveOneAsync(OneChatRequest req)
    {
        var username = cache.FindUsername(Context); //cache.FindUsernameByConnectionId(Context.ConnectionId);
        if (username == null)
            logger.LogWarning($"Username {username} not found in cache");
        else
        {
            logger.LogInformation($"Prompt: {req.Message}");
            List<ChatMessage> msg = [];
            msg.Add(new(GetChatRole(req.Message.Sender), req.Message.Text));

            Stopwatch sw = Stopwatch.StartNew();
            var resp = await client.GetResponseAsync(msg);
            sw.Stop();

            OneChatResponse r = new()
            {
                Username = username,
                ConnectionId = Context.ConnectionId,
                RequestMessage = req.Message.Text,
                ResponseMessage = resp.Message.Text,
                Duration = sw.Elapsed,
                ModelId = resp.ModelId
            };

            await Clients.User(username).SendAsync("OnReceivedOne", r);
            logger.LogInformation($"Response generated & sent. Role: {resp.Message.Role} Duration: {sw.Elapsed}");
        }
    }


    #region Override methods
    public override Task OnConnectedAsync()
    {
        cache.Add(Context);
        logger.LogInformation($"A client connected. Connection ID: {Context.ConnectionId}");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
            logger.LogError(exception.Message);

        var connectionId = Context.ConnectionId;
        logger.LogInformation($"A client has been disconnected. Connection ID: {connectionId}");
        cache.Remove(connectionId, UserSessionKeyType.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
    #endregion
}
