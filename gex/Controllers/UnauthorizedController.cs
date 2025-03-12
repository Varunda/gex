using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gex.Controllers {

    [AllowAnonymous]
    public class UnauthorizedController : Controller {

        public UnauthorizedController() {

        }

        public IActionResult Index() {
            return View();
        }

    }
}
