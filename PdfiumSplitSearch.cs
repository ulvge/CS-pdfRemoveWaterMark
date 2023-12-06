using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfRemoveWaterMark
{
    class PdfiumSplitSearch
    {
        private string fileName;
        private List<IList<PdfRectangle>> wartMarkBounds;

        public PdfiumSplitSearch(string fileName)
        {
            this.fileName = fileName;
            PdfiumSplit(fileName);
        }
        private string PdfiumSplit(string fileName)
        {

            PdfReader reader = new PdfReader(fileName);
            PdfDocument document = new PdfDocument(reader);
            int pageCount = document.GetNumberOfPages();
            for (int i = 1; i < document.GetNumberOfPages() + 1; i++)
            {
                PdfPage page = document.GetPage(i);
            }
            return string.Empty;
        }
        private string PdfiumSearchWartermark(string fileName)
        {
            string warterMark = "Silergy Corp. Confidential-Prepared for Jovial";
            try
            {
                PdfDocument pdfDocumet = PdfDocument.Load(fileName);
                for (int i = 0; i < pdfDocumet.PageCount; i++)
                {
                    PdfMatches matches = pdfDocumet.Search(warterMark, true, true, i);
                    foreach (var match in matches.Items)
                    {
                        IList<PdfRectangle> textBounds = pdfViewer.Renderer.Document.GetTextBounds(match.TextSpan);
                        wartMarkBounds.Add(textBounds);
                    }
                    Console.WriteLine("matches:" + matches.Items.Count);
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return "";
        }
    }
}
