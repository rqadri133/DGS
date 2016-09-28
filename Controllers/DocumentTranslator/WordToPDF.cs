using System;

using System.Collections.Generic;

using System.Linq;

using System.Text;

using u = Microsoft.Office.Interop.Word;

namespace DGS.Content.Controllers.DocumentTranslator
{
    public class WordToPDF
    {
        public static bool ConvertToPDF(string filesource, string pdffile)
        {
            bool converted = false;
            u.Application wordApplication =  new  u.Application ();
            u.Document wordDocument = null;
            object paramSourceDocPath = filesource;
            object paramMissing = Type.Missing;
            string paramExportFilePath = pdffile;
            u.WdExportFormat paramExportFormat = u.WdExportFormat.wdExportFormatPDF;
            bool paramOpenAfterExport = false;
            u.WdExportOptimizeFor paramExportOptimizeFor = u.WdExportOptimizeFor.wdExportOptimizeForPrint;
            u.WdExportRange paramExportRange = u.WdExportRange.wdExportAllDocument;
            int paramStartPage = 0;
            int paramEndPage = 0;
            u.WdExportItem paramExportItem = u.WdExportItem.wdExportDocumentContent;
            bool paramIncludeDocProps = true;
            bool paramKeepIRM = true;
            u.WdExportCreateBookmarks paramCreateBookmarks = u.WdExportCreateBookmarks.wdExportCreateWordBookmarks;
            bool paramDocStructureTags = true;
            bool paramBitmapMissingFonts = true;

            bool paramUseISO19005_1 = false;
            try
            {

                // Open the source document.
                wordDocument = wordApplication.Documents.Open(ref paramSourceDocPath, ref paramMissing, ref paramMissing,

                    ref paramMissing, ref paramMissing, ref paramMissing,

                    ref paramMissing, ref paramMissing, ref paramMissing,

                    ref paramMissing, ref paramMissing, ref paramMissing,

                    ref paramMissing, ref paramMissing, ref paramMissing,

                    ref paramMissing);



                // Export it in the specified format.

                if (wordDocument != null)

                    wordDocument.ExportAsFixedFormat(paramExportFilePath,

                        paramExportFormat, paramOpenAfterExport,

                        paramExportOptimizeFor, paramExportRange, paramStartPage,

                        paramEndPage, paramExportItem, paramIncludeDocProps,

                        paramKeepIRM, paramCreateBookmarks, paramDocStructureTags,

                        paramBitmapMissingFonts, paramUseISO19005_1,

                        ref paramMissing);

                converted = true;
            }

            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.Message + "========" + Environment.NewLine + ex.InnerException.Source);
                        throw ex;

                converted = false;
                // Respond to the error

            }

            finally
            {

                // Close and release the Document object.

                if (wordDocument != null)
                {

                    wordDocument.Close(ref paramMissing, ref paramMissing,ref paramMissing);

                    wordDocument = null;

                }



                // Quit Word and release the ApplicationClass object.

                if (wordApplication != null)
                {

                    wordApplication.Quit(ref paramMissing, ref paramMissing,

                        ref paramMissing);

                    wordApplication = null;

                }



                GC.Collect();

                GC.WaitForPendingFinalizers();

                GC.Collect();

                GC.WaitForPendingFinalizers();
            }
            return converted;
        
        }
    }
}
