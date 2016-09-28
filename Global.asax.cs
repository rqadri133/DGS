using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using DGS.Models;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
 using Microsoft.AspNet.SignalR;

using Owin;
using DGS.Models.DataLayer;

using Microsoft.Owin;
[assembly: OwinStartup(typeof(DGS.Startup))]

namespace DGS
{


    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    // do you really understand
    //  http://www.codeproject.com/Articles/54576/Understanding-ASP-NET-MVC-Model-View-Controller-Ar
    public class MvcApplication : System.Web.HttpApplication
    {
        UsersContext context = null;
        List<IDbDataParameter> parameters = null;
        IDbConnection connection = null;
        SqlConnecter connector = null;
        List<DataRow> rows = null;
        #region "Record Application Instance"

        private void SaveApplicationInstance()
        {

            //this.Context.ApplicationInstance.User.Identity  (Record ApplicationInstanc
            // for each instance generate new class instance of ApplicationInstance or Process 
            // Save this ID in Process ID and the time of instance on Applications Start 
            // Record this process log 
            // 
            // this.Context.AllErrors ( record all errors as alert 
            // this.Context.ApplicationInstance.Server.MachineName  (Record MAchine Name) 


        }



        #endregion

    
        protected void Application_Start()
        {

            
            

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            parameters = new List<IDbDataParameter>();



            this.Context.Application.Clear();
            this.Context.Application.Contents.Clear();
            this.Context.Application.Add("UserId", "");
            this.Context.Application.Add("UserName", "");
            this.Context.Application.Add("DocumentList", null);
            this.Context.Application.Add("documents", null);

            this.Context.Application.Add("servers", null);

            context = new UsersContext();
            connection = context.DGSConnection;
            connector = new SqlConnecter(connection);


            this.Context.Application.Add("connector", connector);

    

        }

        protected void Session_End(object sender, EventArgs e)
        {
            Response.Redirect("/Account/Login");
        }



    }




}