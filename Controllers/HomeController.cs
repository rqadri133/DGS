using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using DGS.Models;
using DGS.Models.DataLayer;

using System.Web.Mvc;

namespace DGS.Controllers
{
    
    public class HomeController : Controller
    {

        UsersContext context = null;
        IDbConnection connection = null;
        SqlConnecter connector = null;
        List<IDbDataParameter> parameters = null;
        public HomeController()
        {
            context = new UsersContext();
            connection = context.DGSConnection;
            connector = new SqlConnecter(connection);
            parameters = new List<IDbDataParameter>();
        }

        public ActionResult Index()
        {
            ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [HttpGet]
        public IEnumerable<DocumentServer> GetServers()
        {
            DocumentServer server = null;
            List<DocumentServer> servers = new List<DocumentServer>();
            List<DataRow> rows = connector.ExecuteResultSet("spLoadAllAvailableServers");
            foreach (DataRow row in rows)
            {
                server = new DocumentServer();
                server.DocumentServerKey = row["DocumentServerID"].ToString();
                server.DocumentServerURL = row["DocumentServerURL"].ToString();
                servers.Add(server);

            }
            return servers.AsEnumerable<DocumentServer>();
        }

    }
}
