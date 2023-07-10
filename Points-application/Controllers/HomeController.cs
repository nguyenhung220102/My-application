using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Points_application.Controllers
{
    public class HomeController : Controller
    {

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }


        public ActionResult Main()
        {
            ViewBag.Message = "Your main page.";

            return View();
        }
        public ActionResult Tutorial()
        {
            ViewBag.Message = "Your tutorial page.";

            return View();
        }

        public ActionResult Output()
        {
            ViewBag.Message = "Your output page.";

            return View();
        }
    }
}