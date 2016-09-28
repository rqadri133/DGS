using System;
using Microsoft.AspNet.SignalR;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Data.Entity;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using DGS.Models;
using DGS.Models.DataLayer;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using DGS.Content.Controllers;
using DGS.Content.Controllers.DocumentTranslator;
using System.Net.Http;
using System.Web.Routing;
using DGS;
using DGS.Content.Security;
namespace DGS.Controllers

{
    
    
    public class AccountController : Controller
    {
        //
        // GET: /Account/Login
        List<SelectListItem> documentList = new List<SelectListItem>();
        UsersContext  context = null;
        List<IDbDataParameter> parameters = null;
        IDbConnection connection = null;
        SqlConnecter connector = null;
        List<DataRow> rows = null;
        private List<SelectListItem> _serverItems = null;



        public AccountController()
        {

        }

        [AllowAnonymous]
        
        public ActionResult Login(string returnUrl)
        {
            Response.ClearHeaders();
            Response.AppendHeader("Cache-Control", "no-cache"); //HTTP 1.1
            Response.AppendHeader("Cache-Control", "private"); // HTTP 1.1
            Response.AppendHeader("Cache-Control", "no-store"); // HTTP 1.1
            Response.AppendHeader("Cache-Control", "must-revalidate"); // HTTP 1.1
            Response.AppendHeader("Cache-Control", "max-stale=0"); // HTTP 1.1
            Response.AppendHeader("Cache-Control", "post-check=0"); // HTTP 1.1
            Response.AppendHeader("Cache-Control", "pre-check=0"); // HTTP 
            ViewBag.ReturnUrl = returnUrl;

            Session["UserId"] = "";
            Session["ValidRequestToken"] = "";

            return View();
        }

        //
        // POST: /Account/Login

        [HttpPost]
        [AllowAnonymous]
        public string Login(string userId, string pwd)
        {

            ActionResult result = null;
            Guid userCredentialID;
            List<IDbDataParameter> parameters = new List<IDbDataParameter>();
            List<DataRow> rows = new List<DataRow>();
            SqlParameter paramUserName = new SqlParameter(	"@userName", userId);
            paramUserName.DbType = DbType.String;
            SqlParameter paramPassword = new SqlParameter("@Password", DGS.Content.Security.DocumentSecurityUserPasswordHash.CreateHash(pwd));
            paramPassword.DbType = DbType.String;
            Session toDB = new Session();

            
            
            parameters.Add(paramUserName);
            parameters.Add(paramPassword);

            

            if(connector == null )
            {
                context = new UsersContext();
                connection = context.DGSConnection;
                connector = new SqlConnecter(connection);
                if (this.Request.RequestContext.HttpContext != null)
                {

                    Request.RequestContext.HttpContext.Application.Set("connector" , connector);

                }

              
                
            
           }



            Session.Add("UserId", "");
            Session.Add("UserName", "");
            Session.Add("DocumentList", null);
            Session.Add("documents", null);
            Session.Add("servers", null);
     

         //   Session["connector"] = connector;
            bool _isValid = false;
            int _isUserInSession = 0;
            string _userIdLogIn = "";
            rows = connector.ExecuteResultSet("spGetUserCredentialToken", parameters);
            if (rows != null)
            {
                if (rows.Count > 0)
                {
                    toDB.SessionVariable = "UserID";
                    toDB.SessionValue = userId;
                    toDB.NewSessionValue = userId;
                    toDB.ClientIPAddress = Request.UserHostAddress;
                  
                    _isValid = DocumentSecurityUserPasswordHash.ValidatePassword(pwd, rows[0]["Password"].ToString());
                    _isUserInSession = DocumentServerUtilityClass.IsValidUser(toDB, connector);
                    if (_isValid)
                    {
                                     
                        Session["UserName"] = userId;
                        Session["loggedin"] = 1;
                        Session["UserId"]= rows[0]["UserId"].ToString();
                        Session["ValidRequestToken"] = userId;

                       _userIdLogIn = userId;
                    }
                    else
                    {
                        Session["loggedin"] = 0;
                        Session["UserId"] = "";
                        _userIdLogIn = "";
                        ModelState.AddModelError("Information", "Please Enter User ID / Password");
                           

                    }
                }

            }       // If we got this far, something failed, redisplay form
            ModelState.AddModelError("Error", "The user name or password provided is incorrect.");
            return _userIdLogIn;
        }

        //
        // POST: /Account/LogOff

        [HttpGet]
        public ActionResult LogOff()
        {
            try
            {

                this.HttpContext.Application.Clear();
                this.Session.Clear();
                this.Session.Abandon();
                Response.ClearHeaders();
                Response.AppendHeader("Cache-Control", "no-cache"); //HTTP 1.1
                Response.AppendHeader("Cache-Control", "private"); // HTTP 1.1
                Response.AppendHeader("Cache-Control", "no-store"); // HTTP 1.1
                Response.AppendHeader("Cache-Control", "must-revalidate"); // HTTP 1.1
                Response.AppendHeader("Cache-Control", "max-stale=0"); // HTTP 1.1
                Response.AppendHeader("Cache-Control", "post-check=0"); // HTTP 1.1
                Response.AppendHeader("Cache-Control", "pre-check=0"); // HTTP 
                Response.AppendHeader("Pragma", "no-cache");
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
                Response.Cache.SetNoStore();

                this.Response.ClearContent();
           
            
            }
            catch (System.Web.Mvc.HttpAntiForgeryException exp)
            {

            }
             return RedirectToAction("Index", "Home");
        
      }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            PortalSecurityControl model = new PortalSecurityControl();
            model.AvailableCultures = getDocumentCulture();
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult tasker()
        {
            return View();
        }


