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
            var userManager = new UserManager();

            //save
            userManager.UpdateUserProfile(userDetails, true);
            
            return View(userDetails);
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult Operators()
        {
            var operatorInfoManager=  new OperatorInfoManager();

            return View(operatorInfoManager.GetOperators());
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult EditOperator(int? id)
        {
            var operatorInfo = new OperatorInfo();

            if (id != null) operatorInfo = new OperatorInfoManager().GetOperatorInfo((int)id);
            return View(operatorInfo);
        }

        [AuthSignedInOnly(Roles = "Admin")]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EditOperator(OperatorInfo operatorInfo)
        {
            if(ModelState.IsValid)
            { 
                var operatorInfoManager = new OperatorInfoManager();

                operatorInfo = operatorInfoManager.UpdateOperatorInfo(operatorInfo);
                return RedirectToAction("Operators", "Admin");
            }
            return View(operatorInfo);
        }


        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult CommentDelete(int id)
        {
            var commentManager = new UserCommentManager();
            var user = new UserManager().GetUser(int.Parse(Session["UserID"].ToString()));
            commentManager.DeleteComment(user.ID, id);
            return RedirectToAction("Index");
        }


        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult MediaDelete(int id)
        {
            var itemManager = new MediaItemManager();
            var user = new UserManager().GetUser(int.Parse(Session["UserID"].ToString()));
            itemManager.DeleteMediaItem(user.ID, id);
            return RedirectToAction("Details","POI");
        }
    }
}
