using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Bug_tracker.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

       
        public ActionResult BlankPage()
        {

            return View();
        }
       
        public ActionResult IndexRTL()
        {

            return View();
        }


    }
}