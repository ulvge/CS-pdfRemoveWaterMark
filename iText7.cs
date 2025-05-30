﻿using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Navigation;
using iText.Kernel.Utils;
using iText.Layout;
using iText.Layout.Element;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfRemoveWaterMark
{
    class iText7
    {
        public List<WatermarkTextFound> wartermarkFoundBounds = new List<WatermarkTextFound>();
        public delegate void AppendLog(string arg, bool isDisplayUI = true);
        public List<int> pageRange = new List<int>();
        private AppendLog appendLog;

        public iText7(AppendLog appendLog)
        {
            this.appendLog = appendLog;
        }
        public bool IsPageInPageRange(int i)
        {
            foreach (var item in pageRange)
            {
                if (item == i)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 是否被限制
        /// </summary>
        /// <param name="fileSize"></param>
        /// <returns>false，不限制;  true, 限制</returns>
        private bool IsLicenseLimit(long fileSize, out float decimalPart)
        {
            float mb = fileSize / (1024 * 1024);
            decimalPart = mb - (float)Math.Floor(mb);
            if (mb < 1.0f)
            {
                return false;
            }
            if (mb > 10.0f)
            {
                return false;
            }
            if (decimalPart > 0.5)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 如果文档大小在license限制条件中，则append 一个bmp
        /// </summary>
        private void PdfSplitAppendObject(string filePath, System.Drawing.Point dumpImageCoordinate)
        {
            if (!File.Exists(filePath))
            {
                return;
            }
            FileInfo fileInfo = new FileInfo(filePath);
            long fileLength = fileInfo.Length;
            Console.WriteLine($"文件大小: {fileLength / 1024} kb,{fileLength / 1024/1024} mb");
            try
            {
                float decimalPart;
                if (IsLicenseLimit(fileLength, out decimalPart))
                {
                    string tempFilePath = filePath.ToLower().Replace(".pdf", "_tmp.pdf");
                    // 打开现有的 PDF 文档
                    WriterProperties writerProperties = new WriterProperties().SetCompressionLevel(CompressionConstants.NO_COMPRESSION);
                    PdfReader pdfReader = new PdfReader(filePath);
                    PdfWriter pdfWriter = new PdfWriter(tempFilePath, writerProperties);
                    PdfDocument pdfDocument = new PdfDocument(pdfReader, pdfWriter);

                    // 获取要添加图片的页面 (例如第 1 页)
                    PdfPage page = pdfDocument.GetPage(1);
                    // 使用 PdfCanvas 来进行页面操作
                    PdfCanvas canvas = new PdfCanvas(page);
                    // 从资源中获取 BMP 图片
                    using (MemoryStream ms = new MemoryStream())
                    {
                        // 将资源中的图片保存到 MemoryStream 中
                        if (decimalPart < 0.3f)
                        {
                            Properties.Resources.dump_600K.Save(ms, ImageFormat.Bmp);
                        }
                        else
                        {
                            Properties.Resources.dump_300K.Save(ms, ImageFormat.Bmp);
                        }

                        // 将 MemoryStream 转换为 iText7 可用的 ImageData 对象
                        ImageData imageData = ImageDataFactory.Create(ms.ToArray());

                        // 创建 iText7 图片对象
                        Image img = new Image(imageData);

                        canvas.AddImageAt(imageData, dumpImageCoordinate.X, dumpImageCoordinate.Y, false);
                    }
                    // 关闭文档
                    pdfDocument.Close();

                    // 将生成的临时文件替换原始文件
                    File.Delete(filePath);  // 删除原始文件
                    File.Move(tempFilePath, filePath);  // 将临时文件重命名为原始文件名
                    Console.WriteLine("图像已成功添加到页面中！");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public bool PdfSplit(string splitTempFolder, string oriFileName, out string msg)
        {
            try
            {
                msg = string.Empty;
                PdfReader oriFilePdfReader = new PdfReader(oriFileName);
                oriFilePdfReader.SetUnethicalReading(true);
                PdfDocument oriFileDoc = new PdfDocument(oriFilePdfReader);
                if (!Directory.Exists(splitTempFolder))
                {
                    Directory.CreateDirectory(splitTempFolder);
                }

                for (int pageNum = 1; pageNum <= oriFileDoc.GetNumberOfPages(); pageNum++)
                {
                    try
                    {
                        if (!IsPageInPageRange(pageNum))
                        {
                            continue;
                        }
                        PdfPage page = oriFileDoc.GetPage(pageNum);

                        // 创建输出PDF文档
                        string outputPdfFilePath = System.IO.Path.Combine(splitTempFolder, $"{pageNum}.pdf");

                        PdfWriter pdfWriter = new PdfWriter(outputPdfFilePath);
                        PdfDocument outputPdfDocument = new PdfDocument(pdfWriter);
                        // 复制当前页到输出文档
                        oriFileDoc.CopyPagesTo(pageNum, pageNum, outputPdfDocument);
                        outputPdfDocument.FlushCopiedObjects(oriFileDoc);
                        appendLog($"Page {pageNum} saved to: {outputPdfFilePath}", false);
                        outputPdfDocument.Close();  // write file
                        pdfWriter.Close();

                        PdfSplitAppendObject(outputPdfFilePath, MainForm.g_dumpImageCoordinate);
                    }
                    catch (Exception ex)
                    {
                        appendLog("PdfSplit:" + ex.Message);
                    }
                }
                oriFileDoc.Close();
                oriFilePdfReader.Close();
            }
            catch (Exception ex)
            {
                msg = "PdfSplit exception: " + ex.Message;
                return false;
            }
            finally
            {
            }
            return true;
        }
        public int PdfGetPageNumber(string fileName)
        {
            PdfReader pdfReader = new PdfReader(fileName);
            PdfDocument document = new PdfDocument(pdfReader);
            int pageNum = document.GetNumberOfPages();
            pdfReader.Close();
            document.Close();
            return pageNum;
        }
        public List<WatermarkTextFound> PdfSearchWartermarkText(string fileName, string[] warterMark, out string msg)
        {
            try
            {   
                PdfReader pdfReader = new PdfReader(fileName);
                PdfDocument document = new PdfDocument(pdfReader);
                for (int pageNum = 1; pageNum <= document.GetNumberOfPages(); pageNum++)
                {
                    if (!IsPageInPageRange(pageNum))
                    {
                        continue;
                    }
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
                        WatermarkTextFound watermarkFound = new WatermarkTextFound(pageNum, bounds);
                        wartermarkFoundBounds.Add(watermarkFound);
                    }
                }
            }
            catch (Exception ex)
            {
                appendLog("PdfSearchWartermarkText" + ex.Message);
                msg = ex.Message;
                return null;
            }
            msg = "Search wartermark finished, found count:" + wartermarkFoundBounds.Count;
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
                        string searchText = searchTextList[i];
                        if ((searchText.Length >= 1) && text.Contains(searchText))
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
        public bool PdfMerge(string removedWarterMarkFolder, string oriFileName, string outputFileName, out string msg)
        {
            msg = string.Empty;
            if (!Directory.Exists(removedWarterMarkFolder))
            {
                return false;
            }

            PdfWriter pdfWriter = new PdfWriter(outputFileName);
            pdfWriter.SetSmartMode(true);
            pdfWriter.SetCompressionLevel(CompressionConstants.BEST_SPEED);
            //PdfDocument pdfDocSave = new PdfDocument(pdfWriter);
            using (PdfDocument pdfDocSave = new PdfDocument(pdfWriter))
            {
                PdfMerger merger = new PdfMerger(pdfDocSave).SetCloseSourceDocuments(true);
                using (PdfDocument oriFileDocument = new PdfDocument(new PdfReader(oriFileName)))
                {
                    for (int pageNum = 1; pageNum <= oriFileDocument.GetNumberOfPages(); pageNum++)
                    {
                        if (pageNum % 20 == 0)
                        {
                            GC.Collect();
                        }
                        if (!IsPageInPageRange(pageNum))
                        {
                            continue;
                        }
                        string onePageFilePath = System.IO.Path.Combine(removedWarterMarkFolder, $"{pageNum}.pdf");
                        if (!File.Exists(onePageFilePath))
                        {
                            continue;
                        }
                        using (PdfReader pdfReader = new PdfReader(onePageFilePath))
                        {
                            using (PdfDocument onePageDoc = new PdfDocument(pdfReader))
                            {
                                try
                                {
                                    merger.Merge(onePageDoc, 1, 1);
                                    appendLog("\tmerge pageNum " + pageNum);
                                }
                                catch (Exception ex)
                                {
                                    appendLog(string.Format("\tmerge pageNum {0}, error:{1}, stack:{2} ", pageNum, ex.Message, ex.StackTrace));
                                }
                                onePageDoc.Close();
                                pdfReader.Close();
                            }
                        }
                    }

                    PdfAddOutLine(oriFileDocument, pdfDocSave);
                    merger.Close();
                    pdfDocSave.Close();
                    oriFileDocument.Close();
                }
            }
            pdfWriter.Close();

            pdfWriter.Dispose();
            msg = outputFileName;
            return true;
        }

        void CopyOutlines(PdfDocument sourcePdf, PdfOutline sourceOutline, PdfDocument targetDocument, PdfOutline targetPdfOutlines)
        {
            if (sourceOutline != null)
            {
                // 获取大纲项的标题
                string title = sourceOutline.GetTitle();

                PdfOutline targetOutline = null;

                // 获取大纲项的目标页码
                int pageNumber = GetPageNumberByTitle(title, sourcePdf);
                if (pageNumber >= 0)
                {
                    // 创建目标页的目标 createXYZ 
                    //PdfExplicitDestination destination = PdfExplicitDestination.CreateFit(targetDocument.GetPage(pageNumber));
                    PdfExplicitDestination destination = PdfExplicitDestination.CreateXYZ(targetDocument.GetPage(pageNumber),300, 100, 1.2f);

                    // 在目标PDF文档中添加相应标题的大纲项
                    targetOutline = targetPdfOutlines.AddOutline(title);
                    targetOutline.AddDestination(destination);
                }
                // 递归处理子目录项
                foreach (var childSourceOutline in sourceOutline.GetAllChildren())
                {
                    PdfOutline childChildOutline = targetOutline != null ? targetOutline : targetPdfOutlines;
                    CopyOutlines(sourcePdf, childSourceOutline, targetDocument, childChildOutline);
                }
            }
        }

        int GetPageNumberByTitle(string title, PdfDocument pdfDocument)
        {
            if (string.IsNullOrEmpty(title))
            {
                return -1; // 未找到匹配的页码
            }
            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                if (!IsPageInPageRange(i))
                {
                    continue;
                }
                PdfPage pdfPage = pdfDocument.GetPage(i);
                IList<PdfOutline> outlineList = pdfPage.GetOutlines(false);
                if (outlineList != null)
                {
                    foreach (PdfOutline pdfOutline in outlineList)
                    {
                        if (pdfOutline.GetTitle().Equals(title))
                        {
                            return i;
                        }
                    }
                }
            }
            return -1; // 未找到匹配的页码
        }
        private void PdfAddOutLine(PdfDocument sourcePdf, PdfDocument targetPdf)
        {
            try
            {
                sourcePdf.InitializeOutlines();
                targetPdf.InitializeOutlines();
                PdfOutline sourcePdfOutlines = sourcePdf.GetOutlines(false);
                PdfOutline targetPdfOutlines = targetPdf.GetOutlines(true);

                // 复制源PDF文档的大纲到新文档
                CopyOutlines(sourcePdf, sourcePdfOutlines, targetPdf, targetPdfOutlines);
                Console.WriteLine("Outlines copied successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
