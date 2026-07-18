using Microsoft.AspNetCore.Mvc;

namespace TMDT1_TH.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Dashboard", new { area = "Admin" });

    public IActionResult Error() => View();
}
