using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Edison.TickTackToe.Domain.DataAccess;
using Edison.TickTackToe.Web.Infrastructure.Attributes;
using Edison.TickTackToe.Web.Resources;
using Microsoft.AspNet.Identity.Owin;

namespace Edison.TickTackToe.Web.Controllers
{
    public class ValidationController: Controller
    {
        [AllowAnonymous, AjaxOnly]
        public async Task<JsonResult> VerifyUserName(string userName)
        {
            var context = HttpContext.GetOwinContext().Get<GameContext>();
            var exist = await context.Users.AnyAsync(u => u.UserName.Equals(userName));
            if (exist)
                return Json(DefaultResources.ValSuchUserNameExists, JsonRequestBehavior.AllowGet);
            return Json(true, JsonRequestBehavior.AllowGet);
        }


        [AllowAnonymous,AjaxOnly]
        public async Task<JsonResult> VerifyEmail(string email)
        {
            var context = HttpContext.GetOwinContext().Get<GameContext>();
            var exist = await context.Users.AnyAsync(u => u.Email.Equals(email));
            if (exist)
                return Json(DefaultResources.ValSuchEmailAlreadyExists, JsonRequestBehavior.AllowGet);
            return Json(true, JsonRequestBehavior.AllowGet);
        }
    }
}