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

    public static ChatSender GetChatSender(ChatRole role) => role.Value switch
    {
        "user" => ChatSender.User,
        "assistant" => ChatSender.Assistant,
        _ => ChatSender.Assistant,
    };

    public static ChatMsg GetLastChatMsg(List<ChatMsg> all)
    {
        if (all.Count == 0)
            return null;

        return all[^1];
    }

    public static List<ChatMsg> BuildPreviousMessages(List<ChatMessage> messages)
    {
        List<ChatMsg> list = [];
        foreach(var m in messages)
        {
            var sender = GetChatSender(m.Role);
            list.Add(new(sender, m.Text));
        }

        return list;
    }

    public static string GetFileInfo(List<ChatFile> files)
    {
        if (files == null || files.Count == 0)
            return "No file attached";

        return $"{files.Count} {(files.Count > 1 ? "files" : "file")} attached";
    }
}
