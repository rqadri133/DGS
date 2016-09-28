using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Reflection;

using System.Web;
using System.Web.SessionState;

using DGS.Models;

using System.Web.Mvc;
using System.Linq.Expressions;
using System.Data.Entity;
using System.Data;
using System.Reflection;
using System.Data.Entity.Core.EntityClient;
using  core=  DocumentFormat.OpenXml;
using  wp=  DocumentFormat.OpenXml.Wordprocessing;
using dl = DGS.Models.DataLayer;
namespace DGS.Content.Controllers
{

    [Serializable]
    public class Session
    {
        public Guid SessionID
        {
            get;
            set;
        }

        public string SessionVariable
        {
            get;
            set;

        }

        public string SessionValue 
        {
            get;
            set;
        }

        public string NewSessionValue
        {
            get;
            set;

        }

        public string ClientIPAddress 
        {
            get;
            set;

        }

        public DateTime SessionStartTime
        {
            get;
            set;
        }

        public int SessionTimeOut
        {
            get;
            set;

        }

    }

    public class DocumentServerUtilityClass
    {


        #region "set"
        public static wp.JustificationValues GetJustification(string key)
        {
            wp.JustificationValues value;
            switch (key)
            {
                case "ar-SA":
                    value = wp.JustificationValues.Right;
                    break;
                case "ur":
                    value = wp.JustificationValues.Right;
                    break;
                case "de":
                    value = wp.JustificationValues.Left;
                    break;
                case "fr":
                    value = wp.JustificationValues.Left;
                    break;
                case "es":
                    value = wp.JustificationValues.Left;
                    break;
                case "tr":
                    value = wp.JustificationValues.Left;
                    break;
                case "no":
                    value = wp.JustificationValues.Left;
                    break;
                case "sv":
                    value = wp.JustificationValues.Left;
                    break;
                case "vi":
                    value = wp.JustificationValues.Left;
                    break;
                case "ms":
                    value = wp.JustificationValues.Left;
                    break;
                case "ru":
                    value = wp.JustificationValues.Left;
                    break;
                case "hi":
                    value = wp.JustificationValues.Left;
                    break;

                case "id":
                    value = wp.JustificationValues.Left;
                    break;
                default:
                    value = wp.JustificationValues.Both;
                    break;
            }

            return value;
        }
       
        
        public static core.EnumValue<wp.ThemeFontValues> GetThemeValue(string key)
        {
            core.EnumValue<wp.ThemeFontValues> value = wp.ThemeFontValues.MajorAscii ; 
            switch(key)
            {
                case "majorEastAsia":
                value =   wp.ThemeFontValues.MajorEastAsia;
                break;
                case "majorBidi" :
                value =wp.ThemeFontValues.MajorBidi;
                break;
                case  "majorAscii" :
                 value = wp.ThemeFontValues.MajorAscii ;
                 break;
                case "majorHAnsi":
                 value = wp.ThemeFontValues.MajorHighAnsi;
                 break;
                case "minorEastAsia":
                 value = wp.ThemeFontValues.MinorEastAsia ; 
                 break; 
                case "minorBidi":
                 value = wp.ThemeFontValues.MinorBidi ; 
                 break; 
                case "minorAscii" :
                  value = wp.ThemeFontValues.MinorAscii ; 
                  break;
                case "minorHAnsi" :
                  value = wp.ThemeFontValues.MinorHighAnsi ;
                  break;  
                default :
                  value = wp.ThemeFontValues.MinorHighAnsi;
                  break;
             }

            return value;
        }
       
        #endregion

        #region "GetBlob Ser Server"


        public static string GetDocumentServerURL(string serverKey )
        {
            UsersContext context = new UsersContext();
            string _url = String.Empty;
            List<DataRow> rows = new List<DataRow>();
            List<IDbDataParameter> parameters = new List<IDbDataParameter>();
            IDbConnection connection  = context.DGSConnection;
            dl.SqlConnecter connector = new dl.SqlConnecter(connection);
            SqlParameter parameter = new SqlParameter("@StorageAccountPK", Guid.Parse(serverKey));
            parameter.SqlDbType = SqlDbType.UniqueIdentifier; 
            parameters.Add(parameter);
            rows  = connector.ExecuteResultSet("spGetStorageAccountInformation",parameters);
            _url = rows[0]["DocumentServerURL"].ToString();
            return _url ;
        }

