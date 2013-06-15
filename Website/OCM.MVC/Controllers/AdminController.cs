using OCM.API.Common;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OCM.MVC.Controllers
{
    public class AdminController : Controller
    {
        //
        // GET: /Admin/
        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult Index()
        {
            return View();
        }
        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult Users()
        {
            var userList = new UserManager().GetUsers().OrderByDescending(u=>u.DateCreated);
            return View(userList);
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult EditUser(int id)
        {
            var user = new UserManager().GetUser(id);
            return View(user);
        }

        [AuthSignedInOnly(Roles = "Admin")]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EditUser(User userDetails )
        {
            var user = new UserManager().GetUser(userDetails.ID);
            //TODO: save
            return View(userDetails);
        }
    }
}
