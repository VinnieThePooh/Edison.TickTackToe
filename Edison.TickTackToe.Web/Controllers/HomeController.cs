using System.Web.Mvc;
using Edison.TickTackToe.Domain.DataAccess;
using Edison.TickTackToe.Web.Resources;

namespace Edison.TickTackToe.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }        
    }
}