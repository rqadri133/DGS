using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DGS.Models;

namespace DGS.Content.Controllers.DocumentTranslator
{
    interface IDocumentTranslator : IDisposable
    {

        Document Translate(Document document , string userName);
        void CreateDocument(Document document);
        void CreateDocument(Document document, bool allElements);
        void CreateNewDocument(Document document);
    }
}
