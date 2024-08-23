using Debug.tools;
using Patagames.Pdf;
using Patagames.Pdf.Net;
using pdfRemoveWaterMark.tools;
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
    public partial class MainForm : Form
    {
        private const string TEMP_SPLIT = "tempSplit__";
        private const string TEMP_PURE = "tempRemoved__";
        private const string TEMP_IMAGES = "tempImages__";
        private const int WARTERMARK_SEARCH_START_PAGE_NUM = 2;//首页有些比较 特殊
        string g_outputPdfFolder = string.Empty;
        string g_outputFileName = string.Empty;
        string g_outputImagePath = string.Empty;
        private int g_pageNumber = 0;
        string[] g_selectedFileList;
        bool isSetSuccess;

        public MainForm(bool isSetSuccess)
        {
            InitializeComponent();
            this.isSetSuccess = isSetSuccess;
        }

        private void AddUsage()
        {
            AppendLog("帮助：");
            AppendLog("\t本软件可去除pdf水印，支持水印类型，文本和图片，也可自动按颜色识别去除相同内容");
            AppendLog("\t本软件识别图片中的文本，开发阶段...");
            //AppendLog(Environment.NewLine);
            AppendLog("关于指定页面：");
            AppendLog("\t可指定想要处理的文件页面，而非全部");
            AppendLog("\t指定范围格式和其它方法通用，1,2,4-6：表示处理第1、2、4、5、6页");
            //AppendLog(Environment.NewLine);
            AppendLog("关于水印是文本：");
            AppendLog("\t如果是水印是文本类型，可一次性去除多条文本，用换行隔开");
            //AppendLog(Environment.NewLine);
            AppendLog("关于水印是图片：");
            AppendLog("\t如果是图片，需要提前设定图片范围，长宽通常在100~800之间。");
            AppendLog("\t也可通过日志，确认去除的内容信息。");
            AppendLog("\t处理完成后，会自动打开新文件所在的目录，新文件名：原文件名+日期+时间");
            AppendLog("\t同时会在这个目录下面，生成一个tempImages__的子文件夹。");
            AppendLog("\t会将pdf文档中，已经去掉的图片保存下来");
            AppendLog("\t图片文件夹名中的宽高参数，可依需要进行调整，确保是想去除的内容");
            AppendLog("\t1_0--wh_401x260.png,表示第1页的第0个水印，宽是401,高是260");
            AppendLog("即不选择文本，也不选择图片，则会自动按颜色识别");
            AppendLog("\t颜色格式，如果16进制，加上0x。");
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            IniHelper iniHelper = new IniHelper();
            iniHelper.IniLoader2Form(this);
            g_selectedFileList = new string[1];
            g_selectedFileList[0] = tb_fileRoot.Text;
            tb_log.Clear();
            AddUsage();

            string pageModeString = iniHelper.getString(this.Name, pageModeFiled, string.Empty);
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
            if (!isSetSuccess)
            {
                //MessageBox.Show("not run in administrator!!!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

            iniHelper.writeString(this.Name, pageModeFiled, rb_all.Checked.ToString());
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
        private bool IsMatchTextWarter(string text, string[] waterList)
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
            string date = ModifyLocalDateTime.GetLocalRealTime().ToString("yyyy_MM_dd");
            string time = DateTime.Now.ToString("HHmmss");
            string outputFileName = System.IO.Path.Combine(outputPdfFolder, $"{oriFileNameOnly}_{date +"-" + time}.pdf");
            return outputFileName;
        }
        private bool IsExistOverlap(FS_RECTF a, FS_RECTF b)
        {
            if (a.Equals(b)|| ((Math.Round(a.Height, 2) == Math.Round(b.Height, 2)) && (Math.Round(a.Width, 2) == Math.Round(b.Width, 2)) &&
                (Math.Round(a.left, 2) == Math.Round(b.left, 2)) || (Math.Round(a.top, 2) == Math.Round(b.top, 2)) ||
                (Math.Round(a.right, 2) == Math.Round(b.right, 2)) || (Math.Round(a.bottom, 2) == Math.Round(b.bottom, 2))
                ))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 找出所有相同的object,not delete
        /// </summary>
        /// <param name="itext7"></param>
        private List<PdfPageObject> AutoFindSameObject(iText7 itext7)
        {
            if (g_pageNumber <= 1)
            {
                Console.WriteLine("It's only one page. It can't be automatically identified");
                return null;
            }
            int validPageCount = 0;
            List<PdfPageObject> foundPageObj = new List<PdfPageObject>();
            PdfDocument baseDoc = null;
            for (int pageNum = WARTERMARK_SEARCH_START_PAGE_NUM; pageNum <= g_pageNumber; pageNum++)
            {
                if (!itext7.IsPageInPageRange(pageNum))
                {
                    continue;
                }
                string splitPdfFilePath = Path.Combine(TEMP_SPLIT, $"{pageNum}.pdf");
                PdfDocument document = PdfDocument.Load(splitPdfFilePath);
                PdfPage pageObj = document.Pages[0]; // only one
                switch (validPageCount)
                {
                   case 0://找到第1页，什么都不做
                        baseDoc = document;
                        break;
                    case 1://找到第1、2页相同的元素，添加到 list
                        for (int j = 0; j < pageObj.PageObjects.Count; j++)
                        {
                            foreach (var bs in baseDoc.Pages[0].PageObjects)
                            {
                                if (!pageObj.PageObjects[j].ObjectType.Equals(bs.ObjectType))
                                {
                                    continue;
                                }
                                if (IsExistOverlap(pageObj.PageObjects[j].BoundingBox, bs.BoundingBox))
                                {
                                    foundPageObj.Add(pageObj.PageObjects[j].Clone());
                                    break;
                                }
                            }
                        }
                        document.Dispose();
                        goto _exit;
                    default:
                        break;
                }
                validPageCount++;
            }
_exit:
            if (baseDoc != null)
            {
                baseDoc.Dispose();
            }
            return foundPageObj;
        }
        private bool SearchObjectFromSameFoundList(PdfPageObject pageObjects, List<PdfPageObject> foundSameObject)
        {
            foreach (PdfPageObject item in foundSameObject)
            {
                if (pageObjects.ObjectType.Equals(item.ObjectType) && IsExistOverlap(pageObjects.BoundingBox, item.BoundingBox))
                {
                    Color SetColor = ColorTools.ARGB2RGB(tb_color.Text);
                    float distance = ColorTools.RGBDistance(pageObjects.FillColor, SetColor);
                    if (distance < 200)
                    {
                        Console.WriteLine("distance :" + distance);
                        return true;
                    }

                    Console.WriteLine("distance not:" + distance);
                }
            }
            return false;
        }
        /// <summary>
        /// 删除 水印
        /// </summary>
        /// <param oriFileName="name"></param>
        private bool Patagames_removeWaterMarkOneByOne(string[] textWarterMark, ImageRectArea imageRectAreaWarterMark, List<WatermarkFound> watermarkFounds, iText7 itext7, out string msg)
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
                List<PdfPageObject> foundSameObjectList = new List<PdfPageObject>();
                if (!cb_isText.Checked && !cb_isImage.Checked) {
                    foundSameObjectList = AutoFindSameObject(itext7);
                }
                for (int pageNum = 1; pageNum <= g_pageNumber; pageNum++)
                { 
                    if (!itext7.IsPageInPageRange(pageNum))
                    {
                        continue;
                    }
                    int removeCount = 0;
                    bool isTextMatchSuccess = false;
                    string splitPdfFilePath = Path.Combine(TEMP_SPLIT, $"{pageNum}.pdf");
                    string outputPdfFilePath = Path.Combine(TEMP_PURE, $"{pageNum}.pdf");
                    Console.WriteLine("start processing file: " + outputPdfFilePath);
                    PdfDocument document;
                    try
                    {
                        document = PdfDocument.Load(splitPdfFilePath);
                    }
                    catch (Exception ex)
                    {
                        File.Copy(splitPdfFilePath, outputPdfFilePath, true);
                        AppendLog(string.Format("pages: {0} text , save error:{1}", pageNum, ex.Message));
                        string errorMsgExpired = "The trial period for Pdfium.Net SDK has expired";
                        if (ex.Message.Contains(errorMsgExpired))
                        {
                            msg = string.Empty;
                            return false;
                        }
                        continue;
                    }

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
                                if (IsMatchTextWarter(objText, textWarterMark))
                                {
                                    removeCount++;
                                    AppendLog($"\tpages: {pageNum} text have found, Remove it :{objText}");
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
                            if (!IsMatchImageRect(rect, imageRectAreaWarterMark))
                            {
                                continue;
                            }
                            switch (pageObj.PageObjects[j].ObjectType)
                            {
                                case Patagames.Pdf.Enums.PageObjectTypes.PDFPAGE_IMAGE:
                                    removeCount++;
                                    AppendLog($"\tpages: {pageNum} image , Remove At Ojbect: {j} , search rect.w h : {(int)rect.Width}, {(int)rect.Height}");
                                    SaveTheRemovedImage(pageObj.PageObjects, pageNum, j, g_outputImagePath);
                                    pageObj.PageObjects.RemoveAt(j);
                                    break;
                                case Patagames.Pdf.Enums.PageObjectTypes.PDFPAGE_PATH:
                                    break;
                                case Patagames.Pdf.Enums.PageObjectTypes.PDFPAGE_TEXT:
                                default:
                                    break;
                            }
                        }
                        else // Automatic identification watermark
                        {
                            if (SearchObjectFromSameFoundList(pageObj.PageObjects[j], foundSameObjectList))
                            {
                                removeCount++;
                                pageObj.PageObjects.RemoveAt(j);
                            }
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
                    pageObj.Dispose();
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message + ex.StackTrace;
                return false;
            }
            return true;
        }

        private bool SaveTheRemovedImage(PdfPageObjectsCollection pageObjects, int page, int idx, string savePath)
        {
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            PdfPageObject objectToSave = pageObjects[idx];
            PdfImageObject imageObject = objectToSave as PdfImageObject;
            if (imageObject == null) {
                return false; //if not an image object then nothing do
            }

            //Save image to disk
            string fileName = page + "_" + idx + "--wh_" + (int)imageObject.BoundingBox.Width + "x" + (int)imageObject.BoundingBox.Height;
            var path = string.Format(savePath + "\\"+ fileName + ".png");
            imageObject.Bitmap.Image.Save(path, ImageFormat.Png);
            imageObject.Dispose();
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
            fileDialog.Filter = "PDF files (*.pdf)|*.pdf";
            fileDialog.FilterIndex = 1;

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                g_selectedFileList = fileDialog.FileNames;
                tb_fileRoot.Text = fileDialog.FileNames[0];
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (g_selectedFileList == null || g_selectedFileList.Length == 0)
            {
                MessageBox.Show("请先选择文件");
                return;
            }
            ThreadMainWork(g_selectedFileList);
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
            g_selectedFileList = e.Data.GetData(DataFormats.FileDrop, false) as string[];
            tb_fileRoot.Text = g_selectedFileList[0];
        }
        Thread g_threadMainWork = null;
        private void ThreadMainWork(string[] fileNames)
        {
            try
            {
                // 创建一个新线程
                if (g_threadMainWork != null)
                {
                    g_threadMainWork.Abort();
                }
                tb_fileRoot.Text = fileNames[0];
                if (fileNames[0].ToLower().EndsWith(".pdf"))
                {
                    g_threadMainWork = new Thread(new ParameterizedThreadStart(RemoveWaterMarkThreadWork));
                    g_threadMainWork.Start(fileNames[0]);
                }
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
        private void RemoveWaterMarkThreadWork(object fileNameObj)
        {
            //string fileName = @"E:\3Proj\16NS109\CPLD\pdf\try\电源DC-DC_01_12_07.pdf";
            Thread.Sleep(50);

            string fileName = fileNameObj.ToString();

            if (!File.Exists(fileName))
            {
                AppendLog("step 0: file not exist");
                return;
            }
            iText7 itext7 = new iText7(AppendLog);
            string msg;
            ClearWorkTemp(g_outputImagePath); // clear last record
            g_outputPdfFolder = Path.GetDirectoryName(fileName);
            g_outputImagePath = g_outputPdfFolder + "\\" + TEMP_IMAGES;
            g_outputFileName = GetOutputNewFileName(fileName);


            g_pageNumber = itext7.PdfGetPageNumber(fileName);
            GetPageRange(fileName, g_pageNumber, itext7.pageRange);
            AppendLog("step 1: search Wartermark");
            string[] textWarterMark = GetWarterMarkListFromUI();
            List<WatermarkFound> watermarkFoundList = itext7.PdfSearchWartermark(fileName, textWarterMark, out msg);
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
            if (itext7.PdfSplit(TEMP_SPLIT, fileName, out msg) == false)
            {
                AppendLog(msg);
                return;
            }
            AppendLog("\tsplit success");
            AppendLog("step 3: remove Water Mark");
            ImageRectArea imageRectAreaWarterMark = new ImageRectArea(w_min.Text, w_max.Text, h_min.Text, h_max.Text);
            if (Patagames_removeWaterMarkOneByOne(textWarterMark, imageRectAreaWarterMark, watermarkFoundList, itext7, out msg) == false)
            {
                AppendLog(msg);
                return;
            }
            AppendLog("step 4: merge");
            if (itext7.PdfMerge(TEMP_PURE, fileName, g_outputFileName, out msg) == false)
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
                Console.WriteLine("ClearWorkTemp : " + path + ".error:"+ ex.Message);
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
