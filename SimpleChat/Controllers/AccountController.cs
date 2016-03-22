using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SimpleChat.Models;
using System.Web.Security;

namespace SimpleChat.Controllers
{
    public class AccountController : Controller
    {
        ChatDataBaseEntities context = new ChatDataBaseEntities();
        //
        // GET: /Login/

        public ActionResult Index()
        {
            return View();
        }


        public ActionResult Login()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User user, string returnUrl)
        {
            // this action is for handle post (login)
            if (ModelState.IsValid) // this is check validity
            {

                var currentUser = context.User.Where(x => x.Name.Equals(user.Name) && x.Password.Equals(user.Password)).FirstOrDefault();
                if (currentUser != null)
                {
                    Session["LogedUserID"] = currentUser.Id.ToString();
                    Session["LogedUserFullname"] = currentUser.Name.ToString();
                    FormsAuthentication.SetAuthCookie(currentUser.Name, false);

                    return RedirectToAction("Index", "Chat");

                }
                else
                {
                    ModelState.AddModelError("", "Login details are wrong.");
                }
            }
            return View(user);
        }

        public ActionResult LogOut()
        {
            Session.Abandon();
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Account");
        }

    }
}
