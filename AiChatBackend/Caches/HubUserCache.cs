using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AiChatBackend.Caches;

public interface IHubUserCache
{
    bool IsExist(string key, UserSessionKeyType type);
    bool IsActive(string connectionId);
    int CountAll();
    ResponseBase Add(string connectionId, string username, bool force = false);
    ResponseBase Add(HubCallerContext context, bool force = false);
    void Remove(string key, UserSessionKeyType type);
    UserSession Find(string key, UserSessionKeyType type);
    string FindUsername(HubCallerContext context);
    string FindUsernameByConnectionId(string connectionId);
    string FindConnectionIdByUsername(string username);
    List<UserSession> ListAllActive();
}

public class HubUserCache(ILogger<HubUserCache> logger) : IHubUserCache, IDisposable
{
    private readonly ILogger<HubUserCache> logger = logger;

    private List<UserSession> Users { get; set; } = [];

    public int CountAll() => Users.Count;

    public List<UserSession> ListAllActive() => Users;

    public bool IsExist(string key, UserSessionKeyType type)
    {
        return type switch
        {
            UserSessionKeyType.Username => Users.Exists(x => x.Username.Equals(key, StringComparison.CurrentCultureIgnoreCase)),
            UserSessionKeyType.ConnectionId => Users.Exists(x => x.ConnectionId.Equals(key, StringComparison.CurrentCultureIgnoreCase)),
            _ => false,
        };
    }

    public bool IsActive(string connectionId) => IsExist(connectionId, UserSessionKeyType.ConnectionId);

    public UserSession Find(string key, UserSessionKeyType type)
    {
        return type switch
        {
            UserSessionKeyType.Username => Users.Find(x => x.Username.Equals(key, StringComparison.CurrentCultureIgnoreCase)),
            UserSessionKeyType.ConnectionId => Users.Find(x => x.ConnectionId.Equals(key, StringComparison.CurrentCultureIgnoreCase)),
            _ => null,
        };
    }

    public string FindUsername(HubCallerContext context)
    {
        var connectionId = context.ConnectionId;

        var user = Find(connectionId, UserSessionKeyType.ConnectionId);
        return user?.Username;
    }

    public string FindUsernameByConnectionId(string connectionId)
    {
        var user = Find(connectionId, UserSessionKeyType.ConnectionId);
        return user?.Username;
    }

    public string FindConnectionIdByUsername(string username)
    {
        var user = Find(username, UserSessionKeyType.Username);
        return user?.ConnectionId;
    }

    private ResponseBase AddCore(string connectionId, string username)
    {
        try
        {
            var connIdExist = IsExist(connectionId, UserSessionKeyType.ConnectionId);

            if (connIdExist)
                return new()
                {
                    IsSuccess = false,
                    Message = $"Connection ID already exist"
                };

            var usernameExist = IsExist(username, UserSessionKeyType.Username);

            if (usernameExist)
                return new()
                {
                    IsSuccess = false,
                    Message = $"Username {username} already exist"
                };

            UserSession user = new()
            {
                Username = username,
                ConnectionId = connectionId,
                StartTime = DateTime.Now
            };

            Users.Add(user);

            return new()
            {
                IsSuccess = true,
                Message = $"{username}:{connectionId} added"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return new()
            {
                IsSuccess = false,
                Message = $"Exception: {ex.Message}"
            };
        }
    }

    private ResponseBase ForceAdd(string connectionId, string username)
    {
        try
        {
            var connIdExist = IsExist(connectionId, UserSessionKeyType.ConnectionId);
            if (connIdExist)
            {
                Remove(connectionId, UserSessionKeyType.ConnectionId);
                return AddCore(connectionId, username);
            }

            var usernameExist = IsExist(username, UserSessionKeyType.Username);
            if (usernameExist)
            {
                Remove(username, UserSessionKeyType.Username);
                return AddCore(connectionId, username);
            }

            return AddCore(connectionId, username);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return new()
            {
                IsSuccess = false,
                Message = $"Exception: {ex.Message}"
            };
        }
    }

    public ResponseBase Add(string connectionId, string username, bool force)
    {
        try
        {
            var connIdExist = IsExist(connectionId, UserSessionKeyType.ConnectionId);
            if (connIdExist)
            {
                Remove(connectionId, UserSessionKeyType.ConnectionId);
                return force ? ForceAdd(connectionId, username) : AddCore(connectionId, username);
            }

            var usernameExist = IsExist(username, UserSessionKeyType.Username);
            if (usernameExist)
            {
                Remove(username, UserSessionKeyType.Username);
                return force ? ForceAdd(connectionId, username) : AddCore(connectionId, username);
            }

            return force ? ForceAdd(connectionId, username) : AddCore(connectionId, username);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return new()
            {
                IsSuccess = false,
                Message = $"Exception: {ex.Message}"
            };
        }
    }

    public ResponseBase Add(HubCallerContext context, bool force = false)
    {
        var id = context.ConnectionId;
        var username = context.GetHttpContext().Request.Query["username"];
        return Add(id, username, force);
    }

    public void Remove(string key, UserSessionKeyType type)
    {
        try
        {
            var user = Find(key, type);
            if (user != null)
                Users.Remove(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }

    public void Dispose()
    {
        Users.Clear();
        GC.SuppressFinalize(this);
    }
}
