using Debug.tools;
using Patagames.Pdf;
using Patagames.Pdf.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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
        private const string TEMP_SPLIT = "tempSplit__";
        private const string TEMP_PURE = "tempRemoved__";
        private const string TEMP_IMAGES = "tempImages__";
        string g_outputPdfFolder = string.Empty;
        string g_outputFileName = string.Empty;
        string g_outputImagePath = string.Empty;
        private int g_pageNumber = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            IniHelper iniHelper = new IniHelper();
            iniHelper.IniLoader2Form(this);
            tb_log.Clear();

            string pageModeString = iniHelper.getString(this.Text, pageModeFiled, string.Empty);
            try
            {
                if (bool.Parse(pageModeString))
                {
                    rb_all.Checked = true;
                    rb_range.Checked = false;
                }
                else
                {
                    rb_all.Checked = false;
                    rb_range.Checked = true;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            cb_isText_CheckedChanged(cb_isText, null);
            cb_isImage_CheckedChanged(cb_isImage, null);
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
        private const string pageModeFiled = "PAGE_NUMER_ALL";
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            IniHelper iniHelper = new IniHelper();

            AbordWorkThread();
            string[] excludeName = { tb_log.Name };
            iniHelper.IniUpdate2File(this, excludeName);

            iniHelper.writeString(this.Text, pageModeFiled, rb_all.Checked.ToString());
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
                Console.WriteLine(ex.Message);
            }
        }
        private string GetOutputNewFileName(string oriFileName)
        {
            string outputPdfFolder = System.IO.Path.GetDirectoryName(oriFileName);
            if (!Directory.Exists(outputPdfFolder))
            {
                Directory.CreateDirectory(outputPdfFolder);
            }
            string oriFileNameOnly = System.IO.Path.GetFileNameWithoutExtension(oriFileName);
            string outputFileName = System.IO.Path.Combine(outputPdfFolder, $"{oriFileNameOnly}_{DateTime.Now.ToString("yyyy_MM_dd-HHmmss")}.pdf");
            return outputFileName;
        }
        /// <summary>
        /// 删除 水印
        /// </summary>
        /// <param oriFileName="name"></param>
        private bool Patagames_removeWaterMarkOneByOne(string[] textWarterMark, ImageRectArea imageRectAreaWarterMark, List<WatermarkFound> watermarkFounds, Pdfium pdfium, out string msg)
        {
            msg = string.Empty;
            if (!Directory.Exists(TEMP_SPLIT))
            {
                return false;
            }
            if (!Directory.Exists(TEMP_PURE))
            {
                Directory.CreateDirectory(TEMP_PURE);
            }
            try
            {
                for (int pageNum = 1; pageNum <= g_pageNumber; pageNum++)
                { 
                    if (!pdfium.IsPageInPageRange(pageNum))
                    {
                        continue;
                    }
                    int removeCount = 0;
                    bool isTextMatchSuccess = false;
                    string splitPdfFilePath = Path.Combine(TEMP_SPLIT, $"{pageNum}.pdf");
                    string outputPdfFilePath = Path.Combine(TEMP_PURE, $"{pageNum}.pdf");
                    PdfDocument document = PdfDocument.Load(splitPdfFilePath);
                    PdfPage pageObj = document.Pages[0]; // only one
                    WatermarkFound targetWatermarkFound = watermarkFounds.FirstOrDefault(w => w.page == pageNum);

                    for (int j = pageObj.PageObjects.Count - 1; j >= 0; j--)
                    {
                        FS_RECTF rect = pageObj.PageObjects[j].BoundingBox;
                        PointF outTolerance = new PointF(0, 0);
                        if (cb_isText.Checked)
                        {
                            if (targetWatermarkFound == null)
                            {
                                continue;
                            }
                            try
                            {
                                string objText = ((PdfTextObject)pageObj.PageObjects[j]).TextUnicode;
                                if (IsMatchTextConect(objText, textWarterMark))
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
                                AppendLog($"\tpages: {pageNum} text rect, Remove:  search rect.w h : {(int)rect.Width} x {(int)rect.Height}" +
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
                            if (IsMatchImageRect(rect, imageRectAreaWarterMark)) { 
                                removeCount++;
                                AppendLog($"\tpages: {pageNum} image , Remove At Ojbect: {j} , search rect.w h : {(int)rect.Width}, {(int)rect.Height}");
                                SaveTheRemovedImage(pageObj.PageObjects, pageNum, j, g_outputImagePath);
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

        private void SaveTheRemovedImage(PdfPageObjectsCollection pageObjects, int page, int idx, string savePath)
        {
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            PdfPageObject objectToSave = pageObjects[idx];
            PdfImageObject imageObject = objectToSave as PdfImageObject;
            if (imageObject == null)
                return; //if not an image object then nothing do

            //Save image to disk
            string fileName = page + "_" + idx + "--wh_" + (int)imageObject.BoundingBox.Width + "x" + (int)imageObject.BoundingBox.Height;
            var path = string.Format(savePath + "\\"+ fileName + ".png");
            imageObject.Bitmap.Image.Save(path, ImageFormat.Png);
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
        private void OpenPath(string path)
        {
            Process process = new Process();
            process.StartInfo.FileName = "explorer.exe"; // 使用资源管理器打开路径
            process.StartInfo.Arguments = path;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            process.WaitForInputIdle(); // 等待资源管理器启动
            process.Close();
        }
        private List<int> GetPageRange(string fileName, int pageNumber, List<int> pageRange)
        {
            pageRange.Clear();
            if (rb_all.Checked)
            {
                for (int i = 1; i <= pageNumber; i++)
                {
                    pageRange.Add(i);
                }
            }
            else if (rb_range.Checked)
            {
                string inputPageString = tb_Range.Text;
                string[] groups = inputPageString.Split(',');
                foreach (string group in groups)
                {
                    int start, end;
                    string[] item = group.Split('-');
                    switch (item.Length)
                    {
                        case 1:
                            start = int.Parse(item[0]);
                            end = start;
                            break;
                        case 2:
                            start = int.Parse(item[0]);
                            end = int.Parse(item[1]);
                            break;
                        default:
                            continue;
                    }
                    for (int i = start; i <= end; i++)
                    {
                        pageRange.Add(i);
                    }
                }
            }
            return pageRange;
        }
        private void mainWork(object fileNameObj)
        {
            //string fileName = @"E:\3Proj\16NS109\CPLD\pdf\try\电源DC-DC_01_12_07.pdf";
            Thread.Sleep(50);

            string fileName = fileNameObj.ToString();
            Pdfium pdfium = new Pdfium(AppendLog);
            string msg;
            ClearWorkTemp(g_outputImagePath); // clear last record
            g_outputPdfFolder = Path.GetDirectoryName(fileName);
            g_outputImagePath = g_outputPdfFolder + "\\" + TEMP_IMAGES;
            g_outputFileName = GetOutputNewFileName(fileName);


            g_pageNumber = pdfium.PdfiumGetPageNumber(fileName);
            GetPageRange(fileName, g_pageNumber, pdfium.pageRange);
            AppendLog("step 1: search Wartermark");
            string[] textWarterMark = GetWarterMarkListFromUI();
            List<WatermarkFound> watermarkFoundList = pdfium.PdfiumSearchWartermark(fileName, textWarterMark, out msg);
            if (watermarkFoundList.Count == 0)
            {
                if (cb_isText.Checked == true) { 
                    AppendLog(msg);
                    return;
                }
            }
            else
            {
                AppendLog("\tsearched Wartermark count:" + watermarkFoundList[0].warterMarkBounds.Count);
            }
            AppendLog("step 2: split");
            if (pdfium.PdfiumSplit(TEMP_SPLIT, fileName, out msg) == false)
            {
                AppendLog(msg);
                return;
            }
            AppendLog("\tsplit success");
            AppendLog("step 3: remove Water Mark");
            ImageRectArea imageRectAreaWarterMark = new ImageRectArea(w_min.Text, w_max.Text, h_min.Text, h_max.Text);
            if (Patagames_removeWaterMarkOneByOne(textWarterMark, imageRectAreaWarterMark, watermarkFoundList, pdfium, out msg) == false)
            {
                AppendLog(msg);
                return;
            }
            AppendLog("step 4: merge");
            if (pdfium.PdfiumMerge(TEMP_PURE, fileName, g_outputFileName, out msg) == false)
            {
                AppendLog(msg);
                return;
            }
            AppendLog("step 5: add outline");

            ClearWorkTemp(TEMP_SPLIT);
            ClearWorkTemp(TEMP_PURE);
            AppendLog("finished success");

            OpenPath(g_outputPdfFolder);
        }

        private void ClearWorkTemp(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true); // 删除目录及其所有子目录和文件
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void rb_CheckedChanged(object sender, EventArgs e)
        {
            //RadioButton rb = (RadioButton)sender;
            tb_Range.Enabled = rb_range.Checked;
        }

        private void cb_isText_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            tb_warterMark.Enabled = cb.Checked;
        }

        private void cb_isImage_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            w_min.Enabled = cb.Checked;
            w_max.Enabled = cb.Checked;
            h_min.Enabled = cb.Checked;
            h_max.Enabled = cb.Checked;
        }
    }
}
