using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DGS.Content.Controllers.DocumentTranslator
{
    public class TempParagraph
    {

        public int ParagraphIndex
        {
            get;
            set;
        }

        public string ParagraphText
        {
            get;
            set;
        }

        public List<int> IndexersForRuns
        {
            get;
            set;
        }

        public List<int> indexersForHyperLinks
        {
            get;
            set;
        }

        public List<int> indexersForBookMarkEnd
        {
            get;
            set;
        }

        public List<int> indexersForBookMarkStart
        {
            get;
            set;
        }

        public List<int> indexersForProofErrors
        {
            get;
            set;
        }


    }
}