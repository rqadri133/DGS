using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using wp=DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using pkg=DocumentFormat.OpenXml.Packaging;

namespace DGS.Models
{
    public class Runner
    {
        public string RunnerID
        {
            get;
            set;
        }

        public string OrignalRunText
        {
            get;
            set;

        }

        // Store clone of child elements and gather them back to create a new translated paraggraph
        public OpenXmlElementList RunElements
        {
            get;
            set;
        }

        public string TranslatedRunText
        {
            get;
            set;
        }


        public bool IsHyperLink
        {
            get;
            set;
        }

        
    }
}