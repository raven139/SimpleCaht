using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace SimpleChat
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        protected void FormsAuthentication_OnAuthenticate(Object sender, FormsAuthenticationEventArgs e)
        {
            if (FormsAuthentication.CookiesSupported == true)
            {
                if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
                {
                    try
                    {
                        // Проверяем дату создания auth cookies если такие есть в наличии
                        var authCookiesDate = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).IssueDate;
                        var today = DateTime.Now.Date;

                        // Если тикет создан позже сегодняшнего дня - убиваем auth cookies
                        if (DateTime.Compare(authCookiesDate, today) < 0)
                        {
                            FormsAuthentication.SignOut();
                        }

                        // Вытягивает имя пользователя               
                        string username = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
                        string roles = string.Empty;

                        // Находим роль в базе данных
                        using (Models.ChatDataBaseEntities context = new Models.ChatDataBaseEntities())
                        {
                            var user = context.User.SingleOrDefault(u => u.ChatName == username);

                        }

                        //Создаем Pricipal'a с нашими параметрами
                        e.User = new System.Security.Principal.GenericPrincipal(
                          new System.Security.Principal.GenericIdentity(username, "Forms"), roles.Split(';'));
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        protected void Application_EndRequest(Object sender, EventArgs e)
        {
            HttpContext context = HttpContext.Current;
            if (context.Response.RedirectLocation != null)
            {
                if (context.Response.RedirectLocation.Contains("SimpleChat/AccountController/Login"))
                {
                    context.Response.RedirectLocation = "/SimpleChat/Account/Index";
                    context.Response.ClearContent();
                }
            }
        }
    }
}