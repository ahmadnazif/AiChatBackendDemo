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

    public async Task ReceiveOneAsync(OneChatRequest req)
    {
        var username = cache.FindUsername(Context); //cache.FindUsernameByConnectionId(Context.ConnectionId);
        if (username == null)
            logger.LogWarning($"Username {username} not found in cache");
        else
        {
            logger.LogInformation($"Prompt: {req.Message}");
            List<ChatMessage> msg = [];

            var role = ChatHelper.GetChatRole(req.Message.Sender);
            var text = req.Message.Text;

            msg.Add(new(role, text));

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
            logger.LogInformation($"Response generated & sent. Role: {resp.Message.Role}, Duration: {sw.Elapsed}");
        }
    }

    public async Task ReceiveChainedAsync(ChainedChatRequest req)
    {
        var username = cache.FindUsername(Context); //cache.FindUsernameByConnectionId(Context.ConnectionId);

        if (username == null)
        {
            logger.LogWarning($"Username {username} not found in cache");
            return;
        }

        if(req == null)
        {
            logger.LogError($"{nameof(req)} is NULL");
            return;
        }

        if (req.LatestMessage == null)
        {
            logger.LogError($"{nameof(req.LatestMessage)} is NULL and it is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(req.LatestMessage.Text))
        {
            logger.LogError($"{nameof(req.LatestMessage.Text)} is NULL and it is required");
            return;
        }

        List<ChatMessage> chatMessages = [];

        if (req.PreviousMessages.Count == 0) // Initial chat
        {
            chatMessages.Add(new()
            {
                Role = ChatHelper.GetChatRole(req.LatestMessage.Sender),
                Text = req.LatestMessage.Text
            });
        }
        else  // Subsequent chat
        {
            foreach (var m in req.PreviousMessages)
            {
                chatMessages.Add(new()
                {
                    Role = ChatHelper.GetChatRole(m.Sender),
                    Text = m.Text,
                });
            }

            chatMessages.Add(new()
            {
                Role = ChatHelper.GetChatRole(req.LatestMessage.Sender),
                Text = req.LatestMessage.Text
            });
        }

        logger.LogInformation($"Prompt: {req.LatestMessage.Text}");

        Stopwatch sw = Stopwatch.StartNew();
        var resp = await client.GetResponseAsync(chatMessages);
        sw.Stop();

        var sender = ChatHelper.GetChatSender(resp.Message.Role);

        ChainedChatResponse r = new()
        {
            Username = username,
            ConnectionId = Context.ConnectionId,
            PreviousMessages = ChatHelper.BuildPreviousMessages(chatMessages),
            ResponseMessage = new(sender, resp.Message.Text),
            Duration = sw.Elapsed,
            ModelId = resp.ModelId
        };

        await Clients.User(username).SendAsync("OnReceivedChained", r);
        logger.LogInformation($"Response generated & sent. [{resp.Choices.Count} choices] Chained: {req.PreviousMessages.Count} msgs, Role: {resp.Message.Role}, Duration: {sw.Elapsed}");
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
