using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Debug.Debug
{
    class TestTemp
    {
		public static void ConversionFormula()
        {
			byte[] pread_buf = new byte[2];
			// para 1
			pread_buf[1] = 0xc9; pread_buf[0] = 0x20;//  0x649  -54.875
			short raw = (short)((pread_buf[1] << 8) | pread_buf[0]); //11bit
			sbyte realTemp = (sbyte)(raw / (8 * 32));
			Console.WriteLine("dev_u25_temp_read: raw[%#x], realTemp[%d]\n", raw, realTemp);

			// para 2
			pread_buf[1] = 0x7e; pread_buf[0] = 0x20;//126.125
			raw = (short)((pread_buf[1] << 8) | pread_buf[0]); //11bit
			realTemp = (sbyte)(raw / (8 * 32));
			Console.WriteLine("dev_u25_temp_read: raw[%#x], realTemp[%d]\n", raw, realTemp);
			pread_buf[0] = (byte)realTemp;
		}
    }
}
