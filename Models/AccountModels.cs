using System;
using System.Web; 
using System.Data;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Web.Security;
using System.Linq;
using System.Linq.Expressions;
using System.Data.Entity.Core.EntityClient;
using System.Linq.Expressions;
using System.Collections.Generic;

using System.Web.Mvc;
using System.Data.SqlClient;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;
 using dl=DGS.Models.DataLayer;
using db=System.Data;

namespace DGS.Models
{
    public class UsersContext  
    {
        private dl.IConnection connection = null;
        public UsersContext()
        {
              connection = new dl.DBSQLConnection(); 
             
                 
              //db.   ConfigurationManager.ConnectionStrings["DGS"].ConnectionString
               


        }

        /** the ef RELACED NOW calL FOR DROPDOWNS , rEPLACE VERSION WITH SECURITY sync ***/
        /** cALL FOR sELECTED bLOB rEFERENCE dOCUMEnts ***/
         

        public IDbConnection DGSConnection
        {

            get 
            {
                 return  connection.Connect(ConfigurationManager.AppSettings["DbType"]);
            }
        
        }




     /*   public DbSet<UserProfile> UserProfiles { get; set; } */
    }

    
    public class UserProfileModel
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }
        public string UserName { get; set; }
    }


    public class PortalSecurityControl
    {
        [Required]
        [Display(Name = "Enter Email Address")]
        [RegularExpression(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,4}", ErrorMessage="Email is not  valid")]
        public string EmailAddress { get; set; }

        public SelectList AvailableCultures{  get;  set;}

        [Required]
        [Display(Name = "Select Langauge")]
        public string  iCultureId { get; set; }
        

        [Required]
        [Display(Name = "Confirm Email Address")]
        [System.Web.Mvc.Compare("EmailAddress", ErrorMessage = "The Email Address and Confirm Email Address do not match.")]
        [RegularExpression(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,4}", ErrorMessage = "Email is not  valid")]
        public string ConfirmEmailAddress { get; set; }
    
        
         [Required]
         [Display(Name = "Enter Windows User Live ID") ]
         public string UserID { get; set; }
               
     
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [System.Web.Mvc.Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
   

    }

    
    public class RegisterExternalLoginModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        public string ExternalLoginData { get; set; }
    }

    public class LocalPasswordModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }
        
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [System.Web.Mvc.Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string selPassword { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class taskerModel
    {

        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

     

    }

    public class RegisterModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        [Required]
           [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [System.Web.Mvc.Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }


            public class ExpressionModel
            {
                [Required]
                [Display(Name = "Expression Description")]
                public string ExpressionDescription { get; set; }

                [Required]
                [Display(Name = "Expression for Document Business Rule")]
                public string Expression { get; set; }


    }

    
    /*** Code Model Version move to 2.0  in October 24 2014 after the year of Beta Released***/
    /*** Customer complaints about my security for documents ***/
    /*** Removing entity framework and leave the rest like that ***/
    /**Move on to table extraction data ***/
    /** Must not change anything in Production ***/
    /*** Document Server table no longer needed or removed for this release **/
    /** the new security document model will not use entity framework ***/
    /*** No need for memeber data member ship ****/

       
    public class DocumentModel
    {


        public List<SelectListItem> DocuumentTypeItems
        {
            get
            {
                return _documentItemTypes;

            }

            set
            {
                _documentItemTypes = value;

            }
        }


        public List<SelectListItem> ServerItems
        {
            get
            {
                return _serverItems;

            }

            set
            {
                _serverItems = value;

            }
        }
        [Required]
        public string SelectDocumentServer { get; set; }

        UsersContext userContext = null;
        IDbConnection connection = null;
        dl.SqlConnecter connecter = null;

        private List<SelectListItem> _documentItemTypes = null;
        private  List<SelectListItem> _serverItems = null;
        public HttpPostedFileBase File { get; set; }

        Guid documentId; 
        [Required]
        [Display(Name = "Enter Document name")]
        public string DocumentName { get; set; }

        public string CurrentUserName
        {
            get;
            set;
        }


        public Guid DocumentID
        {
            get
            {
                return documentId;
            }

            set
            {
                documentId = value;

            }

        }

        public bool IsValidRequest
        {

            get;
            set;

        }

        
        [Required]
        [Display(Name = "Select  Document Type")]
        public string DocumentType { get; set; }

        /* http://www.prideparrot.com/blog/archive/2012/8/uploading_and_returning_files */
       
        

        /* How it works is select the blob storage server accounts will load the BlobContainers */
         
       
        [Display(Name = "Document Server" )]
        public string DocumentServer { get; set; }

        
        /* Will receive the selected container */


        [Required]
        [Display(Name = "Blob Container Reference ")]
        public string BlobContainerReference { get; set; }

        [Required]
        [Display(Name = "Selected Blob  Reference ")]
        public string BlobReference { get; set; }

        
        
        [Display(Name = "Select  Document Description  ")]
        public string DocumentDescription { get; set; }

        /* http://www.aspnetwiki.com/page:creating-custom-html-helpers */

        [Display(Name = "Maximum number of Douments ")]
        public int MaxDocuments { get; set; }



    }

    // Continue ths sync change
    public class DocumentBlobModel
    {
        List<SelectListItem> documentList = new List<SelectListItem>();
        UsersContext context = null;
        List<IDbDataParameter> parameters = null;
        IDbConnection connection = null;
        DGS.Models.DataLayer.SqlConnecter connector = null;
   
         private List<SelectListItem> _serverItems = null;

         public List<SelectListItem> AllServers()
         {
             SelectListItem server = null;
             List<SelectListItem> servers = new List<SelectListItem>();

                 context = new UsersContext();
                 connection = context.DGSConnection;
                 connector = new DGS.Models.DataLayer.SqlConnecter(connection);


             

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

         public SelectList AvailableServers
         {
             get
             {


                 return getDocumentServer(AllServers()); 
             }
      

         }

         public SelectList getDocumentCulture()
         {
             List<DataRow> lstRows = null;
             List<SelectListItem> LanguageCultureItems = new List<SelectListItem>();



             if (connector == null)
             {
                 context = new UsersContext();
                 connection = context.DGSConnection;
                 connector = new DataLayer.SqlConnecter(connection);
             

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



         public SelectList AvailableCultures
         {
             get
             {
                 return getDocumentCulture();

             }
             

         }
        public List<SelectListItem> ServerItems
        {
            get
            {
                return _serverItems;

            }
            set
            {

                _serverItems = value;
            }

        }


        public string UserInSession
        {
            get;
            set;

        }

        public dl.SqlConnecter SqlConnectionObject
        {
            get;
            set;

        }

        public List<SelectListItem> DocumentList
        {
            get;
            set;
        }

        IEnumerable<SelectListItem> serverList = null;
        private List<SelectListItem> documentlist = null;


        [Required]
        [Display(Name = "Select Document")]
        public virtual string DocumentName { get; set; }



    
        [Required]
        [Display(Name = "Select Source Document Culture")]
        public virtual string iDocCultureid { get; set; }

        [Required]
        [Display(Name = "Select Translation Culture")]
         public virtual string iCultureid { get; set; }

        [Required]
        [Display(Name = "Select Document Type")]
        public virtual string iDocumentType { get; set; }

               
        [HiddenInput] 
        public virtual int responseTime { get; set; }
 

        [Required]
        [Display(Name = "Select Document ")]
        public virtual string iDocumentid { get; set; }

        [Required]
        [Display(Name = "Select Document Server Source ")]
        public virtual string iDocumentServerid { get; set; }

        [Required]
        [Display(Name = "Select Document Server Desination ")]
        public virtual string iDocumentServerDestinationid { get; set; }


        public IEnumerable<SelectListItem> DocumentServerList
        {
            get;
            set;
        }

        public IEnumerable<SelectListItem> DocumentTypeItems
        {
            get;
            set;
        }

        



        public SelectList getDocumentServerDestination()
        {
            IEnumerable<SelectListItem> serverList = this._serverItems.AsEnumerable<SelectListItem>().Select(m => new SelectListItem() { Text = m.Text, Value = m.Value });
            DocumentServerList = serverList;
            return new SelectList(serverList, "Value", "Text", iDocumentServerDestinationid);

        }


        public SelectList getDocumentServer( List<SelectListItem>  serverItems)
        {
            IEnumerable<SelectListItem> serverList = serverItems.AsEnumerable<SelectListItem>().Select(m => new SelectListItem() { Text = m.Text, Value = m.Value });
            DocumentServerList = serverList;
            return new SelectList(serverList, "Value", "Text", iDocumentServerid);

        }



   
    
        private string GetDocumentServerURL(string serverKey, List<SelectListItem> lstservers)
        {
            string server_url = string.Empty;
            var servers = from srv in lstservers
                          where   srv.Value == serverKey
                          select srv;

            server_url = servers.First<SelectListItem>().Text;
            return server_url;
        }

        

        private string GetBlockBlobReference(string blob_url)
        {
            string[] blobsstring = blob_url.Split(".".ToCharArray());
            string block_blob = String.Empty;

            try
            {
                block_blob = blobsstring[0];

            }
            catch (Exception excp)
            {


            }
            finally
            {


            }
            return block_blob;


        }


   
    
        
    }

    
    public class TranslationModel
    {
        // Check postback on mvc razor portal here 
        UsersContext context = null;
        IDbConnection connection = null;
        dl.SqlConnecter connector = null;
         

        public List<SelectListItem> DocumentCultures { get; set; }
        
        private List<SelectListItem> lstdocs = new List<SelectListItem>();

        
        public List<SelectListItem> Documents
        {
            get
            {
                return lstdocs;
            }

            set
            {
                lstdocs = value;
            }
        }

        
        public List<SelectListItem> LanguageCultureItems { get; set; }

          
        public string SelectDocumentCulture { get; set; }

        public List<SelectListItem> DocumentServers
        {

            get
            {
                var model = new DocumentModel();
                return model.ServerItems;
            }
        }
        
        public TranslationModel()
        {
               /** EF Removed to sync security with BLOB **/
                context = new UsersContext();
                connection= context.DGSConnection;
                connector = new dl.SqlConnecter(connection);
                
                DocumentCultures = new List<SelectListItem>();
                DocumentCultures.Add(new SelectListItem() { Text = "select", Value = "select" });
                List<DataRow> rows    = connector.ExecuteResultSet("spLoadAllLanguageCultures");   
                       
                foreach (DataRow row  in rows)
                {
                    LanguageCultureItems.Add(new SelectListItem { Text = row["Culture_Country_Lang"].ToString(), Value =  row["LanguageID"].ToString() });
                }

            
        }

        


        [Required]
        public string DocumentServer { get; set; }
                
        [Required]
        public string DocumentName { get; set; }


        [Required]
        [Display(Name = "Select Translation Culture")]
        public string TranslateTo { get; set; }

     
        [Required]
        [Display(Name = "Select  Destination  Server")]
        public string DestinationServer { get; set; }

        
        [Required]
        [Display(Name = "Signature Required")]
        public bool SignatureRequired { get; set; }

  

    }





    // Why the fuck is this computer 
    
    
  
    
    public class DocumentServerListModel
    {
        [Required]
        public Guid DocumentServerID { get; set; }


        [Required]
        [Display(Name = "Document Server Name")]
        public string DocumentServerName { get; set; }

    }
    



    public class ExternalLogin
    {
        public string Provider { get; set; }
        public string ProviderDisplayName { get; set; }
        public string ProviderUserId { get; set; }
    }
}
