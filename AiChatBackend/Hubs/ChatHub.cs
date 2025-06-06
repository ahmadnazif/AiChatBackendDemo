using AiChatBackend.Caches;
using AiChatBackend.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace AiChatBackend.Hubs;

public class ChatHub(ILogger<ChatHub> logger, IHubUserCache cache, LlmService llm) : Hub
{
    private readonly ILogger<ChatHub> logger = logger;
    private readonly IHubUserCache cache = cache;
    private readonly LlmService llm = llm;

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
        var resp = await llm.GetResponseAsync(msg);
        sw.Stop();

        SingleChatResponse data = new()
        {
            Username = username,
            ConnectionId = Context.ConnectionId,
            RequestMessage = new(ChatSender.User, req.Prompt.Text),
            ResponseMessage = new(ChatSender.Assistant, resp.Messages[0].Text), // resp.Message
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
            chatMessages.Add(new(ChatHelper.GetChatRole(req.Prompt.Sender), req.Prompt.Text));
        }
        else
        {
            foreach (var m in req.PreviousMessages)
            {
                chatMessages.Add(new(ChatHelper.GetChatRole(m.Sender), m.Text));
            }

            chatMessages.Add(new(ChatHelper.GetChatRole(req.Prompt.Sender), req.Prompt.Text));
        }

        Stopwatch sw = Stopwatch.StartNew();
        var resp = await llm.GetResponseAsync(chatMessages, req.ModelId);
        sw.Stop();

        var sender = ChatHelper.GetChatSender(resp.Messages[0].Role); // resp.Message.Role

        ChainedChatResponse data = new()
        {
            Username = username,
            ConnectionId = Context.ConnectionId,
            PreviousMessages = ChatHelper.BuildPreviousMessages(chatMessages),
            ResponseMessage = new(sender, resp.Messages[0].Text), // resp.Message.Text
            Duration = sw.Elapsed,
            ModelId = resp.ModelId
        };

