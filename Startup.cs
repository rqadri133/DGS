
using System;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;


using DGS.Models;
using DGS.Models.DataLayer;
using System.Collections.Generic;
using System.Data;
using Microsoft.AspNet.SignalR;
[assembly: OwinStartup(typeof(DGS.Startup))]

namespace DGS
{
    public class Startup
    {
        UsersContext context = null;
        List<IDbDataParameter> parameters = null;
        IDbConnection connection = null;
        SqlConnecter connector = null;
        List<DataRow> rows = null;

        public void Configuration(IAppBuilder app)
        {

            var hubConfiguration = new HubConfiguration();
            hubConfiguration.EnableDetailedErrors = true;        


          
            app.MapSignalR();

            GlobalHost.Configuration.ConnectionTimeout = new TimeSpan(0, 0, 720);
            GlobalHost.Configuration.DisconnectTimeout = new TimeSpan(0, 0, 900);
            GlobalHost.Configuration.KeepAlive = new TimeSpan(0, 0, 300);

         

        }
    }
} 
