using Microsoft.AspNetCore.Mvc;

namespace AiChatBackend.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[ApiController]
[Route("/")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public ActionResult Home() => Redirect("swagger");
}
