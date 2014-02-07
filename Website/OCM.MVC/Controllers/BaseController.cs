using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OCM.MVC.Controllers
{
    public class BaseController : Controller
    {
        public bool IsUserSignedIn
        {
            get
            {
                if (Session["UserID"] != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}