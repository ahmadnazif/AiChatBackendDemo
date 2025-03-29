using AiChatBackend.Caches;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace AiChatBackend.Hubs;

public class ChatHub(ILogger<ChatHub> logger, IChatClient client, IHubUserCache cache) : Hub
{
    private readonly ILogger<ChatHub> logger = logger;
    private readonly IChatClient client = client;
    private readonly IHubUserCache cache = cache;

    public async Task ReceiveSingleAsync(SingleChatRequest req)
    {
        var username = cache.FindUsername(Context);

        if (username == null)
        {
            logger.LogWarning($"Username {username} not found in cache");
            return;
        }

        if (req == null)
        {
            logger.LogError($"{nameof(req)} is NULL");
            return;
        }

        logger.LogInformation($"Prompt: {req.Prompt}");
        List<ChatMessage> msg = [];

        var role = ChatHelper.GetChatRole(req.Prompt.Sender);
        var text = req.Prompt.Text;

        msg.Add(new(role, text));

        Stopwatch sw = Stopwatch.StartNew();
        var resp = await client.GetResponseAsync(msg);
        sw.Stop();

        SingleChatResponse data = new()
        {
            Username = username,
            ConnectionId = Context.ConnectionId,
            RequestMessage = new(ChatSender.User, req.Prompt.Text),
            ResponseMessage = new(ChatSender.Assistant, resp.Message.Text),
            Duration = sw.Elapsed,
            ModelId = resp.ModelId
        };

        await Clients.User(username).SendAsync("OnReceivedSingle", data);
        LogSent(resp, sw);
    }

    public async Task ReceiveChainedAsync(ChainedChatRequest req)
    {
        var username = cache.FindUsername(Context);

        if (username == null)
        {
            logger.LogWarning($"Username {username} not found in cache");
            return;
        }

        if (req == null)
        {
            logger.LogError($"{nameof(req)} is NULL");
            return;
        }

        if (req.Prompt == null)
        {
            logger.LogError($"{nameof(req.Prompt)} is NULL and it is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Prompt.Text))
        {
            logger.LogError($"{nameof(req.Prompt.Text)} is NULL and it is required");
            return;
        }

        logger.LogInformation($"Prompt: {req.Prompt}");
        List<ChatMessage> chatMessages = [];

        if (req.PreviousMessages.Count == 0)
        {
            chatMessages.Add(new()
            {
                Role = ChatHelper.GetChatRole(req.Prompt.Sender),
                Text = req.Prompt.Text
            });
        }
        else
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
                Role = ChatHelper.GetChatRole(req.Prompt.Sender),
                Text = req.Prompt.Text
            });
        }

        Stopwatch sw = Stopwatch.StartNew();
        var resp = await client.GetResponseAsync(chatMessages);
        sw.Stop();

        var sender = ChatHelper.GetChatSender(resp.Message.Role);

        ChainedChatResponse data = new()
        {
            Username = username,
            ConnectionId = Context.ConnectionId,
            PreviousMessages = ChatHelper.BuildPreviousMessages(chatMessages),
            ResponseMessage = new(sender, resp.Message.Text),
            Duration = sw.Elapsed,
            ModelId = resp.ModelId
        };

