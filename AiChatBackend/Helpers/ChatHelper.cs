using Microsoft.Extensions.AI;

namespace AiChatBackend.Helpers;

public static class ChatHelper
{
    public static ChatRole GetChatRole(ChatSender sender) => sender switch
    {
        ChatSender.User => ChatRole.User,
        ChatSender.Assistant => ChatRole.Assistant,
        _ => ChatRole.User,
    };
}
