using System;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Edison.TickTackToe.Domain.DataAccess;
using Edison.TickTackToe.Domain.Infrastructure.Handbooks;
using Edison.TickTackToe.Domain.Models;
using Edison.TickTackToe.Web.Infrastructure;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Edison.TickTackToe.Web.Models;
using Edison.TickTackToe.Web.Resources;
using Microsoft.AspNet.SignalR;

namespace Edison.TickTackToe.Web.Controllers
{
    [System.Web.Mvc.Authorize]
    public class AccountController : Controller
    {
        #region Fields

        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        #endregion

        #region Constructors

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        #endregion

        #region Properties

        public ApplicationSignInManager SignInManager
        {
            get { return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>(); }
            private set { _signInManager = value; }
        }

        public ApplicationUserManager UserManager
        {
            get { return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
            private set { _userManager = value; }
        }

        #endregion

        #region Implementation details
        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost] 
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
                var user= await UserManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", DefaultResources.ValInvalidLoginAttempt);
                    return View(model);
                }

                await SignInManager.SignInAsync(user, model.RememberMe, false);
                OnlineUsersTracker.AddOnlineUser(new UserProjection() { Email = model.Email, Name = user.UserName,  Status = StatusNames.Idle });
                return RedirectToLocal(returnUrl);
        }

        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new Member {UserName = model.UserName, NickName = model.UserName, Email = model.Email};
                    var result = await UserManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        OnlineUsersTracker.AddOnlineUser(new UserProjection { Name = model.UserName, Email = model.Email, Status = StatusNames.Idle });
                        return RedirectToAction("Index", "Games");
                    }
                    AddErrors(result);
            }
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                // string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);		
                // await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                // return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }


        //
        // POST: /Account/LogOff
        // can be refactored
        // there are redundant requests here
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> LogOff()
        {
            var id = HttpContext.User.Identity.GetUserId<int>();
            var user = await UserManager.FindByIdAsync(id);

            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            await SetUserStatus(StatusNames.Offline, user);
            await FinishExistedGame(user);

            OnlineUsersTracker.RemoveOnlineUser(new UserProjection() {Email = user.Email, Name = user.UserName});
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers

        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Games");
        }


        private async Task FinishExistedGame(Member loggingOffUser)
        {
            var gameContext = HttpContext.GetOwinContext().Get<GameContext>();

            var game = await gameContext.Games.SingleOrDefaultAsync(g => g.WinnerUserName == null &&
                                                          (g.PlayerInitiator.UserName.Equals(loggingOffUser.UserName) ||
                                                           g.OpponentUserName.Equals(loggingOffUser.UserName)));

            if (game == null) 
                return;
            
            Member otherPlayer;
            if (loggingOffUser.UserName.Equals(game.OpponentUserName))
                otherPlayer = game.PlayerInitiator;
            else otherPlayer = gameContext.Users.Single(u => u.UserName.Equals(game.OpponentUserName));
            game.WinnerUserName = otherPlayer.UserName;
            game.GameEndingDate = DateTime.Now;
            game.GameDescription = DefaultResources.GameDescriptionUserSuddenlyLeftSite.Replace("#",loggingOffUser.UserName);
            await gameContext.SaveChangesAsync();

            var context = GlobalHost.ConnectionManager.GetHubContext<GamesHub>();
            context.Clients.Client(otherPlayer.ConnectionId).playerSuddenlyLeftTheSite();
        }


        private async Task<MemberStatus> GetStatusByName(string statusName)
        {
            var gameContext = HttpContext.GetOwinContext().Get<GameContext>();
            return await gameContext.MemberStatuses.SingleAsync(ms => ms.Name.Equals(statusName));
        }


        private async Task SetUserStatus(string statusName, Member user)
        {
            var status = await GetStatusByName(statusName);
            var gameContext = HttpContext.GetOwinContext().Get<GameContext>();
            var targetuser = gameContext.Users.Find(user.Id);
            if (statusName.Equals(StatusNames.Offline))
                targetuser.ConnectionId = null;
            targetuser.IdGameStatus = status.StatusId;
            await gameContext.SaveChangesAsync();
        }



        #endregion

        #endregion
    }
}