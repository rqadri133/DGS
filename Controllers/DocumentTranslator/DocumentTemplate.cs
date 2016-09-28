using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DGS.Content.Controllers.DocumentTranslator
{

    public class DocumentTemplate
    {
        public Guid TemplateID
        {
            get;
            set;
        }

        public int ParagraphIndex
        {
            get;
            set;
        }

        public int startIndex
        {
            get;
            set;

        }

        public int endIndex
        {
            get;
            set;
        }

        // the normal paragraphs get ret we need an index here to place back all paragrap
        public List<string> Paragraphs 
        {
            get;
            set;
        }

        // Change design as per Paragraph object and other element 
        public string OriginalParagraph
        {
            get;
            set;
        }

        public ElementObject CurrentElement
        {
            get;
            set;
        }
        public string TranslatedParagraph
        {
            get;
            set;
        }
    

        public List<string> OriginalParagraphs
        {
            get;
            set;
        }

       // the paragraphs with more words 
        public List<Cutter> Cutters
        {
            get;
            set;
        }
 

    }

}