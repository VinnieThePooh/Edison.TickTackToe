using System.Web.Mvc;
using Edison.TickTackToe.Domain.DataAccess;
using Edison.TickTackToe.Web.Resources;

namespace Edison.TickTackToe.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            using (var context = new GameContext())
            {
                context.Database.Initialize(true);
            }
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = DefaultResources.AppDescText;
            return View();
        }
    }
}