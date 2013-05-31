using OCM.API.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OCM.MVC.Controllers
{
    public class ProfileController : Controller
    {
        //
        // GET: /Profile/

        public ActionResult Index()
        {
            
            if (Session["UserID"] != null)
            {
                UserManager userManager=new UserManager();
                var user= userManager.GetUser(int.Parse(Session["UserID"].ToString()));

                return View(user);
            }
            return View();
        }

        //
        // GET: /Profile/Details/5

        public ActionResult SignIn(string redirectUrl)
        {
            return RedirectToAction("Index", "LoginProvider", new {_mode = "silent", _forceLogin = true, _redirectUrl =redirectUrl});
        }

        public ActionResult SignOut()
        {
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Profile/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Profile/Create

        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        //
        // GET: /Profile/Edit/5

        public ActionResult Edit(int id)
        {
            return View();
        }

        //
        // POST: /Profile/Edit/5

        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        //
        // GET: /Profile/Delete/5

        public ActionResult Delete(int id)
        {
            return View();
        }

        //
        // POST: /Profile/Delete/5

        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
