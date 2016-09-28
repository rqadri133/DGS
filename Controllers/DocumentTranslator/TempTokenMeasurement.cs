using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using wp=DocumentFormat.OpenXml.Wordprocessing;
using pkg=DocumentFormat.OpenXml.Packaging;

using coreformat = DocumentFormat.OpenXml;

namespace DGS.Content.Controllers.DocumentTranslator
{
    public class TempTokenMeasurement
    {
        public List<string> tokens
        {
            get;
            set;
        }

        public wp.Styles StyleDefinationPart
        {
            get;
            set;
        }
            
            
        public List<wp.Run> CreatedRuns
        {
            get;
            set;
        }
        public List<coreformat.OpenXmlElement> RunsAll
        {
            get;
            set;

        }

        public int PargraphIndex
        {
           
            get;
            set;
        }
        public bool Unmatched
        {
            get;
            set;
        }
    
        public coreformat.OpenXmlElement Paragraph
        {
            get;
            set;

        }
    }
}