        await Clients.User(username).SendAsync("OnReceivedChained", data);
        LogSent(resp, sw);
    }

    public async IAsyncEnumerable<StreamingChatResponse> StreamChatAsync(ChainedChatRequest req, [EnumeratorCancellation] CancellationToken ct)
    {
        try
        {
            var username = cache.FindUsername(Context);

            if (username == null)
            {
                logger.LogWarning($"Username {username} not found in cache");
                yield break;
            }

            if (req == null)
            {
                logger.LogError($"{nameof(req)} is NULL");
                yield break;
            }

            if (req.Prompt == null)
            {
                logger.LogError($"{nameof(req.Prompt)} is NULL and it is required");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(req.Prompt.Text))
            {
                logger.LogError($"{nameof(req.Prompt.Text)} is NULL and it is required");
                yield break;
            }

            logger.LogInformation($"Prompt: {req.Prompt}");
            List<ChatMessage> chatMessages = [];

            if (req.PreviousMessages.Count == 0)
            {
                chatMessages.Add(new()
                {
                    Role = ChatHelper.GetChatRole(req.Prompt.Sender),
                    Text = req.Prompt.Text
                });
            }
            else
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
                    Role = ChatHelper.GetChatRole(req.Prompt.Sender),
                    Text = req.Prompt.Text
                });
            }

            var id = Generator.NextStreamingId();
            logger.LogInformation($"Streaming: {id}");

            await foreach (var resp in client.GetStreamingResponseAsync(chatMessages, cancellationToken: ct))
            {
                var hasFinished = resp.FinishReason.HasValue;
                yield return new()
                {
                    StreamingId = id,
                    HasFinished = hasFinished,
                    Message = new(ChatSender.Assistant, resp.Text),
                    ModelId = resp.ModelId,
                    CreatedAt = resp.CreatedAt ?? DateTime.UtcNow
                };

                if (hasFinished)
                {
                    logger.LogInformation($"Streaming {id} completed");
                }
            }
        }
        finally
        {
            if (ct.IsCancellationRequested)
                logger.LogInformation($"Streaming cancelled at {DateTime.Now.ToLongTimeString()}");
        }
    }

    public ChannelReader<StreamingChatResponse> StreamChatAsChannel(ChainedChatRequest req, CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<StreamingChatResponse>();
        _ = WriteAsync(channel.Writer);

        return channel.Reader;

        async Task WriteAsync(ChannelWriter<StreamingChatResponse> writer)
        {
            try
            {
                var username = cache.FindUsername(Context);

                if (username == null)
                {
                    logger.LogWarning($"Username {username} not found in cache");
                    return;
                }

                if (req == null)
                {
                    logger.LogError($"{nameof(req)} is NULL");
                    return;
                }

                if (req.Prompt == null)
                {
                    logger.LogError($"{nameof(req.Prompt)} is NULL and it is required");
                    return;
                }

                if (string.IsNullOrWhiteSpace(req.Prompt.Text))
                {
                    logger.LogError($"{nameof(req.Prompt.Text)} is NULL and it is required");
                    return;
                }

                logger.LogInformation($"Prompt: {req.Prompt}");
                List<ChatMessage> chatMessages = [];

                if (req.PreviousMessages.Count == 0)
                {
                    chatMessages.Add(new()
                    {
                        Role = ChatHelper.GetChatRole(req.Prompt.Sender),
                        Text = req.Prompt.Text
                    });
                }
                else
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
                        Role = ChatHelper.GetChatRole(req.Prompt.Sender),
                        Text = req.Prompt.Text
                    });
                }

                var id = Generator.NextStreamingId();
                logger.LogInformation($"Streaming: {id}");

                await foreach (var resp in client.GetStreamingResponseAsync(chatMessages, cancellationToken: ct))
                {
                    var hasFinished = resp.FinishReason.HasValue;
                    await writer.WriteAsync(new()
                    {
                        StreamingId = id,
                        HasFinished = hasFinished,
                        Message = new(ChatSender.Assistant, resp.Text),
                        ModelId = resp.ModelId,
                        CreatedAt = resp.CreatedAt ?? DateTime.UtcNow
                    }, ct);

                    if (hasFinished)
                    {
                        logger.LogInformation($"Streaming {id} completed");
                        writer.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                writer.TryComplete(ex);
                logger.LogError(ex.Message);
            }
            finally
            {
                writer.Complete();
                if (ct.IsCancellationRequested)
                    logger.LogInformation($"Channel streaming cancelled at {DateTime.Now.ToLongTimeString()}");
            }
        }
    }

    public async IAsyncEnumerable<StreamingChatResponse> StreamImageChatAsync(FileChatRequest req, [EnumeratorCancellation] CancellationToken ct)
    {
        try
        {
            var username = cache.FindUsername(Context);

            if (username == null)
            {
                logger.LogWarning($"Username {username} not found in cache");
                yield break;
            }

            if (req == null)
            {
                logger.LogError($"{nameof(req)} is NULL");
                yield break;
            }

            if (req.Prompt == null)
            {
                logger.LogError($"{nameof(req.Prompt)} is NULL and it is required");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(req.Prompt.Text))
            {
                logger.LogError($"{nameof(req.Prompt.Text)} is NULL and it is required");
                yield break;
            }

            if(req.FileStream == null)
            {
                logger.LogError($"File stream is required");
                yield break;
            }

            logger.LogInformation($"Prompt: {req.Prompt}");

            ChatMessage chatMessage = new()
            {
                Role = ChatHelper.GetChatRole(req.Prompt.Sender),
                Text = req.Prompt.Text
            };

            chatMessage.Contents.Add(new DataContent(req.FileStream, req.MediaType));


            var id = Generator.NextStreamingId();
            logger.LogInformation($"Streaming: {id}");

            await foreach (var resp in client.GetStreamingResponseAsync(chatMessage, cancellationToken: ct))
            {
                var hasFinished = resp.FinishReason.HasValue;
                yield return new()
                {
                    StreamingId = id,
                    HasFinished = hasFinished,
                    Message = new(ChatSender.Assistant, resp.Text),
                    ModelId = resp.ModelId,
                    CreatedAt = resp.CreatedAt ?? DateTime.UtcNow
                };

                if (hasFinished)
                {
                    logger.LogInformation($"Streaming {id} completed");
                }
            }
        }
        finally
        {
            if (ct.IsCancellationRequested)
                logger.LogInformation($"Streaming cancelled at {DateTime.Now.ToLongTimeString()}");
        }
    }


    private void LogSent(ChatResponse resp, Stopwatch sw)
    {
        var usage = resp.Usage;
        logger.LogInformation($"Response sent. Role: {resp.Message.Role}, Input token: {usage.InputTokenCount}, Output token: {usage.OutputTokenCount}, Duration: {sw.Elapsed}");
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
        logger.LogInformation($"A client disconnected. Connection ID: {connectionId}");
        cache.Remove(connectionId, UserSessionKeyType.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
    #endregion
}
