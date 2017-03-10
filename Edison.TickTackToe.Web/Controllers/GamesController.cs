using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity.Owin;

namespace Edison.TickTackToe.Web.Controllers
{
    [Authorize]
    public class GamesController : Controller
    {
        // GET: Game
        public async Task<ActionResult> Index()
        {
            var manager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var user = await manager.FindByNameAsync(HttpContext.User.Identity.Name);
            ViewBag.Email = user.Email;
            return View();
        }
    }
}