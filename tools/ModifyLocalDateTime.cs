﻿using System;
using System.Collections.Generic;
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

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetSystemTime(ref Systemtime st);

        private static int g_addDays = 0;
        private static bool SetLocalDateTime(DateTime newTime)
        {
            Systemtime st;
            st.year = (short)newTime.Year;
            st.month = (short)newTime.Month;
            st.dayOfWeek = (short)newTime.DayOfWeek;
            st.day = (short)newTime.Day;
            st.hour = (short)newTime.Hour;
            st.minute = (short)newTime.Minute;
            st.second = (short)newTime.Second;
            st.milliseconds = (short)newTime.Millisecond;

            //转化后的时间进行本地设置，并返回成功与否
            return SetSystemTime(ref st);
        }
        public static bool SetLocalTime()
        {
            // 获取当前时间
            DateTime currentTime = DateTime.UtcNow;
            DateTime licenseValidTime = new DateTime(2024, 1, 2, currentTime.Hour, currentTime.Minute, currentTime.Second, currentTime.Millisecond, DateTimeKind.Local);
            TimeSpan timeSpan = currentTime - licenseValidTime;

            // 计算修改后的时间
            DateTime newTime = currentTime.AddDays(-timeSpan.Days);
            bool isSuccess = SetLocalDateTime(newTime);

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
        public static bool RestoreLocalTime()
        {
            if (g_addDays == 0)
            {
                return true;
            }
            // 获取当前时间
            DateTime currentTime = DateTime.UtcNow;

            // 计算修改后的时间
            DateTime newTime = currentTime.AddDays(-g_addDays);
            bool isSuccess = SetLocalDateTime(newTime);

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
    }
}