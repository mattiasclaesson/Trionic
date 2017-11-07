using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NLog;
using TrionicCANLib.Firmware;

namespace TrionicCANLib.Checksum
{
    public class ChecksumT5
    {
        private readonly static Logger logger = LogManager.GetCurrentClassLogger();

        private static byte[,] T5markers = new byte[2, 4]
        {
            {0x19, 0x0A, 0x4E, 0xF9}, // T5.2 marker (Scan from 1FF90 and down)
            {0x4E, 0xFA, 0xFB, 0xCC}  // T5.5 marker (Scan from 3FF90 and down)
        };

        // Trionic 5.2 markers in different bins:
        // 19 0A 4E F9 00 07 EA 90
        // 19 0A 4E F9 00 07 E9 EA
        // 19 0A 4E F9 00 07 ED 2A

        /// <summary>
        /// Validates a binary before flashing
        /// </summary>
        /// <param name="filename">filename to flash</param>
        /// <returns>ChecksumResult.Ok if checksum is correct</returns>
        public static ChecksumResult VerifyChecksum(string filename)
        {
            FileInfo fi = new FileInfo(filename);
            long len    = fi.Length;

            // Verify file length
            if (len != FileT5.LengthT52 && len != FileT5.LengthT55)
            {
                return ChecksumResult.InvalidFileLength;
            }

            byte[] Localbuffer = FileTools.readdatafromfile(filename, 0, (int) len);

            uint ReadSum = (uint)(
                Localbuffer[len - 4] << 24 | Localbuffer[len - 3] << 16 |
                Localbuffer[len - 2] <<  8 | Localbuffer[len - 1]);

            logger.Debug("Read checksum: " + ReadSum.ToString("X8"));
            logger.Debug("Determining index by size");
            if (!ChecksumMatch(Localbuffer, ReadSum, FindEndmarker(Localbuffer, (byte)(len>>18))))
            {
                logger.Debug("Trying doubled T5.2");
                if (!ChecksumMatch(Localbuffer, ReadSum, FindEndmarker(Localbuffer, 0)))
                {
                    return ChecksumResult.Invalid;
                }
            }
            
            return ChecksumResult.Ok;
        }

        /// <summary>
        /// Validates a dumped binary
        /// </summary>
        /// <param name="Bufr">buffered copy of current binary</param>
        /// <param name="IsT55">true for trionic 5.5, false for 5.2</param>
        /// <returns>Checksum match true or false</returns>
        public static bool ValidateDump(byte[] Bufr, bool IsT55)
        {
            long len = IsT55 ? FileT5.LengthT55 : FileT5.LengthT52;
            uint ReadSum = (uint)(
                Bufr[len - 4] << 24 | Bufr[len - 3] << 16 |
                Bufr[len - 2] <<  8 | Bufr[len - 1]);

            return ChecksumMatch(Bufr, ReadSum, FindEndmarker(Bufr, (byte)(len>>18)));
        }

        /// <summary>
        /// Verifies stored checksum against a calculated one
        /// </summary>
        /// <param name="Bufr">buffered copy of current binary</param>
        /// <param name="Sum">calculated checksum</param>
        /// <param name="End">where to end checksum32 calculations</param>
        /// <returns>Checksum match true or false</returns>
        private static bool ChecksumMatch(byte[] Bufr, uint Sum, uint End)
        {
            for (uint i = 0; i < End; i++)
            {
                Sum -= Bufr[i];
            }

            return (Sum & 0xFFFFFFFF) == 0 ? true : false;
        }

        /// <summary>
        /// Scans binary for an endmarker
        /// </summary>
        /// <param name="Bufr">buffered copy of current binary</param>
        /// <param name="index">1 for trionic 5.5, 0 for 5.2</param>
        /// <returns>Last address of binary + 1</returns>
        private static uint FindEndmarker(byte[] Bufr, byte index)
        {
            uint len = (index == 1 ? FileT5.LengthT55 : FileT5.LengthT52);

            for (uint i = (len - 112); i > 3; i--)
            {
                if (T5markers[index, 3] == Bufr[  i  ] && T5markers[index, 2] == Bufr[i - 1] &&
                    T5markers[index, 1] == Bufr[i - 2] && T5markers[index, 0] == Bufr[i - 3])
                {
                    return i + (uint)(index == 1? 1 : 5);
                }
            }

            logger.Debug("NO MARKER !!!");
            return 0;
        }
    }
}
