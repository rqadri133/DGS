using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DGS.Models
{
    public class DocumentLanguageCulture
    {

        #region Constructors

        public DocumentLanguageCulture()
        {
        }
        #endregion

        #region Private Fields

        private string _LanguageID;
        private string _Culture_Country_Lang;
        private string _Ascii;
        private string _asciiTheme;
        private string _ComplexScript;
        private string _ComplexScriptTheme;
        private string _eastAsia;
        private string _eastAsiaTheme;
        private string _Typeface;
        private string _CharaterSet;
        private string _Panose;
        private string _PitchFamily;
        #endregion

        #region Public Properties

        public string LanguageID
        {
            get { return _LanguageID; }
            set { _LanguageID = value; }
        }
        public string Culture_Country_Lang
        {
            get { return _Culture_Country_Lang; }
            set { _Culture_Country_Lang = value; }
        }
        public string Ascii
        {
            get { return _Ascii; }
            set { _Ascii = value; }
        }
        public string asciiTheme
        {
            get { return _asciiTheme; }
            set { _asciiTheme = value; }
        }
        public string ComplexScript
        {
            get { return _ComplexScript; }
            set { _ComplexScript = value; }
        }
        public string ComplexScriptTheme
        {
            get { return _ComplexScriptTheme; }
            set { _ComplexScriptTheme = value; }
        }
        public string eastAsia
        {
            get { return _eastAsia; }
            set { _eastAsia = value; }
        }
        public string eastAsiaTheme
        {
            get { return _eastAsiaTheme; }
            set { _eastAsiaTheme = value; }
        }
        public string Typeface
        {
            get { return _Typeface; }
            set { _Typeface = value; }
        }
        public string CharaterSet
        {
            get { return _CharaterSet; }
            set { _CharaterSet = value; }
        }
        public string Panose
        {
            get { return _Panose; }
            set { _Panose = value; }
        }
        public string PitchFamily
        {
            get { return _PitchFamily; }
            set { _PitchFamily = value; }
        }
        #endregion
    }
    public class Document
    {
        //
        public System.Guid DocumentID { get; set; }
        public string DocumentName { get; set; }
        public string DocumentDescription { get; set; }
        public DocumentLanguageCulture LanguageCulture
        {
            get;
            set;
        }
        public string documentUser
        {
            get;
            set;
        }
        public List<string> Paragraphs
        {
            get;
            set;
        }

        public System.IO.Stream StreamData
        {
            get;
            set;

        }

        public System.IO.FileStream InputStream
        {

            get;
            set;
        }


        public System.IO.FileStream PDFInputStream
        {

            get;
            set;
        }

        public string TranslateTo_Lang_Culture
        {

            get;
            set;
        }

        public string SourceLang_Culture
        {

            get;
            set;
        }

        public string DocumentServerURL
        {
            get;
            set;
        }

        public string StorageConnectionPoint
        {
            get;
            set;

        }

        public System.IO.Packaging.Package Package
        {
            get;
            set;
        }


        public List<DGS.Content.Controllers.DocumentTranslator.TempParagraph> Temps
        {
            get;
            set;

        }


        public string ParagraphText
        {
            get;
            set;

        }

        public DocumentFormat.OpenXml.Packaging.WordprocessingDocument WordDocument
        {
            get;
            set;

        }


        public DocumentFormat.OpenXml.Packaging.WordprocessingDocument ClonedWordDocument
        {
            get;
            set;

        }

        public int ParagraphIndex
        {
            get;
            set;

        }

        public DGS.Content.Controllers.DocumentTranslator.DocumentStructure DocumentStructure
        {
            get;
            set;

        }

        public string AuthenticationToken
        {
            get;
            set;

        }

        public List<DGS.Models.Paragrapah> PickParagraphs
        {
            get;
            set;

        }

        public List<System.Text.StringBuilder> Paragraph_Builder
        {
            get;
            set;

        }


        public int ProcessTime
        {

            get;
            set;
        }
        // GET: /Document/

    }
}
