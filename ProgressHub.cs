using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Data;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Owin;


using DGS.Models;
using DGS.Models.DataLayer;
using DGS.Content.Controllers.DocumentTranslator;
namespace DGS
{
    
    [HubName("progressHub")]
    
    public class ProgressHub : Microsoft.AspNet.SignalR.Hub
    {


        
        UsersContext context = null;
        List<IDbDataParameter> parameters = null;
        IDbConnection connection = null;
        SqlConnecter connector = null;
        bool _uploaded = true;
        public string msg = "Initializing and Preparing...";
        string documentStatus = String.Empty ;
        System.Threading.Timer timer = null;
        private bool _status = false;
        public void TranslateDocument()
        {
            int _counter = 0;
            bool _isCompleted = false;

            while (!_isCompleted)
            {
                IsDocumentUploadedSuccessfully(ref _counter, ref _isCompleted);
                Thread.Sleep(100);
            }
             msg = "Downloading Document stream....";
             Clients.Caller.sendMessage(string.Format
                                 (msg + " {0} and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), _counter), 100);
      

            //var timer = new System.Threading.Timer((e) =>
            //{
            //    IsDocumentUploadedSuccessfully(ref _counter , ref _isCompleted);
                
            //}, null, 0, TimeSpan.FromSeconds(2).Seconds);


         


        }
        private void IsDocumentUploadedSuccessfully(ref int count  , ref bool _isCompleted)
        {
            string documentStatus = Environment.GetEnvironmentVariable("translationDuration");
            if(!_isCompleted)
            {
                if (count < 100)
                {
                    count = count + 1;
                }
            
            }
            if (documentStatus == DocumentStatus.Initializing.ToString())
            {
                msg = " Initializing document stream....";
                Clients.Caller.sendMessage(string.Format
                                    (msg + " {0} and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count),count);
            }

            else if (documentStatus == DocumentStatus.TransactionRequestFailed.ToString())
            {
                

                msg = " Processing Translation Request Wait...";
                Clients.Caller.sendMessage(string.Format
                                    (msg + " {0} and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count), count);
            }

            else if( documentStatus == DocumentStatus.TranslatedDocumentAlreaadyExit.ToString())
            {
                _isCompleted = true;
                count = 100;
                msg = "Translated Document already exist please upload a different document";
                Clients.Caller.sendMessage(string.Format
                                    (msg + " {0} and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count), count);
          

            }
            else if (documentStatus == DocumentStatus.Failed.ToString())
            {
                msg = "Failed to Translated Document ";
                 Clients.Caller.sendMessage(string.Format
                                    (msg + " {0} and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count), count);
            }

            else if (documentStatus == DocumentStatus.Completed.ToString())
            {

                Clients.Caller.sendMessage(string.Format
                                    (msg + " {0} and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count), count);
            }



            else if (documentStatus == DocumentStatus.Translating.ToString())
            {
                count = 50;
                msg = "Translating document....";
                Clients.Caller.sendMessage(string.Format
                                    (msg + " {0} and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count) , count);
          

            }

            else if (documentStatus == DocumentStatus.SettingUpNewTranslatedDocument.ToString())
            {
                count = 75;
                msg =  " Setting Up New document stream....";
                Clients.Caller.sendMessage(string.Format
                                    (msg + " {0} and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count), count);
            }

            else if (documentStatus == DocumentStatus.CreatingDocument.ToString())
            {
                count = 80;
                msg = "Creatig New  Document....";
                Clients.Caller.sendMessage(string.Format
                                    (msg + " {0} and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count), count);
             

            }


            else if (documentStatus == DocumentStatus.Translated.ToString())
            {
                msg = " Translated";
                Clients.Caller.sendMessage(string.Format
                                    (msg + " {0} and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count),count);
           
            }

            else if (documentStatus == DocumentStatus.Downlaoding.ToString())
            {
                msg = " Downloading...";
                count = 90;
         
                Clients.Caller.sendMessage(string.Format
                                    (msg + " {0} and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count), count);
            }

            else if (documentStatus == DocumentStatus.Downloaded.ToString())
            {
                _isCompleted = true;
                msg = " Downloaded";
                Clients.Caller.sendMessage(string.Format
                                    (msg + " {0} and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count), count);

            }

            else if (documentStatus == DocumentStatus.CreateNewDocument.ToString())
            {
                msg = " Creating Translated Document....";
                Clients.Caller.sendMessage(string.Format
                                    (msg + " {0} and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count) , count);

           
            }


            else if (documentStatus == DocumentStatus.Uploading.ToString())
            {
                msg = " Uploading Document on Server....";
                Clients.Caller.sendMessage(string.Format
                                    (msg + " {0} and percentage completed is {1}", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count) ,count);

          
            }

            else if (documentStatus == DocumentStatus.Uploaded.ToString())
            {
                msg = " Uploaded Document on Server....";
                Clients.Caller.sendMessage(string.Format
                                    (msg + " {0} and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count) , count);

          
            }


           
        }
    }
}