        public static  string GetDocumentServerURLFromList(string serverKey, List<SelectListItem> lstservers)
        {
            string server_url = string.Empty;
            var servers = from srv in lstservers
                          where srv.Value == serverKey
                          select srv;

            server_url = servers.First<SelectListItem>().Text;

            return server_url;
        }



        public static DataRowCollection InsertDocuments(Dictionary<Guid , string> documents , dl.SqlConnecter connector , Guid userID  )
        {
            DataTable dt = new DataTable("Document");
            dt.Columns.Add("DocumentID", typeof(Guid)) ;
            dt.Columns.Add("DocumentName", typeof(String));
            DataRow row = null; 
            foreach(Guid key in documents.Keys  )
            {
                row = dt.NewRow();
                row["DocumentID"] = key;
                row["DocumentName"] = documents[key];
                dt.Rows.Add(row);

            }
            List<IDbDataParameter> parameters  = new List<IDbDataParameter>(); 
            parameters.Add(new SqlParameter() { ParameterName= "@TVDocument"  ,  TypeName= "Document" , SqlDbType =  SqlDbType.Structured  , Value = dt   });
            parameters.Add(new SqlParameter() { ParameterName ="@UserId" , Value = userID , SqlDbType = SqlDbType.UniqueIdentifier} );
            connector.ExecuteTranQuery("spSaveDocumentList", parameters);

            return dt.Rows;

        }
       
        public static dl.SqlConnecter GetConnecter()
        {
            UsersContext context = new UsersContext();
            IDbConnection connection = context.DGSConnection;
            dl.SqlConnecter connector = new dl.SqlConnecter(connection);
            return connector;
        }


        public static List<TranslationAccount> GetTranslationAccount(int totalneeded , dl.SqlConnecter connector)
        {

            List<TranslationAccount> tranAccounts = new List<TranslationAccount>();
            // Entity Frame work is removed and synced with Blob Storage
            int _maxlimit = Convert.ToInt32( System.Configuration.ConfigurationManager.AppSettings["CharactersAllocated"]);
            int _minlimit = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MinimumCharactersAllocated"]);
            TranslationAccount translationAccount = null;
            List<IDbDataParameter> parameters = new List<IDbDataParameter>();
            SqlParameter parameter_one = new SqlParameter("@totalCharactersNeeded",totalneeded );
            parameter_one.SqlDbType = SqlDbType.Int; 
            parameters.Add(parameter_one);
                              
 
            // @charctersAllocated


            SqlParameter parameter_two = new SqlParameter("@charctersAllocated", _maxlimit );
            parameter_two.SqlDbType = SqlDbType.Int; 
            parameters.Add(parameter_two);
                            
 
            List<DataRow> accounts = connector.ExecuteResultSet("spGetTranslationAccountStatistics", parameters);
             // MarketIndex ,  SecretKey , ClientID , CharactersUsed , MarkedFull 
             // Load All Accounts here
            


            foreach(DataRow row in accounts)
            {
                translationAccount = new TranslationAccount();
                translationAccount.CharactersUsed = Convert.ToInt32(row["CharactersUsed"]);
                translationAccount.MarkedFull = Convert.ToBoolean(row["MarkedFull"]);
                translationAccount.MarketIndex = Convert.ToInt16(row["MarketIndex"]);
                translationAccount.SecretKey =  row["SecretKey"].ToString();
                translationAccount.ClientID = row["ClientID"].ToString();
                tranAccounts.Add(translationAccount);

            }

              
            return  tranAccounts; 
        }

