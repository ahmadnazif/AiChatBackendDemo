using Microsoft.AspNetCore.Mvc;

namespace AiChatBackend.Controllers;

[Route("/")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public ActionResult Home() => Redirect("swagger");
}
