using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
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
        public List<WatermarkFound> wartermarkFoundBounds = new List<WatermarkFound>();
        public delegate void AppendLog(string arg, bool isDisplayUI = true);
        public string outputPdfFolder;
        private AppendLog appendLog;

        public PdfiumSplitSearch(string splitTempFolder, AppendLog appendLog)
        {
            this.appendLog = appendLog;
            this.outputPdfFolder = splitTempFolder;
        }
        public bool PdfiumSplit(string fileName, out string msg)
        {
            try
            {
                msg = string.Empty;
                PdfReader reader = new PdfReader(fileName);
                PdfDocument document = new PdfDocument(reader);
                if (!Directory.Exists(outputPdfFolder))
                {
                    Directory.CreateDirectory(outputPdfFolder);
                }

                for (int pageNum = 1; pageNum <= document.GetNumberOfPages(); pageNum++)
                {
                    PdfPage page = document.GetPage(pageNum);

                    // 创建输出PDF文档
                    string outputPdfFilePath = System.IO.Path.Combine(outputPdfFolder, $"{pageNum}.pdf");
                    using (PdfDocument outputPdfDocument = new PdfDocument(new PdfWriter(outputPdfFilePath)))
                    {
                        // 复制当前页到输出文档
                        document.CopyPagesTo(pageNum, pageNum, outputPdfDocument);
                    }
                    appendLog($"Page {pageNum} saved to: {outputPdfFilePath}", false);
                }
                document.Close();
                reader.Close();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                return false;
            }
            return true;
        }
        public List<WatermarkFound> PdfiumSearchWartermark(string fileName, out string msg)
        {
            string[] warterMark = { "Silergy Corp. Confidential-Prepared for Jovial", "12345678" };
            try
            {   
                PdfReader pdfReader = new PdfReader(fileName);
                PdfDocument document = new PdfDocument(pdfReader);
                for (int pageNum = 1; pageNum <= document.GetNumberOfPages(); pageNum++)
                {
                    PdfPage pdfPage = document.GetPage(pageNum);

                    // 创建文本提取策略
                   // SimpleTextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    var strategy = new CustomTextExtractionStrategy(warterMark);

                    // 创建 PdfCanvasProcessor 对象 // NuGet 处理页面内容 itext7.font-asian
                    PdfCanvasProcessor pdfCanvasProcessor = new PdfCanvasProcessor(strategy);
                    pdfCanvasProcessor.ProcessPageContent(pdfPage);

                    // 获取提取的文本
                    string extractedText = strategy.GetResultantText();

                    // 检查文本是否包含搜索字符串
                    if (strategy.isFound)
                    {
                        // 输出搜索到的文本及其位置信息
                        appendLog($"Found water text on page {pageNum}:{ Environment.NewLine}{string.Join(Environment.NewLine, warterMark)} { Environment.NewLine}", false);
                        List<Rectangle>  bounds = strategy.GetBoundingBoxList();
                        WatermarkFound watermarkFound = new WatermarkFound(pageNum, bounds);
                        wartermarkFoundBounds.Add(watermarkFound);
                    }
                }
            }
            catch (Exception ex)
            {
                appendLog("PdfiumSearchWartermark" + ex.Message);
                msg = ex.Message;
                return null;
            }
            msg = "handler success";
            return wartermarkFoundBounds;
        }


        class CustomTextExtractionStrategy : LocationTextExtractionStrategy
        {
            private readonly string[] searchTextList;
            public List<Rectangle> searchTextListBounds = new List<Rectangle>();
            public bool isFound = false;

            public CustomTextExtractionStrategy(string[] searchTextList)
            {
                this.searchTextList = searchTextList;
            }

            public override void EventOccurred(IEventData data, EventType type)
            {
                if (type == EventType.RENDER_TEXT)
                {
                    TextRenderInfo renderInfo = (TextRenderInfo)data;
                    string text = renderInfo.GetText();
                    if (text.Length > 4){
                        Console.WriteLine("EventOccurred :"+ text);
                    }
                    for (int i = 0; i < searchTextList.Length; i++)
                    {
                        if (text.Contains(searchTextList[i]))
                        {
                            Rectangle boundingBox = GetTextRectangle(renderInfo);
                            searchTextListBounds.Add(boundingBox);
                            isFound = true;
                        }
                    }
                }
            }

            /// <summary>
            /// 单个文本，如char
            /// </summary>
            /// <returns></returns>
            public List<Rectangle> GetBoundingBoxList()
            {
                return searchTextListBounds;
            }
            /// <summary>
            /// 整个文本块
            /// </summary>
            /// <param name="renderInfo"></param>
            /// <returns></returns>

            private iText.Kernel.Geom.Rectangle GetTextRectangle(TextRenderInfo renderInfo)
            {
                //Matrix textToUserSpaceTransform = renderInfo.GetTextMatrix().Multiply(renderInfo.GetTextMatrix());
                //float x = textToUserSpaceTransform.Get(6);
                //float y = textToUserSpaceTransform.Get(7);
                //float width = renderInfo.GetDescentLine().GetLength();
                //float height = renderInfo.GetAscentLine().GetLength();
                //float allTextLength = renderInfo.GetDescentLine().GetLength();
                return new iText.Kernel.Geom.Rectangle(renderInfo.GetDescentLine().GetBoundingRectangle());
            }
        }
    }
}
