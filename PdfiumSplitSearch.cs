using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
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
        //private List<IList<PdfRectangle>> wartMarkBounds;

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
                PdfReader pdfReader = new PdfReader(fileName);
                PdfDocument document = new PdfDocument(pdfReader, new PdfWriter("output.pdf"));
                for (int pageNum = 0; pageNum < document.GetNumberOfPages(); pageNum++)
                {
                    PdfPage pdfPage = document.GetPage(pageNum);

                    // 创建文本提取策略
                    var strategy = new SimpleTextExtractionStrategy();

                    // 创建 PdfCanvasProcessor 对象 // 处理页面内容
                    new PdfCanvasProcessor(strategy).ProcessPageContent(pdfPage);

                    // 获取提取的文本
                    string extractedText = strategy.GetResultantText();

                    // 检查文本是否包含搜索字符串
                    if (extractedText.Contains(warterMark))
                    {
                        // 获取字符间的位置信息
                        //var charInfos = strategy.GetLocations();

                        // 输出搜索到的文本及其位置信息
                        Console.WriteLine($"Found text '{warterMark}' on page {pageNum}:");

                        /*foreach (var charInfo in charInfos)
                        {
                            Console.WriteLine($"Char: {charInfo.Text}, X: {charInfo.GetX()}, Y: {charInfo.GetY()}");
                        }*/
                    }
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