        public static Session StoreInSession(Session objSession ,dl.SqlConnecter connector)
        {
            Session fromDB = new Session();

            List<IDbDataParameter> parameters = new List<IDbDataParameter>();
            List<DataRow> rows = null;
            SqlParameter paramSessionName = new SqlParameter( "@SessionVariable" ,SqlDbType.NVarChar ,50);
            paramSessionName.Value = objSession.SessionVariable;
            SqlParameter paramSessionValue = new SqlParameter( "@SessionValue" ,SqlDbType.NVarChar ,300);
            paramSessionValue.Value = objSession.SessionValue;
            SqlParameter paramSessionNewValue = new SqlParameter( "@SessionNewValue" ,SqlDbType.NVarChar ,300);
            paramSessionNewValue.Value =objSession.NewSessionValue  ;
            SqlParameter paramClientIPAddress = new SqlParameter( "@ClientIPAddress" ,SqlDbType.NVarChar ,19);
            paramClientIPAddress.Value = objSession.ClientIPAddress;

            parameters.Add(paramSessionName);
            parameters.Add(paramSessionNewValue);
            parameters.Add(paramSessionValue);
            parameters.Add(paramClientIPAddress);

            rows = connector.ExecuteResultSet("spInsertLoginAuthenticateSession", parameters);
            if(rows != null)
            {
                if(rows.Count > 0 )
                {
                    fromDB.ClientIPAddress = rows[0]["ClientIPAddress"].ToString();
                    fromDB.SessionVariable = rows[0]["SessionVariable"].ToString();
                    fromDB.SessionValue = rows[0]["SessionValue"].ToString();
                    fromDB.SessionID = Guid.Parse( rows[0]["SessionID"].ToString());
                    fromDB.SessionStartTime = Convert.ToDateTime( rows[0]["SessionStart"]);
                    fromDB.SessionTimeOut = Convert.ToInt32(rows[0]["SessionTimeOut"]);


                }
           }

            return fromDB;

        }

        public static int IsValidUser(Session objSession, dl.SqlConnecter connector)
        {
            int _IsValidUser = 0;
            List<IDbDataParameter> parameters = new List<IDbDataParameter>();
            List<DataRow> rows = null;
            SqlParameter paramSessionName = new SqlParameter("@SessionVariable", SqlDbType.NVarChar, 50);
            paramSessionName.Value = objSession.SessionVariable;
            SqlParameter paramSessionValue = new SqlParameter("@SessionValue", SqlDbType.NVarChar, 300);
            paramSessionValue.Value = objSession.SessionValue;
            SqlParameter paramClientIPAddress = new SqlParameter("@ClientIPAddress", SqlDbType.NVarChar, 19);
            paramClientIPAddress.Value = objSession.ClientIPAddress;

            parameters.Add(paramSessionName);
            parameters.Add(paramSessionValue);
            parameters.Add(paramClientIPAddress);

            object val = connector.ExecuteScalarResult("spIsUserInSession", parameters);
            if (val != null)
            {
                    _IsValidUser = (int) val ;
                            
            }

            return _IsValidUser;

        }

        public static void UpdateCharacterCount(List<System.Text.StringBuilder> translatedparagraphs , TranslationAccount account , dl.SqlConnecter connector )
        {
            
            /** Chnage from EF to ADO.NET  sync to security**/
            int total_characters_processed = 0;
            List<DataRow> lstRows = null;

            foreach (System.Text.StringBuilder trans in translatedparagraphs)
            {
                total_characters_processed = total_characters_processed + trans.ToString().ToCharArray().Count<char>();
            }
            account.CharactersUsed = account.CharactersUsed + total_characters_processed;

            int _maxlimit = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["CharactersAllocated"]);
        

            System.Data.SqlClient.SqlParameter parameter = new System.Data.SqlClient.SqlParameter("@UpdateCharacterCount", account.CharactersUsed);
            System.Data.SqlClient.SqlParameter parameter_count = new System.Data.SqlClient.SqlParameter("@secretKey", account.SecretKey);
            System.Data.SqlClient.SqlParameter parameter_maxlimit = new System.Data.SqlClient.SqlParameter("@MaximumLimit", _maxlimit);
     
