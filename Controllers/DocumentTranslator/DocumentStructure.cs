using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using wp = DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;

namespace DGS.Content.Controllers.DocumentTranslator
{

    public class ImageInDocument
    {

        public string ContentType
        {
            get;
            set;
        }



        public OpenXmlElement RootElementObject
        {
            get;
            set;
        }

        public string Uri
        {
            get;
            set;
        }

        public int ImageIndex
        {
            get;
            set;
        }

        public System.IO.MemoryStream MemoryObject
        {
            get;
            set;
        }

    }

     
    public class DocumentStructure
    {



        public List<DocumentFormat.OpenXml.Packaging.HeaderPart> HeaderParts
        {
            get;
            set;
        }

        public List<DocumentFormat.OpenXml.Packaging.ImagePart> ImageParts
        {
            get;
            set;
        }


        public List<ElementObject> Elements 
        {
            get;set;
        }
        public List<ImageInDocument> Images
        {
            get;
            set;
        }

        public  OpenXmlElement DocumentCloned
        {
            get;
            set;
        }

    }


    public class ElementObject
    {
         public int ElementIndex
        {
            get;
            set;
        }

         public OpenXmlElement RealElementObjectCloned
         {
             get;
             set;
         }

         public wp.Document ClonedDocument
         {
             get;
             set;
         }

         public bool IsParagraph
         {
             get;
             set;
         }


        public string ElementName
        {
            get;
            set;
        }

        public string ElementInnerText
        {
            get;
            set;
        }

        public string[] TransTokens
        {
            get;
            set;
        }
        public bool IsTable
        {
            get;
            set;
        }

        public List<Child> Childrens
        {
            get;
            set;
        }
        // 
        public List<OpenXmlElement> ElementChildrensInDocument
        {
            get;
            set;
        }

        public string ElementText
        {
            get;
            set;
        }


        public string TranslatedElementText
        {
            get;
            set;
        }



    }



    public interface IElementProcessor
    {
        ElementObject ProcessElement(ElementObject element); 
    }

    public class Child
    {
        public string ChildName
        {
            get;
            set;


        }

        public OpenXmlElement TableCellClone
        {
            get;
            set;
        }
        public bool HasParent
        {
            get;
            set;
        }
        public OpenXmlElement TableRowProperties
        {
            get;
            set;
        }



        public List<Child> RecurssiveChildren
        {
            get;
            set;
        }


        public OpenXmlElement ChildTableProperties
        {
            get;
            set;
        }


        public OpenXmlElement  TableGridClone
        {
            get;
            set;
        }

        public wp.Hyperlink HyperLinkElement
        {
            get;
            set;
        }

        public bool IsRun
        {
            get;
            set;
        }


        public bool IsBookMarkStart
        {
            get;
            set;
        }


        public bool IsBookMarkEnd
        {
            get;
            set;
        }

        public bool IsProofError
        {
            get;
            set;
        }

        public wp.Run RunElement
        {
            get;
            set;
        }


        public bool IsHyperLink
        {
            set;
            get;
        }

        public int ChildIndex
        {
            get;
            set;
        }

        public string ChildText
        {
            get;
            set;
        }

        
    }

    public class TableElementProcessor : IElementProcessor
    {
        public ElementObject  ProcessElement(ElementObject element)
        {
            return new ElementObject();
        }

    }


    public class ImageElementProcessor : IElementProcessor
    {
        public ElementObject ProcessElement(ElementObject element)
        {
            return new ElementObject();
        }

    }



}