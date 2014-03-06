using System;
using System.Collections.Generic;
using System.Text;

namespace TrionicCANLib
{
    public class ByteCoder
    {
        private int btcount = 0;
        private int cnt = 0;

        public void ResetCounter()
        {
            cnt = 0;
        }

        public byte codeByte(byte b)
        {
            byte rb = b;
            try
            {
                if (cnt == 0) rb = (byte)(b ^ 0x39);
                else if (cnt == 1) rb = (byte)(b ^ 0x68);
                else if (cnt == 2) rb = (byte)(b ^ 0x77);
                else if (cnt == 3) rb = (byte)(b ^ 0x6D);
                else if (cnt == 4) rb = (byte)(b ^ 0x47);
                else if (cnt == 5) rb = (byte)(b ^ 0x39);
            }
            catch (Exception)
            {

            }
            btcount++;
            cnt++;
            if (cnt == 6) cnt = 0;
            return rb;
        }
    }
}
