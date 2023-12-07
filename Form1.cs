using Debug.tools;
using Patagames.Pdf;
using Patagames.Pdf.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace pdfRemoveWaterMark
{
    public partial class Form1 : Form
    {
        private const string g_splitTempFolder = "pdfSplit__";
        private const string g_removedTempFolder = "removed__";
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            IniHelper iniHelper = new IniHelper();
            iniHelper.IniLoader2Form(this);
            tb_log.Clear();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            IniHelper iniHelper = new IniHelper();
            iniHelper.IniUpdate2File(this);
        }

        private class ImageRectArea
        {
            public int w_min;
            public int w_max;
            public int h_min;
            public int h_max;
            public ImageRectArea(string w_min, string w_max, string h_min, string h_max)
            {
                this.w_min = ConvertString2Int(w_min);
                this.w_max = ConvertString2Int(w_max) + 1;
                this.h_min = ConvertString2Int(h_min);
                this.h_max = ConvertString2Int(h_max) + 1;
            }
            private int ConvertString2Int(string strFloat)
            {
                try
                {
                    return int.Parse(strFloat);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }
        /// <summary>
        /// 根据 text rect的 宽高范围，判断是否是指定查找的object
        /// </summary>
        /// <param name="objRect"> page中的某个对象</param>
        /// <param name="textRect">搜索到的水印坐标</param>
        /// <param name="outAccuracy">输出:实际的误差 textRect - objRect</param>
        /// <param name="accuracy">输入，允许的最大误差</param>
        /// <param name="tolerance">输入，允许的最大误差</param>
        /// <returns></returns>
        private bool IsMatchTextRect(FS_RECTF objRect, WatermarkFound textRect, out PointF outTolerance, float accuracy = 30, float inTolerance = 0.1f)
        {
            outTolerance = new PointF(0, 0);
            foreach (iText.Kernel.Geom.Rectangle rect in textRect.warterMarkBounds)
            {
                if (((rect.GetWidth() - accuracy) <= objRect.Width) && (objRect.Width <= (rect.GetWidth() + accuracy)) &&
                    ((rect.GetHeight() - accuracy) <= objRect.Height) && (objRect.Height <= (rect.GetHeight() + accuracy)))
                {
                    outTolerance.X = rect.GetWidth() - objRect.Width;
                    outTolerance.Y = rect.GetHeight() - objRect.Height;
                    if (((Math.Abs(outTolerance.X) / rect.GetWidth()) > inTolerance) || ((Math.Abs(outTolerance.X) / rect.GetHeight()) > inTolerance))
                    {
                        return false;
                    }
                    //outTolerance.X = (int)(rect.GetWidth() - objRect.Width);
                    //outTolerance.Y = (int)(rect.GetHeight() - objRect.Height);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        /// <summary>
        /// 根据 UI设定的 宽高范围，判断是否是指定查找的object
        /// </summary>
        /// <param oriFileName="objRect"></param>
        /// <returns></returns>
        private bool IsMatchImageRect(FS_RECTF objRect, ImageRectArea imageRect)
        {
            if (objRect.Width * objRect.Height <= 1)
            {
                return false;
            }
            if ((imageRect.w_min <= objRect.Width) && (objRect.Width <= imageRect.w_max) &&
                (imageRect.h_min <= objRect.Height) && (objRect.Height <= imageRect.h_max) )
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public delegate void LogDelegate(string msg, bool isDisplayUI = true);
        public void AppendLog(string msg, bool isDisplayUI = true)
        {
            // 在这里执行刷新UI所需的操作
            Invoke(new Action(() =>
            {
                Console.WriteLine(msg);
                if (isDisplayUI) { 
                    tb_log.AppendText(msg + Environment.NewLine);
                }
            }));
        }
        /// <summary>
        /// 删除 水印
        /// </summary>
        /// <param oriFileName="name"></param>
        private bool Patagames_removeWaterMarkOneByOne(List<WatermarkFound> watermarkFounds, out string msg)
        {
            ImageRectArea imageRectArea = new ImageRectArea(w_min.Text, w_max.Text, h_min.Text, h_max.Text);
            msg = string.Empty;
            if (!Directory.Exists(g_splitTempFolder))
            {
                return false;
            }
            try
            {
                for (int pageNum = 1; pageNum <= watermarkFounds.Count; pageNum++)
                {
                    int removeCount = 0;
                    string splitPdfFilePath = Path.Combine(g_splitTempFolder, $"{pageNum}.pdf");
                    string outputPdfFilePath = Path.Combine(g_removedTempFolder, $"{pageNum}.pdf");
                    if (!Directory.Exists(g_removedTempFolder))
                    {
                        Directory.CreateDirectory(g_removedTempFolder);
                    }
                    PdfDocument document = PdfDocument.Load(splitPdfFilePath);
                    PdfPage pageObj = document.Pages[0]; // only one
                    WatermarkFound targetWatermarkFound = watermarkFounds.FirstOrDefault(w => w.page == pageNum);
                    if (targetWatermarkFound == null)
                    {
                        continue;
                    }

                    for (int j = pageObj.PageObjects.Count - 1; j >= 0; j--)
                    {
                        FS_RECTF rect = pageObj.PageObjects[j].BoundingBox;
                        PointF outTolerance = new PointF(0, 0);
                        if (IsMatchTextRect(rect, targetWatermarkFound, out outTolerance, 50, 0.1f))
                        {
                            removeCount++;
                            AppendLog($"\tpages: {pageNum} text , Remove At Ojbect: {j} , search rect.w h : {(int)rect.Width}, {(int)rect.Height}" +
                                $", outTolerance:{ outTolerance.X },{ outTolerance.Y }");
                            pageObj.PageObjects.RemoveAt(j);
                        }
                        else if (IsMatchImageRect(rect, imageRectArea))
                        {
                            removeCount++;
                            AppendLog($"\tpages: {pageNum} image , Remove At Ojbect: {j} , search rect.w h : {(int)rect.Width}, {(int)rect.Height}");
                            pageObj.PageObjects.RemoveAt(j);
                        }
                    }
                    if (removeCount == 0)
                    {
                        AppendLog(string.Format("pages: {0} text , not remove any watermark", pageNum));
                    }
                    pageObj.GenerateContent();

                    // save
                    Console.WriteLine("newName: " + outputPdfFilePath);
                    document.WriteBlock += (s, ex) => {
                        using (var stream = new FileStream(outputPdfFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        {
                            stream.Seek(0, SeekOrigin.End);
                            stream.Write(ex.Buffer, 0, ex.Buffer.Length);
                        }
                    };
                    document.Save(Patagames.Pdf.Enums.SaveFlags.NoIncremental | Patagames.Pdf.Enums.SaveFlags.RemoveUnusedObjects);
                    document.Dispose();
                }
            }
            catch (Exception)
            {

                throw;
            }
            return true;
        }
        /// <summary>
        /// 选择 pdf 文件
        /// </summary>
        /// <param oriFileName="sender"></param>
        /// <param oriFileName="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            CreateBackgroundThread(string.Empty);
            return;
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.Filter = "pdf文件|*.pdf";

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                //选择图片进行加载
                tb_fileRoot.Text = fileDialog.FileName;
                if (tb_fileRoot.Text.EndsWith(".pdf"))
                {
                    CreateBackgroundThread(tb_fileRoot.Text);
                }
            }
        }

        private void CreateBackgroundThread(string fileName)
        {
            fileName = @"E:\3Proj\16NS109\CPLD\pdf\try\电源DC-DC_01_12_07.pdf";
            PdfiumSplitSearch pdfiumSplitSearch = new PdfiumSplitSearch(g_splitTempFolder, AppendLog);
            string msg;
            AppendLog("step 1: search Wartermark");
            List<WatermarkFound> watermarkFoundList = pdfiumSplitSearch.PdfiumSearchWartermark(fileName, out msg);
            if (watermarkFoundList == null)
            {
                AppendLog(msg);
                return;
            }
            AppendLog("step 2: split");
            if (pdfiumSplitSearch.PdfiumSplit(fileName, out msg) == false)
            {
                AppendLog(msg);
                return;
            }
            AppendLog("step 3: remove");
            if (Patagames_removeWaterMarkOneByOne(watermarkFoundList, out msg) == false)
            {
                AppendLog(msg);
                return;
            }
            AppendLog("step 4: merge");
        }
    }
}
