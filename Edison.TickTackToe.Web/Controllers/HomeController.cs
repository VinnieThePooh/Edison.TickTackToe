using System.Web.Mvc;
using Edison.TickTackToe.Web.Resources;

namespace Edison.TickTackToe.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = Default.TextApplicationDescriptionPage;
            return View();
        }
    }
}