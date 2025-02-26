﻿namespace AiChatBackend.Services;

public interface IHubUserService
{
    bool IsExist(string key, UserSessionKeyType type);
    bool IsActive(string connectionId);
    int CountAll();
    ResponseBase Add(string connectionId, string username, bool force = false);
    void Remove(string key, UserSessionKeyType type);
    UserSession Find(string key, UserSessionKeyType type);
    string FindUsername(string connectionId);
    string FindConnectionId(string username);
    List<UserSession> ListAllActive();

}
