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
        /// <summary>
        /// 根据 UI设定的 宽高范围，判断是否是指定查找的object
        /// </summary>
        /// <param name="objRect"></param>
        /// <returns></returns>
        private bool isFilter(FS_RECTF objRect)
        {
            if ( (int.Parse(w_min.Text) <= objRect.Width) && (objRect.Width) >= int.Parse(w_max.Text) &&
                (int.Parse(h_min.Text) <= objRect.Height) && (objRect.Height >= int.Parse(h_max.Text)) )
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void AppendLog(string msg)
        {
            Console.WriteLine(msg);
            tb_log.AppendText(msg + Environment.NewLine);
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
                    if (isFilter(rect))
                    {
                        removeCount++;
                        AppendLog(string.Format("pages: {0}, RemoveAt Ojbect: {1} , rect.w : {2}, rect.h : {3}", i, j, rect.Width, rect.Height));
                        pageObj.PageObjects.RemoveAt(j);
                    }
                }
                if (removeCount == 0)
                {
                    AppendLog(string.Format("pages: {0}, not found any watermark", i));
                }
                pageObj.GenerateContent();
            }
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
            new PdfiumSplitSearch(fileName);
            pdfHandler(fileName);
        }
    }
}