        #region "force Exception" 

        private bool CanThrowValidation(List<UserContainer> usercontainer)
        {
             bool _throw = false;
             var userInfo = from userInformation in usercontainer
                            where userInformation.ContainerCreateForUser = false
                            select userInformation; 
                        
               if( userInfo.Count<UserContainer>() > 0)
               {
                   _throw = true ;
               }
            return _throw;

        }



        #endregion

        #region "Create a private container for this user in all allocated servers"
        public List<UserContainer> CreateContainer(List<DataRow> storageConnectionKeys , string username )
        {
             StorageConnectionObject  server_Connection =  null ;
             CloudBlobClient blobClient = null; 
             CloudStorageAccount storage_account = null ;
             CloudBlobContainer container = null;
             UserContainer userContainerStatus = null;
             List<UserContainer > lstContainers = new List<UserContainer>();  
             bool ifContainerExistsForUser  = false;
          try 
          {
             // Server connection is synchronous 
             foreach( DataRow  storageConnectionKey in  storageConnectionKeys)     
             {
                  server_Connection = DocumentServerUtilityClass.GetBlobConnection(storageConnectionKey);
                  storage_account    = CloudStorageAccount.Parse(System.Configuration.ConfigurationManager.AppSettings["StorageConnectionBLOB"].Replace("[AccountName]", server_Connection.AccountName).Replace("[AccountKey]", server_Connection.AccountKey));
                  blobClient = storage_account.CreateCloudBlobClient();
                  blobClient.DefaultDelimiter = "/";
            
                  container = blobClient.GetContainerReference(username);
                  
                  Thread.Sleep(200);
                  ifContainerExistsForUser = container.CreateIfNotExists();
               
                   userContainerStatus = new UserContainer();
                   userContainerStatus.ContainerCreateForUser = ifContainerExistsForUser;
                   userContainerStatus.StorageServerName = server_Connection.Server_URL;
                   userContainerStatus.UserName = username;
                   userContainerStatus.ContainerAlreadyExistForUser = container.Exists();
                   lstContainers.Add(userContainerStatus);
                      
            }
                 // Run Parallel threads for this container creation
           
            // prayer break maghrib continue...
        }
            catch(Exception excp)
            {



            }

           Thread.Sleep(100);
             
            return lstContainers;

        } 


        #endregion

        //
        // POST: /Account/Register

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Register(PortalSecurityControl model)
        {
            Session["ValidRequestToken"] = "";
            bool _throwCantCreateUser = false;
            List<DataRow> rows = null;
            string _userId = null;
            List<DataRow> drows = null;
            List<Guid> documentServerKeys= new List<Guid>();
            List<IDbDataParameter> lstParameter = new List<IDbDataParameter>();
            SqlParameter paramUserName = new SqlParameter("@UserName",model.UserID );
            paramUserName.DbType = DbType.String;
            SqlParameter paramPassword = new SqlParameter("@Password" , DocumentSecurityUserPasswordHash.CreateHash(model.NewPassword));
            paramPassword.DbType = DbType.String;
            
            SqlParameter   paramEmailAddress = new SqlParameter("@EmailAddress" , model.EmailAddress);
            paramEmailAddress.DbType = DbType.String;
            lstParameter.Add(paramUserName);
            lstParameter.Add(paramPassword);
            lstParameter.Add(paramEmailAddress);

            context = new UsersContext();
            connection = context.DGSConnection;
            connector = new SqlConnecter(connection);

            parameters = new List<IDbDataParameter>();

 
        
              // Attempt to register the user
                try
                {
                    //sql injection occusrs here  
                    drows = connector.ExecuteResultSet("spCreateNewUser", lstParameter);

                    // Create Windows azure directectory acess user 
                    // Test case for hacker deception turned it on so he wont be able to load from ip
                    if (drows != null)
                    {
                        if (drows.Count > 0)
                        {

                            

                            Session.Add("UserName", model.UserID);

                            rows = connector.ExecuteResultSet("spLoadAllAvailableServers");
                            _throwCantCreateUser = CanThrowValidation(CreateContainer(rows, model.UserID));

                                ModelState.AddModelError("Error", "Can't create user at this time");
                                Session.Abandon();
                                Response.AppendHeader("Cache-Control", "no-cache"); //HTTP 1.1
                                Response.AppendHeader("Cache-Control", "private"); // HTTP 1.1
                                Response.AppendHeader("Cache-Control", "no-store"); // HTTP 1.1
                                Response.AppendHeader("Cache-Control", "must-revalidate"); // HTTP 1.1
                                Response.AppendHeader("Cache-Control", "max-stale=0"); // HTTP 1.1
                                Response.AppendHeader("Cache-Control", "post-check=0"); // HTTP 1.1
                                Response.AppendHeader("Cache-Control", "pre-check=0"); // HTTP 
                                Response.AppendHeader("Pragma", "no-cache");
                                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                                Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
                                Response.Cache.SetNoStore();

                                HttpContext.Application.Clear();


                            
                            return RedirectToAction("Index", "Home");
                        }
                        else
                            return RedirectToAction("Index", "Home");

                    }

                    else
                    {
                        ModelState.AddModelError("Error", "Can't create user at this time");
                        Session.Abandon();
                        HttpContext.Application.Clear();
                        return RedirectToAction("Login", "Account");

                    }
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Error", "Either User already Exit , please try again" );
                }
            

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult TranslationView()
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
            Response.Cache.SetNoStore();

            DocumentBlobModel model = new DocumentBlobModel();
                
              return View(model);
        }



