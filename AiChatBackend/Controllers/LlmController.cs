using Microsoft.AspNetCore.Mvc;

namespace AiChatBackend.Controllers;

[Route($"{BASE_ROUTE}/llm")]
[ApiController]
public class LlmController(IConfiguration config) : ControllerBase
{
    private readonly IConfiguration config = config;

    [HttpGet("get-model")]
    public ActionResult<LlmModel> GetModel([FromQuery] LlmModelType type)
    {
        return LlmModelHelper.GetModel(config, type);
    }
}
