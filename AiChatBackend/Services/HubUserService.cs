using Microsoft.Extensions.Logging;

namespace AiChatBackend.Services;

public class HubUserService(ILogger<HubUserService> logger) : IHubUserService, IDisposable
{
    private ILogger<HubUserService> logger = logger;
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

    public bool IsActive(string connectionId)
    {
        return IsExist(connectionId, UserSessionKeyType.ConnectionId);
    }

    public UserSession Find(string key, UserSessionKeyType type)
    {
        return type switch
        {
            UserSessionKeyType.Username => Users.Find(x => x.Username.ToLower() == key.ToLower()),
            UserSessionKeyType.ConnectionId => Users.Find(x => x.ConnectionId.ToLower() == key.ToLower()),
            _ => null,
        };
    }

    public string FindUsername(string connectionId)
    {
        var user = Find(connectionId, UserSessionKeyType.ConnectionId);
        return user?.Username;
    }

    public string FindConnectionId(string username)
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
