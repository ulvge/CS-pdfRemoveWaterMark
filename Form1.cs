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
using System.Threading;
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

        private void AbordWorkThread()
        {
            try
            {
                // 创建一个新线程
                if (g_threadMainWork != null)
                {
                    g_threadMainWork.Abort();
                }
            }
            catch (Exception)
            {

            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            IniHelper iniHelper = new IniHelper();

            AbordWorkThread();
            string[] excludeName = { tb_log.Name };
            iniHelper.IniUpdate2File(this, excludeName);
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
        private bool IsMatchTextConect(string text, string[] waterList)
        {
            if (waterList == null)
            {
                return false;
            }
            foreach (var item in waterList)
            {
                if (text.Contains(item))
                {
                    return true;
                }
            }
            return false;
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
            try
            {
                // 在这里执行刷新UI所需的操作
                Invoke(new Action(() =>
                {
                    Console.WriteLine(msg);
                    if (isDisplayUI)
                    {
                        tb_log.AppendText(msg + Environment.NewLine);
                    }
                }));
            }
            catch (Exception ex)
            {

            }
        }
        /// <summary>
        /// 删除 水印
        /// </summary>
        /// <param oriFileName="name"></param>
        private bool Patagames_removeWaterMarkOneByOne(string[] warterMark, List<WatermarkFound> watermarkFounds, out string msg)
        {
            ImageRectArea imageRectArea = new ImageRectArea(w_min.Text, w_max.Text, h_min.Text, h_max.Text);
            msg = string.Empty;
            if (!Directory.Exists(g_splitTempFolder))
            {
                return false;
            }
            if (!Directory.Exists(g_removedTempFolder))
            {
                Directory.CreateDirectory(g_removedTempFolder);
            }
            try
            {
                for (int pageNum = 1; pageNum <= watermarkFounds.Count; pageNum++)
                {
                    int removeCount = 0;
                    bool isTextMatchSuccess = false;
                    string splitPdfFilePath = Path.Combine(g_splitTempFolder, $"{pageNum}.pdf");
                    string outputPdfFilePath = Path.Combine(g_removedTempFolder, $"{pageNum}.pdf");
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
                        if (cb_isText.Checked)
                        {
                            try
                            {
                                string objText = ((PdfTextObject)pageObj.PageObjects[j]).TextUnicode;
                                if (IsMatchTextConect(objText, warterMark))
                                {
                                    removeCount++;
                                    AppendLog($"\tpages: {pageNum} text Conect, Remove :{objText}");
                                    pageObj.PageObjects.RemoveAt(j);
                                    isTextMatchSuccess = true;
                                }
                            }
                            catch (Exception)
                            {
                            }

                            if ((isTextMatchSuccess == false) && IsMatchTextRect(rect, targetWatermarkFound, out outTolerance, 50, 0.1f))
                            {
                                removeCount++;
                                AppendLog($"\tpages: {pageNum} text rect, Remove:  search rect.w h : {(int)rect.Width}, {(int)rect.Height}" +
                                    $", outTolerance:{ outTolerance.X },{ outTolerance.Y }");
                                pageObj.PageObjects.RemoveAt(j);
                            }
                            else
                            {
                                //AppendLog($"\tpages: {pageNum} text , search rect.w h : {(int)rect.Width}, {(int)rect.Height}" +
                                //    $", outTolerance:{ outTolerance.X },{ outTolerance.Y }");
                            }
                        }
                        else if (cb_isImage.Checked)
                        {
                            if (IsMatchImageRect(rect, imageRectArea)) { 
                                removeCount++;
                                AppendLog($"\tpages: {pageNum} image , Remove At Ojbect: {j} , search rect.w h : {(int)rect.Width}, {(int)rect.Height}");
                                pageObj.PageObjects.RemoveAt(j);
                            }
                        }
                        else
                        {
                            msg = "请选择水印的类型，文本或图片，至少选择一种";
                            return false;
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
                            stream.Close();
                        }
                    };
                    document.Save(Patagames.Pdf.Enums.SaveFlags.NoIncremental | Patagames.Pdf.Enums.SaveFlags.RemoveUnusedObjects);
                    document.Dispose();
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                return false;
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
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.Filter = "pdf文件|*.pdf";

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                tb_fileRoot.Text = fileDialog.FileName;
                if (tb_fileRoot.Text.EndsWith(".pdf"))
                {
                    CreateBackgroundThreadWork(tb_fileRoot.Text);
                }
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (tb_fileRoot.Text.EndsWith(".pdf"))
            {
                CreateBackgroundThreadWork(tb_fileRoot.Text);
            }
            else
            {
                MessageBox.Show("请先选择一个pdf文件");
            }
        }

        /// <summary>
        /// DragEnter事件中将拖动源中的数据链接到放置目标。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void tb_fileRoot_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.All;
            else e.Effect = DragDropEffects.None;
        }
        private void tb_fileRoot_DragDrop(object sender, DragEventArgs e)
        {
            //获取第一个文件名
            string fileName = (e.Data.GetData(DataFormats.FileDrop, false) as String[])[0];
            CreateBackgroundThreadWork(fileName);
        }
        Thread g_threadMainWork = null;
        private void CreateBackgroundThreadWork(string fileName)
        {
            try
            {
                // 创建一个新线程
                if (g_threadMainWork != null)
                {
                    g_threadMainWork.Abort();
                }
                tb_fileRoot.Text = fileName;
                g_threadMainWork = new Thread(new ParameterizedThreadStart(mainWork));
                g_threadMainWork.Start(fileName);
            }
            catch (Exception ex)
            {
                AppendLog("CreateBackgroundThreadWork Exception: " + ex);
            }
        }
        private string[] GetWarterMarkListFromUI()
        {
            string[] warterMark = tb_warterMark.Text.Split('\n');
            List<string> list = new List<string>();
            foreach (string item in warterMark)
            {
                string str = item.Trim();
                if (str.Length == 0)
                {
                    continue;
                }
                list.Add(str);
            }
            return list.ToArray();
        }
        private void mainWork(object fileNameObj)
        {
            //string fileName = @"E:\3Proj\16NS109\CPLD\pdf\try\电源DC-DC_01_12_07.pdf";
            Thread.Sleep(50);
            string fileName = fileNameObj.ToString();
            Pdfium pdfium = new Pdfium(AppendLog);
            string msg;
            //AppendLog("step 1: search Wartermark");
            //string[] warterMark = GetWarterMarkListFromUI();
            //List<WatermarkFound> watermarkFoundList = pdfium.PdfiumSearchWartermark(fileName, warterMark, out msg);
            //if (watermarkFoundList.Count == 0)
            //{
            //    AppendLog(msg);
            //    return;
            //}
            //AppendLog("\tsearched Wartermark count:" + watermarkFoundList[0].warterMarkBounds.Count);
            //AppendLog("step 2: split");
            //if (pdfium.PdfiumSplit(g_splitTempFolder, fileName, out msg) == false)
            //{
            //    AppendLog(msg);
            //    return;
            //}
            //AppendLog("\tsplit success");
            //AppendLog("step 3: remove Water Mark");
            //if (Patagames_removeWaterMarkOneByOne(warterMark, watermarkFoundList, out msg) == false)
            //{
            //    AppendLog(msg);
            //    return;
            //}
            AppendLog("step 4: merge");
            if (pdfium.PdfiumMerge(g_removedTempFolder, fileName, out msg) == false)
            {
                AppendLog(msg);
                return;
            }
            AppendLog("step 5: add outline");

            AppendLog("finished success");
        }


    }
}
