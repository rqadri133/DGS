using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Net;
using System.Xml.Linq;
using System.Runtime.Serialization.Json;

using System.IO;
using System.ServiceModel.Channels;
using System.ServiceModel;
using DGS.Models;
using System.Text;
using DGS.Content.Controllers;
using System.Web;
using pkg = DocumentFormat.OpenXml.Packaging;
using wp = DocumentFormat.OpenXml.Wordprocessing;
using coreformat = DocumentFormat.OpenXml;
using System.IO.Packaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using DGS.Models.DataLayer;

namespace DGS.Content.Controllers.DocumentTranslator
{
    public class DocumentServerUri
    {
        public string DocumentName
        {
            get;
            set;
        }

        public Uri Document_Uri
        {
            get;
            set;
        }
    }

    public class AdmAuthentication
    {

        public static readonly string DatamarketAccessUri = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
        private string clientId;
        private string cientSecret;
        private string request;

        public AdmAuthentication(string clientId, string clientSecret)
        {
            this.clientId = clientId;
            this.cientSecret = clientSecret;
            //If clientid or client secret has special characters, encode before sending request
            this.request = string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=http://api.microsofttranslator.com", HttpUtility.UrlEncode(clientId), HttpUtility.UrlEncode(clientSecret));

        }


        public AdmAccessToken GetAccessToken()
        {
            return HttpPost(DatamarketAccessUri, this.request);
        }

