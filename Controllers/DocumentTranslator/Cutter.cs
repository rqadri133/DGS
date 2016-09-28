using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DGS.Content.Controllers.DocumentTranslator
{
    public class Sentence
    {
        public int SentenceSequenceIndex
        {
            get;
            set;
        }
        
        public Guid ParagraphID
        {
            get;
            set;
        }

        public string SentenialsSantes
        {
            get;
            set;
        }
 
    }

    public class Cutter
    {
        public Guid ParagraphID
        {
            get;
            set;
        }

        public int ParagraphIndex
        {
            get;
            set;
        }
        
        public List<Sentence> cutted_paragraphs
        {
            get;
            set;
        }

        public string orignal_paragraph
        {
            get;
            set;
        }

    }
}