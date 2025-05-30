using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace pdfRemoveWaterMark.tools
{
    class ModifyLocalDateTime
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Systemtime
        {
            public short year;
            public short month;
            public short dayOfWeek;
            public short day;
            public short hour;
            public short minute;
            public short second;
            public short milliseconds;
        }

        private static int g_addDays = 0;
        private static DateTime g_localRealTime = DateTime.Now;
        private static bool SetLocalDateTime(DateTime newTime, out string msg)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C date {newTime.Year}/{newTime.Month}/{newTime.Day}",
                RedirectStandardOutput = true,  // 重定向标准输出
                RedirectStandardError = true,   // 重定向错误输出
                UseShellExecute = false,       // 必须设为 false 才能重定向
                CreateNoWindow = true          // 不显示 CMD 窗口
            };

            using (Process process = new Process { StartInfo = psi })
            {
                process.Start();

                // 读取标准输出（例如 "The current date is: ..."）
                string output = process.StandardOutput.ReadToEnd();

                // 读取错误输出（例如 "系统无法接受输入的日期"）
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                Console.WriteLine("标准输出: " + output);
                Console.WriteLine("错误输出: " + error);
                Console.WriteLine("退出代码: " + process.ExitCode);  // 0 表示成功

                msg = output + error;
                return process.ExitCode == 0 ? true : false;
            }
        }
        public static bool SetLocalTime(out string msg)
        {
            // 获取当前时间
            DateTime currentTime = DateTime.UtcNow;
            DateTime licenseValidTime = new DateTime(2023, 12, 2, currentTime.Hour, currentTime.Minute, currentTime.Second, currentTime.Millisecond, DateTimeKind.Local);
            TimeSpan timeSpan = currentTime - licenseValidTime;

            // 计算修改后的时间
            DateTime newTime = currentTime.AddDays(-timeSpan.Days);
            bool isSuccess = SetLocalDateTime(newTime, out msg);

            if (isSuccess)
            {
                g_addDays = timeSpan.Days;
                Console.WriteLine("修改时间: 成功");
            }
            else
            {
                Console.WriteLine("修改时间: 失败");
            }
            return isSuccess;
        }
        public static bool RestoreLocalTime(out string msg)
        {
            msg = string.Empty;
            if (g_addDays == 0)
            {
                return true;
            }
            // 获取当前时间
            DateTime currentTime = DateTime.UtcNow;

            // 计算修改后的时间
            DateTime newTime = currentTime.AddDays(-g_addDays);
            bool isSuccess = SetLocalDateTime(newTime, out msg);

            if (isSuccess)
            {
                g_addDays = 0;
                Console.WriteLine("还原时间: 成功");
            }
            else
            {
                Console.WriteLine("还原时间: 失败");
            }
            return isSuccess;
        }
        public static DateTime GetLocalRealTime()
        {
            return g_localRealTime;
        }
    }
}