        await Clients.User(username).SendAsync("OnReceivedChained", data);
        LogSent(resp, sw);
    }

    public async IAsyncEnumerable<StreamingChatResponse> StreamChatAsync_OLD(ChainedChatRequest req, [EnumeratorCancellation] CancellationToken ct)
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
                chatMessages.Add(new(ChatHelper.GetChatRole(req.Prompt.Sender), req.Prompt.Text));
            }
            else
            {
                foreach (var m in req.PreviousMessages)
                {
                    chatMessages.Add(new(ChatHelper.GetChatRole(m.Sender), m.Text));
                }
                chatMessages.Add(new(ChatHelper.GetChatRole(req.Prompt.Sender), req.Prompt.Text));
            }

            await foreach (var resp in llm.StreamResponseAsync(chatMessages, req.ModelId, ct))
            {
                yield return resp;
            }

            //var id = Generator.NextStreamingId();
            //logger.LogInformation($"Streaming: {id}");

            //await foreach (var resp in client.GetStreamingResponseAsync(chatMessages, cancellationToken: ct))
            //{
            //    var hasFinished = resp.FinishReason.HasValue;
            //    yield return new()
            //    {
            //        StreamingId = id,
            //        HasFinished = hasFinished,
            //        Message = new(ChatSender.Assistant, resp.Text),
            //        ModelId = resp.ModelId,
            //        CreatedAt = resp.CreatedAt ?? DateTime.UtcNow
            //    };

            //    if (hasFinished)
            //    {
            //        logger.LogInformation($"Streaming {id} completed");
            //    }
            //}
        }
        finally
        {
            if (ct.IsCancellationRequested)
                logger.LogInformation($"Streaming cancelled at {DateTime.Now.ToLongTimeString()}");
        }
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
                chatMessages.Add(new(ChatHelper.GetChatRole(req.Prompt.Sender), req.Prompt.Text));
            }
            else
            {
                foreach (var m in req.PreviousMessages)
                {
                    chatMessages.Add(new(ChatHelper.GetChatRole(m.Sender), m.Text));
                }

                chatMessages.Add(new(ChatHelper.GetChatRole(req.Prompt.Sender), req.Prompt.Text));
            }

            await foreach (var resp in llm.StreamResponseAsync(chatMessages, req.ModelId, ct))
            {
                yield return resp;
            }

            //await foreach (var resp in client.GetStreamingResponseAsync(chatMessages, cancellationToken: ct))
            //{
            //    var hasFinished = resp.FinishReason.HasValue;
            //    yield return new()
            //    {
            //        StreamingId = id,
            //        HasFinished = hasFinished,
            //        Message = new(ChatSender.Assistant, resp.Text),
            //        ModelId = resp.ModelId,
            //        CreatedAt = resp.CreatedAt ?? DateTime.UtcNow
            //    };

            //    if (hasFinished)
            //    {
            //        logger.LogInformation($"Streaming {id} completed");
            //    }
            //}
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
                    chatMessages.Add(new(ChatHelper.GetChatRole(req.Prompt.Sender), req.Prompt.Text));
                }
                else
                {
                    foreach (var m in req.PreviousMessages)
                    {
                        chatMessages.Add(new(ChatHelper.GetChatRole(m.Sender), m.Text));
                    }
                    chatMessages.Add(new(ChatHelper.GetChatRole(req.Prompt.Sender), req.Prompt.Text));
                }

                await foreach (var resp in llm.StreamResponseAsync(chatMessages, req.ModelId, ct))
                {
                    await writer.WriteAsync(resp, ct);

                    if (resp.HasFinished)
                        writer.Complete();
                }

                //await foreach (var resp in client.GetStreamingResponseAsync(chatMessages, cancellationToken: ct))
                //{
                //    var hasFinished = resp.FinishReason.HasValue;
                //    await writer.WriteAsync(new()
                //    {
                //        StreamingId = id,
                //        HasFinished = hasFinished,
                //        Message = new(ChatSender.Assistant, resp.Text),
                //        ModelId = resp.ModelId,
                //        CreatedAt = resp.CreatedAt ?? DateTime.UtcNow
                //    }, ct);

                //    if (hasFinished)
                //    {
                //        logger.LogInformation($"Streaming {id} completed");
                //        writer.Complete();
                //    }
                //}
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

    public async IAsyncEnumerable<StreamingChatResponse> StreamFileChatAsync(FileChatRequest req, [EnumeratorCancellation] CancellationToken ct)
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

            if (req.FileStream == null)
            {
                logger.LogError($"File stream is required");
                yield break;
            }

            logger.LogInformation($"Prompt: {req.Prompt}");

            ChatMessage chatMessage = new(ChatHelper.GetChatRole(req.Prompt.Sender), req.Prompt.Text);

            logger.LogInformation((req.FileStream == null) ? "Filestream is NULL" : $"Filestream length: {req.FileStream.Length}");
            logger.LogInformation(string.IsNullOrWhiteSpace(req.MediaType) ? "Filestream is NULL" : $"Filestream mediatype: {req.MediaType}");

            chatMessage.Contents.Add(new DataContent(req.FileStream, req.MediaType));

            await foreach (var resp in llm.StreamResponseAsync(chatMessage, req.ModelId, ct))
            {
                yield return resp;
            }

            //var id = Generator.NextStreamingId();
            //logger.LogInformation($"Streaming: {id}");

            //await foreach (var resp in client.GetStreamingResponseAsync(chatMessage, cancellationToken: ct))
            //{
            //    var hasFinished = resp.FinishReason.HasValue;
            //    var txt = resp.Text;
            //    yield return new()
            //    {
            //        StreamingId = id,
            //        HasFinished = hasFinished,
            //        Message = new(ChatSender.Assistant, txt),
            //        ModelId = resp.ModelId,
            //        CreatedAt = resp.CreatedAt ?? DateTime.UtcNow
            //    };

            //    if (hasFinished)
            //    {
            //        logger.LogInformation($"Streaming {id} completed");
            //    }
            //}
        }
        finally
        {
            if (ct.IsCancellationRequested)
                logger.LogInformation($"Streaming cancelled at {DateTime.Now.ToLongTimeString()}");
        }
    }

    public async IAsyncEnumerable<StreamingChatResponse> StreamFileChatNewAsync(ChatRequest req, [EnumeratorCancellation] CancellationToken ct)
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

            if (req.Latest == null)
            {
                logger.LogError($"{nameof(req.Latest)} is NULL and it is required");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(req.Latest.Message.Text))
            {
                logger.LogError($"{nameof(req.Latest.Message.Text)} is NULL and it is required");
                yield break;
            }

            logger.LogInformation($"PROMPT: {req.Latest.Message.Text} | ATTACHEMENT: {ChatHelper.GetFileInfo(req.Latest.Files)}");
            foreach (var file in req.Latest.Files)
                logger.LogInformation($"-- File: {file.Filename} ({file.MediaType})");

            List<ChatMessage> chatMessages = [];
            if (req.Previous.Count == 0)
            {
                List<DataContent> dataContents = [];
                foreach (var file in req.Latest.Files)
                    dataContents.Add(new(file.FileStream, file.MediaType));

                ChatMessage chatMessage = new(ChatHelper.GetChatRole(req.Latest.Message.Sender), req.Latest.Message.Text)
                {
                    Contents = [.. dataContents]
                };

                chatMessages.Add(chatMessage);
            }
            else
            {
                foreach (var m in req.Previous)
                {
                    List<DataContent> prevDataContents = [];
                    foreach (var file in m.Files)
                        prevDataContents.Add(new(file.FileStream, file.MediaType));

                    ChatMessage chatMessage = new(ChatHelper.GetChatRole(m.Message.Sender), m.Message.Text)
                    {
                        Contents = [.. prevDataContents]
                    };

                    chatMessages.Add(chatMessage);
                }

                List<DataContent> latestDataContents = [];
                foreach (var file in req.Latest.Files)
                    latestDataContents.Add(new(file.FileStream, file.MediaType));

                ChatMessage latestChatMessage = new(ChatHelper.GetChatRole(req.Latest.Message.Sender), req.Latest.Message.Text)
                {
                    Contents = [.. latestDataContents]
                };

                chatMessages.Add(latestChatMessage);
            }

            await foreach (var resp in llm.StreamResponseAsync(chatMessages, req.ModelId, ct))
            {
                yield return resp;
            }

            //var id = Generator.NextStreamingId();
            //logger.LogInformation($"Streaming: {id}");

            //await foreach (var resp in client.GetStreamingResponseAsync(chatMessages, cancellationToken: ct))
            //{
            //    var hasFinished = resp.FinishReason.HasValue;
            //    var txt = resp.Text;
            //    yield return new()
            //    {
            //        StreamingId = id,
            //        HasFinished = hasFinished,
            //        Message = new(ChatSender.Assistant, txt),
            //        ModelId = resp.ModelId,
            //        CreatedAt = resp.CreatedAt ?? DateTime.UtcNow
            //    };

            //    if (hasFinished)
            //    {
            //        logger.LogInformation($"Streaming {id} completed");
            //    }
            //}
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
        logger.LogInformation($"Response sent. Role: {resp.Messages[0].Role}, Input token: {usage.InputTokenCount}, Output token: {usage.OutputTokenCount}, Duration: {sw.Elapsed}");
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
