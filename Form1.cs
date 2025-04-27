using Debug.tools;
using Patagames.Pdf;
using Patagames.Pdf.Enums;
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
        static public Point g_dumpImageCoordinate = new Point(100, 100);
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
            AppendLog("\t本软件可去除pdf密码，去除水印，支持水印类型，文本和图片，也可自动按颜色识别去除相同内容");
            AppendLog("\t本软件识别图片中的文本，开发阶段...");
            //AppendLog(Environment.NewLine);
            AppendLog("关于指定页面：");
            AppendLog("\t可指定想要处理的文件页面，而非全部");
            AppendLog("\t指定范围格式和其它方法通用，1,2,4-6：表示处理第1、2、4、5、6页");
            //AppendLog(Environment.NewLine);
            AppendLog("关于 只解密，不去除水印：");
            AppendLog("\t优先级最高。去除密码后，目录不丢失");
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
            AppendLog("关于水印按颜色来去除：");
            AppendLog("\t颜色格式，如果16进制，加上0x。");
            AppendLog("自动搜索到水印后，如果已经指定颜色。需要同时满足颜色相近，才会被确认为是真正的水印");
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            IniHelper iniHelper = new IniHelper();
            iniHelper.IniLoader2Form(this);
            g_selectedFileList = new string[1];
            g_selectedFileList[0] = tb_fileRoot.Text;
            tb_log.Clear();
            AddUsage();

            try
            {
                string pageModeString = iniHelper.getString(this.Name, pageModeFiled, string.Empty);
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
                PdfCommon.Initialize();
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
        private bool IsMatchTextRect(FS_RECTF objRect, WatermarkTextFound textRect, out PointF outTolerance, float accuracy = 30, float inTolerance = 0.1f)
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
        /// <summary>
        /// 两个矩形，是否非常相似。
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
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
        private bool IsExistOverlapAccurately(FS_RECTF a, FS_RECTF b)
        {
            if (a.Equals(b))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 根据查找到的所有obj，计算出最大的矩形外框，新建的查找到的文件中，页面尺寸，就用这个值
        /// </summary>
        /// <param name="sameObjectList"></param>
        /// <returns></returns>
        private FS_RECTF CalculateMaxRectangle(List<PdfPageObject> sameObjectList)
        {
            float expandValue = 5f; // 扩大5mm
            if (sameObjectList == null || sameObjectList.Count == 0)
            {
                //bottom-left corner
                FS_RECTF defalutRect = new FS_RECTF(0, 11.69f * 72, 8.27f * 72,  0);
                return defalutRect;
            }

            // 初始化最小外接矩形
            FS_RECTF maxRect = sameObjectList[0].BoundingBox;

            // 遍历列表，更新最小外接矩形
            foreach (var obj in sameObjectList)
            {
                var rect = obj.BoundingBox;
                maxRect.left = Math.Min(maxRect.left, rect.left);
                maxRect.bottom = Math.Min(maxRect.bottom, rect.bottom);

                maxRect.top = Math.Max(maxRect.top, rect.top);
                maxRect.right = Math.Max(maxRect.right, rect.right);
            }

            maxRect.left = Math.Max(maxRect.left - expandValue, 0);
            maxRect.bottom = Math.Max(maxRect.bottom - expandValue, 0);

            maxRect.top += expandValue;
            maxRect.right += expandValue;
            return maxRect;
        }
        /// <summary>
        /// 把所有查找到的相同obj，画出来，存放到单独的文件名
        /// </summary>
        /// <param name="objectList"></param>
        public void CreatePdfWithObjects(List<PdfPageObject> objectList, string fileName)
        {
            try
            {
                if (objectList.Count == 0)
                {
                    return;
                }
                // Create a new PDF document
                PdfDocument doc = PdfDocument.CreateNew();
                FS_RECTF pageSize = CalculateMaxRectangle(objectList);
                // Step 2: Add new page
                // Arguments: page width: 8.27", page height: 11.69", Unit of measure: inches
                //  The PDF unit of measure is point. There are 72 points in one inch.
                var page = doc.Pages.InsertPageAt(doc.Pages.Count, pageSize.right, pageSize.top);

                foreach (PdfPageObject item in objectList)
                {
                    page.PageObjects.Add(item);
                }
                page.GenerateContent();

                doc.Save(fileName, SaveFlags.NoIncremental);
                //doc.Dispose();
            }
            catch (Exception ex)
            {
                AppendLog(ex.Message);
            }
        }
        
        /// <summary>
        /// 找出连续两张页面中，相同的object,存放到foundPageObj
        /// </summary>
        /// <param name="foundPageObj">存放obj的变量</param>
        /// <param name="itext7"></param>
        /// <param name="searchStartPageNum"> 查找时，起始页</param>
        /// <returns>因为查找到的内容，虽然已经clone到了foundPageObj中，但仍然不能释放doc，否则后面存储到单独文件时，会出现对象已释放的错误。
        /// 所以先保存需要释放的doc，后续再择机释放</returns>
        private List<PdfDocument> AutoFindPaintSameObject(List<PdfPageObject> foundPageObj, iText7 itext7, int searchStartPageNum)
        {
            if (g_pageNumber <= 1)
            {
                Console.WriteLine("It's only one page. It can't be automatically identified");
                return null;
            }
            int validPageCount = 0;

            Color setColor;
            bool isSpecifiedColor = ColorTools.ARGB2RGB(tb_color.Text, out setColor);

            PdfDocument baseDoc = null; // 基础页
            PdfDocument ref1Doc = null; // 参考页1， 和baseDoc比较，相同，则添加到list
            PdfDocument ref2Doc = null; // 参考页2， 进一步筛选list
            List<PdfDocument> pdfNeedRelease = new List<PdfDocument>();
            for (int pageNum = searchStartPageNum; pageNum <= g_pageNumber; pageNum++)
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
                                    // 虽然这两个元素相同，但还要看是否是用户指定 的颜色，只有颜色也匹配，才真的是想要去除的水印。
                                    //没有指定颜色，或者指定的颜色相符
                                    if (!isSpecifiedColor || (isSpecifiedColor && SearchObjectByColor(pageObj.PageObjects[j], setColor)))
                                    {
                                        foundPageObj.Add(pageObj.PageObjects[j].Clone());
                                    }

                                    break;
                                }
                            }
                        }
                        //document.Dispose();
                        ref1Doc = document;
                        pdfNeedRelease.Add(ref1Doc);
                        //goto _exit;
                        break;
                    case 2://进一步筛选list，查看在剩下页中，某个元素x,是否存在于list中，如果存在，则保留，说明是共有的；不存在，则将list中的x删掉
                        for (int found = foundPageObj.Count - 1; found >= 0; found--)
                        {
                            bool isFoundSame = false;
                            PdfPageObject foundObj = foundPageObj[found];
                            int j;
                            for (j = 0; j <= pageObj.PageObjects.Count - 1; j++)
                            {
                                if (!pageObj.PageObjects[j].ObjectType.Equals(foundObj.ObjectType))
                                {
                                    continue;
                                }
                                if (IsExistOverlap(pageObj.PageObjects[j].BoundingBox, foundObj.BoundingBox))
                                {
                                    isFoundSame = true;
                                    break;
                                }
                            }
                            if (!isFoundSame)
                            {
                                // not found
                                foundPageObj.RemoveAt(found);
                            }
                        }
                        
                        ref2Doc = document;
                        pdfNeedRelease.Add(ref2Doc);
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
            return pdfNeedRelease;
        }
        /// <summary>
        /// 当既没有设定水印是图片，也没有设定是文本的时候，就用自动搜索到的每页都存在的obj,foundSameObject，
        /// 在page中查找，类型相同，并且尺寸匹配，返回true。
        /// </summary>
        /// <param name="pageObjects"></param>
        /// <param name="foundSameObject"></param>
        /// <returns></returns>
        private bool SearchObjectFromSameFoundList(PdfPageObject pageObjects, List<PdfPageObject> foundSameObject)
        {
            foreach (PdfPageObject item in foundSameObject)
            {
                // 类型相同，并且尺寸相等
                if (pageObjects.ObjectType.Equals(item.ObjectType) && IsExistOverlapAccurately(pageObjects.BoundingBox, item.BoundingBox))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 当既没有设定水印是图片，也没有设定是文本的时候，就用自动搜索到的每页都存在的obj,foundSameObject，在page中查找，如果匹配，返回true,则删除它。
        /// </summary>
        /// <param name="pageObjects"></param>
        /// <param name="foundSameObject"></param>
        /// <returns></returns>
        private bool SearchObjectByColor(PdfPageObject pageObjects, Color destColor)
        {
            Color SetColor;

            //有指定，则既要满足坐标，也要满足颜色
            float distance = ColorTools.RGBDistance(pageObjects.FillColor, destColor);
            if (distance < 20)
            {
                Console.WriteLine("distance :" + distance);
                return true;
            }

            Console.WriteLine("distance not:" + distance);
            return false;
        }
        private void AppenDumpImageToList(List<PdfPageObject> objectList)
        {
            try
            {
                // Create a new PDF document
                PdfDocument doc = PdfDocument.CreateNew();

                // Step 3: Add graphics and text contents to the page
                // Insert image from file using standart System.Drawing.Bitmap class
                using (PdfBitmap img = PdfBitmap.FromBitmap(Properties.Resources.dump_300K))
                {
                    PdfImageObject imageObject = PdfImageObject.Create(doc, img, g_dumpImageCoordinate.X, g_dumpImageCoordinate.Y);
                    objectList.Add(imageObject.Clone());
                }
                using (PdfBitmap img = PdfBitmap.FromBitmap(Properties.Resources.dump_600K))
                {
                    PdfImageObject imageObject = PdfImageObject.Create(doc, img, g_dumpImageCoordinate.X, g_dumpImageCoordinate.Y);
                    objectList.Add(imageObject.Clone());
                }
                doc.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        List<PdfPageObject> foundSameObjectList = new List<PdfPageObject>();
        /// <summary>
        /// 删除 水印
        /// </summary>
        /// <param oriFileName="name"></param>
        private bool Patagames_removeWaterMarkOneByOne(string[] textWarterMark, ImageRectArea imageRectAreaWarterMark, List<WatermarkTextFound> watermarkFounds, iText7 itext7, out string msg)
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
                foundSameObjectList.Clear();
                if (!cb_isText.Checked && !cb_isImage.Checked && !checkBox_decryptOnly.Checked) {
                    List<PdfDocument> releaseDocLater = AutoFindPaintSameObject(foundSameObjectList, itext7, WARTERMARK_SEARCH_START_PAGE_NUM);
                    CreatePdfWithObjects(foundSameObjectList, TEMP_PURE + "\\sameObj.pdf");
                    foreach (var item in releaseDocLater)
                    {
                        if (item != null)
                        {
                            item.Dispose();
                        }
                    }
                }
                AppenDumpImageToList(foundSameObjectList);
                Color setColor;
                bool isSpecifiedColor = ColorTools.ARGB2RGB(tb_color.Text, out setColor);
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
                    AppendLog("start processing page: " + pageNum);
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
                    WatermarkTextFound targetWatermarkFound = watermarkFounds.FirstOrDefault(w => w.page == pageNum);

                    for (int j = pageObj.PageObjects.Count - 1; j >= 0; j--)
                    {
                        FS_RECTF rect = pageObj.PageObjects[j].BoundingBox;
                        PointF outTolerance = new PointF(0, 0);
                        // check ,if exist dump image
                        if (SearchObjectFromSameFoundList(pageObj.PageObjects[j], foundSameObjectList))
                        {
                            removeCount++;
                            pageObj.PageObjects.RemoveAt(j);
                        }
                        else if (cb_isText.Checked)
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
                msg = ex.Message + Environment.NewLine + ex.StackTrace;
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
            string fileName = page + "_" + idx + "--wh_" + (int)imageObject.BoundingBox.Width + "x" + (int)imageObject.BoundingBox.Height +
                "_l_" + (int)imageObject.BoundingBox.left + "_b_" + (int)imageObject.BoundingBox.bottom;
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
            ClearWorkTemp(TEMP_SPLIT);
            ClearWorkTemp(TEMP_PURE);
            Thread.Sleep(50);

            string fileName = fileNameObj.ToString();

            if (!File.Exists(fileName))
            {
                AppendLog("step 0: file not exist");
                return;
            }
            iText7 itext7 = new iText7(AppendLog);
            string msg = string.Empty;
            ClearWorkTemp(g_outputImagePath); // clear last record
            g_outputPdfFolder = Path.GetDirectoryName(fileName);
            g_outputImagePath = g_outputPdfFolder + "\\" + TEMP_IMAGES;
            g_outputFileName = GetOutputNewFileName(fileName);


            g_pageNumber = itext7.PdfGetPageNumber(fileName);
            GetPageRange(fileName, g_pageNumber, itext7.pageRange);
            AppendLog("step 1: search Wartermark");
            string[] textWarterMark = GetWarterMarkListFromUI();
            List<WatermarkTextFound> watermarkFoundList;
            if (checkBox_decryptOnly.Checked)
            {
                watermarkFoundList = new List<WatermarkTextFound>();
            }
            else
            {
                watermarkFoundList = itext7.PdfSearchWartermarkText(fileName, textWarterMark, out msg);
            }
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

            OpenPath(g_outputPdfFolder);
            AppendLog("finished success");
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
                AppendLog("ClearWorkTemp error : " + path + ".error:"+ ex.Message);
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
