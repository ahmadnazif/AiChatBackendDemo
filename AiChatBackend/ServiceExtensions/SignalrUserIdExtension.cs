using AiChatBackend.Services;
using Microsoft.AspNetCore.SignalR;

namespace AiChatBackend.ServiceExtensions;

public static class SignalrUserIdExtension
{
    public static void AddSignalrUserIdentifier(this IServiceCollection services)
    {
        services.AddSingleton<IUserIdProvider, ChatHubUserIdService>();
    }
}
