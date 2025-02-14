﻿
using Alliance_for_Life.Models;
using Alliance_for_Life.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Alliance_for_Life.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext db;
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController()
        {
            db = new ApplicationDbContext();
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //indexpage
        [AllowAnonymous]
        public ActionResult Index(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            List<SubContractor> OrgList = db.SubContractors.OrderBy(a => a.OrgName).ToList();
            ViewBag.Subcontractors = OrgList;


            return View();
        }
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

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: true);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //list user information
        [Authorize]
        public ActionResult IndexforRegisteredUsers(string searchString, string currentFilter, int? page, int? pgSize)
        {
            ViewBag.Subcontractor = new SelectList(db.SubContractors.OrderBy(a => a.OrgName), "OrgName", "OrgName");

            List<Userinformation> modelLst = new List<Userinformation>();
            var role = db.Roles.Include(x => x.Users).ToList();
            foreach (var r in role)
            {
                foreach (var u in r.Users)
                {
                    var usr = db.Users.Find(u.UserId);
                    var usersub = db.SubContractors.Find(usr.SubcontractorId).OrgName;

                    if (usersub == null)
                    {
                        usersub = "";
                    }


                    // looking for the searchstring
                    if (searchString != null)
                    {
                        page = 1;
                    }
                    else
                    {
                        currentFilter = searchString;
                    }

                    ViewBag.CurrentFilter = searchString;

                    var obj = new Userinformation
                    {
                        Id = new Guid(usr.Id),
                        Firstname = usr.FirstName,
                        Lastname = usr.LastName,
                        Email = usr.Email,
                        Role = r.Name,
                        Organization = usersub
                    };

                    modelLst.Add(obj);
                }
            }
            if (!String.IsNullOrEmpty(searchString))
            {

                foreach (var items in modelLst.ToList())
                {
                    if (items.Organization != searchString)
                    {
                        modelLst.Remove(items);
                    }
                }
            }

            int pageNumber = (page ?? 1);
            int defaSize = (pgSize ?? 15);

            ViewBag.psize = defaSize;

            ViewBag.PageSize = new List<SelectListItem>()
            {
                new SelectListItem() { Value="10", Text= "10" },
                new SelectListItem() { Value="20", Text= "20" },
                new SelectListItem() { Value="30", Text= "30" },
                new SelectListItem() { Value="40", Text= "40" },
            };
            
            return View(modelLst.OrderBy(s => s.Organization).ToPagedList(pageNumber, defaSize));
        }

        [Authorize]
        public ActionResult Edit(Guid? id)
        {

            var client = db.Users.Find(id.ToString());
            var clientrole = client.Roles.FirstOrDefault().RoleId;
            var viewModel = new RegisterViewModel
            {
                FirstName = client.FirstName,
                LastName = client.LastName,
                Email = client.Email,
                Subcontractors = db.SubContractors.ToList(),
                RoleName = db.Roles.Find(clientrole).Name
            };
            viewModel.SubcontractorId = client.SubcontractorId;
            ViewBag.SubcontractorId = new SelectList(db.SubContractors.ToList(), "SubcontractorId", "OrgName", client.SubcontractorId);
            ViewBag.Role = new SelectList(db.Roles.ToList(), "Id", "Name", clientrole);

            return View("Edit", viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Userinformation user)
        {
            var client = db.Users.Find(user.Id.ToString());
            var clientrole = client.Roles.FirstOrDefault().RoleId;
            var oldrolename = db.Roles.SingleOrDefault(r => r.Id == clientrole).Name;
            var org = Request["SubcontractorId"];

            //updating database
            if (ModelState.IsValid)
            {

                var updateduser = db.Users.Single(s => s.Id == user.Id.ToString());
                updateduser.SubcontractorId = new Guid(org);
                updateduser.FirstName = user.Firstname;
                updateduser.LastName = user.Lastname;
                updateduser.Email = user.Email;
                updateduser.UserName = user.Email;
                PasswordHasher password = new PasswordHasher();
                var passwordhash = password.HashPassword(user.Password);
                updateduser.PasswordHash = passwordhash;

                if (oldrolename != db.Roles.SingleOrDefault(r => r.Id == user.Role).Name)
                {
                    UserManager.RemoveFromRole(user.Id.ToString(), oldrolename);
                    UserManager.AddToRole(user.Id.ToString(), db.Roles.SingleOrDefault(r => r.Id == user.Role).Name);
                }

                db.Entry(updateduser).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("IndexforRegisteredUsers");
            }
            client = db.Users.Find(user.Id);
            clientrole = client.Roles.FirstOrDefault().RoleId;

            ViewBag.SubcontractorId = new SelectList(db.SubContractors.ToList(), "SubcontractorId", "OrgName", user.Organization);
            ViewBag.Role = new SelectList(db.Roles.ToList(), "Id", "Name", clientrole);

            return View("Register", user);
        }

        // GET: RegisteredUsers/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Role user = db.Users.Include(r => r.Roles).SingleOrDefault(s => s.Id == id);

            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: RegisteredUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {

            Role user = db.Users.Include(r => r.Roles).SingleOrDefault(s => s.Id == id);

            db.Users.Remove(user);
            db.SaveChanges();
            return RedirectToAction("IndexforRegisteredUsers");
        }

        [Authorize]
        // GET: /Account/Register
        public ActionResult Register()
        {
            var viewModel = new RegisterViewModel
            {
                Subcontractors = db.SubContractors.OrderBy(a => a.OrgName).ToList()
            };

            ViewBag.RoleName = new SelectList(db.Roles.ToList(), "Name", "Name");
            return View(viewModel);
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                viewModel.Subcontractors = db.SubContractors.ToList();
                return View("Create", viewModel);
            }

            {
                var user = new Role
                {
                    SubcontractorId = viewModel.SubcontractorId,
                    Email = viewModel.Email,
                    FirstName = viewModel.FirstName,
                    LastName = viewModel.LastName,
                    UserName = viewModel.Email
                };

                db.Users.Add(user);

                var result = await UserManager.CreateAsync(user, viewModel.Password);
                if (result.Succeeded)
                {
                    //  await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    //add to role

                    UserManager.AddToRole(user.Id, db.Roles.SingleOrDefault(r => r.Id == "f11bb44b-b367-4650-b48e-abbcedf296b0").Name);
                    return RedirectToAction("Create", "AFLForm");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(viewModel);
        }

        [Authorize]
        [HttpGet]
        public ActionResult RegisterRole()
        {
            ViewBag.RoleName = new SelectList(db.Roles.ToList(), "Name", "Name");
            ViewBag.UserName = new SelectList(db.Users.ToList(), "UserName", "UserName");
            return View();
        }

        //POST: /Account/Register
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RegisterRole(RegisterViewModel model, Role user)
        {
            var userId = db.Users.Where(i => i.UserName == user.UserName).Select(s => s.Id);
            string updateId = "";
            foreach (var i in userId)
            {
                updateId = i.ToString();
            }
            //Assign Role to user here
            await this.UserManager.AddToRoleAsync(updateId, model.RoleName);

            return RedirectToAction("Index", "Home");
        }


        //
        // GET: /Account/ConfirmEmail
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
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
                string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
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
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new Role { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Account");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
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

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

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
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}