using AiChatBackend.Caches;
using Microsoft.AspNetCore.Mvc;

namespace AiChatBackend.Controllers;

[Route("rest-api/hub")]
[ApiController]
public class HubController(IHubUserCache user) : ControllerBase
{
    private readonly IHubUserCache user = user;

    [HttpGet("user/is-username-registered")]
    public ActionResult<bool> IsUsernameRegistered([FromQuery] string? username) => user.IsExist(username, UserSessionKeyType.Username);

    [HttpGet("user/is-connection-id-active")]
    public ActionResult<bool> IsConnectionIdActive([FromQuery] string? connectionId) => user.IsActive(connectionId);

    [HttpGet("user/get-by-username")]
    public ActionResult<UserSession> GetUserSessionByUsername([FromQuery] string? username) => user.Find(username, UserSessionKeyType.Username);

    [HttpGet("user/get-by-connection-id")]
    public ActionResult<UserSession> GetUserSessionByConnectionId([FromQuery] string? connectionId) => user.Find(connectionId, UserSessionKeyType.ConnectionId);

    [HttpGet("user/count-all")]
    public ActionResult<int> CountAllUser() => user.CountAll();

    [HttpGet("user/list-all")]
    public ActionResult<List<UserSession>> ListAllUser() => user.ListAllActive();
}
