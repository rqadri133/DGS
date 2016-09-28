using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using wp = DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using pkg = DocumentFormat.OpenXml.Packaging;

namespace DGS.Models
{
    public class Paragrapah
    {
        public Guid ParagraphID
        {
            get;
            set;
        }
        public OpenXmlElement PPr
        {
            get;
            set;
        }

        public int ParagraphIndex
        {
            get;
            set;
        }

       

        public wp.ParagraphProperties PrProperties
        {
            get;
            set;
        }

        public string ParagraphText
        {
            get;
            set;
        }

        public List<Runner> Runs
        {
            get;
            set;
        }

        public OpenXmlElementList ChildElements
        {
            get;
            set;
        }

    }
}