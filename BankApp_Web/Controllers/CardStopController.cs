using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankApp_Web.Controllers
{
    [AllowAnonymous]
    public class CardStopController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
