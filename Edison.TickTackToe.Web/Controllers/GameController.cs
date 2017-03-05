using System.Web.Mvc;

namespace Edison.TickTackToe.Web.Controllers
{
    public class GamesController : Controller
    {
        // GET: Game
        public ActionResult Index()
        {
            return View();
        }
    }
}