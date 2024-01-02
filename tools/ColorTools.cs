using Patagames.Pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace pdfRemoveWaterMark.tools
{
    class ColorTools
    {
        private static int ConvertString2Number(string str)
        {
            // 匹配10进制数字
            Match decimalMatch = Regex.Match(str, @"-?\d+");
            if (decimalMatch.Success)
            {
                int decimalValue = int.Parse(decimalMatch.Value);
                //Console.WriteLine($"Decimal value: {decimalValue}");
                return decimalValue;
            }
            // 匹配16进制字符串
            Match hexMatch = Regex.Match(str, @"-?[0-9a-fA-F]+");
            if (hexMatch.Success)
            {
                int hexValue = int.Parse(hexMatch.Value, NumberStyles.HexNumber);
                //Console.WriteLine($"Hexadecimal value: {hexValue}");
                return hexValue;
            }
            return 0;
        }
        private static int channle(int chVal, int alpha)
        {
            int ch = (chVal * alpha + (255 - alpha) * 255) / 255;
            return ch;
        }
        public static Color ARGB2RGB(string color)
        {
            Color defaultColor = Color.FromArgb(0, 0, 0);
            string[] co = color.Split(',');
            if (co.Length != 3)
            {
                return defaultColor;
            }

            int R = channle(ConvertString2Number(co[0]), 255);
            int G = channle(ConvertString2Number(co[1]), 255);
            int B = channle(ConvertString2Number(co[2]), 255);
            try
            {
                Color res = Color.FromArgb(R, G, B);
                return res;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ARGB2RGB :" + ex.Message);
            }
            return defaultColor;
        }
        public static float RGBSimilarity1(FS_COLOR col1, Color col2)
        {
            HSV hsv1 = new HSV();
            HSV hsv2 = new HSV();

            SkiaSharp.SKColor sKColor1 = new SkiaSharp.SKColor((byte)col1.R, (byte)col1.G, (byte)col1.B, (byte)col1.A);
            sKColor1.ToHsv(out hsv1.h, out hsv1.s, out hsv1.v);

            SkiaSharp.SKColor sKColor2 = new SkiaSharp.SKColor((byte)col2.R, (byte)col2.G, (byte)col2.B, (byte)col2.A);
            sKColor2.ToHsv(out hsv2.h, out hsv2.s, out hsv2.v);

            float detah = hsv1.h - hsv2.h;
            float detas = hsv1.s - hsv2.s;
            float detav = hsv1.v - hsv2.v;
            double len = Math.Pow(detah * detah + detas * detas + detav * detav, 0.5);
            if (len > 1) len = 1.0;
            float similarity = (float)((1.0 - len) / 1.0);

            return similarity;
        }


        public static float RGBDistance(FS_COLOR col1, Color col2)
        {
            int Rmean = (col1.R + col2.R) / 2;
            int R = col1.R - col2.R;
            int G = col1.G - col2.G;
            int B = col1.B - col2.B;
            //return (float)Math.Pow((2+ Rmean / 256)*(R*R) + 4*(G*G) +(2 + (255 - Rmean)/256) * (B*B), 0.5);
            return (float)Math.Pow((((512 + Rmean) * R * R) >> 8) + 4 * G * G + (((767 - Rmean) * B * B) >> 8), 0.5);
        }
        private class HSV
        {
            public float h;
            public float s;
            public float v;

            public HSV()
            {
            }
            public HSV(float h, float s, float v)
            {
                this.h = h;
                this.s = s;
                this.v = v;
            }
        }
    }
}
