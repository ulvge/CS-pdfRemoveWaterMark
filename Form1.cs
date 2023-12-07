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
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            IniHelper iniHelper = new IniHelper();
            iniHelper.IniLoader2Form(this);
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
        /// 根据 UI设定的 宽高范围，判断是否是指定查找的object
        /// </summary>
        /// <param name="objRect"></param>
        /// <returns></returns>
        private bool IsMatch(FS_RECTF objRect, ImageRectArea imageRect)
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
        public delegate void LogDelegate(string msg);
        public void AppendLog(string msg)
        {
            // 在这里执行刷新UI所需的操作
            Invoke(new Action(() =>
            {
                Console.WriteLine(msg);
                tb_log.AppendText(msg + Environment.NewLine);
            }));
        }
        /// <summary>
        /// 删除 水印
        /// </summary>
        /// <param name="name"></param>
        private void pdfHandler(string name)
        {
            string path = name.Substring(0, name.LastIndexOf('\\') + 1);
            string[] fileNameExt = name.Substring(name.LastIndexOf('\\') + 1).Split('.');
            string newName = path + fileNameExt[0] + "_" + DateTime.Now.ToString("yyyy_MM_dd-HHmmss") + "." + fileNameExt[1];
            PdfDocument document = PdfDocument.Load(name);
            ImageRectArea imageRectArea = new ImageRectArea(w_min.Text, w_max.Text, h_min.Text, h_max.Text);
            for (int i = 0; i < document.Pages.Count; i++)
            {
                PdfPage pageObj = document.Pages[i];
                Console.WriteLine(pageObj.Text.GetText(0, 1));
                FS_SIZEF pageSize = document.GetPageSizeByIndex(i);
                Console.WriteLine("page: " + i + ", " + pageObj.PageObjects.Count);
                int removeCount = 0;

                for (int j = pageObj.PageObjects.Count - 1; j >= 0; j--)
                {
                    FS_RECTF rect = pageObj.PageObjects[j].BoundingBox;
                    if (IsMatch(rect, imageRectArea))
                    {
                        removeCount++;
                        AppendLog(string.Format("pages: {0}, RemoveAt Ojbect: {1} , rect.w : {2}, rect.h : {3}", i, j, rect.Width, rect.Height));
                        pageObj.PageObjects.RemoveAt(j);
                    }
                }
                if (removeCount == 0)
                {
                    AppendLog(string.Format("pages: {0}, not remove any watermark", i));
                }
                pageObj.GenerateContent();
            }

            Console.WriteLine("newName: " + newName);
            document.WriteBlock += (s, ex) => {
                using (var stream = new FileStream(newName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    stream.Seek(0, SeekOrigin.End);
                    stream.Write(ex.Buffer, 0, ex.Buffer.Length);
                }
            };
            document.Save(Patagames.Pdf.Enums.SaveFlags.NoIncremental | Patagames.Pdf.Enums.SaveFlags.RemoveUnusedObjects);
            document.Dispose();
        }
        /// <summary>
        /// 选择 pdf 文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            PdfiumSplitSearch pdfiumSplitSearch = new PdfiumSplitSearch(AppendLog);
            string msg;
            pdfiumSplitSearch.PdfiumSplit(fileName, out msg);

            List<WatermarkFound> watermarkFoundList = pdfiumSplitSearch.PdfiumSearchWartermark(fileName, out msg);
            if (watermarkFoundList == null)
            {
                AppendLog(msg);
            }
            pdfHandler(fileName);
        }
    }
}
