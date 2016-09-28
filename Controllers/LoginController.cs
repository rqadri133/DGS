using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script;
using System.Web.Script.Serialization;

using System.Web.Mvc;

namespace TaskerWebApp.Controllers
{
    public class LoginController : Controller
    {
        // GET: Login

        [HttpPost]
        public string Login(string userId, string pwd)
        {
            string _userId = String.Empty;
      
            return _userId;
        }
    }
}