        [AllowAnonymous]
        [HttpGet]
        public ActionResult DocumentEditView()
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
            Response.Cache.SetNoStore();

            DocumentBlobModel model = new DocumentBlobModel();

            return View(model);
        }


        #region "Load all available Servers and documents on selection"

 



        public SelectList getDocumentCulture()
        {
            List<DataRow> lstRows = null;
            List<SelectListItem> LanguageCultureItems = new List<SelectListItem>();
            
            
            
            if (connector == null)
            {
                context = new UsersContext();
                connection = context.DGSConnection;
                connector = new SqlConnecter(connection);
                

            }

             lstRows = connector.ExecuteResultSet("spLoadAllLanguageCultures");


            foreach (DataRow row in lstRows)
            {
                LanguageCultureItems.Add(new SelectListItem { Text = row["Culture_Country_Lang"].ToString(), Value = row["LanguageID"].ToString() });
            }
            connector.Close();

            IEnumerable<SelectListItem> listCultures = LanguageCultureItems.AsEnumerable<SelectListItem>().Select(m => new SelectListItem() { Text = m.Text, Value = m.Value });
            return new SelectList(listCultures, "Value", "Text");

            
        }


        public List<SelectListItem> AvailableServers()
        {
            SelectListItem server = null;
            List<SelectListItem> servers = new List<SelectListItem>();

            if (this.Request.RequestContext.HttpContext != null)
            {

                connector = (SqlConnecter)Request.RequestContext.HttpContext.Application.Get("connector");

            }

            if (connector == null)
            {
                context = new UsersContext();
                connection = context.DGSConnection;
                connector = new SqlConnecter(connection);
       

            }

            SelectList lstServer = null;
            List<DataRow> rows = connector.ExecuteResultSet("spLoadAllAvailableServers");
            server = new SelectListItem() { Text = "Select", Value = "Select" };
            servers.Add(server);
            foreach (DataRow row in rows)
            {
                server = new SelectListItem();
                server.Value = row["StorageAccountPK"].ToString();
                server.Text = row["DocumentServerURL"].ToString();
               
                servers.Add(server);

            }

            
            return servers;
        }

        #endregion


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public FileResult TranslationView(DocumentBlobModel tmodel)
        {
         
            string _username  =Session["UserName"].ToString();
            List<SelectListItem> documentList = (List<SelectListItem>)Session["documents"];
            if (connector == null)
            {
                context = new UsersContext();
                connection = context.DGSConnection;
                connector = new SqlConnecter(connection);


            }
            
            tmodel.SqlConnectionObject = connector;
            tmodel.UserInSession = _username;
            
            
            string documentName = String.Empty;
            documentName    =translateDocument(tmodel,documentList);

            
            DocumentLanguageCulture culture    =  DocumentServerUtilityClass.GetCulture(tmodel.iCultureid, tmodel.SqlConnectionObject); 

            Environment.SetEnvironmentVariable("translationDuration", "Downloading");


            tmodel.UserInSession = Session["UserName"].ToString();
            // passig these jason values sourceCultureId: cultureId , destinationCultureId: d estinationCultureId , serverSource: serversourceId , serverDestination:  serverDestinationId , documentsourceId
        
            tmodel.iDocumentid = documentName.Replace(".docx",   "_" + culture.Culture_Country_Lang +  "_" + _username + ".docx") ;

            Environment.SetEnvironmentVariable("translationDuration", "Downloaded");

            FileStreamResult result =   getThisDocument( tmodel);
            
           
            return result;

            

          
        }


        
            
       [HttpGet]
       [AllowAnonymous]
        public ActionResult DocumentDownloadView()
        {
        
            var model = new DocumentBlobModel();
            List<SelectListItem> allItems = new List<SelectListItem>();
            allItems.Add(new SelectListItem() { Text = "Select Document From Server", Value = "Select Document From Server" });
            allItems.AddRange( AvailableServers());
            return View(model);
        }


       [HttpGet]
       [AllowAnonymous]
        public ActionResult DocumentDeleteView()
       {
           var model = new DocumentBlobModel();

           return View(model);
  
       }


