using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tesseract;

namespace pdfRemoveWaterMark
{
    class RenameFiles
    {
        public delegate void AppendLog(string arg, bool isDisplayUI = true);
        private AppendLog appendLog;
        private string g_traineddataPath = Environment.CurrentDirectory + "\\traineddata";
        private static int g_runCount = -1;
        public RenameFiles(AppendLog appendLog)
        {
            this.appendLog = appendLog;
        }
        public void RenameFilesThread(object fileNameObj)
        {
            if (fileNameObj == null)
            {
                return;
            }
            Thread.Sleep(50);

            string[] fileNameList = (string[])fileNameObj;
            string msg;
            foreach (var item in fileNameList)
            {
                ExtractionTextFromImage(item, out msg);
            }
        }

        private void SaveResult(Pix pix, string directoryName, string filename)
        {
            string fullFileName = Path.Combine(directoryName, filename);
            pix.Save(fullFileName);
        }
        private Pix GetPix(string imageFileName)
        {
            string directoryName = Path.GetDirectoryName(imageFileName);
            Pix sourcePix = Pix.LoadFromFile(imageFileName);

            Pix grayscalePix = sourcePix.ConvertRGBToGray(1, 1, 1);
            SaveResult(grayscalePix, directoryName, "grayscalePix.png");

            //Pix binarizedImage = grayscalePix.BinarizeSauvola(10, 0.15f, true);
            Pix binarizedImage = grayscalePix.BinarizeOtsuAdaptiveThreshold(200, 200, 50, 50, 0.1F);
            SaveResult(binarizedImage, directoryName, "binarizedImage.png");

            sourcePix.Dispose();
            grayscalePix.Dispose();
            return binarizedImage;
        }
        private bool ExtractionTextFromImage(string imageFileName, out string msg)
        {
            msg = string.Empty;
            try
            {
                string whitelist = "tessedit_char_whitelist";
                //string whitelistValue = "0123456789";
                string whitelistValue = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                //string whitelistValue = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

                //Tesseract.Page
                //Page page = new TesseractEngine(@"D:\", "chi_sim", EngineMode.Default).Process(PixConverter.ToPix(image));
                //Page page = new TesseractEngine(g_traineddataPath, "chi_sim", EngineMode.Default).Process(PixConverter.ToPix(image));
                // Page page = new TesseractEngine(g_traineddataPath, "eng", EngineMode.TesseractAndLstm).Process(PixConverter.ToPix(image));
                //Page page = new TesseractEngine(g_traineddataPath, "eng", EngineMode.Default).Process(PixConverter.ToPix(image));
                TesseractEngine engine = new TesseractEngine(g_traineddataPath, "eng", EngineMode.Default);
                //TesseractEngine engine = new TesseractEngine(g_traineddataPath, "eng+chi_sim", EngineMode.Default);
                var variableWasSet = engine.SetVariable(whitelist, whitelistValue);

                g_runCount++;
                //PageSegMode.SingleBlock
                //PageSegMode.SingleLine
                //PageSegMode.SingleChar
                //PageSegMode.SingleLine
                Page page = engine.Process(GetPix(imageFileName), (PageSegMode)g_runCount);

                //打印识别率
                appendLog(string.Format("file:{0} \r\n\t{1:P}", imageFileName, page.GetMeanConfidence()));
                //打印识别文本 //替换'/n'为'(空)'//替换'(空格)'为'(空)'
                appendLog("count:" + g_runCount + " " + page.GetText().Replace("\n", "").Replace(" ", ""));
                //释放程序对图片的占用
                if (g_runCount > (int)PageSegMode.Count)
                {
                    g_runCount = 0;
                }
                return true;
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                return false;
            }
        }
    }
}