            List<IDbDataParameter> parameters = new List<IDbDataParameter>();
            parameter.DbType = DbType.Int32;
            parameters.Add(parameter);

            parameter_count.DbType = DbType.String;
            parameters.Add(parameter_count);

            parameter_maxlimit.DbType = DbType.Int32;
            parameters.Add(parameter_maxlimit);

            int result = connector.ExecuteTranQuery("spUpdateCharactersCount", parameters);
                 
             
             // GetConnecter().ExecuteTranQuery()
            
           
        }
        
      
        

        public static DocumentLanguageCulture FillData(  DataRow row)
        {
            DocumentLanguageCulture culture = new DocumentLanguageCulture()
            {
                LanguageID = row["LanguageID"].ToString(),
                Culture_Country_Lang = row["Culture_Country_Lang"].ToString(),
                Ascii = row["Ascii"].ToString(),
                asciiTheme = row["asciiTheme"].ToString(),
                ComplexScript = row["ComplexScript"].ToString(),
                ComplexScriptTheme = row["ComplexScriptTheme"].ToString(),
                eastAsia = row["eastAsia"].ToString(),
                eastAsiaTheme = row["eastAsiaTheme"].ToString(),
                Typeface = row["Typeface"].ToString(),
                CharaterSet = row["CharaterSet"].ToString(),
                Panose = row["Panose"].ToString(),
                PitchFamily = row["PitchFamily"].ToString()

            };

            return culture;
        }

        public static DocumentLanguageCulture GetCulture(string cultureID , dl.SqlConnecter connector)
        {
            List<DataRow> lstRows = null;
            DocumentLanguageCulture langCulture = new DocumentLanguageCulture();
            System.Data.SqlClient.SqlParameter parameter = new System.Data.SqlClient.SqlParameter("@cultureId", cultureID);
            List<IDbDataParameter> parameters = new List<IDbDataParameter>();
            Type typeVar = langCulture.GetType();
            parameter.DbType = DbType.String;
            parameters.Add(parameter);
            lstRows = connector.ExecuteResultSet("spLoadSpecificCulture", parameters);
            if(lstRows !=null)
            {
                if(lstRows.Count > 0)
                {

                    langCulture = FillData(lstRows[0]);
                }

            }


            return langCulture;

        }










        public static StorageConnectionObject GetBlobConnection(DataRow row)
        {
            StorageConnectionObject storage_connection = null;

                storage_connection = new Models.StorageConnectionObject()
                {
                    AccountKey = row["StorageAccountKey1"].ToString(),
                    AccountName = row["StorageAccountName"].ToString(),
                    Server_URL = row["DocumentServerURL"].ToString()
                };

                //connection.First<StorageConnectionObject>();
            
            return storage_connection;
        }
        
        
        public static StorageConnectionObject GetBlobConnection(dl.SqlConnecter connecter ,  Guid serverkey)
        {
            List<DataRow> lstRows = new List<DataRow>();
            List<IDbDataParameter> parameters = new List<IDbDataParameter>();
            IDbDataParameter paramStorageAccountKey = new System.Data.SqlClient.SqlParameter("@StorageAccountPK", serverkey);
            parameters.Add(paramStorageAccountKey); 
            StorageConnectionObject storage_connection = null;
          
            lstRows = connecter.ExecuteResultSet("spGetStorageAccountInformation" ,parameters  );
            if(lstRows.Count > 0)
            {
                storage_connection = new StorageConnectionObject()
               {    AccountKey =  lstRows[0] ["StorageAccountKey1"].ToString(), 
                    AccountName = lstRows[0] ["StorageAccountName"].ToString(),
                    Server_URL =  lstRows[0]["DocumentServerURL"].ToString() 
               };
              
            //connection.First<StorageConnectionObject>();
            }
            connecter.Close();
            return storage_connection;
        }
        

        public static string GetBlockBlobReference(string blob_url)
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

  

        #endregion

    }
}