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
        
        /// <summary>
        /// Validates a binary before flashing.
        /// </summary>
        /// <param name="filename">filename to flash</param>
        /// <returns>ChecksumResult.Ok if checksum is correct</returns>
        public static ChecksumResult VerifyChecksum(string filename)
        {
            FileInfo fi = new FileInfo(filename);
            long len = fi.Length;

            // Verify file length.
            if (len != FileT5.LengthT52 && len != FileT5.LengthT55)
                return ChecksumResult.InvalidFileLength;

            // Store file in a local buffer.
            byte[] Bufr = FileTools.readdatafromfile(filename, 0, (int)len);

            return ChecksumMatch(Bufr, (uint)len) ? ChecksumResult.Ok : ChecksumResult.Invalid;
        }
        
        
        /// <summary>
        /// Converts one ASCII character into one nibble.
        /// </summary>
        /// <param name="In">One ASCII character</param>
        /// <returns>One nibble in binary format</returns>
        private static int ToNibble(char In)
        {
            if (In > 0x29 && In < 0x40)          // 0 - 9
                return In & 0xF;
            else if ((In > 0x40 && In < 0x47) || // A - F
                     (In > 0x60 && In < 0x67))   // a - f
                return (In + 9) & 0xF;
            else
                return -1; // Not ASCII!
        }

        /// <summary>
        /// Verifies whether a binary has the correct checksum or not.
        /// </summary>
        /// <param name="Bufr">A buffered copy of the binary</param>
        /// <param name="Len">Length of passed buffer</param>
        /// <returns>Checksum correct: true or false</returns>
        private static bool ChecksumMatch(byte[] Bufr, uint Len)
        {
            uint Loc = Len - 5; // Current location in file.
            uint End = 0x00000; // Last used address.  

            // Last four bytes contains a Checksum. Store those for further processing.
            uint StoredChecksum = (uint)(
                Bufr[Len - 4] << 24 | Bufr[Len - 3] << 16 |
                Bufr[Len - 2] <<  8 | Bufr[Len - 1]);

            // Attempt to find the 0xFE container.
            while (Bufr[Loc - 1] != 0xFE)
            {
                Loc -= ((uint)(Bufr[Loc]) + 2);

                // No binary has this little data; Abort!
                if (Loc < (Len / 2))
                {
                    logger.Debug("Could not find container!");
                    return false;
                }
            }

            // Store length of end-marker string.
            int MrkL = Bufr[Loc];

            // Abort if string is longer than 8 chars.
            if (MrkL > 8)
            {
                logger.Debug("Marker too long!");
                return false;
            }

            // Convert ASCII string into usable data.
            for (int i = MrkL; i > 0; i--)
            {
                int Nibble = ToNibble((char)Bufr[(Loc - (MrkL - i)) - 2]);

                // Abort if non-ASCII characters are read!
                if (Nibble == -1)
                {
                    logger.Debug("Read invalid data!");
                    return false;
                }

                // Bitshift result into the correct location.
                End |= (uint)Nibble << ((i - 1) * 4);
            }

            // Convert from physical address to file address.
            End -= (0x7FFFF - Len);

            // Do not checksum outside of binary.
            if (End > Len - 7)
            {
                logger.Debug("Pointer outside of binary!");
                return false;
            }

            // Subtract byte from the checksum to, hopefully, end up with 0 as the total sum.
            for (uint i = 0; i < End; i++)
                StoredChecksum -= (byte)Bufr[i];

            return StoredChecksum == 0 ? true : false;
        }
        
        /// <summary>
        /// Validates a dumped binary.
        /// </summary>
        /// <param name="Bufr">buffered copy of current binary</param>
        /// <param name="IsT55">true for trionic 5.5, false for 5.2</param>
        /// <returns>Checksum match: true or false</returns>
        public static bool ValidateDump(byte[] Bufr, bool IsT55)
        {
            return ChecksumMatch(Bufr, IsT55 ? FileT5.LengthT55 : FileT5.LengthT52);
        }
    }
}
