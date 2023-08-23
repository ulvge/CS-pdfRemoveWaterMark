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
        private static int ConvertString2Number(string input)
        {
            try
            {
                // 检查字符串是否是十六进制
                if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    // 处理带有0x前缀的十六进制字符串
                    return Convert.ToInt32(input, 16);
                }
                else if (input.All(c => "abcdefABCDEF".Contains(c)))
                {
                    // 处理不带前缀的十六进制字符串
                    return Convert.ToInt32(input, 16);
                }
                else
                {
                    // 处理十进制字符串
                    return int.Parse(input);
                }

            }
            catch (Exception)
            {
                return 0;
            }
        }
        private static int channle(int chVal, int alpha)
        {
            int ch = (chVal * alpha + (255 - alpha) * 255) / 255;
            return ch;
        }
        public static bool ARGB2RGB(string color, out Color argb)
        {
            argb = Color.FromArgb(0, 0, 0);
            string[] co = color.Split(',');
            if (co.Length != 3)
            {
                return false;
            }

            int R = channle(ConvertString2Number(co[0]), 255);
            int G = channle(ConvertString2Number(co[1]), 255);
            int B = channle(ConvertString2Number(co[2]), 255);

            try
            {
                argb = Color.FromArgb(R, G, B);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ARGB2RGB :" + ex.Message);
            }
            return false;
        }

        public static float RGBDistance(FS_COLOR col1, Color col2)
        {
            Color col1RGB = Color.FromArgb(
                (int)((col1.R) * (col1.A / 255.0) + 255 - col1.A),
                (int)((col1.G) * (col1.A / 255.0) + 255 - col1.A),
                (int)((col1.B) * (col1.A / 255.0) + 255 - col1.A));


            int Rmean = (col1RGB.R + col2.R) / 2;
            int A = col1RGB.A - col2.A;
            int R = col1RGB.R - col2.R;
            int G = col1RGB.G - col2.G;
            int B = col1RGB.B - col2.B;
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