        private AdmAccessToken HttpPost(string DatamarketAccessUri, string requestDetails)
        {

            //Prepare OAuth request 
            WebRequest webRequest = WebRequest.Create(DatamarketAccessUri);
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";
            byte[] bytes = Encoding.ASCII.GetBytes(requestDetails);
            webRequest.ContentLength = bytes.Length;
            using (Stream outputStream = webRequest.GetRequestStream())
            {
                outputStream.Write(bytes, 0, bytes.Length);
            }
            using (WebResponse webResponse = webRequest.GetResponse())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AdmAccessToken));
                //Get deserialized object from JSON stream
                AdmAccessToken token = (AdmAccessToken)serializer.ReadObject(webResponse.GetResponseStream());
                return token;
            }
        }
    }

    public class AdmAccessToken
    {

        public string access_token { get; set; }

        public string token_type { get; set; }

        public string expires_in { get; set; }

        public string scope { get; set; }
    }

    public class TranslationWrapper
    {
        SqlConnecter _connector = null;

        public TranslationWrapper(SqlConnecter connector)
        {

            this._connector = connector;

        }


        #region "Upload the translated document to the destination Server"





        #endregion



        



        public bool TranslateDocument(DocumentBlobModel documentModel , string UserName , string document )
        {
            // Load from CloudBlobStorage 
            //Document doc = new Document();
            // Add Table Support
            /** Entity framework removed ***/
            bool _isTranslated = false;
            /*** EDMX FILE **/
            Document selectedDocument = new Document();
            selectedDocument.LanguageCulture = DocumentServerUtilityClass.GetCulture(documentModel.iCultureid, _connector);                

            selectedDocument.DocumentName = document;
            string[] documentnameparts = selectedDocument.DocumentName.Split(".".ToCharArray());
            IDocumentTranslator translator = null;
            string[] documenttype = null;
            StorageConnectionObject server_connection = null;


            string _translation = String.Empty;
            try
            {


                server_connection = DocumentServerUtilityClass.GetBlobConnection(_connector, Guid.Parse( documentModel.iDocumentServerid));
                selectedDocument.DocumentServerURL = server_connection.Server_URL;
         
                Environment.SetEnvironmentVariable("translationDuration", "Setting Up Connections To File Server...");
         
                selectedDocument.StorageConnectionPoint = System.Configuration.ConfigurationManager.AppSettings["StorageConnectionBLOB"].Replace("[AccountName]", server_connection.AccountName).Replace("[AccountKey]", server_connection.AccountKey);
                selectedDocument.TranslateTo_Lang_Culture = documentModel.iCultureid;
                selectedDocument.SourceLang_Culture = documentModel.iDocCultureid;
                documenttype = selectedDocument.DocumentName.Split(".".ToCharArray());
                switch (documenttype[documenttype.Length - 1])
                {
                    case "docx":
                        {


                            translator = new WordDocumentTranlator(_connector);

                            Environment.SetEnvironmentVariable("translationDuration", "Translating");


                            try
                            {
                                selectedDocument = translator.Translate(selectedDocument, UserName);
                                _isTranslated = true;
                            }
                            catch (Exception excp)
                            {
                                Environment.SetEnvironmentVariable("translationDuration", "Translating");
                                return false;
                           
                           
                    
                            }
                          
                           
                            if(!_isTranslated)
                            {

                                return _isTranslated;

                            }



                            // Azur blob connection 
                            // Also add Share point support for this 
                            server_connection = DocumentServerUtilityClass.GetBlobConnection( _connector , Guid.Parse( documentModel.iDocumentServerDestinationid));
                            selectedDocument.StorageConnectionPoint = System.Configuration.ConfigurationManager.AppSettings["StorageConnectionBLOB"].Replace("[AccountName]", server_connection.AccountName).Replace("[AccountKey]", server_connection.AccountKey);
                            selectedDocument.documentUser = UserName;
                            selectedDocument.DocumentServerURL = server_connection.Server_URL;

                            selectedDocument.DocumentName = documentnameparts[0] + "_" + selectedDocument.LanguageCulture.Culture_Country_Lang +  "_" + UserName + "." + documentnameparts[1];
                            
                            // The difference here i use is to process tables but i stop here on finding issues on process tables
                             // Will INSHALLAH continue on tables once the New UI Loaded .
                            Environment.SetEnvironmentVariable("translationDuration", "Processing");

                            server_connection = DocumentServerUtilityClass.GetBlobConnection(_connector, Guid.Parse(documentModel.iDocumentServerDestinationid));
                            selectedDocument.DocumentServerURL = server_connection.Server_URL;

                            Environment.SetEnvironmentVariable("translationDuration", "Setting Up Connections To File Server...");

                            selectedDocument.StorageConnectionPoint = System.Configuration.ConfigurationManager.AppSettings["StorageConnectionBLOB"].Replace("[AccountName]", server_connection.AccountName).Replace("[AccountKey]", server_connection.AccountKey);
                            translator.CreateDocument(selectedDocument);
                            
                             
                                Environment.SetEnvironmentVariable("translationDuration", "Downloading");
                                selectedDocument.StreamData.Dispose();
                            translator.Dispose();
                            
                            break;
                        }
                    case "xlsx":
                        break;
                    default:
                        break;



                }
                documentModel.responseTime = 100;
                _isTranslated =true;
            }
            catch (Exception excp)
            {
                _isTranslated = false;
                documentModel.responseTime = 40;
            }
            finally
            {
                server_connection = null;

            }
            // Once translated create and upload the new translated document  
           
            
            return _isTranslated;

        }




        public int TranslateDocument( ref string message , DGS.HubModel documentModel , Microsoft.AspNet.SignalR.Hubs.IHubCallerConnectionContext<dynamic> Clients  )
        {
            int count = 10;
            
           TimeSpan timespan  =  System.DateTime.Now.TimeOfDay; 
            // Load from CloudBlobStorage 
            //Document doc = new Document();
            
            // Add Table Support
            /** Entity framework removed ***/

            /*** EDMX FILE **/
           message = "Loading Document From Server " + timespan.Seconds;

           Clients.Caller.sendMessage(string.Format
                     (message + " {0}% of {1}%", "Current Local Time " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count));                

            Document selectedDocument = new Document();
            selectedDocument.LanguageCulture = DocumentServerUtilityClass.GetCulture(documentModel.iCultureid, _connector);

            selectedDocument.DocumentName = documentModel.DocumentName;
            string[] documentnameparts = selectedDocument.DocumentName.Split(".".ToCharArray());
            IDocumentTranslator translator = null;
            string[] documenttype = null;
            StorageConnectionObject server_connection = null;



            try
            {
                server_connection = DocumentServerUtilityClass.GetBlobConnection(_connector, Guid.Parse(documentModel.iDocumentServerid));
                selectedDocument.DocumentServerURL = server_connection.Server_URL;

                selectedDocument.StorageConnectionPoint = System.Configuration.ConfigurationManager.AppSettings["StorageConnectionBLOB"].Replace("[AccountName]", server_connection.AccountName).Replace("[AccountKey]", server_connection.AccountKey);
                selectedDocument.TranslateTo_Lang_Culture = documentModel.iCultureid;
                selectedDocument.SourceLang_Culture = documentModel.iDocCultureid;
                documenttype = selectedDocument.DocumentName.Split(".".ToCharArray());
                switch (documenttype[documenttype.Length - 1])
                {
                    case "docx":
                        {
                            translator = new WordDocumentTranlator(_connector);
                            selectedDocument = translator.Translate(selectedDocument, documentModel.UserInSession);
                            selectedDocument.documentUser = documentModel.UserInSession;
                            message = "Document Translation In Progress " + timespan.Seconds;
                            Clients.Caller.sendMessage(string.Format
          (message + " {0}% of {1}%", "Current Local Time " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count));

                            count = 20;
                            // Azur blob connection 
                            // Also add Share point support for this 
                            server_connection = DocumentServerUtilityClass.GetBlobConnection(_connector, Guid.Parse(documentModel.iDocumentServerDestinationid));
                            selectedDocument.StorageConnectionPoint = System.Configuration.ConfigurationManager.AppSettings["StorageConnectionBLOB"].Replace("[AccountName]", server_connection.AccountName).Replace("[AccountKey]", server_connection.AccountKey);
                            selectedDocument.DocumentName = documentnameparts[0] + "_" + selectedDocument.LanguageCulture.Culture_Country_Lang + "." + documentnameparts[1];
                            selectedDocument.DocumentServerURL = server_connection.Server_URL;

                            // The difference here i use is to process tables but i stop here on finding issues on process tables
                            // Will INSHALLAH continue on tables once the New UI Loaded .
                            count = 45;
                            
                            translator.CreateDocument(selectedDocument);
                            message = "Creating New Document On Server " + timespan.Seconds;

                            Clients.Caller.sendMessage(string.Format
                           (message + " {0}% and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count));
                            count = 40;
                            documentModel.responseTime = selectedDocument.ProcessTime;

                            message = "Uploading Document In Progress " + timespan.Seconds;

                            Clients.Caller.sendMessage(string.Format
                           (message + " {0}% and percentage completed is {1}%", "Local time : " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count));                

                            count = 90; 
                            Clients.Caller.sendMessage(string.Format
                                      (message + " {0}% of {1}%", "Current Local Time " + System.DateTime.Now.ToLocalTime().ToShortTimeString(), count));                
                           

                            break;
                        }
                    case "xlsx":
                        break;
                    default:
                        break;



                }
                documentModel.responseTime = 100;

            }
            catch (Exception excp)
            {
                documentModel.responseTime = 40;
            }
            finally
            {
                server_connection = null;

            }
            // Once translated create and upload the new translated document  
            return documentModel.responseTime;

        }




    }


    
    public class WordDocumentTranlator : IDocumentTranslator 
    {
        // Change Set to 1.1
        // Change Set from the Same Developer n Architect Syed Qadri 
        SqlConnecter _connector = null;
        public WordDocumentTranlator(SqlConnecter connector)
        {
            this._connector = connector;

        }

        
        public Document Translate(Document document , string userName)
        {
            // remove the test prototype

            Document doc = LoadWordDocument(document , userName);
            //List<Paragrapah> lstparagraphs =;
           // document.PickParagraphs = lstparagraphs;
            //doc.Paragraphs = lstparagraphs;
            try
            {
                doc.Paragraph_Builder = TranslateDocumentText(doc);

                if (doc.Paragraph_Builder != null)
                {
                    if (doc.Paragraph_Builder.Count > 0)
                    {
                        doc.ProcessTime = 100;
                    }
                    else
                    {
                        doc.ProcessTime = 21;


                    }
                }

            }
            catch(Exception excp)
            {
                Environment.SetEnvironmentVariable("translationDuration", "Failed");
            }
            // Create and Uploaad the new document
            return doc;
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


        private void WalkBlobDirHierarchy(CloudBlobDirectory dir, ref List<DocumentServerUri> lstDocuments)
        {

            var entries = dir.ListBlobs().ToArray<IListBlobItem>();
            string[] separate_block_name = null;
            DocumentServerUri docuri = null;
            foreach (var entry in entries.OfType<ICloudBlob>())
            {
                if (entry.Name.Contains("/"))
                {
                    docuri = new DocumentServerUri();
                    separate_block_name = entry.Name.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    docuri.DocumentName = separate_block_name[separate_block_name.Length - 1];
                    docuri.Document_Uri = entry.Uri;
                    lstDocuments.Add(docuri);
                }
            }

            foreach (var entry in entries.OfType<CloudBlobDirectory>())
            {
                string[] dir_segments = entry.Uri.Segments;
                CloudBlobDirectory subdir = dir.GetSubdirectoryReference(dir_segments[dir_segments.Length - 1]);
                WalkBlobDirHierarchy(subdir, ref lstDocuments);
            }

        }


        #region Create Paragraphs

        #endregion

        // so no short cut here creation of new paragraphs required from squential read of translated paragraphs 
        // along with the properties of pragraph and run 
        // so pick the translateld text assign properties to it assign 
        // call it TranslatedParagraph object conatins all defined runs properties thats
        // these new paragraphs will form a new document and will be done seequential so let not get into runs here just 
        // paraindex thats it corresponding paragraphs and create runs from them  

        #region " create document from scratch"
        #region "Adjust Tokens"

        private string[] AdjustTokens(ElementObject tempToken)
        {
            string[] links = null;
            try
            {
                if (tempToken.TransTokens != null)
                {
                    for (int i = 0; i < tempToken.TransTokens.Length; i++)
                    {

                        if (tempToken.TransTokens[i].Contains("₩₩"))
                        {

                            links = tempToken.TransTokens[i].Split("₩₩".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            tempToken.TransTokens.ToList().Remove(tempToken.TransTokens[i]);
                            for (int j = 0; j < links.Length; j++)
                            {
                                if (tempToken.TransTokens.Length - 1 > i)
                                {
                                    tempToken.TransTokens.ToList().Insert(i, links[j]);
                                }
                                else
                                {
                                    tempToken.TransTokens.ToList().Add(links[j]);
                                }
                            }

                        }

                        else if (tempToken.TransTokens[i].Contains("₣₣"))
                        {
                            links = tempToken.TransTokens[i].Split("₣₣".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                            tempToken.TransTokens.ToList().Remove(tempToken.TransTokens[i]);

                            for (int j = 0; j < links.Length; j++)
                            {
                                if (tempToken.TransTokens.ToList().Count - 1 > i)
                                {
                                    tempToken.TransTokens.ToList().Insert(i, links[j]);
                                }
                                else
                                {
                                    tempToken.TransTokens.ToList().Add(links[j]);
                                }
                            }

                        }
                        else if (tempToken.TransTokens[i].Contains("₳₳"))
                        {
                            if (tempToken.TransTokens[i].Trim() == "₳₳")
                            {
                                links = tempToken.TransTokens[i].Split("₳₳".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                tempToken.TransTokens.ToList().Remove(tempToken.TransTokens[i]);
                                for (int j = 0; j < links.Length; j++)
                                {
                                    if (tempToken.TransTokens.Length - 1 > i)
                                    {
                                        tempToken.TransTokens.ToList().Insert(i, links[j]);
                                    }
                                    else
                                    {
                                        tempToken.TransTokens.ToList().Add(links[j]);
                                    }
                                }

                            }
                        }
                        else if (tempToken.TransTokens[i].Contains("₦"))
                        {
                            if (tempToken.TransTokens[i].Trim() == "₦")
                            {
                                links = tempToken.TransTokens[i].Split("₦".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                tempToken.TransTokens.ToList().Remove(tempToken.TransTokens[i]);
                                for (int j = 0; j < links.Length; j++)
                                {
                                    if (tempToken.TransTokens.Length - 1 > i)
                                    {
                                        tempToken.TransTokens.ToList().Insert(i, links[j]);
                                    }
                                    else
                                    {
                                        tempToken.TransTokens.ToList().Add(links[j]);
                                    }
                                }
                            }
                        }
                    }

                }
            }
            catch (Exception excp)
            {

            }

            List<string> passbyreference = tempToken.TransTokens.ToList();
            passbyreference = EliminateSpeacialSymbols(ref passbyreference);

            tempToken.TransTokens = passbyreference.ToArray();
            return tempToken.TransTokens;


        }

        private List<string> AdjustTokens( TempTokenMeasurement tempToken)
        {
            string[] links = null;
            try
            {
                for (int i = 0; i < tempToken.RunsAll.Count; i++)
                {
                    for (int k = 0; k < tempToken.tokens.Count; k++)
                    {
                        if (tempToken.tokens[k].Contains("₩₩"))
                        {

                            links = tempToken.tokens[k].Split("₩₩".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            tempToken.tokens.Remove(tempToken.tokens[k]);
                            for (int j = 0; j < links.Length; j++)
                            {
                                if (tempToken.tokens.Count - 1 > k)
                                {
                                    tempToken.tokens.Insert(k, links[j]);
                                }
                                else
                                {
                                    tempToken.tokens.Add(links[j]);
                                }
                            }

                        }

                        else if (tempToken.tokens[k].Contains("₣₣"))
                        {
                            links = tempToken.tokens[k].Split("₣₣".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            tempToken.tokens.Remove(tempToken.tokens[k]);
                            for (int j = 0; j < links.Length; j++)
                            {
                                if (tempToken.tokens.Count - 1 > k)
                                {
                                    tempToken.tokens.Insert(k, links[j]);
                                }
                                else
                                {
                                    tempToken.tokens.Add(links[j]);
                                }
                            }

                        }
                        else if (tempToken.tokens[k].Contains("₳₳"))
                        {
                            if (tempToken.tokens[k].Trim() == "₳₳")
                            {
                                links = tempToken.tokens[k].Split("₳₳".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                tempToken.tokens.Remove(tempToken.tokens[k]);
                                for (int j = 0; j < links.Length; j++)
                                {
                                    if (tempToken.tokens.Count - 1 > k)
                                    {
                                        tempToken.tokens.Insert(k, links[j]);
                                    }
                                    else
                                    {
                                        tempToken.tokens.Add(links[j]);
                                    }
                                }

                            }
                        }
                        else if (tempToken.tokens[k].Contains("₦"))
                        {
                            if (tempToken.tokens[k].Trim() == "₦")
                            {
                                links = tempToken.tokens[k].Split("₦".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                tempToken.tokens.Remove(tempToken.tokens[k]);
                                for (int j = 0; j < links.Length; j++)
                                {
                                    if (tempToken.tokens.Count - 1 > k)
                                    {
                                        tempToken.tokens.Insert(k, links[j]);
                                    }
                                    else
                                    {
                                        tempToken.tokens.Add(links[j]);
                                    }
                                }
                            }
                        }
                    }
                }

                
            }
            catch (Exception excp)
            {

            }

            List<string> passbyreference = tempToken.tokens;
             passbyreference = EliminateSpeacialSymbols(ref passbyreference);

            tempToken.tokens = passbyreference;
           return tempToken.tokens ;


        }



        private List<string> EliminateSpeacialSymbols(ref List<string> temptokens)
        {
            int _symbolfoundAt = 0;
            List<string> nosymbolstring = new List<string>();
            string _replaced = String.Empty;
            foreach (string strref in temptokens)
            {
                if (strref.Contains("₦"))
                {
                    _replaced =  strref.Replace("₦", "");

                }
                else if (strref.Contains("₣₣"))
                {
                     _replaced = strref.Replace("₣₣", " ");

                }
                else if (strref.Contains("₳₳"))
                {
                    _replaced = strref.Replace("₳₳", " ");

                }
                else if (strref.Contains("₩₩"))
                {
                    _replaced = strref.Replace("₩₩", " ");

                }
                else
                {
                    _replaced = strref;

                }
               _replaced =_replaced.Replace("₦","");
                nosymbolstring.Add(_replaced);
            }

            return nosymbolstring;
        }

        
        #endregion




        private wp.Paragraph[] CreateNewParagraphs(List<TempTokenMeasurement> temps, DocumentLanguageCulture culture)
        {
            wp.Paragraph paragraph = null;
            wp.RunFonts run_fonts = null;
            wp.BookmarkStart bmStart = null;
            wp.BookmarkEnd bmEnd = null;
            
            wp.ParagraphProperties pPr = null;
            wp.Run run = null;
            List<coreformat.OpenXmlElement> nRuns = null;
            List<wp.Hyperlink> hLinks = null;
            List<wp.Paragraph> paragraphs = new List<wp.Paragraph>();
            wp.Languages l = null;
             wp.Hyperlink hlink = null;
             string[] links = null;
             wp.Justification justification = null;
            try
            {

                foreach (TempTokenMeasurement token in temps)
                {
                    paragraph = new wp.Paragraph();
                    nRuns = new List<coreformat.OpenXmlElement>();

                    var otherElements = from pr in token.Paragraph.ChildElements
                                        where pr.LocalName != "pPr" || pr.LocalName != "r" || pr.LocalName != "hyperlink"
                                        select pr.CloneNode(true);
                     
                    var pProps = from pr in token.Paragraph.ChildElements
                                 where pr.LocalName == "pPr"
                                 select pr.CloneNode(true);
                    
                 
                    nRuns = new List<coreformat.OpenXmlElement>();
                    if (pProps != null)
                    {
                        paragraph.Append(pProps);
                        if (paragraph.ParagraphProperties != null)
                        {
                            if (paragraph.ParagraphProperties.Justification != null)
                            {
                                justification = paragraph.ParagraphProperties.Justification;
                                paragraph.ParagraphProperties.RemoveChild<wp.Justification>(justification);
                                justification = new wp.Justification() { Val = DocumentServerUtilityClass.GetJustification(culture.LanguageID) };
                            }
                            else
                            {
                                justification = new wp.Justification() { Val = DocumentServerUtilityClass.GetJustification(culture.LanguageID) };
                                paragraph.ParagraphProperties.AppendChild(justification);
                            }
                        }
                     }

                    // Adjust the tokens out here
                    token.tokens =  AdjustTokens(token);
                    if (token.Unmatched)
                    {
                        for (int i = 0; i < token.RunsAll.Count; i++)
                        {
                            if (token.RunsAll[i].LocalName == "bookmarkStart")
                            {
                                bmStart = new wp.BookmarkStart();
                                if (token.RunsAll[i].ChildElements.Count > 0)
                                {
                                    var childElements = from child in token.RunsAll[i].ChildElements
                                                        where child.LocalName != "r"
                                                        select child.CloneNode(true);
                                    List<coreformat.OpenXmlElement> elements = childElements.ToList();
                                    // run elemeent
                                    if (childElements.Count<coreformat.OpenXmlElement>() > 0)
                                    {
                                        bmStart.Append(childElements);
                                    }



                                }

                                nRuns.Add(bmStart);
                            }
                            else if(token.RunsAll[i].LocalName == "bookmarkEnd")
                            {
                                    bmEnd = new wp.BookmarkEnd();
                                if (token.RunsAll[i].ChildElements.Count > 0)
                                {
                                    var childElements = from child in token.RunsAll[i].ChildElements
                                                        where child.LocalName != "r"
                                                        select child.CloneNode(true);
                                    List<coreformat.OpenXmlElement> elements = childElements.ToList();
                                    // run elemeent
                                    if (childElements.Count<coreformat.OpenXmlElement>() > 0)
                                    {
                                        bmEnd.Append(childElements);
                                    }


                                }

                                nRuns.Add(bmEnd);
                            

                            }

                            else if (token.RunsAll[i].LocalName == "hyperlink")
                            {
                                hlink = new wp.Hyperlink();
                                
                                var childElements = from child in token.RunsAll[i].ChildElements
                                                    where child.LocalName != "r"
                                                    select child.CloneNode(true);
                                List<coreformat.OpenXmlElement> elements = childElements.ToList();
                                // run elemeent
                                if (childElements.Count<coreformat.OpenXmlElement>() > 0)
                                {
                                    hlink.Append(childElements);
                                }

                                childElements = from child in token.RunsAll[i].ChildElements
                                                where child.LocalName == "r"
                                                select child.CloneNode(true);

                                 elements = childElements.ToList();

                                 if (elements.Count > 0)
                                 {
                                     var elem = elements.First();
                                     run = new wp.Run();
                                     var childElement = from child in elem.ChildElements
                                                         where child.LocalName != "t"
                                                         select child.CloneNode(true);
                                      elements = childElement.ToList();
                                     // run elemeent
                                     run.Append(elements);
                                     if (run.RunProperties != null)
                                     {
                                         if (run.RunProperties.Languages != null)
                                         {
                                             run.RunProperties.Languages.Remove();

                                         }
                                         l = new wp.Languages();
                                         l.Val = culture.LanguageID;
                                         l.Bidi = culture.LanguageID;
                                         l.EastAsia = culture.LanguageID;

                                         run.RunProperties.Append(l);
                                         if (run.RunProperties.RunFonts != null)
                                         {
                                             run.RunProperties.RunFonts.Remove();
                                         }
                                         run_fonts = new wp.RunFonts();

                                         // Set this in DB Config just check for now but should  placed in language culture table to later get from linq
                                         run_fonts.Ascii = culture.Ascii;
                                         run_fonts.AsciiTheme = DocumentServerUtilityClass.GetThemeValue(culture.asciiTheme);
                                         run_fonts.ComplexScript = culture.ComplexScript;
                                         run_fonts.ComplexScriptTheme = DocumentServerUtilityClass.GetThemeValue(culture.ComplexScriptTheme);
                                         run_fonts.EastAsia = culture.eastAsia;
                                         run_fonts.EastAsiaTheme = DocumentServerUtilityClass.GetThemeValue(culture.eastAsiaTheme);
                                         run.RunProperties.Append(run_fonts);


                                     }


                                     // insert translated text here // remember its new run it doesn't have any text
                                     int token_count = token.tokens.Count;
                                     int run_count = token.RunsAll.Count;
                                     int difference = 0;
                                     if(token.tokens.Count <= i )
                                     {
                                        difference   = run_count - token_count;
                                        run.AppendChild<wp.Text>(new wp.Text(""));

                                     }
                                     else
                                     {

                                         run.AppendChild<wp.Text>(new wp.Text(token.tokens[i]));

                                     }
                                     hlink.AppendChild(run);
                                

                                 }
                                 nRuns.Add(hlink);
       
                            }
                            else if(token.RunsAll[i].LocalName == "r")
                            {
                                run = new wp.Run();
                                // at this point all child elements cloned appended except for text  

                                var childElements = from child in token.RunsAll[i].ChildElements
                                                    where child.LocalName != "t" && child.LocalName !="drawing"
                                                    select child.CloneNode(true);
                                List<coreformat.OpenXmlElement> elements = childElements.ToList();
                                run.Append(childElements);
                                // insert translated text here // remember its new run it doesn't have any text
                               if(token.tokens.Count <= i )
                               {
                                   run.AppendChild<wp.Text>(new wp.Text(" "));
                               }
                               else
                               {

                                   run.AppendChild<wp.Text>(new wp.Text(token.tokens[i].Trim() + " "));
                               }

                                nRuns.Add(run);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < token.RunsAll.Count; i++)
                        {
                            if (token.RunsAll[i].LocalName=="hyperlink") 
                            {
                                hlink = new wp.Hyperlink();
                                
                                var childElements = from child in token.RunsAll[i].ChildElements
                                                    where child.LocalName != "r"
                                                    select child.CloneNode(true);
                                List<coreformat.OpenXmlElement> elements = childElements.ToList();
                                // run elemeent
                                if (childElements.Count<coreformat.OpenXmlElement>() > 0)
                                {
                                    hlink.Append(childElements);
                                }


                                childElements = from child in token.RunsAll[i].ChildElements
                                                where child.LocalName == "r"
                                                select child.CloneNode(true);

                                 elements = childElements.ToList();
                                 if (elements.Count > 0)
                                 {
                                     var elem = elements.First();
                                     run = new wp.Run();
                                     var childElement = from child in elem.ChildElements
                                                        where child.LocalName != "t"
                                                        select child.CloneNode(true);
                                     elements = childElement.ToList();
                                     // run elemeent
                                     run.Append(elements);
                                     if (run.RunProperties != null)
                                     {
                                         if (run.RunProperties.Languages != null)
                                         {
                                             run.RunProperties.Languages.Remove();

                                         }
                                         l = new wp.Languages();
                                         l.Val = culture.LanguageID;
                                         l.Bidi = culture.LanguageID;
                                         l.EastAsia = culture.LanguageID;

                                         run.RunProperties.Append(l);
                                         if (run.RunProperties.RunFonts != null)
                                         {
                                             run.RunProperties.RunFonts.Remove();
                                         }
                                         run_fonts = new wp.RunFonts();

                                         // Set this in DB Config just check for now but should  placed in language culture table to later get from linq
                                         run_fonts.Ascii = culture.Ascii;
                                         run_fonts.AsciiTheme = DocumentServerUtilityClass.GetThemeValue(culture.asciiTheme);
                                         run_fonts.ComplexScript = culture.ComplexScript;
                                         run_fonts.ComplexScriptTheme = DocumentServerUtilityClass.GetThemeValue(culture.ComplexScriptTheme);
                                         run_fonts.EastAsia = culture.eastAsia;
                                         run_fonts.EastAsiaTheme = DocumentServerUtilityClass.GetThemeValue(culture.eastAsiaTheme);
                                         run.RunProperties.Append(run_fonts);


                                     }
                                     // insert translated text here // remember its new run it doesn't have any text

                                     if (token.tokens.Count <= i)
                                     {

                                         run.AppendChild<wp.Text>(new wp.Text(token.tokens[i-1]));
                                     }
                                     else
                                     {
                                         run.AppendChild<wp.Text>(new wp.Text(token.tokens[i]));
                          
                                     }

                                     hlink.AppendChild(run);
                                 }
                                 nRuns.Add(hlink);
              
                                  
                            }
                            else if (token.RunsAll[i].LocalName == "r")
                            {
                                
                                run = new wp.Run();
                                // at this point all child elements cloned appended except for text  
                                
                                
                                var childElements = from child in token.RunsAll[i].ChildElements
                                                    where child.LocalName != "t" && child.LocalName != "drawing"
                                                    select child.CloneNode(true);
                                
                                
                                List<coreformat.OpenXmlElement> elements = childElements.ToList();
                                // run elemeent
                                run.Append(childElements);
                                if (run.RunProperties != null)
                                {
                                    if (run.RunProperties.Languages != null)
                                    {
                                        run.RunProperties.Languages.Remove();

                                    }
                                    l = new wp.Languages();
                                    l.Val = culture.LanguageID;
                                    l.Bidi = culture.LanguageID;
                                    l.EastAsia = culture.LanguageID;

                                    run.RunProperties.Append(l);
                                    if (run.RunProperties.RunFonts != null)
                                    {
                                        run.RunProperties.RunFonts.Remove();
                                    }
                                    run_fonts = new wp.RunFonts();

                                    // Set this in DB Config just check for now but should  placed in language culture table to later get from linq
                                    run_fonts.Ascii = culture.Ascii;
                                    run_fonts.AsciiTheme = DocumentServerUtilityClass.GetThemeValue(culture.asciiTheme);
                                    run_fonts.ComplexScript = culture.ComplexScript;
                                    run_fonts.ComplexScriptTheme = DocumentServerUtilityClass.GetThemeValue(culture.ComplexScriptTheme);
                                    run_fonts.EastAsia = culture.eastAsia;
                                    run_fonts.EastAsiaTheme = DocumentServerUtilityClass.GetThemeValue(culture.eastAsiaTheme);
                                    run.RunProperties.Append(run_fonts);
                                
                                   

                                }
                                // insert translated text here // remember its new run it doesn't have any text
                                if (token.tokens.Count <= i)
                                {

                                    run.AppendChild<wp.Text>(new wp.Text(token.tokens[i - 1]));
                                }
                                else
                                {
                                    run.AppendChild<wp.Text>(new wp.Text(token.tokens[i]));

                                }
                                nRuns.Add(run);
                            }

                        }

                    }
                    paragraph.Append(nRuns);
                    paragraphs.Add(paragraph);
                }
            }
            catch (Exception excp)
            {

            }
            finally
            {

            }
            return paragraphs.ToArray();
        }


        #endregion

        public void CreateNewDocument(Document doc)
        {
            Environment.SetEnvironmentVariable("translationDuration", "CreatingNewDocument");
         
            string pdf_file = string.Empty;
            List<string> checkmatchedcounts = null;
            wp.DocDefaults docStyleDefaults = null;
            List<string> onlyRuns = new List<string>();
            string filepath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("..\\bin"),
               System.IO.Path.GetFileName(doc.DocumentName));
            pkg.StyleDefinitionsPart style_part = null;
            pkg.StylesWithEffectsPart style_effects = null;
            Package package = Package.Open(doc.StreamData, FileMode.Open, FileAccess.ReadWrite);
            string str = String.Empty;
            List<TempTokenMeasurement> tems = null;
            wp.Languages languages = null;
            wp.RunFonts runfonts = null;
            List<coreformat.OpenXmlElement> nRuns = null;
            using (pkg.WordprocessingDocument wordDocument = pkg.WordprocessingDocument.Open(package))
            {
                // Add a main document part. 

                IEnumerable<wp.Paragraph> pgraphs = from para in wordDocument.MainDocumentPart.Document.Body.Elements<wp.Paragraph>()
                                                    where para.InnerText != "" || para.InnerText != str || para.InnerText != " "
                                                    select para;
                string valfound = String.Empty;
                List<wp.Paragraph> paragraphs = pgraphs.ToList<wp.Paragraph>().FindAll(p => p.InnerText.Contains(valfound));
                var extract_paragraphs = from pr in pgraphs
                                         join rg in pgraphs
                                         on pr.InnerText equals rg.InnerText
                                         select pr;


                string _translated = String.Empty;
                wp.Paragraph wp_paragraph = null;
                coreformat.OpenXmlElement[] wp_runs = null;
                string[] trans_tokens = null;
                wp.Paragraph[] arr_paragraph = extract_paragraphs.ToArray<wp.Paragraph>();
                arr_paragraph = arr_paragraph.ToList().FindAll(p => p.ChildElements.Count > 0 && p.InnerText.Length > 0).ToArray<wp.Paragraph>();
                arr_paragraph = arr_paragraph.ToList().FindAll(p => p.InnerText != " ").ToArray();
                arr_paragraph = arr_paragraph.Distinct().ToArray();
                wp.Text n_text = null;
                // please dont chnage this class name its being excplicity defined as in norwegian code class to differ from wp.P 
                List<Paragrapah> lstparagraphs = doc.PickParagraphs;
                Paragrapah paragrapha = null;
                try
                {
                    checkmatchedcounts = new List<string>();
                    tems = new List<TempTokenMeasurement>();

                    for (int paraIndex = 0; paraIndex < arr_paragraph.Length; paraIndex++)
                    {
                         paragrapha = lstparagraphs[paraIndex];
                        _translated = doc.Paragraphs[paraIndex];
                        // change set 1.1 
                        // Author Syed Qadri derive this change set him self and decided after identification
                        // Requested CE0 Hassan M Khan
                    
                        wp_paragraph = arr_paragraph[paraIndex];
                        wp_runs = wp_paragraph.Elements<coreformat.OpenXmlElement>().ToArray();

                        var runs_withtext = from r in wp_runs.ToList()
                                            join r_copy in wp_runs
                                            on r.InnerText equals r_copy.InnerText
                                            where r.InnerText != valfound || !r.InnerText.Contains("")
                                            select r;

                        wp_runs = runs_withtext.ToArray<coreformat.OpenXmlElement>();
                        wp_runs = wp_runs.Distinct().ToList().FindAll(p => p.InnerText != " " && p.LocalName == "r" || p.LocalName == "hyperlink").ToArray();

                        // Assign Ppr , Assign Runs and all child as clone elements 
                        // Once update the list 
                        
                        
                        
                    }
                    DocumentLanguageCulture doc_culture = DocumentServerUtilityClass.GetCulture(doc.TranslateTo_Lang_Culture,_connector);
                    arr_paragraph = CreateNewParagraphs(tems, doc_culture);
                    // create a new body by copying properties from the clone merge back the in bodyue
                    pkg.WordprocessingDocument cloneWord = pkg.WordprocessingDocument.Create(filepath, coreformat.WordprocessingDocumentType.Document);
                    pkg.MainDocumentPart mainDocumentPart = cloneWord.AddMainDocumentPart();
                    wp.Document clonedocument = (wp.Document)wordDocument.MainDocumentPart.Document.CloneNode(true);
                    wp.Document docN = new wp.Document();
                    docN.DocumentBackground = clonedocument.DocumentBackground;
                    wp.Body body = new wp.Body();
                    body.Append(arr_paragraph);
                    docN.AppendChild<wp.Body>(body);
                    wp.Styles styles = null;
                    wp.Styles styles_eff = null;
                    pkg.StyleDefinitionsPart stylepart = null;
                    coreformat.OpenXmlElement elements_styles = null;
                    coreformat.OpenXmlElement elements_style_effects = null;
                    cloneWord.MainDocumentPart.Document = docN;
                    IEnumerable<pkg.HeaderPart> heaaderparts = wordDocument.MainDocumentPart.HeaderParts;
                    foreach (pkg.HeaderPart headerpart in heaaderparts)
                    {

                        pkg.HeaderPart hpart = cloneWord.MainDocumentPart.AddNewPart<pkg.HeaderPart>();
                        wp.Header header = (wp.Header)headerpart.Header.CloneNode(true);
                        hpart.Header = header;

                    }
                    IEnumerable<pkg.ImagePart> imageparts = wordDocument.MainDocumentPart.ImageParts;
                    foreach (pkg.ImagePart imagepart in imageparts)
                    {

                        pkg.ImagePart ipart = cloneWord.MainDocumentPart.AddImagePart(imagepart.ContentType);
                        ipart.FeedData(imagepart.GetStream());
                    }

                    XDocument styleXml = null;
                    if (wordDocument.MainDocumentPart.StyleDefinitionsPart != null)
                    {
                        stylepart = wordDocument.MainDocumentPart.StyleDefinitionsPart;
                        if (stylepart != null)
                        {
                            using (var reader = System.Xml.XmlNodeReader.Create(
                            stylepart.GetStream(FileMode.Open, FileAccess.Read)))
                            {
                                styleXml = System.Xml.Linq.XDocument.Load(reader);
                            }
                        }

                        style_part = cloneWord.MainDocumentPart.AddNewPart<pkg.StyleDefinitionsPart>();
                        styles = new wp.Styles(styleXml.ToString());
                        styles.Save(style_part);
                    }
                    if (wordDocument.MainDocumentPart.StylesWithEffectsPart != null)
                    {
                        var style_eff_part = wordDocument.MainDocumentPart.StylesWithEffectsPart;
                        if (style_eff_part != null)
                        {
                            using (var reader = System.Xml.XmlNodeReader.Create(
                            style_eff_part.GetStream(FileMode.Open, FileAccess.Read)))
                            {
                                styleXml = System.Xml.Linq.XDocument.Load(reader);
                            }
                        }

                        style_effects = cloneWord.MainDocumentPart.AddNewPart<pkg.StylesWithEffectsPart>();
                        styles_eff = new wp.Styles(styleXml.ToString());
                        styles_eff.Save(style_effects);
                    }
                    docStyleDefaults = style_part.Styles.DocDefaults;
                    // do a shallow here
                    if (docStyleDefaults != null)
                    {
                        if (docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Languages == null)
                        {
                            // embed your language here dynamically set
                            languages = new wp.Languages() { Val = doc_culture.LanguageID, Bidi = doc_culture.LanguageID, EastAsia = doc_culture.eastAsia };
                            docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Append(languages);
                            style_part.Styles.Save();

                        }
                        else
                        {
                            docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Languages.Remove();
                            languages = new wp.Languages() { Val = doc_culture.LanguageID, Bidi = doc_culture.LanguageID, EastAsia = doc_culture.eastAsia };



                            docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Append(languages);
                            if (docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.RunFonts != null)
                            {
                                docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.RunFonts.Remove();
                            }
                            // this is to test it must go in lang culture table

                            // Set this in DB Config just check for now but should  placed in language culture table to later get from linq

                            runfonts = new wp.RunFonts();
                            runfonts.Ascii = doc_culture.Ascii;
                            runfonts.AsciiTheme = DocumentServerUtilityClass.GetThemeValue(doc_culture.asciiTheme);
                            runfonts.ComplexScript = doc_culture.ComplexScript;
                            runfonts.ComplexScriptTheme = DocumentServerUtilityClass.GetThemeValue(doc_culture.ComplexScriptTheme);
                            runfonts.EastAsia = doc_culture.eastAsia;
                            runfonts.EastAsiaTheme = DocumentServerUtilityClass.GetThemeValue(doc_culture.eastAsiaTheme);
                            docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Append(runfonts);
                            style_part.Styles.Save();
                        }
                    }
                    else
                    {

                    }
                    if (style_effects != null)
                    {
                        docStyleDefaults = style_effects.Styles.DocDefaults;
                        if (docStyleDefaults != null)
                        {
                            languages = new wp.Languages() { Val = doc.TranslateTo_Lang_Culture, Bidi = doc.TranslateTo_Lang_Culture };
                            if (docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Languages == null)
                            {
                                docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Append(languages);
                                style_effects.Styles.Save();
                            }

                        }

                    }


                    // Once added get the other parts back



                    pkg.DocumentSettingsPart objDocumentSettingPart =
                    mainDocumentPart.AddNewPart<pkg.DocumentSettingsPart>();
                    objDocumentSettingPart.Settings = new wp.Settings();
                    wp.ThemeFontLanguages target_theme_languages = new wp.ThemeFontLanguages { Val = doc.TranslateTo_Lang_Culture, EastAsia = doc.TranslateTo_Lang_Culture, Bidi = doc.TranslateTo_Lang_Culture };

                    objDocumentSettingPart.Settings.Append(target_theme_languages);

                    wp.Compatibility objCompatibility = new wp.Compatibility();
                    wp.CompatibilitySetting objCompatibilitySetting =
                        new wp.CompatibilitySetting()
                        {
                            Name = wp.CompatSettingNameValues.CompatibilityMode,
                            Uri = "http://schemas.microsoft.com/office/word",
                            Val = "14"
                        };
                    objCompatibility.Append(objCompatibilitySetting);

                    wp.ActiveWritingStyle aws = new wp.ActiveWritingStyle()
                    {
                        Language = doc.TranslateTo_Lang_Culture
                    };
                    objDocumentSettingPart.Settings.Append(objCompatibility);
                    objDocumentSettingPart.Settings.Append(aws);

                    XDocument themeXml = null;
                    if (wordDocument.MainDocumentPart.ThemePart != null)
                    {
                        using (var reader = System.Xml.XmlNodeReader.Create(
                                wordDocument.MainDocumentPart.ThemePart.GetStream(FileMode.Open, FileAccess.Read)))
                        {
                            themeXml = XDocument.Load(reader);
                        }
                        DocumentFormat.OpenXml.Drawing.Theme themes = new DocumentFormat.OpenXml.Drawing.Theme(themeXml.ToString());

                        pkg.ThemePart themepart = cloneWord.MainDocumentPart.AddNewPart<pkg.ThemePart>();
                        themes.Save(themepart);

                    }
                    // later header could be arabic as well                    

                    wordDocument.Close();

                    cloneWord.MainDocumentPart.Document.Save();
                    cloneWord.Close();
                    FileStream fsWord = null;
                    FileStream fs = null;
             
                    fsWord = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite);
                    pdf_file = filepath.Replace("docx", "pdf");
             
                    try
                    {
                        if (WordToPDF.ConvertToPDF(filepath, pdf_file))
                        {
                            fs = new FileStream(pdf_file, FileMode.Open, FileAccess.ReadWrite);
                        }
                    }
                    catch (Exception excp)
                    {
                        System.Diagnostics.Trace.WriteLine(excp.Message);
                        fsWord = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite);

                    }
                    doc.InputStream = fs;
                    Upload(doc,filepath);
                    cloneWord.Close();
                    fs.Close();
                    try
                    {
                        File.Delete(filepath);
                        File.Delete(pdf_file);
                    }
                    catch (Exception eexcp)
                    {
                        System.Threading.Thread.Sleep(50);
                        File.Delete(filepath);
                        File.Delete(pdf_file);

                    }
                }

                //UPLOAD TO desTINTIOn sERVER
                catch (Exception excp)
                {
                    System.Diagnostics.Trace.TraceError(excp.Message + "========" + Environment.NewLine + excp.InnerException.Source);
                    throw excp;
                }
                finally
                {
                    wp_runs.ToList().Clear();
                    arr_paragraph.ToList().Clear();

                }


            }
        }

   
        // Object must be closed on server before fi=urther in processing is done
        // Redefined owned architecture for issues needs to address for other memeber like table , picture smart art
        public void CreateDocument(Document doc , bool processAllElements )
        {
            string pdf_file = string.Empty;
            wp.Table nTable = null;
            wp.BookmarkStart bmStart = null;
            List<string> checkmatchedcounts = null;
            wp.DocDefaults docStyleDefaults = null;
            List<string> onlyRuns = new List<string>();
            string filepath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("..\\bin"),
               System.IO.Path.GetFileName(doc.DocumentName));
            pkg.StyleDefinitionsPart style_part = null;
            pkg.StylesWithEffectsPart style_effects = null;
            Package package = Package.Open(doc.StreamData, FileMode.Open, FileAccess.ReadWrite);
            string str = String.Empty;
            List<TempTokenMeasurement> tems = null;
            wp.Languages languages = null;
            wp.RunFonts runfonts = null;
            wp.Languages l = null;
            string[] trans_tokens = null;
            wp.Justification justification = null;
         
             // Simple and Suddel
            List<wp.Paragraph> paragraphs = new List<wp.Paragraph>();
            wp.Paragraph brand_new_para = null;
            coreformat.OpenXmlElement childElementObject = null; 
            wp.Run brand_new_run = null;
            wp.BookmarkEnd bmEnd = null;
            wp.Run run = null;
            wp.Hyperlink hlink = null;
            wp.RunFonts run_fonts = null;
            List<coreformat.OpenXmlElement>  nRuns = new List<coreformat.OpenXmlElement>();
           pkg.WordprocessingDocument cloneWord  = null;
            DocumentLanguageCulture doc_culture = null;
          pkg.MainDocumentPart mainDocumentPart  = null; 
             wp.Document clonedocument = null;
            wp.Document docN = null;
            wp.Body body = null;

            try
            {
             doc_culture = DocumentServerUtilityClass.GetCulture(doc.TranslateTo_Lang_Culture , _connector );
             cloneWord = pkg.WordprocessingDocument.Create(filepath, coreformat.WordprocessingDocumentType.Document);
             mainDocumentPart = cloneWord.AddMainDocumentPart();

             clonedocument = (wp.Document)doc.DocumentStructure.DocumentCloned;
             docN = new wp.Document();
             docN.DocumentBackground = clonedocument.DocumentBackground;
             body = new wp.Body();

             foreach (ElementObject element in doc.DocumentStructure.Elements)
             {
                 // here we go the architecture now able to handle all  completed move the paragraph creation c
                 if (element.RealElementObjectCloned.LocalName == "p")
                 {
                     // Append Paragraph here 

                     // this architecture will create one brand new paragraph at a time from a newly translated 
                     // will not lost the properties defined for previous paragraph
                     brand_new_para = new wp.Paragraph();

                     trans_tokens = element.TranslatedElementText.Split("ɷʘ".ToCharArray());
                     // no need for extra proxy object aby more 


                     var runs = from r in element.RealElementObjectCloned.ChildElements
                                where r.LocalName == "r" || r.LocalName == "hyperlink" || r.LocalName == "bookmarkStart" || r.LocalName == "bookmarkEnd" || r.LocalName == "proofErr"
                                select r;

                     List<coreformat.OpenXmlElement> runners = runs.ToList<coreformat.OpenXmlElement>();
                     element.TransTokens = trans_tokens;
                     element.TransTokens = AdjustTokens(element);
                     // inversion of control don't explicity defined object here
                     // Removing my own rushed based architecture
                     for (int i = 0; i < element.RealElementObjectCloned.ChildElements.Count; i++)
                     {
                         childElementObject = element.RealElementObjectCloned.ChildElements[i];

                         if (childElementObject.LocalName == "bookmarkStart")
                         {
                             bmStart = new wp.BookmarkStart();
                             if (childElementObject.ChildElements.Count > 0)
                             {
                                 var childElements = from child in childElementObject.ChildElements
                                                     where child.LocalName != "r"
                                                     select child.CloneNode(true);
                                 List<coreformat.OpenXmlElement> elements = childElements.ToList();
                                 // run elemeent
                                 if (childElements.Count<coreformat.OpenXmlElement>() > 0)
                                 {
                                     bmStart.Append(childElements);
                                 }



                             }

                             nRuns.Add(bmStart);
                         }
                         else if (childElementObject.LocalName == "bookmarkEnd")
                         {
                             bmEnd = new wp.BookmarkEnd();
                             if (childElementObject.ChildElements.Count > 0)
                             {
                                 var childElements = from child in childElementObject.ChildElements
                                                     where child.LocalName != "r"
                                                     select child.CloneNode(true);
                                 List<coreformat.OpenXmlElement> elements = childElements.ToList();
                                 // run elemeent
                                 if (childElements.Count<coreformat.OpenXmlElement>() > 0)
                                 {
                                     bmEnd.Append(childElements);
                                 }


                             }

                             nRuns.Add(bmEnd);


                         }

                         else if (childElementObject.LocalName == "hyperlink")
                         {
                             hlink = new wp.Hyperlink();

                             var childElements = from child in childElementObject.ChildElements
                                                 where child.LocalName != "r"
                                                 select child.CloneNode(true);
                             List<coreformat.OpenXmlElement> elements = childElements.ToList();
                             // run elemeent
                             if (childElements.Count<coreformat.OpenXmlElement>() > 0)
                             {
                                 hlink.Append(childElements);
                             }

                             childElements = from child in childElementObject.ChildElements
                                             where child.LocalName == "r"
                                             select child.CloneNode(true);

                             elements = childElements.ToList();

                             if (elements.Count > 0)
                             {
                                 var elem = elements.First();
                                 run = new wp.Run();
                                 var childElement = from child in elem.ChildElements
                                                    where child.LocalName != "t"
                                                    select child.CloneNode(true);
                                 elements = childElement.ToList();
                                 // run elemeent
                                 run.Append(elements);
                                 if (run.RunProperties != null)
                                 {
                                     if (run.RunProperties.Languages != null)
                                     {
                                         run.RunProperties.Languages.Remove();

                                     }
                                     l = new wp.Languages();
                                     l.Val = doc_culture.LanguageID;
                                     l.Bidi = doc_culture.LanguageID;
                                     l.EastAsia = doc_culture.LanguageID;

                                     run.RunProperties.Append(l);
                                     if (run.RunProperties.RunFonts != null)
                                     {
                                         run.RunProperties.RunFonts.Remove();
                                     }
                                     run_fonts = new wp.RunFonts();

                                     // Set this in DB Config just check for now but should  placed in language culture table to later get from linq
                                     run_fonts.Ascii = doc_culture.Ascii;
                                     run_fonts.AsciiTheme = DocumentServerUtilityClass.GetThemeValue(doc_culture.asciiTheme);
                                     run_fonts.ComplexScript = doc_culture.ComplexScript;
                                     run_fonts.ComplexScriptTheme = DocumentServerUtilityClass.GetThemeValue(doc_culture.ComplexScriptTheme);
                                     run_fonts.EastAsia = doc_culture.eastAsia;
                                     run_fonts.EastAsiaTheme = DocumentServerUtilityClass.GetThemeValue(doc_culture.eastAsiaTheme);
                                     run.RunProperties.Append(run_fonts);


                                 }
                                 // insert translated text here // remember its new run it doesn't have any text
                                 run.AppendChild<wp.Text>(new wp.Text(element.TransTokens[i]));
                                 hlink.AppendChild(run);


                             }
                             nRuns.Add(hlink);

                         }
                         else if (childElementObject.LocalName == "r")
                         {
                             run = new wp.Run();
                             // at this point all child elements cloned appended except for text  

                             var childElements = from child in childElementObject.ChildElements
                                                 where child.LocalName != "t" && child.LocalName != "drawing"
                                                 select child.CloneNode(true);
                             List<coreformat.OpenXmlElement> elements = childElements.ToList();
                             run.Append(childElements);
                             // insert translated text here // remember its new run it doesn't have any text
                             if (i <= element.TransTokens.Length)
                             {
                                 run.AppendChild<wp.Text>(new wp.Text(element.TransTokens[i].Trim() + " "));
                             }

                             nRuns.Add(run);
                         }
                     }

                     brand_new_para.Append(nRuns);
                     body.AppendChild<wp.Paragraph>(brand_new_para);
                     // copy below the old logic for paragraph creation here now
                 }
                 else if (element.RealElementObjectCloned.LocalName == "tbl")
                 {
                     if (element.IsTable)
                     {
                         nTable = new wp.Table();
                         nTable.Append(element.RealElementObjectCloned.ChildElements);
                         // Create a new table from clone 
                         var rows = from tableElement in nTable.ChildElements
                                    where tableElement.LocalName == "tr"
                                    select tableElement;

                         // do for tables here 
                         foreach (var row in rows)
                         {

                             var cells = from cell in row.ChildElements
                                         where cell.LocalName == "tc"
                                         select cell;

                             // remove existing paragraph for each table and create new paragraph
                             foreach (var para_cell in cells)
                             {


                                 var prgrphs = from paragraph in para_cell.ChildElements
                                               where paragraph.LocalName == "p"
                                               select paragraph;

                                 wp.ParagraphProperties pPr = null;
                                 string table_translated_text = element.TranslatedElementText;
                                 string[] translated = element.TranslatedElementText.Split(" ".ToCharArray());
                                 List<wp.Paragraph> tblParagraphs = new List<wp.Paragraph>();
                                 foreach (wp.Paragraph para in prgrphs)
                                 {
                                     brand_new_para = new wp.Paragraph();
                                     brand_new_para.Append(para.CloneNode(true).ChildElements);
                                     // get all   para.ChildElements create a clone of it and identify each element and 
                                     // create a new element and then break the translated text into number of elements
                                     // just as u did before 
                                     // also change the text inside the paragraph with the translated text

                                     trans_tokens = element.TranslatedElementText.Split("ɷʘ".ToCharArray());
                                     // no need for extra proxy object aby more 

                                     var runs = from r in element.RealElementObjectCloned.ChildElements
                                                where r.LocalName == "r" || r.LocalName == "hyperlink" || r.LocalName == "bookmarkStart" || r.LocalName == "bookmarkEnd" || r.LocalName == "proofErr"
                                                select r;

                                     List<coreformat.OpenXmlElement> runners = runs.ToList<coreformat.OpenXmlElement>();
                                     element.TransTokens = AdjustTokens(element);

                                     // inversion of control don't explicity defined object here
                                     // Removing my own rushed based architecture
                                     for (int i = 0; i < element.RealElementObjectCloned.ChildElements.Count; i++)
                                     {
                                         childElementObject = element.RealElementObjectCloned.ChildElements[i];

                                         if (childElementObject.LocalName == "bookmarkStart")
                                         {
                                             bmStart = new wp.BookmarkStart();
                                             if (childElementObject.ChildElements.Count > 0)
                                             {
                                                 var childElements = from child in childElementObject.ChildElements
                                                                     where child.LocalName != "r"
                                                                     select child.CloneNode(true);
                                                 List<coreformat.OpenXmlElement> elements = childElements.ToList();
                                                 // run elemeent
                                                 if (childElements.Count<coreformat.OpenXmlElement>() > 0)
                                                 {
                                                     bmStart.Append(childElements);
                                                 }



                                             }

                                             nRuns.Add(bmStart);
                                         }
                                         else if (childElementObject.LocalName == "bookmarkEnd")
                                         {
                                             bmEnd = new wp.BookmarkEnd();
                                             if (childElementObject.ChildElements.Count > 0)
                                             {
                                                 var childElements = from child in childElementObject.ChildElements
                                                                     where child.LocalName != "r"
                                                                     select child.CloneNode(true);
                                                 List<coreformat.OpenXmlElement> elements = childElements.ToList();
                                                 // run elemeent
                                                 if (childElements.Count<coreformat.OpenXmlElement>() > 0)
                                                 {
                                                     bmEnd.Append(childElements);
                                                 }


                                             }

                                             nRuns.Add(bmEnd);


                                         }

                                         else if (childElementObject.LocalName == "hyperlink")
                                         {
                                             hlink = new wp.Hyperlink();

                                             var childElements = from child in childElementObject.ChildElements
                                                                 where child.LocalName != "r"
                                                                 select child.CloneNode(true);
                                             List<coreformat.OpenXmlElement> elements = childElements.ToList();
                                             // run elemeent
                                             if (childElements.Count<coreformat.OpenXmlElement>() > 0)
                                             {
                                                 hlink.Append(childElements);
                                             }

                                             childElements = from child in childElementObject.ChildElements
                                                             where child.LocalName == "r"
                                                             select child.CloneNode(true);

                                             elements = childElements.ToList();

                                             if (elements.Count > 0)
                                             {
                                                 var elem = elements.First();
                                                 run = new wp.Run();
                                                 var childElement = from child in elem.ChildElements
                                                                    where child.LocalName != "t"
                                                                    select child.CloneNode(true);
                                                 elements = childElement.ToList();
                                                 // run elemeent
                                                 run.Append(elements);
                                                 if (run.RunProperties != null)
                                                 {
                                                     if (run.RunProperties.Languages != null)
                                                     {
                                                         run.RunProperties.Languages.Remove();

                                                     }
                                                     l = new wp.Languages();
                                                     l.Val = doc_culture.LanguageID;
                                                     l.Bidi = doc_culture.LanguageID;
                                                     l.EastAsia = doc_culture.LanguageID;

                                                     run.RunProperties.Append(l);
                                                     if (run.RunProperties.RunFonts != null)
                                                     {
                                                         run.RunProperties.RunFonts.Remove();
                                                     }
                                                     run_fonts = new wp.RunFonts();

                                                     // Set this in DB Config just check for now but should  placed in language culture table to later get from linq
                                                     run_fonts.Ascii = doc_culture.Ascii;
                                                     run_fonts.AsciiTheme = DocumentServerUtilityClass.GetThemeValue(doc_culture.asciiTheme);
                                                     run_fonts.ComplexScript = doc_culture.ComplexScript;
                                                     run_fonts.ComplexScriptTheme = DocumentServerUtilityClass.GetThemeValue(doc_culture.ComplexScriptTheme);
                                                     run_fonts.EastAsia = doc_culture.eastAsia;
                                                     run_fonts.EastAsiaTheme = DocumentServerUtilityClass.GetThemeValue(doc_culture.eastAsiaTheme);
                                                     run.RunProperties.Append(run_fonts);


                                                 }
                                                 // insert translated text here // remember its new run it doesn't have any text
                                                 run.AppendChild<wp.Text>(new wp.Text(element.TransTokens[i]));
                                                 hlink.AppendChild(run);


                                             }
                                             nRuns.Add(hlink);

                                         }
                                         else if (childElementObject.LocalName == "r")
                                         {
                                             run = new wp.Run();
                                             // at this point all child elements cloned appended except for text  
                                             var childElements = from child in childElementObject.ChildElements
                                                                 where child.LocalName != "t" && child.LocalName != "drawing"
                                                                 select child.CloneNode(true);
                                             List<coreformat.OpenXmlElement> elements = childElements.ToList();
                                             run.Append(childElements);
                                             // insert translated text here // remember its new run it doesn't have any text

                                             // Creating new runs .... issue again back in 2014 damn
                                             if (i <= element.TransTokens.Length - 1)
                                             {
                                                 run.AppendChild<wp.Text>(new wp.Text(element.TransTokens[i].Trim() + " "));
                                             }

                                             nRuns.Add(run);
                                         }
                                     }

                                     brand_new_para.Append(nRuns);
                                     if (para.ParagraphProperties != null)
                                     {
                                         brand_new_para.ParagraphProperties = (wp.ParagraphProperties)para.ParagraphProperties.CloneNode(true);
                                         // change the paragraph properties in the table according to  culture information
                                         if (para.ParagraphProperties.Justification != null)
                                         {
                                             justification = brand_new_para.ParagraphProperties.Justification;
                                             brand_new_para.ParagraphProperties.RemoveChild<wp.Justification>(justification);
                                             justification = new wp.Justification() { Val = DocumentServerUtilityClass.GetJustification(doc_culture.LanguageID) };
                                         }
                                         else
                                         {
                                             justification = new wp.Justification() { Val = DocumentServerUtilityClass.GetJustification(doc_culture.LanguageID) };
                                             brand_new_para.ParagraphProperties.AppendChild(justification);

                                         }

                                     }


                                     tblParagraphs.Add(brand_new_para);

                                 }
                                 para_cell.RemoveAllChildren<wp.Paragraph>();
                                 para_cell.Append(tblParagraphs);


                             }

                         }

                     }
                     body.AppendChild<wp.Table>(nTable);

                     // Append table here 

                 }
                 else // anyother smart art or picture or image
                 {
                     body.AppendChild(element.RealElementObjectCloned);
                 }
             }
                    docN.AppendChild<wp.Body>(body);
                    wp.Styles styles = null;
                    wp.Styles styles_eff = null;
                    pkg.StyleDefinitionsPart stylepart = null;
                    coreformat.OpenXmlElement elements_styles = null;
                    coreformat.OpenXmlElement elements_style_effects = null;
                    cloneWord.MainDocumentPart.Document = docN;
                   
                    IEnumerable<pkg.HeaderPart> heaaderparts =  doc.DocumentStructure.HeaderParts;
                    
                    foreach (pkg.HeaderPart headerpart in heaaderparts)
                    {

                        pkg.HeaderPart hpart = cloneWord.MainDocumentPart.AddNewPart<pkg.HeaderPart>();
                        wp.Header header = (wp.Header)headerpart.Header.CloneNode(true);
                        hpart.Header = header;

                    }
                    IEnumerable<pkg.ImagePart> imageparts = doc.DocumentStructure.ImageParts;
                    foreach (pkg.ImagePart imagepart in imageparts)
                    {

                        pkg.ImagePart ipart = cloneWord.MainDocumentPart.AddImagePart(imagepart.ContentType);
                        ipart.FeedData(imagepart.GetStream());
                    }

                    XDocument styleXml = null;
                    if (clonedocument.MainDocumentPart.StyleDefinitionsPart != null)
                    {
                        stylepart = clonedocument.MainDocumentPart.StyleDefinitionsPart;
                        if (stylepart != null)
                        {
                            using (var reader = System.Xml.XmlNodeReader.Create(
                            stylepart.GetStream(FileMode.Open, FileAccess.Read)))
                            {
                                styleXml = System.Xml.Linq.XDocument.Load(reader);
                            }
                        }

                        style_part = cloneWord.MainDocumentPart.AddNewPart<pkg.StyleDefinitionsPart>();
                        styles = new wp.Styles(styleXml.ToString());
                        styles.Save(style_part);
                    }

                    if (clonedocument.MainDocumentPart.StylesWithEffectsPart != null)
                    {
                        var style_eff_part = clonedocument.MainDocumentPart.StylesWithEffectsPart;
                        if (style_eff_part != null)
                        {
                            using (var reader = System.Xml.XmlNodeReader.Create(
                            style_eff_part.GetStream(FileMode.Open, FileAccess.Read)))
                            {
                                styleXml = System.Xml.Linq.XDocument.Load(reader);
                            }
                        }

                        style_effects = cloneWord.MainDocumentPart.AddNewPart<pkg.StylesWithEffectsPart>();
                        styles_eff = new wp.Styles(styleXml.ToString());
                        styles_eff.Save(style_effects);
                    }
                    docStyleDefaults = style_part.Styles.DocDefaults;
                    // do a shallow here
                    if (docStyleDefaults != null)
                    {
                        if (docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Languages == null)
                        {
                            // embed your language here dynamically set
                            languages = new wp.Languages() { Val = doc_culture.LanguageID, Bidi = doc_culture.LanguageID, EastAsia = doc_culture.eastAsia };
                            docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Append(languages);
                            style_part.Styles.Save();

                        }
                        else
                        {
                            docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Languages.Remove();
                            languages = new wp.Languages() { Val = doc_culture.LanguageID, Bidi = doc_culture.LanguageID, EastAsia = doc_culture.eastAsia };



                            docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Append(languages);
                            if (docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.RunFonts != null)
                            {
                                docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.RunFonts.Remove();
                            }
                            // this is to test it must go in lang culture table

                            // Set this in DB Config just check for now but should  placed in language culture table to later get from linq

                            runfonts = new wp.RunFonts();
                            runfonts.Ascii = doc_culture.Ascii;
                            runfonts.AsciiTheme = DocumentServerUtilityClass.GetThemeValue(doc_culture.asciiTheme);
                            runfonts.ComplexScript = doc_culture.ComplexScript;
                            runfonts.ComplexScriptTheme = DocumentServerUtilityClass.GetThemeValue(doc_culture.ComplexScriptTheme);
                            runfonts.EastAsia = doc_culture.eastAsia;
                            runfonts.EastAsiaTheme = DocumentServerUtilityClass.GetThemeValue(doc_culture.eastAsiaTheme);
                            docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Append(runfonts);
                            style_part.Styles.Save();
                        }
                    }
                    else
                    {

                    }
                    if (style_effects != null)
                    {
                        docStyleDefaults = style_effects.Styles.DocDefaults;
                        if (docStyleDefaults != null)
                        {
                            languages = new wp.Languages() { Val = doc.TranslateTo_Lang_Culture, Bidi = doc.TranslateTo_Lang_Culture };
                            if (docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Languages == null)
                            {
                                docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Append(languages);
                                style_effects.Styles.Save();
                            }

                        }

                    }


                    // Once added get the other parts back

                    pkg.DocumentSettingsPart objDocumentSettingPart =
                    mainDocumentPart.AddNewPart<pkg.DocumentSettingsPart>();
                    objDocumentSettingPart.Settings = new wp.Settings();
                    wp.ThemeFontLanguages target_theme_languages = new wp.ThemeFontLanguages { Val = doc.TranslateTo_Lang_Culture, EastAsia = doc.TranslateTo_Lang_Culture, Bidi = doc.TranslateTo_Lang_Culture };

                    objDocumentSettingPart.Settings.Append(target_theme_languages);

                    wp.Compatibility objCompatibility = new wp.Compatibility();
                    wp.CompatibilitySetting objCompatibilitySetting =
                        new wp.CompatibilitySetting()
                        {
                            Name = wp.CompatSettingNameValues.CompatibilityMode,
                            Uri = "http://schemas.microsoft.com/office/word",
                            Val = "14"
                        };
                    objCompatibility.Append(objCompatibilitySetting);

                    wp.ActiveWritingStyle aws = new wp.ActiveWritingStyle()
                    {
                        Language = doc.TranslateTo_Lang_Culture
                    };
                    objDocumentSettingPart.Settings.Append(objCompatibility);
                    objDocumentSettingPart.Settings.Append(aws);

                    XDocument themeXml = null;
                    if (clonedocument.MainDocumentPart.ThemePart != null)
                    {
                        using (var reader = System.Xml.XmlNodeReader.Create(
                                clonedocument.MainDocumentPart.ThemePart.GetStream(FileMode.Open, FileAccess.Read)))
                        {
                            themeXml = XDocument.Load(reader);
                        }
                        DocumentFormat.OpenXml.Drawing.Theme themes = new DocumentFormat.OpenXml.Drawing.Theme(themeXml.ToString());

                        pkg.ThemePart themepart = cloneWord.MainDocumentPart.AddNewPart<pkg.ThemePart>();
                        themes.Save(themepart);
                    }
                

                FileStream fs = null;
                pdf_file = filepath.Replace("docx", "pdf");
                try
                {
                    if (WordToPDF.ConvertToPDF(filepath, pdf_file))
                    {
                        fs = new FileStream(pdf_file, FileMode.Open, FileAccess.ReadWrite);

                    }
                }
                catch (Exception excp)
                {
                    System.Diagnostics.Trace.WriteLine(excp.Message);
                    fs = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite);

                }
                doc.InputStream = fs;

            }

            catch (Exception excp)
            {



            }

            finally
            {
                doc.Package.Close();
                doc.WordDocument.Close();
                Upload(doc,filepath);

            }
           
    
            
        }


        // make it more clear rather object to wast 2000 objects  each paragraph
        public void CreateDocument(Document doc)
        {
            string pdf_file = string.Empty;
            List<string> checkmatchedcounts = null;
            wp.DocDefaults docStyleDefaults = null;
            List<string> onlyRuns = new List<string>();
             string[] files = null;
             string _directoryPath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("\\Downloads"));
            string filepath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath("\\Downloads"),
               System.IO.Path.GetFileName(doc.DocumentName));
                if(File.Exists(filepath))
                {
                 System.IO.File.Delete(filepath);
                }

                System.Threading.Thread.Sleep(1000);
                     

            pkg.StyleDefinitionsPart style_part = null;
            pkg.StylesWithEffectsPart style_effects = null;
            Package package = Package.Open(doc.StreamData, FileMode.Open, FileAccess.ReadWrite);
            string str = String.Empty;
            List<TempTokenMeasurement> tems = null;
            wp.Languages languages = null;
            wp.RunFonts runfonts = null;

            pkg.WordprocessingDocument wordDocument = pkg.WordprocessingDocument.Open(package);
           
                // Add a main document part. 



                IEnumerable<wp.Paragraph> pgraphs = from para in wordDocument.MainDocumentPart.Document.Body.Elements<wp.Paragraph>()
                                                    where para.InnerText != "" || para.InnerText != str || para.InnerText != " "
                                                    select para;
                string valfound = String.Empty;
                List<wp.Paragraph> paragraphs = pgraphs.ToList<wp.Paragraph>().FindAll(p => p.InnerText.Contains(valfound));
                var extract_paragraphs = from pr in pgraphs
                                         join rg in pgraphs
                                         on pr.InnerText equals rg.InnerText
                                         select pr;




                string _translated = String.Empty;
                wp.Paragraph wp_paragraph = null;
                coreformat.OpenXmlElement[] wp_runs = null;
                string[] trans_tokens = null;
                wp.Paragraph[] arr_paragraph = extract_paragraphs.ToArray<wp.Paragraph>();
                arr_paragraph = arr_paragraph.ToList().FindAll(p => p.ChildElements.Count > 0 && p.InnerText.Length > 0 ).ToArray<wp.Paragraph>();
                arr_paragraph = arr_paragraph.ToList().FindAll(p => p.InnerText != " " ).ToArray();
                arr_paragraph = arr_paragraph.Distinct().ToArray();
                wp.Text n_text = null;
                try
                {
                    checkmatchedcounts = new List<string>();
                    tems = new List<TempTokenMeasurement>();
                    for (int paraIndex = 0; paraIndex < arr_paragraph.Length; paraIndex++)
                    {
                        _translated = doc.Paragraph_Builder[paraIndex].ToString();
                        if (!String.IsNullOrEmpty(_translated))
                        {
                            trans_tokens = _translated.Split("ɷʘ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            trans_tokens = trans_tokens.ToList().FindAll(p => p != " ").ToArray();
                        }

                        wp_paragraph = arr_paragraph[paraIndex];
                        wp_runs = wp_paragraph.Elements<coreformat.OpenXmlElement>().ToArray();

                        var runs_withtext = from r in wp_runs.ToList()
                                            join r_copy in wp_runs
                                            on r.InnerText equals r_copy.InnerText
                                            where r.InnerText != valfound || !r.InnerText.Contains("")
                                            select r;

                        wp_runs = runs_withtext.ToArray<coreformat.OpenXmlElement>();
                        wp_runs = wp_runs.Distinct().ToList().FindAll(p => p.InnerText != " " && p.LocalName == "r" || p.LocalName == "hyperlink" ).ToArray();


                        // Adjust Transtokens create new translated paragraph/runs for each paragraph
                        // Once generated reformed back 
                        // Remember don't delete or insert any run 
                        // here just create a new list of wp.Paragraphs from translated paragraphs and runs inside it with 
                        // return this list and create a new dcoument out of it
                        // if you insert you messed up the runs 
                        // no need for clones or duplicate document 
                        // for instance paragraphIndex = 0 ( Create a new paragraph)
                        // then clone the properties and add new properties to it 
                        // that way no order will be destructed .

                    
                        
                        
                        
                        if (trans_tokens.Length < wp_runs.Length)
                        {
                            tems.Add(new TempTokenMeasurement() { tokens = trans_tokens.ToList(), RunsAll = wp_runs.ToList(), Paragraph = (coreformat.OpenXmlElement)wp_paragraph.CloneNode(true), Unmatched = true, PargraphIndex = paraIndex, StyleDefinationPart = null });
                        }
                        else if (trans_tokens.Length == wp_runs.Length)
                        {
                            tems.Add(new TempTokenMeasurement() { tokens = trans_tokens.ToList(), RunsAll = wp_runs.ToList(), Paragraph = (coreformat.OpenXmlElement)wp_paragraph.CloneNode(true), Unmatched = false, PargraphIndex = paraIndex, StyleDefinationPart = null });
                        }
                        else
                        {
                            tems.Add(new TempTokenMeasurement() { tokens = trans_tokens.ToList(), RunsAll = wp_runs.ToList(), Paragraph = (coreformat.OpenXmlElement)wp_paragraph.CloneNode(true), Unmatched = false, PargraphIndex = paraIndex, StyleDefinationPart = null });

                        }
                    }
                    DocumentLanguageCulture doc_culture = DocumentServerUtilityClass.GetCulture(doc.TranslateTo_Lang_Culture , _connector);
                    arr_paragraph = CreateNewParagraphs(tems, doc_culture);
                    // create a new body by copying properties from the clone merge back the in bodyue
                    pkg.WordprocessingDocument cloneWord = pkg.WordprocessingDocument.Create(filepath, coreformat.WordprocessingDocumentType.Document);
                    pkg.MainDocumentPart mainDocumentPart = cloneWord.AddMainDocumentPart();
                    wp.Document clonedocument = (wp.Document)wordDocument.MainDocumentPart.Document.CloneNode(true);
                    wp.Document docN = new wp.Document();
                    docN.DocumentBackground = clonedocument.DocumentBackground;
                    wp.Body body = new wp.Body();
                    body.Append(arr_paragraph);
                    docN.AppendChild<wp.Body>(body);
                    wp.Styles styles = null;
                    wp.Styles styles_eff = null;
                    pkg.StyleDefinitionsPart stylepart = null;
                    coreformat.OpenXmlElement elements_styles = null;
                    coreformat.OpenXmlElement elements_style_effects = null;
                    cloneWord.MainDocumentPart.Document = docN;
                    IEnumerable<pkg.HeaderPart> heaaderparts = wordDocument.MainDocumentPart.HeaderParts;
                    foreach (pkg.HeaderPart headerpart in heaaderparts)
                    {

                        pkg.HeaderPart hpart = cloneWord.MainDocumentPart.AddNewPart<pkg.HeaderPart>();
                        wp.Header header = (wp.Header)headerpart.Header.CloneNode(true);
                        hpart.Header = header;

                    }
                    IEnumerable<pkg.ImagePart> imageparts = wordDocument.MainDocumentPart.ImageParts;
                    foreach (pkg.ImagePart imagepart in imageparts)
                    {
                                              
                        pkg.ImagePart ipart = cloneWord.MainDocumentPart.AddImagePart(imagepart.ContentType);
                        ipart.FeedData(imagepart.GetStream());
                    }

                    XDocument styleXml = null;
                    if (wordDocument.MainDocumentPart.StyleDefinitionsPart != null)
                    {
                        stylepart = wordDocument.MainDocumentPart.StyleDefinitionsPart;
                        if (stylepart != null)
                        {
                            using (var reader = System.Xml.XmlNodeReader.Create(
                            stylepart.GetStream(FileMode.Open, FileAccess.Read)))
                            {
                                styleXml = System.Xml.Linq.XDocument.Load(reader);
                            }
                        }

                        style_part = cloneWord.MainDocumentPart.AddNewPart<pkg.StyleDefinitionsPart>();
                        styles = new wp.Styles(styleXml.ToString());
                        styles.Save(style_part);
                    }
                    if (wordDocument.MainDocumentPart.StylesWithEffectsPart != null)
                    {
                        var style_eff_part = wordDocument.MainDocumentPart.StylesWithEffectsPart;
                        if (style_eff_part != null)
                        {
                            using (var reader = System.Xml.XmlNodeReader.Create(
                            style_eff_part.GetStream(FileMode.Open, FileAccess.Read)))
                            {
                                styleXml = System.Xml.Linq.XDocument.Load(reader);
                            }
                        }

                        style_effects = cloneWord.MainDocumentPart.AddNewPart<pkg.StylesWithEffectsPart>();
                        styles_eff = new wp.Styles(styleXml.ToString());
                        styles_eff.Save(style_effects);
                    }
                    docStyleDefaults = style_part.Styles.DocDefaults;
                    // do a shallow here
                    if (docStyleDefaults != null)
                    {
                        if (docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Languages == null)
                        {
                            // embed your language here dynamically set
                            languages = new wp.Languages() { Val = doc_culture.LanguageID, Bidi = doc_culture.LanguageID   , EastAsia = doc_culture.eastAsia  };
                            docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Append(languages);
                            style_part.Styles.Save();

                        }
                        else
                        {
                            docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Languages.Remove();
                                                 languages = new wp.Languages() { Val = doc_culture.LanguageID, Bidi = doc_culture.LanguageID   , EastAsia = doc_culture.eastAsia  };



                            docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Append(languages);
                            if (docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.RunFonts != null)
                            {
                                docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.RunFonts.Remove();
                            }
                            // this is to test it must go in lang culture table

                            // Set this in DB Config just check for now but should  placed in language culture table to later get from linq

                            runfonts = new wp.RunFonts();
                            runfonts.Ascii = doc_culture.Ascii;
                            runfonts.AsciiTheme = DocumentServerUtilityClass.GetThemeValue(doc_culture.asciiTheme);
                            runfonts.ComplexScript = doc_culture.ComplexScript;
                            runfonts.ComplexScriptTheme = DocumentServerUtilityClass.GetThemeValue(doc_culture.ComplexScriptTheme);
                            runfonts.EastAsia = doc_culture.eastAsia;
                            runfonts.EastAsiaTheme = DocumentServerUtilityClass.GetThemeValue(doc_culture.eastAsiaTheme);
                            docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Append(runfonts);
                            style_part.Styles.Save();
                        }
                    }
                    else
                    {

                    }
                    if (style_effects != null)
                    {
                        docStyleDefaults = style_effects.Styles.DocDefaults;
                        if (docStyleDefaults != null)
                        {
                            languages = new wp.Languages() { Val = doc.TranslateTo_Lang_Culture, Bidi = doc.TranslateTo_Lang_Culture };
                            if (docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Languages == null)
                            {
                                docStyleDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.Append(languages);
                                style_effects.Styles.Save();
                            }

                        }

                    }


                    // Once added get the other parts back



                    pkg.DocumentSettingsPart objDocumentSettingPart =
                    mainDocumentPart.AddNewPart<pkg.DocumentSettingsPart>();
                    objDocumentSettingPart.Settings = new wp.Settings();
                    wp.ThemeFontLanguages target_theme_languages = new wp.ThemeFontLanguages { Val = doc.TranslateTo_Lang_Culture, EastAsia = doc.TranslateTo_Lang_Culture, Bidi = doc.TranslateTo_Lang_Culture };

                    objDocumentSettingPart.Settings.Append(target_theme_languages);

                     wp.Compatibility objCompatibility = new wp.Compatibility();
                    wp.CompatibilitySetting objCompatibilitySetting =
                        new wp.CompatibilitySetting()
                        {
                            Name = wp.CompatSettingNameValues.CompatibilityMode,
                            Uri = "http://schemas.microsoft.com/office/word",
                            Val = "14"
                        };
                    objCompatibility.Append(objCompatibilitySetting); 

                    wp.ActiveWritingStyle aws = new wp.ActiveWritingStyle()
                    {
                        Language = doc.TranslateTo_Lang_Culture
                    };
                     objDocumentSettingPart.Settings.Append(objCompatibility);
                    objDocumentSettingPart.Settings.Append(aws);

                    XDocument themeXml = null;
                    if (wordDocument.MainDocumentPart.ThemePart != null)
                    {
                        using (var reader = System.Xml.XmlNodeReader.Create(
                                wordDocument.MainDocumentPart.ThemePart.GetStream(FileMode.Open, FileAccess.Read)))
                        {
                            themeXml = XDocument.Load(reader);
                        }
                        DocumentFormat.OpenXml.Drawing.Theme themes = new DocumentFormat.OpenXml.Drawing.Theme(themeXml.ToString());

                        pkg.ThemePart themepart = cloneWord.MainDocumentPart.AddNewPart<pkg.ThemePart>();
                        themes.Save(themepart);

                    }
                    // later header could be arabic as well                    

                    wordDocument.Close();

                    cloneWord.MainDocumentPart.Document.Save();
                    cloneWord.Close();
                    
                    FileStream fsWord = null;
                    FileStream fs = null;
                    try
                    {
                        fsWord = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite);
                        // Close the document so that it coould be  accessded 
                        pdf_file = filepath.Replace("docx", "pdf");
                        fsWord.Close();
                        if (WordToPDF.ConvertToPDF(filepath, pdf_file))
                        {
                               fs = new FileStream(pdf_file, FileMode.Open, FileAccess.ReadWrite);
                               
                        }
                    }
                    catch (Exception excp)
                    {
                        System.Diagnostics.Trace.WriteLine(excp.Message);
                       
                    }
                    fsWord = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite);
                    doc.InputStream = fsWord;
                    doc.PDFInputStream = fs;
                    Upload(doc ,filepath);
                    doc.PDFInputStream.Close();
                    doc.InputStream.Close();
                    cloneWord.Close();
                    fs.Close();
                    fsWord.Close();
                    fsWord.Dispose();
                    fs.Dispose();
                    doc.ClonedWordDocument.Close();
                    wordDocument.Dispose();

                    File.Delete(filepath);

                    
                   
                 }

                //UPLOAD TO desTINTIOn sERVER
                catch (Exception excp)
                {
                    System.Diagnostics.Trace.TraceError(excp.Message + "========" + Environment.NewLine + excp.InnerException.Source);
                    throw excp;
                }
                finally
                {
                    wp_runs.ToList().Clear();
                    arr_paragraph.ToList().Clear();
                    wordDocument.Close();
                    package.Close();
                    
                }


            
        }

        #region "Create new runs from clone"
        private void CreateRunForParagraph(ref wp.Paragraph pr, wp.Run cloneRun, string translated_text)
        {
            wp.Run run = new wp.Run();
            coreformat.OpenXmlElement element = null;
            foreach (coreformat.OpenXmlElement oldelement in cloneRun.ChildElements)
            {
                element = (coreformat.OpenXmlElement)oldelement.Clone();
                run.AppendChild<coreformat.OpenXmlElement>(element);

            }

            wp.Text textElement = run.Elements<wp.Text>().First();
            run.RemoveChild<wp.Text>(textElement);
            run.AppendChild<wp.Text>(new wp.Text(translated_text));
            pr.AppendChild<wp.Run>(run);

        }
        #endregion
        private void Upload(Document document ,string filePath )
        {
            string _filename = String.Empty;
            MemoryStream mem_stream = null;
            FileStream fs = null;
            FileStream fsPdf = null;
            string _blobcontainer_reference = String.Empty;
            string _blobblockreference = String.Empty;
            string[] server_urls = null;
            string server_url = document.DocumentServerURL;
            CloudStorageAccount storageAccount = null;
            CloudBlobClient blobClient = null;
            CloudBlobContainer container = null;
            CloudBlockBlob blockblob = null;
            CloudBlobDirectory directory = null;
            
            try
            {
                if (!String.IsNullOrEmpty(server_url))
                {
                    server_urls = server_url.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                   
                    if (!String.IsNullOrEmpty(document.DocumentName))
                    {
                        _filename = document.DocumentName;
                        // Before that load all dropdown list for document servers  
                        Environment.SetEnvironmentVariable("translationDuration", "Uploading");

                        storageAccount = CloudStorageAccount.Parse(document.StorageConnectionPoint);
                        
                        blobClient = storageAccount.CreateCloudBlobClient();
                        blobClient.DefaultDelimiter = "/";
                        container = blobClient.GetContainerReference(document.documentUser);
                        // Create the container if it doesn't already exist.
                        // Go to the manage azure portal create a blob in blob container 
                        // Store the reference of blob in Database
                        // leave it to the team B
                        directory = container.GetDirectoryReference(document.documentUser);
                        // Retrieve reference to a blob named "myblob".
                        blockblob = container.GetBlockBlobReference(document.documentUser);
                        //  var stream =     document.WordDocument.MainDocumentPart.GetStream(FileMode.CreateNew);
                        // Create or overwrite the "myblob" blob with contents from a local file.
                        fs = (FileStream)document.InputStream;
                        string[] _filenames = fs.Name.Split("\\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        CloudBlockBlob blockBlob =   directory.GetBlockBlobReference(_filenames[_filenames.Length - 1]);
                        blockBlob.UploadFromStream(fs, fs.Length, AccessCondition.GenerateIfNoneMatchCondition("*"));
                        System.Threading.Thread.Sleep(1000);
                        fs.Close();                     
                        // One docx copy and one pdf copy
                        fsPdf = (FileStream)document.PDFInputStream;
                       
                       _filenames = fsPdf.Name.Split("\\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        System.Threading.CancellationToken token = new System.Threading.CancellationToken(false);
                        blockBlob.UploadFromStream(fsPdf , fsPdf.Length, AccessCondition.GenerateIfNoneMatchCondition("*"));
                        fsPdf.Close();
                        System.Threading.Thread.Sleep(1000);
                                           
                         Environment.SetEnvironmentVariable("translationDuration", "Uploaded");
                         // the above code will only work if the configuration on Azure portal is perfect

                         if (File.Exists(filePath))
                         {
                             System.IO.File.Delete(filePath);
                                
                         }

                         System.Threading.Thread.Sleep(1000);
                     
                    }

                }

            }
            catch (Exception excp)
            {
                System.Diagnostics.Trace.TraceError(excp.Message + "========" + Environment.NewLine + excp.InnerException.Source);
                Environment.SetEnvironmentVariable("translationDuration", "TranslatedDocumentAlreaadyExit");
                System.Diagnostics.EventLog.WriteEntry("Translation File Upload Process" + this.ToString(), "Unable to Upload on Blob" + Environment.NewLine + excp.Message, System.Diagnostics.EventLogEntryType.Error);
                throw new Exception("Unable to Upload on Blob" + Environment.NewLine + excp.Message);

            }
            finally
            {

                fs.Close();
                fs.Dispose();
                fsPdf.Close();
                fsPdf.Dispose();
                     
            }


        }



        private Document LoadWordDocument(Document doc,string userName)
        {
            CloudStorageAccount storageAccount = null;
            CloudBlobDirectory dir = null;
            List<DocumentServerUri> lstDocuments = new List<DocumentServerUri>();
            string _blobcontainer_reference = null;
            string _blobblockreference = String.Empty;
            string[] server_urls = null;
            Document document_work = new Document();
            
            DocumentStructure doc_structure = new DocumentStructure();
            try
            {
                if (!String.IsNullOrEmpty(doc.DocumentName) || (!String.IsNullOrEmpty(doc.DocumentServerURL)))
                {
                    server_urls = doc.DocumentServerURL.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    if (server_urls.Length > 0)
                    {
                        _blobcontainer_reference = server_urls[server_urls.Length - 1];
                        _blobblockreference = GetBlockBlobReference(server_urls[1]);
                    }
                    storageAccount = CloudStorageAccount.Parse(doc.StorageConnectionPoint);

                    // Create the blob client.
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                    // Retrieve reference to a previously created container.
                    CloudBlobContainer container = blobClient.GetContainerReference(userName);

                    // Retrieve reference to a blob named "myblob.txt"
                    CloudBlockBlob blockBlob2 = container.GetBlockBlobReference(userName);
                    dir = container.GetDirectoryReference(userName);
                    // Create the container if it doesn't already exist.
                    // Go to the manage azure portal create a blob in blob container 
                    // Store the reference of blob in Database
                    // leave it to the team B
                    // Retrieve reference to a blob named "myblob".
                    // Loop over items within the container and output the length and URI.

                    
                    var stream = new MemoryStream();

                    string text = String.Empty;
                    CloudBlockBlob blob = dir.GetBlockBlobReference(doc.DocumentName);
                    blob.DownloadToStream(stream);
                    doc.StreamData = stream;

                    
                    // List<string> paragraphs = GetParagraphsFromDocument(ref doc);
                    // Replaced on structure because of needed change 
                    // Decorator Pattern 
                    doc_structure  = GetParagraphsFromDocument(ref doc);
                    document_work = doc;
                    document_work .DocumentStructure = doc_structure;
                    // the whole things will be moved around docuument structure now it will reduce the time from 70 secs for 100 pages to 30 secs
                 



                }
            }
            catch (Exception excp)
            {
                System.Diagnostics.Trace.TraceError(excp.Message + "========" + Environment.NewLine + excp.InnerException.Source);
                throw excp;    
            }
            return document_work;

        }


        private List<string> GetParagraphsFromDocument( bool include_childelements ,  ref Document doc)
        {
            List<string> paragraphs = new List<string>();
            List<string> runs = new List<string>();
            wp.Paragraph para = null;
            wp.Run run = null;
            Package package = null;
            string para_text = String.Empty;
            List<string> lstRuns = new List<string>();
            Guid rval;
            try
            {
                package = Package.Open(doc.StreamData, FileMode.Open, FileAccess.ReadWrite);
                string _text = String.Empty;
                // Write
                Paragrapah prgrh = null;
                Runner runner = null;
                List<Runner> runners = null;
                int pIndex = 0;
                List<Paragrapah> prgrphs = new List<Paragrapah>();
                using (pkg.WordprocessingDocument wordDocument =
             pkg.WordprocessingDocument.Open(package))
                {
                    // Add a main document part. 
                    pkg.MainDocumentPart mainPart = wordDocument.MainDocumentPart;
                    // Create the document structure and add some text.
                    wp.Body body = mainPart.Document.Body;
                    foreach (wp.Paragraph paragraph in body.Elements<wp.Paragraph>())
                    {
                        para_text = "";
                        runners = new List<Runner>();
                        foreach (coreformat.OpenXmlElement element in paragraph.ChildElements)
                        {
                             runner = new Runner();
                             rval = Guid.NewGuid();
                            _text = element.InnerText;
                            if (String.IsNullOrEmpty(_text))
                            {
                                _text = "ɷʘ"; 
                            }
                            runner.RunnerID = rval.ToString();
                            runner.OrignalRunText = element.InnerText;
                            runner.RunElements = element.ChildElements;
                            runners.Add(runner);
                            para_text = para_text + _text;

                        }
                     
                        prgrh = new Paragrapah();
                        prgrh.ParagraphID = Guid.NewGuid();
                        prgrh.ParagraphText = paragraph.InnerText;
                        prgrh.ParagraphIndex = pIndex;
                        prgrh.Runs = runners;
                        if (paragraph.ParagraphProperties != null)
                        {
                            prgrh.PPr = paragraph.ParagraphProperties.CloneNode(true);
                        }
                        prgrphs.Add(prgrh);
                        paragraphs.Add(para_text);

                        pIndex = pIndex + 1;
                    }
                    doc.PickParagraphs = prgrphs;
                    doc.Paragraphs = paragraphs;
                    doc.WordDocument = wordDocument;
                    doc.ClonedWordDocument = wordDocument;
                    doc.Package = package;
                 //     wordDocument.Close();
                // package.Close();
                }
            }

            catch (Exception excp)
            {
                System.Diagnostics.Trace.TraceError(excp.Message + "========" + Environment.NewLine + excp.InnerException.Source);

                // Send document error codes
            }
            finally
            {


            }

            return doc.Paragraphs;

        }


        private List<string> GetAllParagraphTextFromDocument(ref Document doc)
        {
            List<string> paragraphs = new List<string>();
            List<string> runs = new List<string>();
            wp.Paragraph para = null;
            wp.Run run = null;
            Package package = null;
            string para_text = String.Empty;
            List<string> lstRuns = new List<string>();
            Guid rval;
            try
            {
                package = Package.Open(doc.StreamData, FileMode.Open, FileAccess.ReadWrite);
                string _text = String.Empty;
                // Write
                Paragrapah prgrh = null;
                Runner runner = null;
                List<Runner> runners = null;
                int pIndex = 0;
                
                List<Paragrapah> prgrphs = new List<Paragrapah>();
                using (pkg.WordprocessingDocument wordDocument =
             pkg.WordprocessingDocument.Open(package))
                {
                    // Add a main document part. 
                    pkg.MainDocumentPart mainPart = wordDocument.MainDocumentPart;
                    // Create the document structure and add some text.
                     wp.Body body = mainPart.Document.Body;
                     foreach (coreformat.OpenXmlElement child in body.ChildElements)
                     {
                         if (child.LocalName == "p")
                         {

                         }
                         else if (child.LocalName == "t")
                         {

                         }
                     }
                     
                    doc.PickParagraphs = prgrphs;
                    doc.Paragraphs = paragraphs;
                    doc.WordDocument = wordDocument;
                  //  wordDocument.Close();
                   //package.Close();
                }
            }

            catch (Exception excp)
            {
                System.Diagnostics.Trace.TraceError(excp.Message + "========" + Environment.NewLine + excp.InnerException.Source);

                // Send document error codes
            }
            finally
            {


            }
            
            return doc.Paragraphs;

        }
        #region "Extract images from document"

        private List<ImageInDocument> ExtractImages(pkg.MainDocumentPart mainpart )
        {
           
            var e =   mainpart.ImageParts.GetEnumerator();
            int picNum = 0;
            MemoryStream fstream = null;
            ImageInDocument image_in_document = null;
            List<ImageInDocument> images = new List<ImageInDocument>();

            if (e != null)
            {
                while (e.MoveNext())
                {
                    image_in_document = new ImageInDocument();
                    image_in_document.ContentType = e.Current.ContentType;
                    picNum++;
                    pkg.ImagePart imagePart = e.Current;
                    Stream stream = imagePart.GetStream();
                    long length = stream.Length;
                    byte[] byteStream = new byte[length];
                    stream.Read(byteStream, 0, (int)length);
                    fstream = new MemoryStream(byteStream, true);
                    fstream.Write(byteStream, 0, (int)length);
                    image_in_document.MemoryObject = fstream;
                    if (e.Current.RootElement != null)
                    {
                        image_in_document.RootElementObject = e.Current.RootElement;
                    }

                    image_in_document.ImageIndex = picNum;
                    image_in_document.Uri = e.Current.Uri.ToString();
                    images.Add(image_in_document);
                }
            }
            return images;

        }

        #endregion


        #region "Create a whole Document Structure with Clone"


        #region recurrsive table grid cell


        #endregion
        public DocumentStructure CreateDocumentStucture(wp.Body body , wp.Document documentWord)
        {
            ElementObject node_element = null;
            List<ElementObject> elment_objects = new List<ElementObject>();
            Child child_element = null;
            List<wp.Paragraph> paragraphs = null;
            int childIndex = 0;
            List<Child> children = null;
            int pIndex = 0;
            int elmentIndex = 0;
            List<wp.Paragraph> prgrphs = null;
            bool _isHyperLink = false;
            bool _isRun = false ;
            bool _isBookMarkStart = false ;
            bool _isBookMarkEnd = false;
            bool _isProofError = false;
            string para_text = String.Empty;
            List<Child> morechildern = null; 
            
            string _text = String.Empty;
            Child anotherChild = null;
            int _recursivechildIndex = 0;
            DocumentStructure doc_structure = new DocumentStructure();
            doc_structure.DocumentCloned = documentWord.CloneNode(true);
            foreach (coreformat.OpenXmlElement element_generic in body.Elements<coreformat.OpenXmlElement>())
            {
                node_element = new ElementObject();
                node_element.ElementName = element_generic.LocalName;
                node_element.RealElementObjectCloned = element_generic.CloneNode(true);
                children = new List<Child>();
                if (element_generic.LocalName == "tbl")
                {
                    para_text = string.Empty;

                    //Load and clone all properties here to
                    node_element.IsTable = true;
                    node_element.ElementIndex = elmentIndex;
                    node_element.ElementInnerText = element_generic.InnerText;           
                    foreach (coreformat.OpenXmlElement child_elm in element_generic)
                    {
                        if (child_elm.LocalName == "tblPr")
                        {
                            child_element = new Child() { ChildIndex = childIndex, ChildText = child_elm.InnerText, IsHyperLink = false, IsRun = true };
                            child_element.ChildTableProperties = child_elm.CloneNode(true);
                        }
                        
                        else if (child_elm.LocalName == "tblGrid")
                        {
                            child_element = new Child() { ChildIndex = childIndex, ChildText = child_elm.InnerText, IsHyperLink = false, IsRun = true };
                            child_element.TableGridClone = child_elm.CloneNode(true);
                        }
                        else if (child_elm.LocalName == "tr")
                        {

                            child_element = new Child() { ChildIndex = childIndex, ChildText = child_elm.InnerText, IsHyperLink = false, IsRun = true };
                            morechildern = new List<Child>();
                            foreach (coreformat.OpenXmlElement child_child in child_elm)
                            {
                                anotherChild = new Child();
                                anotherChild.ChildName = child_child.LocalName;
                                if (child_child.LocalName == "trPr")
                                {
                                    anotherChild.TableRowProperties = child_child.CloneNode(true);
                                    
                                }
                                else if (child_child.LocalName == "tc")
                                {
                                     anotherChild.TableCellClone = child_child.CloneNode(true);

                                     var paras = from cell_para in child_child.ChildElements
                                                 where cell_para.LocalName == "p"
                                                 select cell_para;
                                             foreach (var prgrph in paras)
                                             {
                                                    para_text = String.Empty;
                                                    node_element.ElementName = prgrph.LocalName;
                                                    node_element.ElementIndex = elmentIndex;
                                                    node_element.IsParagraph = true;
                                                    node_element.IsTable = false;
                                                    para_text = String.Empty;
                                                    // change1
                                                    foreach (coreformat.OpenXmlElement element in prgrph.ChildElements)
                                                    {
                                                        child_element = new Child();
                                                        _text = element.InnerText;
                                                        if (!String.IsNullOrEmpty(_text) && element.LocalName == "r")
                                                        {
                                                            _text = _text.Replace(_text, "ɷʘ" + " " + _text);
                                                        }
                                                        else if (String.IsNullOrEmpty(_text) && element.LocalName == "r")
                                                        {
                                                            if (element.ChildElements.Count > 0)
                                                            {
                                                                var drawing = from run_children in element.ChildElements
                                                                              where run_children.LocalName == "drawing" && run_children.ChildElements.Count > 0
                                                                              select run_children;

                                                                if (drawing.Count() > 0)
                                                                {
                                                                    _text = "ɷʘ" + "₹₹₹₹";
                                                                }
                                                            }
                                                        }
                                                        else if (!String.IsNullOrEmpty(_text) && element.LocalName == "hyperlink")
                                                        {
                                                            var fieldcharacters = from fieldcharacter in element.ChildElements
                                                                                  where fieldcharacter.LocalName == "FieldChar"
                                                                                  select fieldcharacter;

                                                            // treat the above as a different notation text and on identification of notaction 
                                                            // create a Field character while creating new paragraphs 
                                                            _text = _text.Replace(_text, "₩₩" + " " + _text);
                                                        }
                                                        else if (element.LocalName == "bookmarkStart")
                                                        {

                                                            if (!String.IsNullOrEmpty(_text))
                                                            {
                                                                _text = _text.Replace(_text, "₣₣" + " " + _text);
                                                            }
                                                            else
                                                            {
                                                                _text = "₣₣";
                                                            }


                                                        }
                                                        else if (element.LocalName == "bookmarkEnd")
                                                        {
                                                            if (!String.IsNullOrEmpty(_text))
                                                            {
                                                                _text = _text.Replace(_text, "₳₳" + " " + _text);
                                                            }
                                                            else
                                                            {
                                                                _text = "₳₳";
                                                            }
                                                        }
                                                        else if (element.LocalName == "proofErr")
                                                        {
                                                            if (!String.IsNullOrEmpty(_text))
                                                            {
                                                                _text = _text.Replace(_text, "₦" + " " + _text);
                                                            }
                                                            else
                                                            {
                                                                _text = "₦";
                                                            }

                                                        }

                                                        child_element.ChildText = element.InnerText;
                                                        children.Add(child_element);
                                                        para_text = para_text + _text;
                                                        node_element.Childrens = children;
                   
                                                    }
                                                    node_element.ElementIndex = elmentIndex;
                                                    node_element.ElementInnerText = para_text;
                                                     // chnage1
                                             


                                     }

                                    
                                 }
                                morechildern.Add(anotherChild);
                               }
                               child_element.RecurssiveChildren = morechildern;
                        }



                        childIndex++;
                        children.Add(child_element);
                        // only binary search for tablecells
                    }
                 
                    node_element.ElementInnerText =  element_generic.InnerText;           
                 
                    // so here element index is regardless of the  object
                    // it will tell you where to insert the the table or image 
                    // recurrsive call will be made here for table a
                    node_element.Childrens = children;
                }
                    // Architect chnage needed but the support will keep on going for previous version 
                else if (element_generic.LocalName == "p")
                {
                    para_text = String.Empty;
                    node_element.ElementName = element_generic.LocalName;
                    node_element.ElementIndex = elmentIndex;
                    node_element.IsParagraph = true;
                    node_element.IsTable = false;
                    para_text = String.Empty;
                    // change1
                    foreach (coreformat.OpenXmlElement element in element_generic.ChildElements)
                    {
                        child_element = new Child();
                        _text = element.InnerText;
                        if (!String.IsNullOrEmpty(_text) && element.LocalName == "r")
                        {
                            _text = _text.Replace(_text, "ɷʘ" + " " + _text);
                        }
                        else if (String.IsNullOrEmpty(_text) && element.LocalName == "r")
                        {
                            if (element.ChildElements.Count > 0)
                            {
                                var drawing = from run_children in element.ChildElements
                                              where run_children.LocalName == "drawing" && run_children.ChildElements.Count > 0
                                              select run_children;

                                if (drawing.Count() > 0)
                                {
                                    _text = "ɷʘ" + "₹₹₹₹";
                                }
                            }
                        }
                        else if (!String.IsNullOrEmpty(_text) && element.LocalName == "hyperlink")
                        {
                            var fieldcharacters = from fieldcharacter in element.ChildElements
                                                  where fieldcharacter.LocalName == "FieldChar"
                                                  select fieldcharacter;

                            // treat the above as a different notation text and on identification of notaction 
                            // create a Field character while creating new paragraphs 
                            _text = _text.Replace(_text, "₩₩" + " " + _text);
                        }
                        else if (element.LocalName == "bookmarkStart")
                        {

                            if (!String.IsNullOrEmpty(_text))
                            {
                                _text = _text.Replace(_text, "₣₣" + " " + _text);
                            }
                            else
                            {
                                _text = "₣₣";
                            }


                        }
                        else if (element.LocalName == "bookmarkEnd")
                        {
                            if (!String.IsNullOrEmpty(_text))
                            {
                                _text = _text.Replace(_text, "₳₳" + " " + _text);
                            }
                            else
                            {
                                _text = "₳₳";
                            }
                        }
                        else if (element.LocalName == "proofErr")
                        {
                            if (!String.IsNullOrEmpty(_text))
                            {
                                _text = _text.Replace(_text, "₦" + " " + _text);
                            }
                            else
                            {
                                _text = "₦";
                            }

                        }

                        child_element.ChildText = element.InnerText;
                        children.Add(child_element);
                        para_text = para_text + _text;
                        node_element.Childrens = children;
                   
                    }
                    node_element.ElementIndex = elmentIndex;
                    node_element.ElementInnerText = para_text;
                     // chnage1


                }
                
                elment_objects.Add(node_element);
                elmentIndex++;
                /** Look for only content in word tables ***/
            }
            doc_structure.DocumentCloned = documentWord;
            doc_structure.Elements = elment_objects;
            

            return doc_structure;
        }
        #endregion


        #region "Create Header Parts"
        public  List<pkg.HeaderPart> GenerateHeaderParts( wp.Document doc )
        {
            IEnumerable<pkg.HeaderPart> headerparts =  doc.MainDocumentPart.HeaderParts;

            return headerparts.ToList<pkg.HeaderPart>();  
        
        }

        #endregion

        public List<pkg.ImagePart> GenerateImageParts(wp.Document doc)
        {
           IEnumerable<pkg.ImagePart> imageparts = doc.MainDocumentPart.ImageParts;
           return imageparts.ToList<pkg.ImagePart>();   
        
        }

        private DocumentStructure GetParagraphsFromDocument(ref Document doc)
        {

            List<string> paragraphs = new List<string>();
            List<string> runs = new List<string>();
            wp.Paragraph para = null;

            wp.Run run = null;
            Package package = null;
            string para_text = String.Empty;
            List<string> lstRuns = new List<string>();
            Guid rval;
            DocumentStructure doc_structure = null;
           
            try
            {
                package = Package.Open(doc.StreamData, FileMode.Open, FileAccess.ReadWrite);
                string _text = String.Empty;
                // Write
                Paragrapah prgrh = null;
                Runner runner = null;
                List<Runner> runners = null;
                ElementObject node_element = null;
                List<ElementObject> elment_objects = new List<ElementObject>();
                Child child_element = null;
                int childIndex = 0;
                List<ImageInDocument> images_in_document = null; 
                List<Child> children = new List<Child>();
                coreformat.OpenXmlElement clonedDoc = null;
                int pIndex = 0;
                int elmentIndex = 0;
                List<Paragrapah> prgrphs = new List<Paragrapah>();
                pkg.WordprocessingDocument wordDocument = pkg.WordprocessingDocument.Open(package);
                
                    // Add a main document part. 
                    // Create the document structure and add some text.
                    images_in_document = ExtractImages(wordDocument.MainDocumentPart);
                     wp.Document docOnly  =   wordDocument.MainDocumentPart.Document;
                    wp.Body body = docOnly.Body;
                    doc_structure = CreateDocumentStucture(body , docOnly  );
                    /*** Load only tables from documment structure ****/

                    if (images_in_document.Count > 0)
                    {
                        doc_structure.Images = images_in_document;
                    }
                   
                    doc.DocumentStructure = doc_structure;

                    // it will break up the download cost from 0.50 $ to 0.25
                    // remove the code below 
                    // A well articulated structure the  so the below my own logic is better for beta relased 

                    // the above logic will only mark the elment name ,type and innertext and index 
                    // the below code will be passed to method  
                    // so leave this as it is 
                    // only get the table text from it

                    doc.WordDocument = wordDocument;
                    doc.ClonedWordDocument = wordDocument;
                    doc.Package = package;
                    doc_structure.HeaderParts = GenerateHeaderParts(doc.ClonedWordDocument.MainDocumentPart.Document);
                    doc_structure.ImageParts = GenerateImageParts(doc.ClonedWordDocument.MainDocumentPart.Document);
                      
               

            }

            catch (Exception excp)
            {
                System.Diagnostics.Trace.TraceError(excp.Message + "========" + Environment.NewLine + excp.InnerException.Source);
                 
                // Send document error codes
            }
            finally
            {


            }
           
            return doc_structure;

        }

        #region "Load Paraagraph along with Table Index "

        #endregion



        private List<StringBuilder> TranslateDocumentText(Document doc )
        {
            
            List<StringBuilder> lstparas = new List<StringBuilder>();

            string headerValue;
            string _translated = String.Empty;
            //Get Client Id and Client Secret from https://datamarket.azure.com/developer/applications/
            //Refer obtaining AccessToken (http://msdn.microsoft.com/en-us/library/hh454950.aspx) 
            int total_characters_needed = 0;
            foreach (ElementObject element in doc.DocumentStructure.Elements)
            {
                if(!String.IsNullOrEmpty(element.ElementInnerText)) 
                total_characters_needed = total_characters_needed + element.ElementInnerText.ToCharArray().Count<char>();
            }

            List<TranslationAccount> trans_authentication = DocumentServerUtilityClass.GetTranslationAccount(total_characters_needed,_connector);
            foreach (TranslationAccount tran_authentication in trans_authentication)
            {
                try
                {
                    lstparas = ProcessTranslationRequest(tran_authentication, doc);
                    if (lstparas.Count > 0)
                    {
                       break;
                    }
             
                    // Pass a maximum of 5 levels if one failed try other token 
                }
                catch (Exception excp)
                {
                    Environment.SetEnvironmentVariable("translationDuration", "TransactionRequestFailed");
                    lstparas = null;
                }
            }

            return lstparas;
        }


        #region "Process Translation Multiple Translation Attemps"
          private List<StringBuilder> ProcessTranslationRequest(TranslationAccount tran_authentication  ,  Document document  ) 
          {
              AdmAuthentication admAuth = null;
              List<StringBuilder> localParagraphs = new List<StringBuilder>();
              string headerValue;
              string _translated = String.Empty;

               AdmAccessToken admToken;

               /** Attempting to get translations done if one failed try go for next , if any one passes break out from loop **/
                   admAuth = new AdmAuthentication(tran_authentication.SecretKey, tran_authentication.ClientID);
                   // if this failed try next 


                   try
                   {
                       admToken = admAuth.GetAccessToken();
                       DateTime tokenReceived = DateTime.Now;
                       // Create a header with the access_token property of the returned token
                       headerValue = "Bearer " + admToken.access_token;
                       document.AuthenticationToken = headerValue;
                       localParagraphs = TranslateMethod(document);
                       DocumentServerUtilityClass.UpdateCharacterCount(localParagraphs, tran_authentication, _connector);
                   }

                   catch (System.Net.Http.HttpRequestException excp)
                   {
                       Environment.SetEnvironmentVariable("translationDuration", "TransactionRequestFailed ");
                       // mother fuckers trying to be smart injecting code
                       localParagraphs = null;
                     
                   }
                   catch (Exception ex)
                   {
                       Environment.SetEnvironmentVariable("translationDuration", "TransactionRequestFailed");
                             // mother fuckers trying to be smart injecting code
                 
                       localParagraphs = null;
                   }
                   finally
                   {

                   }
               
              return localParagraphs; 
          }

        #endregion

        public static List<int> AllIndexesOf(string str, string value)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentException("the string to find may not be empty", "value");
            List<int> indexes = new List<int>();
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }

        //
        private void InsertBackAllIndexes(List<DocumentTemplate> doctemplates , List<TempParagraph> temps)
        {

            //  get the indexes for special token and insert them back to Document template paragraphs to create new runs 
            // should be done by tonight 



        }

       // So need to send 2 times for one valid translation 
        // so once the position returned marked them and insert them back to create valid runs and properties
        
        private string GetURLEncodedTextFromParagraphs(List<string> paragraphs)
        {
            StringBuilder strbuider = new StringBuilder();
            List<int> all_indexes = null;
            TempParagraph temp = null;
            List<TempParagraph> temps = new List<TempParagraph>();
            foreach (string para in paragraphs)
            {
                 //all_indexes = new List<int>();
                 //all_indexes =  AllIndexesOf(para , "ɷʘ");
                 //string orignal_para = para.Replace("ɷʘ", " ");
                // temp = new TempParagraph();
                 //temp.IndexersForRuns = all_indexes;
                 //all_indexes = AllIndexesOf(para, "₩₩");
                 //orignal_para = para.Replace("₩₩", " ");
                 //temp.indexersForHyperLinks = all_indexes;
                 strbuider.Append(Environment.NewLine + para.Trim());
                 //temps.Add(temp);

            }
            string encoded = HttpUtility.UrlEncode (strbuider.ToString());
            return encoded;

        }



        private List<string> SendTranslationRequest(List<string> paragraphs, string source_culture, string authenticationToken, string destinationCulture)
        {
            List<string> translated_paragraphs = new List<string>();
            string uri = "http://api.microsofttranslator.com/v2/Http.svc/Translate?text=" + GetURLEncodedTextFromParagraphs(paragraphs) + "&from=" + source_culture + "&to=" + destinationCulture;

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Headers.Add("Authorization", authenticationToken);
            WebResponse response = null;
            try
            {
                httpWebRequest.Timeout = 1000000000;
                response = httpWebRequest.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {
                    System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(Type.GetType("System.String"));
                    string translation = (string)dcs.ReadObject(stream);
                    translated_paragraphs = translation.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList<string>();
                }
            }
            catch (HttpException excp)
            {
                
            }
            catch (WebException excp)
            {
            }
            finally
            {
                if (response != null)
                {

                    response.Close();
                    response = null;
                }
            }
            return translated_paragraphs;

        }

        #region "Find Paragraph Index target to cut and merge replaced"
        private void GenerateNewParagraphsFromCuttedParagraph(List<Cutter> cutters, ref Document doc)
        {

            List<Sentence> sentences = null;

            foreach (Cutter cutter in cutters)
            {
                sentences = cutter.cutted_paragraphs;
                var paras_temp = from sente in sentences
                                 select sente.SentenialsSantes;
                doc.Paragraphs.RemoveAt(cutter.ParagraphIndex);
                doc.Paragraphs.InsertRange(cutter.ParagraphIndex, paras_temp);
            }

            // return        exact_matches.ToList<DocumentTemplate>();                            

        }

        #endregion

        #region "Paragraph Threads Calculator"
        #region "send no additional characters"
        
        // create new runs on the basis of length divsions
        private List<coreformat.OpenXmlElement> CreateNewChildElementsFormSourceDocument(List<coreformat.OpenXmlElement> sourceElements, string paragraphtext)
        {

            List<coreformat.OpenXmlElement> lstopenxmlements = new List<coreformat.OpenXmlElement>();
            List<string> run_texts = new List<string>();
            var runs_hyperlinks = from ru_hyper in sourceElements 
                                  where ru_hyper.LocalName =="r" ||  ru_hyper.LocalName == "hyperlink"
                                  select ru_hyper;
            
            int num_runs_hyperlinks = paragraphtext.Length / runs_hyperlinks.Count<coreformat.OpenXmlElement>();
            int  increased_by = 0;
            char[] run_characters  =paragraphtext.ToCharArray();
            // Splitting up and creating new charcters is based upon especial charcters 

            // long term solution

            return lstopenxmlements;
        }


        private wp.Paragraph[] CreateNewParagraphs(List<Paragrapah> paragraphas , DocumentLanguageCulture culture)
        {
            wp.Paragraph paragraph = null;
            wp.RunFonts run_fonts = null;
            wp.BookmarkStart bmStart = null;
            wp.BookmarkEnd bmEnd = null;
            coreformat.OpenXmlElement pProps = null;
            wp.ParagraphProperties pPr = null;
            wp.Run run = null;
            List<coreformat.OpenXmlElement> nRuns = null;
            List<wp.Hyperlink> hLinks = null;
            List<wp.Paragraph> paragraphs = new List<wp.Paragraph>();
            wp.Languages l = null;
            wp.Hyperlink hlink = null;
            string[] links = null;
            List<Runner> runs = null;
            wp.Justification justification = null;
            int runs_count = 0;
            try
            {

                foreach (Paragrapah paragrapha in paragraphas)
                {
                    paragraph = new wp.Paragraph();
                    nRuns = new List<coreformat.OpenXmlElement>();
                    runs = paragrapha.Runs;
                    pProps = paragrapha.PPr;
                    if (pProps != null)
                    {
                        paragraph.Append(pProps);
                        if (paragraph.ParagraphProperties != null)
                        {
                            if (paragraph.ParagraphProperties.Justification != null)
                            {
                                justification = paragraph.ParagraphProperties.Justification;
                                paragraph.ParagraphProperties.RemoveChild<wp.Justification>(justification);
                                justification = new wp.Justification() { Val = DocumentServerUtilityClass.GetJustification(culture.LanguageID) };
                            }
                            else
                            {
                                justification = new wp.Justification() { Val = DocumentServerUtilityClass.GetJustification(culture.LanguageID) };
                                paragraph.ParagraphProperties.AppendChild(justification);
                            }
                        }
                    }
                    // segregate child elements and dissemenate text
                    // Adjust the tokens out here
                    // split text on the basis of runs text and hyperlink texts
                    if (paragrapha.ChildElements.Count > 0)
                    {


                    }
                }
            }
            catch (Exception excp)
            {

            }
            finally
            {

            }
            return paragraphs.ToArray();
        }

        
        private Document  GetThreadCountForParagraphs(ref Document doc)
        {
            Document document = new Document();
            int number_of_threads = doc.PickParagraphs.Count  ;
            int number_of_paragraphs = doc.PickParagraphs.Count;
            // remaining
            document = doc;
            return document;

        }
        private List<Paragrapah> PerformTranslation( int number_of_threads ,Document doc)
        {
            // send here
            List<string> translated_paragraphs = new List<string>();
            System.Threading.Tasks.Parallel.ForEach(doc.PickParagraphs, document =>
            {
                // The more computational work you do here, the greater  
                // the speedup compared to a sequential foreach loop. in parallel requests 
                string translation = SendTranslationRequestForParagraph(document.ParagraphText, doc.SourceLang_Culture, doc.AuthenticationToken, doc.TranslateTo_Lang_Culture);
                document.ParagraphText = translation;
            } //close lambda expression
        );
            // create parrallel request here for the 4 parallel sets
            //System.Threading.Thread[] thread_doc
            var paragraphs   = doc.PickParagraphs.OrderBy<Paragrapah, int>(p => p.ParagraphIndex).ToList<Paragrapah>();
          
          
            //      MergeBackTheParagraphs(ref translated_paragraphs, doc.Cutters);
            return paragraphs;

        }
        private string SendTranslationRequestForParagraph(string paragraphText, string source_culture, string authenticationToken, string destinationCulture)
        {
            string translation = String.Empty;
            List<string> translated_paragraphs = new List<string>();
            string uri = "http://api.microsofttranslator.com/v2/Http.svc/Translate?text=" + GetURLEncodedTextFromParagraph(paragraphText) + "&from=" + source_culture + "&to=" + destinationCulture;
            
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Headers.Add("Authorization", authenticationToken);
            WebResponse response = null;
            try
            {
                httpWebRequest.Timeout = 1000000000;
                response = httpWebRequest.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {
                     System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(Type.GetType("System.String"));
                     translation = (string)dcs.ReadObject(stream);
                }
            }
            catch (HttpException excp)
            {
                
            }
            catch (WebException excp)
            {
                
            }
            finally
            {
                if (response != null)
                {

                    response.Close();
                    response = null;
                }
            }
            return translation;

        }
        private string GetURLEncodedTextFromParagraph(string paragraph)
        {
            string encoded = HttpUtility.UrlEncode(paragraph);
            return encoded;
        }

        #region "This function loads  Paragraphs from Table "
        
        
        private List<Paragrapah>  LoadParagraphsFromTable( wp.Table table )
        {
            List<Paragrapah> lstparagraphs = new List<Paragrapah>();
            Paragrapah paragraph = null;
      
            try
            {
                if(table.ChildElements.Count > 0)
                {
                    foreach(wp.TableRow row  in  table.Elements<wp.TableRow>() )
                    {
                        var cells = from cell in row.Elements<wp.TableCell>()
                                    select cell; 
                         foreach( var cell in cells)
                         {
                             var paragraphs = from para in cell.Elements<wp.Paragraph>()
                                             select para;
                         
                              
                             foreach(wp.Paragraph prgrph in paragraphs )
                             {
                                 paragraph = new Paragrapah();
                                 lstparagraphs.Add(paragraph);


                             }
                                             


                         }



                    }


                }
            }
            catch(Exception exccp)
            {



            }
            finally
            {



            }

            return lstparagraphs;
        }

        #endregion 


        private List<Paragrapah> TranslateMethod(List<Paragrapah> paragraphs , ref Document doc)
        {
            List<Document> documents = new List<Document>();
            List<Paragrapah> translated_paragraphs = new List<Paragrapah>();
                // this will quickly resolve issue for big encoder like 200 paragraphs
            Document document  = GetThreadCountForParagraphs(ref doc);
            translated_paragraphs = PerformTranslation(document.PickParagraphs.Count , document);
            

            
            return translated_paragraphs;
        }
     



        #endregion

        private List<DocumentTemplate> GetCounterThreadsForParagraph(ref Document doc)
        {
                  
            DocumentTemplate documentTemplate = null;
            List<DocumentTemplate> documentTemplates = new List<DocumentTemplate>();
            
            // Replace para by Element object 
            foreach (ElementObject elementObj in doc.DocumentStructure.Elements)
            {
                if (!String.IsNullOrEmpty(elementObj.ElementInnerText))
                {
                    documentTemplate = new DocumentTemplate();
                    documentTemplate.OriginalParagraph = elementObj.ElementInnerText;
                    documentTemplate.ParagraphIndex = elementObj.ElementIndex;
                    documentTemplate.CurrentElement = elementObj;
                    documentTemplates.Add(documentTemplate);
                }
             }
            // remaining 
            return documentTemplates;
        }


        #region "Remove temp index after merge"

        private void MergeBackTheParagraphs(ref List<string> translatedParagraph, List<Cutter> cutters)
        {
            string findcuttedparagraph = null;
            List<Sentence> sentences = null;
            string _senetial = String.Empty;
            int _newseedIndex = 0;
            string _mergedparagraph = String.Empty;

            try
            {
                foreach (Cutter cutter in cutters)
                {
                    _senetial = String.Empty;
                    // problem 

                    findcuttedparagraph = translatedParagraph[cutter.ParagraphIndex];
                    if (!String.IsNullOrEmpty(findcuttedparagraph))
                    {
                        sentences = cutter.cutted_paragraphs;
                        _newseedIndex = cutter.ParagraphIndex;
                        foreach (Sentence sentence in sentences)
                        {
                            _senetial = _senetial + translatedParagraph[_newseedIndex];
                            _newseedIndex = _newseedIndex + 1;
                        }
                        _mergedparagraph = _senetial;

                        translatedParagraph[cutter.ParagraphIndex] = _mergedparagraph;
                     }
                }

                RemoveIndexes(ref translatedParagraph, cutters);

            }
            catch (Exception excp)
            {

            }
            finally
            {

            }


        }
        
        private void RemoveIndexes(ref List<string> translated, List<Cutter> cutters)
        {
            
             int _newseedIndex = 0;
              
            foreach(Cutter cutter in cutters)
            {
                _newseedIndex = cutter.ParagraphIndex;
                _newseedIndex = _newseedIndex + 1;
              for (int i = _newseedIndex ; i < (_newseedIndex + cutter.cutted_paragraphs.Count- 1); i++)
              {
                  translated.RemoveAt(i);
              }

            }
        }


        #endregion
        private List<StringBuilder> PartialTrans( List<DocumentTemplate> documentTemplates, Document doc)
        {
            List<string> translated_paragraphs = new List<string>();
            List<StringBuilder> builder_translated_paragraphs = new List<StringBuilder>();
            string translated = String.Empty;
            string _checkedfornull = string.Empty;
            List<int> all_indexes = new List<int>();
            TempParagraph temp = null;
            List<TempParagraph> temps = new List<TempParagraph>();
            int indexer = 0;
            foreach (DocumentTemplate strbuilder in documentTemplates)
            {
                // here is a problem i need to pass indexes at document.Paragraph Level

                if (!String.IsNullOrEmpty(strbuilder.OriginalParagraph))
                {
                    all_indexes = new List<int>();
                    all_indexes = AllIndexesOf(strbuilder.OriginalParagraph.ToString(), "ɷʘ");
                    temp = new TempParagraph();
                    temp.ParagraphIndex = indexer;
                    temp.ParagraphText = strbuilder.OriginalParagraph.ToString().ToString();
                    temp.IndexersForRuns = all_indexes;
                    all_indexes = AllIndexesOf(strbuilder.OriginalParagraph.ToString().ToString(), "₩₩");
                    temp.indexersForHyperLinks = all_indexes;
                    all_indexes = AllIndexesOf(strbuilder.OriginalParagraph.ToString().ToString(), "₣₣");
                    temp.indexersForBookMarkEnd = all_indexes;
                    all_indexes = AllIndexesOf(strbuilder.OriginalParagraph.ToString().ToString(), "₦");
                    temp.indexersForProofErrors = all_indexes;
                    all_indexes = AllIndexesOf(strbuilder.OriginalParagraph.ToString().ToString(), "₳₳");
                    temp.indexersForBookMarkStart = all_indexes;
                    temps.Add(temp);
                    indexer++;
                }
            }
            doc.Temps = temps;
            
            FindReplaceCharctersFromTemplate(ref documentTemplates);

            System.Threading.Tasks.Parallel.ForEach(documentTemplates, document =>
            {
                // The more computational work you do here, the greater  
                // the speedup compared to a sequential foreach loop. in parallel requests 
                  document.TranslatedParagraph  = SendTranslationRequestForParagraph(document.OriginalParagraph, doc.SourceLang_Culture, doc.AuthenticationToken, doc.TranslateTo_Lang_Culture);
                  document.CurrentElement.TranslatedElementText = document.TranslatedParagraph;
            } //close lambda expression
        );
            // create parrallel request here for the 4 parallel sets
            //System.Threading.Thread[] thread_doc
            documentTemplates = documentTemplates.OrderBy<DocumentTemplate, int>(p => p.ParagraphIndex).ToList<DocumentTemplate>();
            var trans_tokens = from doctemplate in documentTemplates
                               where doctemplate.TranslatedParagraph != _checkedfornull  || doctemplate.TranslatedParagraph != " "
                               select doctemplate.TranslatedParagraph ;

            // Find the index from the cutter again    

                foreach (string para in  trans_tokens)
                {
                    //translated_paragraphs.Add(para);
                    if(!String.IsNullOrEmpty(para) || para != "")
                    builder_translated_paragraphs.Add(new StringBuilder(para,5000));
                }
            

            //      MergeBackTheParagraphs(ref translated_paragraphs, doc.Cutters);

            // collect all indexer postions

            return builder_translated_paragraphs;

        }

        #endregion


        #region "Cut Off Paragraphs "

        private List<Sentence> GetSentenceFromParagraph(string paragraph, Guid ParaID)
        {
            string[] sentences = paragraph.Split(".".ToCharArray());
            List<Sentence> sentenialsantes = new List<Sentence>();
            List<Sentence> sentenialsente = new List<Sentence>();
            Sentence sentence = null;
            List<Sentence> sentencecomplex = null;
            int i = 1;
            foreach (string sente in sentences)
            {
                sentence = new Sentence() { ParagraphID = ParaID, SentenceSequenceIndex = i, SentenialsSantes = sente + "." };
                sentenialsantes.Add(sentence);
                i++;
            }
            return sentenialsantes;
        }

        // we need to cut the paragraph 
        private List<Cutter> CuttOffParagraphs(List<Paragrapah> real_paragraphs)
        {
            List<Cutter> cutters = new List<Cutter>();
            Cutter cutter = null;

            var maxs = from real in real_paragraphs
                       where real.ParagraphText.Split(" ".ToCharArray()).Count<string>() > 50
                       select real;
            // take a judgement of 50r

            foreach (var max in maxs)
            {
                cutter = new Cutter();
                cutter.ParagraphID = max.ParagraphID;
                cutter.ParagraphIndex = max.ParagraphIndex;
                cutter.orignal_paragraph = max.ParagraphText;
                cutter.cutted_paragraphs = GetSentenceFromParagraph(max.ParagraphText, cutter.ParagraphID);
                cutters.Add(cutter);
            }

            return cutters;
        }

        #endregion
        #region "Extract Table Text from Document Structure and insert them in document Templates with position index"
        private List<DocumentTemplate> ExtractTextDataFromTables(DocumentStructure structure)
        {



            // Implement this by extracting Table Data from Document Structure and Assign the elment index to document Template   
            return new List<DocumentTemplate>();

        }


        
         
        #endregion


        // Chnaged architecture for sclability and optimization reasons on 02/20/2014
        private void FindReplaceCharctersFromTemplate(ref List<DocumentTemplate> templates )
        {
            foreach (DocumentTemplate documentTemplate in templates)
            {
                    if (documentTemplate.OriginalParagraph.Contains("ɷʘ"))
                    {
                          documentTemplate.OriginalParagraph =  documentTemplate.OriginalParagraph.Replace("ɷʘ", " ");
                    }
                    else if (documentTemplate.OriginalParagraph.Contains("₩₩"))
                    {
                        documentTemplate.OriginalParagraph  = documentTemplate.OriginalParagraph.Replace("₩₩", " ");
                    }

                    else if (documentTemplate.OriginalParagraph.Contains("₣₣"))
                    {
                        documentTemplate.OriginalParagraph = documentTemplate.OriginalParagraph.Replace("₣₣", " ");
                    }

                    else if (documentTemplate.OriginalParagraph.Contains("₦"))
                    {
                        documentTemplate.OriginalParagraph = documentTemplate.OriginalParagraph.Replace("₦", " ");
                    }

                    else if (documentTemplate.OriginalParagraph.Contains("₳₳"))
                    {
                        documentTemplate.OriginalParagraph = documentTemplate.OriginalParagraph.Replace("₳₳", " ");
                    }
             
            }
          
        }
        // Wrong code been take over lets work on right one 
        private List<StringBuilder> TranslateMethod(Document doc)
        {
            List<Document> documents = new List<Document>();
            List<StringBuilder> builder_translated_paragraphs = new List<StringBuilder>();
            List<string> translated_paragraphs = new List<string>();
           
            List<string> paragraphs = null;
            List<DocumentTemplate> documentTemplates = null;
            TempParagraph temp = null;
            List<TempParagraph> temps = new List<TempParagraph>();  
           
            // this will quickly resolve issue for big encoder like 200 paragraphs
                documentTemplates = GetCounterThreadsForParagraph(ref doc);
                builder_translated_paragraphs = PartialTrans(documentTemplates, doc);
             // removing condition for paras         
             // Record indexers position send back again with no
           
            // Call translation again
            // Here When i am replacing charaacters and sending back then it comes out to be less so i have to insert 
            // more paragraphs like with free "" "" this 
            // so it comes 
            temps = doc.Temps.FindAll(p => p.ParagraphText != "" || p.ParagraphText != " ");
            Environment.SetEnvironmentVariable("tranlsationDuration", "SettingUpNewTranslatedDocument");
            for (int index = 0; index < temps.ToArray().Length; index++ )
            {
                  temp = (TempParagraph) temps[index];
                  
                  if (temp.IndexersForRuns.Count > 0)
                  {

                      try
                      {
                          foreach (int pos in temp.IndexersForRuns)
                          {
                              if (builder_translated_paragraphs[index].Length - 1 > pos && !String.IsNullOrEmpty(builder_translated_paragraphs[index].ToString()))
                              {
                                  builder_translated_paragraphs[index].Insert(pos, "ɷʘ");
                                  doc.DocumentStructure.Elements[index].TranslatedElementText.Insert(pos, "ɷʘ");
                              }

                          }

                          foreach (int posIbs in temp.indexersForBookMarkStart)
                          {
                              if (builder_translated_paragraphs[index].Length - 1 > posIbs && !String.IsNullOrEmpty(builder_translated_paragraphs[index].ToString()))
                              {
                                  builder_translated_paragraphs[index].Insert(posIbs, "₳₳");

      
                              }
                          }

                          foreach (int posI in temp.indexersForHyperLinks)
                          {
                              if (builder_translated_paragraphs[index].Length - 1 > posI && !String.IsNullOrEmpty(builder_translated_paragraphs[index].ToString()))
                              {
                                  builder_translated_paragraphs[index].Insert(posI, "₩₩");
                              }
                          }
                          // the translation comes back with less objects
                          foreach (int posIbe in temp.indexersForBookMarkEnd)
                          {
                              if (builder_translated_paragraphs[index].Length - 1 > posIbe && !String.IsNullOrEmpty(builder_translated_paragraphs[index].ToString()))
                              {
                                  builder_translated_paragraphs[index].Insert(posIbe, "₣₣");
                              }
                          }
                          foreach (int posIpErr in temp.indexersForHyperLinks)
                          {
                              if (builder_translated_paragraphs[index].Length - 1 > posIpErr && !String.IsNullOrEmpty(builder_translated_paragraphs[index].ToString()))
                              {
                                  builder_translated_paragraphs[index].Insert(posIpErr, "₦₦₦");
                              }
                          }

                      }
                      catch (Exception excp)
                      {

                      }

                      doc.DocumentStructure.Elements[index].TranslatedElementText = builder_translated_paragraphs[index].ToString();

                  }
            }


            Environment.SetEnvironmentVariable("tranlsationDuration", "CreatingDocument");
            
                       // Once returned should be able to good to go now
            return builder_translated_paragraphs;
        }


     bool disposed = false;
   public void Dispose()
   {
       this.Dispose(true);
       GC.Collect();
      
       GC.WaitForPendingFinalizers();

       GC.Collect();

       GC.WaitForPendingFinalizers();
        
     
      GC.RemoveMemoryPressure(200000);
   }
   protected virtual void Dispose(bool disposing)
   {
       if (disposing)
       {
       }
   }
         

   // Protected implementation of Dispose pattern. 

 }
}