       [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
       public ActionResult DocumentDeleteView(DocumentBlobModel tmodel)
       {
          // SelectList  DocumentList = getDocument(tmodel.iDocumentServerid) ;
           List<SelectListItem> DocumentList    =    (List<SelectListItem>)  Session["documents"];

           // passig these jason values sourceCultureId: cultureId , destinationCultureId: destinationCultureId , serverSource: serversourceId , serverDestination:  serverDestinationId , documentsourceId
           var documentName = from doc in DocumentList
                              where doc.Value == tmodel.iDocumentid
                              select doc;
           tmodel.iDocumentid = documentName.First().Text;
            
           return  DeleteDocument(tmodel);
      }

       public ActionResult DeleteDocument(DocumentBlobModel tmodel)
       {

           string _filename = String.Empty;
           string _blobcontainer_reference = String.Empty;
           string _blobblockreference = String.Empty;
           string[] server_urls = null;
           StorageConnectionObject server_Connection = null;
           string server_url = tmodel.iDocumentServerid;
           CloudStorageAccount storageAccount = null;
           CloudBlobClient blobClient = null;
           CloudBlobContainer container = null;
           CloudBlockBlob blockblob = null;
           CloudBlobDirectory directory = null;
           bool _deleted = false;
           string  _userName = String.Empty;

           try
           {
               _filename = tmodel.iDocumentid;
               if (connector == null)
               {
                   context = new UsersContext();
                   connection = context.DGSConnection;
                   connector = new SqlConnecter(connection);


               }
               
                 _userName  = Session["UserName"].ToString();
               // Before that load all dropdown list for document servers  
               server_Connection = DocumentServerUtilityClass.GetBlobConnection(connector,Guid.Parse(tmodel.iDocumentServerid));
               storageAccount = CloudStorageAccount.Parse(System.Configuration.ConfigurationManager.AppSettings["StorageConnectionBLOB"].Replace("[AccountName]", server_Connection.AccountName).Replace("[AccountKey]", server_Connection.AccountKey));
               blobClient = storageAccount.CreateCloudBlobClient();
               blobClient.DefaultDelimiter = "/";
               server_urls = server_Connection.Server_URL.Split("/".ToCharArray());

               container = blobClient.GetContainerReference(_userName);
               // Create the container if it doesn't already exist.
               // Go to the manage azure portal create a blob in blob container 
               // Store the reference of blob in Database
               // leave it to the team B
               directory = container.GetDirectoryReference(_userName);
               // Retrieve reference to a blob named "myblob".
               // Create or overwrite the "myblob" blob with contents from a local file.

               blockblob = directory.GetBlockBlobReference(_filename);

               _deleted  =  blockblob.DeleteIfExists();


               // the above code will only work if the configuration on Azure portal is perfect

               // 100 percent with no exception

           }
           catch (Exception excp)
           {

               _deleted = false;
           }
           finally
           {

               connector.Close();
           }

           
           return View(tmodel) ;  

       }



        [HttpPost]
        [AllowAnonymous]
        public FileResult DocumentDownloadView( DocumentBlobModel tmodel)
        {
          
            List<SelectListItem> documentList = (List<SelectListItem>) Session["documents"];
            

            tmodel.UserInSession = Session["UserName"].ToString();
            // passig these jason values sourceCultureId: cultureId , destinationCultureId: d estinationCultureId , serverSource: serversourceId , serverDestination:  serverDestinationId , documentsourceId
        
            var documentName = from doc in documentList
                               where doc.Value == tmodel.iDocumentid 
                               select doc.Text;
            tmodel.iDocumentid = documentName.ToArray<string>()[0];
            return  getThisDocument( tmodel);
        }

        public FileStreamResult getThisLocalDocument(DocumentBlobModel tmodel)
        {

            string _filename = String.Empty;
            string _blobcontainer_reference = String.Empty;
            string _blobblockreference = String.Empty;
            string[] server_urls = null;
            StorageConnectionObject server_Connection = null;
            string server_url = tmodel.iDocumentServerid;
            CloudStorageAccount storageAccount = null;
            CloudBlobClient blobClient = null;
            CloudBlobContainer container = null;
            CloudBlockBlob blockblob = null;
            CloudBlobDirectory directory = null;
            HttpPostedFileBase filebase = null;
            string filePath = String.Empty;
            FileStream fs = null;
            try
            {
                _filename = tmodel.iDocumentid;

                filePath = System.IO.Path.Combine(this.Request.PhysicalApplicationPath +"\\Downloads",
                                         System.IO.Path.GetFileName(_filename));

                string status = string.Empty;
                status = Environment.GetEnvironmentVariable("translationDuration");

                while (status != "Downloaded")
                {
                    status = Environment.GetEnvironmentVariable("translationDuration");
                    Thread.Sleep(100);
                }

             
                fs = new FileStream(filePath, FileMode.Open);
          

            }
            catch (Exception excp)
            {


            }
            finally
            {


            }
    

             return File(fs, "application/vnd.ms-word", _filename);
         
        }


        public FileStreamResult getThisDocument(DocumentBlobModel tmodel)
        {
         
            string _filename = String.Empty;
            string _blobcontainer_reference = String.Empty;
            string _blobblockreference = String.Empty;
            string[] server_urls = null;
            StorageConnectionObject server_Connection = null;
            string server_url = tmodel.iDocumentServerid;
            CloudStorageAccount storageAccount = null;
            CloudBlobClient blobClient = null;
            CloudBlobContainer container = null;
            CloudBlockBlob blockblob = null;
            CloudBlobDirectory directory = null;
            HttpPostedFileBase filebase = null;
            string filePath = String.Empty;
            FileStream fs = null;
            try
            {
                       _filename = tmodel.iDocumentid;
                       filePath = System.IO.Path.Combine(this.Request.PhysicalApplicationPath + "\\Downloads",
                                       System.IO.Path.GetFileName(_filename));
                        // Before that load all dropdown list for document servers  
                  //     fs = new FileStream(filePath, FileMode.CreateNew);



                       if (connector == null)
                       {
                           context = new UsersContext();
                           connection = context.DGSConnection;
                           connector = new SqlConnecter(connection);


                       }
                server_Connection = DocumentServerUtilityClass.GetBlobConnection(connector, Guid.Parse(tmodel.iDocumentServerDestinationid));
                        storageAccount = CloudStorageAccount.Parse(System.Configuration.ConfigurationManager.AppSettings["StorageConnectionBLOB"].Replace("[AccountName]", server_Connection.AccountName).Replace("[AccountKey]", server_Connection.AccountKey));
                        blobClient = storageAccount.CreateCloudBlobClient();
                        blobClient.DefaultDelimiter = "/";
                         server_urls = server_Connection.Server_URL.Split("/".ToCharArray());
                         
                        container = blobClient.GetContainerReference(tmodel.UserInSession);
                        // Create the container if it doesn't already exist.
                        // Go to the manage azure portal create a blob in blob container 
                        // Store the reference of blob in Database
                        // leave it to the team B
                        directory = container.GetDirectoryReference(tmodel.UserInSession);
                        // Retrieve reference to a blob named "myblob".
                         // Create or overwrite the "myblob" blob with contents from a local file.
 
                       blockblob  = directory.GetBlockBlobReference(_filename);

                       if (System.IO.File.Exists(filePath))
                       {
                           Thread.Sleep(3000);
                           System.IO.File.Delete(filePath);
                           Thread.Sleep(1000);
                       }

                      // blockblob.DownloadToFile(filePath, FileMode.Create);
                
                  // End download stream from blob
                       fs = new FileStream(filePath, FileMode.Create);
                       
                       blockblob.DownloadToStream(fs);
                       Thread.Sleep(2000);
                       fs.Close();
                       fs.Dispose();
                       Thread.Sleep(1000);
                       // the above code will only work if the configuration on Azure portal is perfect
            
                // 100 percent with no exception
               
            }
            catch (Exception excp)
            {
            
         
            }
            finally
            {
            
            }
            Thread.Sleep(3000);
                      
            return File(new FileStream(filePath , FileMode.Open) , "application/vnd.ms-word", _filename);  

        }



 

        [HttpPost]
        public ActionResult TranslateDocument(int responseTime ,  string sourceCultureId , string destinationCultureId  , string serverSource  , string serverDestination , string documentsourceId , DocumentBlobModel tmodel )
        {

            tmodel.DocumentList = (List<SelectListItem>)Session["DocumentList"] ;  

            // passig these jason values sourceCultureId: cultureId , destinationCultureId: destinationCultureId , serverSource: serversourceId , serverDestination:  serverDestinationId , documentsourceId
            tmodel.iCultureid = sourceCultureId;
            tmodel.iDocCultureid = destinationCultureId;
            tmodel.iDocumentid = documentsourceId;
            tmodel.iDocumentServerid = serverSource;
            tmodel.iDocumentServerDestinationid = serverDestination;
            tmodel.responseTime = responseTime;
            return Json(translateDocument(responseTime, tmodel));
        }


        public bool translateDocument(DocumentBlobModel tmodel, SelectList documentList)
        {

            TranslationWrapper wrapper = new TranslationWrapper(tmodel.SqlConnectionObject);
            bool _translated = false;
            try
            {
                int time_spent = 0;
                IEnumerable<string> _documentNames = null;
                _documentNames = from doc in documentList
                                 where doc.Value == tmodel.iDocumentid
                                 select doc.Text;

                 
                Environment.SetEnvironmentVariable("translationDuration", "Initializing");
                if (_documentNames != null && _documentNames.Count<string>() > 0)
                {
                    _translated = wrapper.TranslateDocument(tmodel, tmodel.UserInSession, _documentNames.ToArray<string>()[0]);
                }

            }
            catch(Exception excp )
            {
                _translated = false;
                Environment.SetEnvironmentVariable("translationDuration", "Failed");
               
            }
            return _translated;
        }


        public string  translateDocument(DocumentBlobModel tmodel, List<SelectListItem> documentList  )
        {
            string documentName = string.Empty;
            TranslationWrapper wrapper = new TranslationWrapper(tmodel.SqlConnectionObject);
            bool _translated = false;
            try
            {
                int time_spent = 0;
                IEnumerable<string> _documentNames = null;
                _documentNames = from doc in documentList
                                 where doc.Value == tmodel.iDocumentid
                                 select doc.Text;
                 documentName = _documentNames.ToArray<string>()[0];

                Environment.SetEnvironmentVariable("translationDuration", "Initializing");
                if (_documentNames != null && _documentNames.Count<string>() > 0)
                {
                    _translated = wrapper.TranslateDocument(tmodel, tmodel.UserInSession, _documentNames.ToArray<string>()[0]);
                      if(!_translated)
                      {
                          RedirectToAction("TranslationView", "Account");

                      }
                
                }

            }
            catch (Exception excp)
            {
                _translated = false;
                Environment.SetEnvironmentVariable("translationDuration", "Failed");

            }
            return documentName;
        }
 


        public bool translateDocument(int responseTime, DocumentBlobModel tmodel)
        {

            if (connector == null)
            {
                context = new UsersContext();
                connection = context.DGSConnection;
                connector = new SqlConnecter(connection);


            }
            TranslationWrapper wrapper = new TranslationWrapper(connector);
             SelectList documents = null;
             bool _response = false;
            IEnumerable<string>  _documentNames = null;
             _documentNames =  from doc in documents 
                              where doc.Value == tmodel.iDocumentid 
                              select doc.Text;
             string _applicationUser =Session["UserName"].ToString();

            if(_documentNames != null  && _documentNames.Count<string>() > 0  )
            {
                _response = wrapper.TranslateDocument(tmodel, _applicationUser, _documentNames.ToArray<string>()[0]);
            }

            connector.Close();
            return _response;
        }

        [HttpGet]

        [AllowAnonymous]
        public ActionResult RegisterApplicationView()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        
        public ActionResult RegisterApplicationView(PortalSecurityControl model)
        {
            return View(model);
        }



        #region "Validate the user in Session Again"
        public bool IsValidRequest
        {
            get
            {
                bool _isValid = false;
                string _userId = String.Empty;
                if (rows.Count > 0)
                {
                    _userId   =Session["UserId"].ToString();
                    if (rows[0]["CredentialTokenID"].ToString() == _userId)

                    {
                        _isValid = true;

                    }
                    else
                    {
                        _isValid = false;

                    }
                }

                return _isValid;

            }


        }

        #endregion

        [HttpGet]
        public ActionResult GetServers()
        {
            DocumentServer server = null;
            List<DocumentServer> servers = new List<DocumentServer>();
            if (connector == null)
            {
                context = new UsersContext();
                connection = context.DGSConnection;
                connector = new SqlConnecter(connection);


            }        
            List<DataRow> rows = connector.ExecuteResultSet("spLoadAllAvailableServers");
            foreach (DataRow row in rows)
            {
                server = new DocumentServer();
                server.DocumentServerKey = row["StorageAccountPK"].ToString();
                server.DocumentServerURL = row["DocumentServerURL"].ToString();
                servers.Add(server);

            }
            connector.Close();
            return PartialView("DocumentInfo" ,servers ) ;
        }


        [AllowAnonymous]
        [HttpGet]
        public ActionResult UploadView()
        {
           
            Environment.SetEnvironmentVariable("translationDuration", "Completed");
            DocumentModel modelInfo = null;
            string _userName = Session["UserName"].ToString();
            string _userId = Session["UserId"].ToString();
            try
            {
                if (_userId != null)
                {
                    /*** Compare the one in session with the DB dont just keep moving as it null session ***/
                    parameters = new List<IDbDataParameter>();
                    SqlParameter parameter = new SqlParameter("@TokenID", Guid.Parse(_userId));
                    parameter.SqlDbType = SqlDbType.UniqueIdentifier;
                    parameters.Add(parameter);

                    if (connector == null)
                    {
                        context = new UsersContext();
                        connection = context.DGSConnection;
                        connector = new SqlConnecter(connection);


                    }



                    rows = connector.ExecuteResultSet("spValidateToken", parameters);
                    if (rows.Count > 0)
                    {
                        if (rows[0]["CredentialTokenID"] != null)
                        {
                            modelInfo = new DocumentModel();

                            modelInfo.IsValidRequest = true;
                            modelInfo.CurrentUserName = rows[0]["UserName"].ToString();
                            rows = connector.ExecuteResultSet(System.Configuration.ConfigurationManager.AppSettings["LoadAvailableServer"]);

                            // loas specific servers using connector 
                            modelInfo.ServerItems = new List<SelectListItem>();
                            foreach (DataRow row in rows)
                            {
                                modelInfo.ServerItems.Add(new SelectListItem { Text = row["DocumentServerURL"].ToString(), Value = row["StorageAccountPK"].ToString() });
                            }

                            rows = connector.ExecuteResultSet("spGetAllDocumentTypes");
                            modelInfo.DocuumentTypeItems = new List<SelectListItem>();
                            foreach (DataRow row in rows)
                            {
                                modelInfo.DocuumentTypeItems.Add(new SelectListItem { Text = row["DocumentTypeName"].ToString(), Value = row["DocumentTypeID"].ToString() });

                            }



                        }
                    }

                }
                else
                {
                    return RedirectToAction("Index", "Home");


                }
            }

            catch(Exception excp)
            {

              RedirectToAction("Login", "Account");
            }
            return View(modelInfo);


        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
       public ActionResult UploadView(DocumentModel blobModel )
       {
            try
            {
                string _filename = String.Empty;
                HttpPostedFileBase fileBase = null; 
                List<DataRow> rows = null;
                string _blobcontainer_reference = String.Empty;
                string _blobblockreference = String.Empty;
                string[] server_urls = null;
                StorageConnectionObject server_Connection = null;
                List<IDbDataParameter> parameters = new List<IDbDataParameter>();
                string server_url = DocumentServerUtilityClass.GetDocumentServerURL(blobModel.SelectDocumentServer);
                CloudStorageAccount storageAccount = null;
                CloudBlobClient blobClient = null;
                CloudBlobContainer container = null;
                CloudBlockBlob blockblob = null;
                CloudBlobDirectory directory = null;
                string _userId = string.Empty;
                SqlParameter paramStorageKey = null;
                SqlParameter paramCredentialToken = null;
                string[] _breakFilePath = null;

               string _appUserName =   Session["UserName"].ToString();
               if (connector == null)
               {
                   context = new UsersContext();
                   connection = context.DGSConnection;
                   connector = new SqlConnecter(connection);


               }
                /*** THIS WILL SYNC SECURITY alSO AFTER uPLOAD TO aZURE CLOUd sTORAGE mODEL ***/
                    if (!String.IsNullOrEmpty(server_url))
                    {
                            _blobcontainer_reference = _appUserName ;
                              // Updated pervious version for separete containers for each user registered for a container 
                       
                        

                        if (Request.Files.Count > 0 )
                        {
                            fileBase = Request.Files[0];                 
                           
                            if (fileBase.ContentLength > 0)
                            {
                                string filePath = System.IO.Path.Combine(HttpContext.Server.MapPath("../Uploads"),
                                System.IO.Path.GetFileName(fileBase.FileName));

                            }
                            // view is returning selected sever Id  
                            Guid selectedServerId = Guid.Parse(blobModel.SelectDocumentServer);
                            string user_name =Session["UserName"].ToString();

                            System.IO.Stream stream = fileBase.InputStream;
                            _filename = fileBase.FileName;
                            // Before that load all dropdown list for document servers  
                            server_Connection = DocumentServerUtilityClass.GetBlobConnection(connector, selectedServerId);

                            storageAccount = CloudStorageAccount.Parse(System.Configuration.ConfigurationManager.AppSettings["StorageConnectionBLOB"].Replace("[AccountName]", server_Connection.AccountName).Replace("[AccountKey]", server_Connection.AccountKey));
                            blobClient = storageAccount.CreateCloudBlobClient();
                            blobClient.DefaultDelimiter = "/";
                            container = blobClient.GetContainerReference(user_name);
                            // Create the container if it doesn't already exist.
                            // Go to the manage azure portal create a blob in blob container 
                            // Store the reference of blob in Database
                            // leave it to the team B
                            directory = container.GetDirectoryReference(user_name);
                            // Retrieve reference to a blob named "myblob".
                            blockblob = container.GetBlockBlobReference(_blobcontainer_reference);
                                // Create or overwrite the "myblob" blob with contents from a local file.
                            // problem with synchronous users with blobs must be logged
                            _breakFilePath = _filename.Split("\\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                            directory.GetBlockBlobReference(_breakFilePath[_breakFilePath.Length - 1] ).UploadFromStream(stream);
                            // make it sleep
                            System.Threading.Thread.Sleep(600);

                         

                            // the above code will only work if the configuration on Azure portal is perfect

                        }


                    }
                }


                catch (Exception excp)
                {

                    connector.Close();


                }
                finally
                {


                }
                   return RedirectToAction("TranslationView", "Account");
                         
        
         }

        



        





       


        

        
        [HttpPost]
        [AllowAnonymous]
        
        public ActionResult getDocumentJson(string serverId )
        {
            SelectList documentList = getDocument(serverId);
          
            return Json(documentList);

        }
        /** The chnanges added to make site more secured removed entity frame and use TSQL Stored proc model **/
        [AllowAnonymous]
        
        public SelectList getDocument(string serverId )
        {

            string _userName =Session["UserName"].ToString();
           
            if (!string.IsNullOrEmpty(serverId))
            {

            foreach (string document in  GetDocuments(  serverId , _userName ))
                if (documentList.Where(p => p.Text.Contains(document)) != null)
                {
                    documentList.Add(new SelectListItem() { Text = document, Value =   Guid.NewGuid().ToString() });
                    
                }
            }

            if (documentList.Count<SelectListItem>() < 1)
            {
                documentList.Add(new SelectListItem() { Text = "No Document On Server", Value = "-111" });
            }

           
            Session.Add("documents", documentList);

            return new SelectList(documentList, "Value", "Text", 0);


        }

        private void  WalkBlobDirHierarchy(CloudBlobDirectory dir  ,ref List<String> lstDocuments )
        {
            
            var entries = dir.ListBlobs().ToArray<IListBlobItem>();
            string[] separate_block_name = null;
            foreach (var entry in entries.OfType<ICloudBlob>())
            {
                if (entry.Name.Contains("/"))
                {
                    byte[] arr = entry.ServiceClient.Credentials.ExportKey();
                    string s = System.Text.ASCIIEncoding.UTF8.GetString(arr);
     
                    separate_block_name = entry.Name.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                   
                    lstDocuments.Add(separate_block_name[separate_block_name.Length-1]);
                }
            }

            foreach (var entry in entries.OfType<CloudBlobDirectory>())
            {
                string[] dir_segments = entry.Uri.Segments;
                CloudBlobDirectory subdir = dir.GetSubdirectoryReference(dir_segments[dir_segments.Length-1]);
                WalkBlobDirHierarchy(subdir ,ref lstDocuments);
            } 

        }

        
        private List<string> GetDocuments(string serverkey, string userName  )
        {
            List<string> lstDocuments = new List<String>();
            string _filename = String.Empty;
            string _blobcontainer_reference = String.Empty;
            string _blobblockreference = String.Empty;
            IListBlobItem[] entries = null;
            StorageConnectionObject server_connection =null ;

            if (connector == null)
            {
                context = new UsersContext();
                connection = context.DGSConnection;
                connector = new SqlConnecter(connection);


            }
            server_connection   = DocumentServerUtilityClass.GetBlobConnection(connector, Guid.Parse(serverkey)) ;
            List<IDbDataParameter> parameters = new List<IDbDataParameter>();
            
            
            IEnumerable<string> documents = null;
            string[] server_urls = null;
            string server_url = server_connection.Server_URL;
            CloudStorageAccount storageAccount = null;
            CloudBlobClient blobClient = null;
            CloudBlobContainer container = null;
            CloudBlockBlob blockblob = null;
            CloudBlobDirectory directory = null ;
            try
            {
                if (!String.IsNullOrEmpty(server_url))
                {
               

                     _blobcontainer_reference =   userName;
                    // Before that load all dropdown list for document servers  
                    /** the storage account will remain same for while but later this will selection based storage for sharepoint document ***/
                    storageAccount = CloudStorageAccount.Parse(System.Configuration.ConfigurationManager.AppSettings["StorageConnectionBLOB"].Replace("[AccountName]",server_connection.AccountName).Replace("[AccountKey]",server_connection.AccountKey));
                    blobClient = storageAccount.CreateCloudBlobClient();
                    container = blobClient.GetContainerReference(_blobcontainer_reference);
                    directory = container.GetDirectoryReference(_blobcontainer_reference);
                    // Create the container if it doesn't already exist.
                    // Go to the manage azure portal create a blob in blob container 
                    // Store the reference of blob in Database
                    // leave it to the team B
                    // Retrieve reference to a blob named "myblob".
                    // Loop over items within the container and output the length and URI.
     
                    foreach (IListBlobItem item in container.ListBlobs(null, false))
                    {

                        if (item.GetType() == typeof(CloudBlobDirectory))
                        {
                            directory = (CloudBlobDirectory)  item;
                            WalkBlobDirHierarchy(directory , ref lstDocuments );
                
                        }
                        else if (item.GetType() == typeof(CloudBlockBlob))
                        {
                            lstDocuments.Add(item.Uri.ToString());
                        }
                         
                    }
                    

               /** this will be the user credential token and The blobreference **/
                    /** The new logic will sync from the CredentialTokenID and Storage Account ID ***/
                    /*** will get the Blob reference and will check the document ***/
                    /*** earlier each document uploaded must be synced with this model ***/
                    /** Now a credential token Id in Session ***/
                    
                    //Parallel execution 

                    
                    /** Match the documents returned from the synced lock to the blob ***/
                    /*** Reminder :::::Also change the upload code and implement sync log with Blob Reference ***/
                                               
                }

            }
            catch (Exception excp)
            {



            }
            finally
            {
                connector.Close();
            }



            return  lstDocuments ;

        }

        #region "remove unknow items from list only "
        private void RemoveFromList(ref List<string> lstDocuments, ref List<string> documentsOnServer)
        {
            foreach (string document in lstDocuments)
            {
                if (!documentsOnServer.ToList().Exists(p => p.ToString() == document))
                {
                    lstDocuments.Remove(document);
                    break;
                }
            }

            if (lstDocuments.Count != documentsOnServer.Count)
            {
                RemoveFromList(ref lstDocuments, ref documentsOnServer);
            }

        }
        #endregion



        
        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase filebase , DocumentModel documentModel)
        {
            string _filename = String.Empty;
            List<DataRow> rows = null;
            string _blobcontainer_reference = String.Empty;
            string _blobblockreference = String.Empty;
            string[] server_urls = null;
            StorageConnectionObject server_Connection = null;
            List<IDbDataParameter> parameters = new List<IDbDataParameter>();
            string server_url = DocumentServerUtilityClass.GetDocumentServerURL(documentModel.SelectDocumentServer);
            CloudStorageAccount storageAccount  = null;
            CloudBlobClient blobClient = null;
            CloudBlobContainer container = null;
            CloudBlockBlob blockblob = null;
            CloudBlobDirectory directory = null;
            string _userId = string.Empty;
            SqlParameter paramStorageKey  = null;
            SqlParameter paramCredentialToken = null;
                    

            try
            {
                if (!String.IsNullOrEmpty(server_url))
                {
                    server_urls = server_url.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    
                    
                    if (server_urls.Length > 0)
                    {
                        _blobcontainer_reference = server_urls[server_urls.Length - 1];
                        _blobblockreference = DocumentServerUtilityClass.GetBlockBlobReference(server_urls[1]);   
                    }
                    if (Request.Files.Count > 0)
                    {
                       filebase = Request.Files["Upload"];

                        if (filebase.ContentLength > 0)
                        {
                            string filePath = System.IO.Path.Combine(HttpContext.Server.MapPath("../Uploads"),
                            System.IO.Path.GetFileName(filebase.FileName));
                            

                        }
                      

                                Guid selectedServerId = Guid.Parse(documentModel.SelectDocumentServer);

                                string user_name =Session["UserName"].ToString();
                        
                                System.IO.Stream stream = filebase.InputStream;
                                _filename = filebase.FileName;
                                // Before that load all dropdown list for document servers  
                                server_Connection = DocumentServerUtilityClass.GetBlobConnection(connector, Guid.Parse( documentModel.SelectDocumentServer)  );

                                storageAccount = CloudStorageAccount.Parse(System.Configuration.ConfigurationManager.AppSettings["StorageConnectionBLOB"].Replace("[AccountName]", server_Connection.AccountName).Replace("[AccountKey]", server_Connection.AccountKey));
                                blobClient = storageAccount.CreateCloudBlobClient();
                                blobClient.DefaultDelimiter = "/";
                                container = blobClient.GetContainerReference(user_name);
                                // Create the container if it doesn't already exist.
                                // Go to the manage azure portal create a blob in blob container 
                                // Store the reference of blob in Database
                                // leave it to the team B
                                directory = container.GetDirectoryReference(_blobblockreference);
                                // Retrieve reference to a blob named "myblob".
                                blockblob = container.GetBlockBlobReference(_blobblockreference);
                                string[] _filenames = _filename.Split("\\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                                // Create or overwrite the "myblob" blob with contents from a local file.
                                // problem with synchronous users with blobs must be logged
                                directory.GetBlockBlobReference(_filenames[_filenames.Length - 1]).UploadFromStream(stream);
                                container.CreateIfNotExists();
                                 // make it sleep
                                System.Threading.Thread.Sleep(600);
                                 
                                  
                            
                            // the above code will only work if the configuration on Azure portal is perfect
                        
                       }


                    }
                }

            
            catch (Exception excp)
            {

                connector.Close();


            }
            finally
            {


            }

            return RedirectToAction("TranslationView");
        } 





        
        
    }
}
