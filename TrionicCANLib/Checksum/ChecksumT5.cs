using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NLog;
using TrionicCANLib;
using TrionicCANLib.Firmware;
using System.Windows.Forms;

namespace TrionicCANLib.Checksum
{
    public class ChecksumT5
    {
        static private Logger logger = LogManager.GetCurrentClassLogger();

        private static byte[,] T5markers = new byte[2, 4]
        {
            {0x00, 0x07, 0xED, 0x2A}, // T5.2 marker (Scan from 1FF90 and down)
            {0x4E, 0xFA, 0xFB, 0xCC}  // T5.5 marker (Scan from 3FF90 and down)
        };

        public static ChecksumResult VerifyChecksum(string filename)
        {
            FileInfo fi     = new FileInfo(filename);
            long len        = fi.Length;

            // Verify file lenth
            if (len != 0x20000 && len != 0x40000)
                return ChecksumResult.InvalidFileLength;

            byte[] Localbuffer = FileTools.readdatafromfile(filename, 0, (int) len);

            uint ReadSum = (uint)(
                Localbuffer[len - 4] << 24 | Localbuffer[len - 3] << 16 |
                Localbuffer[len - 2] <<  8 | Localbuffer[len - 1]);

            // Console.WriteLine("Read checksum: " + ReadSum.ToString("X8"));

            // Check normal Trionic 5.2
            if (len == 0x20000)
            {
                if (!ChecksumMatch(filename, ReadSum, 0, FindEndmarker(filename, 0)))
                    return ChecksumResult.Invalid;
            }
            else
            {
                // Console.WriteLine("Calculating as normal T5.5");
                if (!ChecksumMatch(filename, ReadSum, 0, FindEndmarker(filename, 1)))
                {
                    // Console.WriteLine("Fail!");
                    // Console.WriteLine("Calculating as doubled T5.2");
                    if (!ChecksumMatch(filename, ReadSum, 0x20000, FindEndmarker(filename, 0)))
                        return ChecksumResult.Invalid;
                }
            }
            
            return ChecksumResult.Ok;
        }

        private static bool ChecksumMatch(string filename, uint Sum, uint Start, uint End)
        {
            FileInfo fi = new FileInfo(filename);
            byte[] Bufr = FileTools.readdatafromfile(filename, (int)Start, (int)(End - Start));
            uint CalcS  = 0;

            for (uint i = 0; i < (End - Start); i++)
            {
                CalcS += Bufr[i];
            }

            if (Sum == CalcS)
                return true;

            return false;
        }

        private static uint FindEndmarker(string filename, byte index)
        {
            FileInfo fi = new FileInfo(filename);
            uint len    = (uint)fi.Length;
            byte[] Bufr = FileTools.readdatafromfile(filename, 0, (int)len);

            for (uint i = len - 112; i > 3; i--)
            {
                if (T5markers[index, 3] == Bufr[  i  ] && T5markers[index, 2] == Bufr[i - 1] &&
                    T5markers[index, 1] == Bufr[i - 2] && T5markers[index, 0] == Bufr[i - 3])
                {
                    // Console.WriteLine("Found marker at: " + i.ToString("X10"));
                    return i + 1;
                }
            }
            // Console.WriteLine("NO MARKER !!!");
            return 0;
        }
    }
}
