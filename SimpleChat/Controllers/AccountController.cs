using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SimpleChat.Models;
using System.Web.Security;
using System.Data.Entity.Validation;

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
        
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(User user)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var isLogineAvalible = context.User.Where(x => x.ChatName == user.ChatName).FirstOrDefault();

                    if (isLogineAvalible == null)
                    {
                        user.IsOnline = false;

                        context.User.Add(user);
                        context.SaveChanges();
                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {
                        ModelState.AddModelError("", "User is already exist.");
                    }

                }
                else
                {
                    ModelState.AddModelError("", "Please check all required fields.");
                }
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"", ve.PropertyName, ve.ErrorMessage);
                    }
                }
                throw;
            }
            return View();
        }

        public ActionResult Login()
        {
            if (FormsAuthentication.CookiesSupported)
            {
                var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
                if (authCookie != null && Session["LogedUserID"] != null)
                {
                    return RedirectToAction("Index", "Chat");
                }
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User user)
        {

            var currentUser = context.User.Where(x => x.ChatName.Equals(user.ChatName) && x.Password.Equals(user.Password)).FirstOrDefault();
            if (currentUser != null)
            {
                currentUser.IsOnline = true;

                context.SaveChanges();

                Session["LogedUserID"] = currentUser.Id.ToString();
                Session["LogedUserFullname"] = currentUser.FullName.ToString();
                FormsAuthentication.SetAuthCookie(currentUser.ChatName, false);

                return RedirectToAction("Index", "Chat");

            }
            else
            {
                ModelState.AddModelError("", "Login details are wrong.");
            }

            return View(user);
        }

        public ActionResult LogOut()
        {
            if (Session["LogedUserID"] != null)
            {
                var currentUser = context.User.FirstOrDefault(x => x.Id == (int)Session["LogedUserID"]);
                if (currentUser != null)
                {
                    currentUser.IsOnline = false;
                    currentUser.LastOnlineDate = DateTime.Now;
                }
            }
            FormsAuthentication.SignOut();
            Session.Abandon();

            return RedirectToAction("Login", "Account");
        }

        //private bool IsValid(string email, string password)
        //{
        //    var crypto = new SimpleCrypto.PBKDF2();
        //    bool IsValid = false;

        //    using (var db = new LoginInMVC4WithEF.Models.UserEntities2())
        //    {
        //        var user = db.Registrations.FirstOrDefault(u => u.Email == email);
        //        if (user != null)
        //        {
        //            if (user.Password == crypto.Compute(password, user.PasswordSalt))
        //            {
        //                IsValid = true;
        //            }
        //        }
        //    }
        //    return IsValid;
        //} 

    }
}
