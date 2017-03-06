using System.Web.Mvc;

namespace Edison.TickTackToe.Web.Controllers
{
    [Authorize]
    public class GamesController : Controller
    {
        // GET: Game
        public ActionResult Index()
        {
            return View();
        }
    }
}