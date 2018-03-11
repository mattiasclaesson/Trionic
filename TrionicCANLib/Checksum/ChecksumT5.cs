using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NLog;
using TrionicCANLib.Firmware;
using System.Text;

namespace TrionicCANLib.Checksum
{
    public class ChecksumT5
    {
        static private Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Validates a binary before flashing.
        /// </summary>
        /// <param name="filename">filename to flash</param>
        /// <returns>ChecksumResult.Ok if checksum is correct</returns>
        public static ChecksumResult VerifyChecksum(string filename, bool autocorrect, ChecksumDelegate.ChecksumUpdate delegateShouldUpdate)
        {
            FileInfo fi = new FileInfo(filename);
            long len = fi.Length;
            uint End;
            uint Checksum;

            // Verify file length.
            if (len != FileT5.LengthT52 && len != FileT5.LengthT55)
                return ChecksumResult.InvalidFileLength;

            // Store file in a local buffer.
            byte[] Bufr = FileTools.readdatafromfile(filename, 0, (int)len);


            // No need for further checks if result is ok.
            if (ChecksumMatch(Bufr, (uint)len, out End, out Checksum) == true)
            {
                return ChecksumResult.Ok;
            }

            // Can't do anything if footer is broken.
            else if (End == 0)
            {
                return ChecksumResult.Invalid;
            }

            uint StoredChecksum = (uint)(
                Bufr[len - 4] << 24 | Bufr[len - 3] << 16 |
                Bufr[len - 2] <<  8 | Bufr[len - 1]);

            if (autocorrect == true)
            {
                Writefile(filename, Checksum, (uint)len);
                return ChecksumResult.Ok;
            }
            else
            {
                string CalculatedSum = Checksum.ToString("X");
                string ActualSum = StoredChecksum.ToString("X");

                if (delegateShouldUpdate(null, ActualSum, CalculatedSum))
                {
                    Writefile(filename, Checksum, (uint)len);
                    return ChecksumResult.Ok;
                }
            }

            return ChecksumResult.Invalid;
        }

        /// <summary>
        /// Writes an updated checksum to the binary.
        /// </summary>
        /// <param name="a_filename">Filename to open for writing.</param>
        /// <param name="Bufr">Actual buffer to be written.</param>
        /// <param name="Len">Length of buffer.</param>
        private static void Writefile(string a_filename, uint newChecksum, uint Len)
        {
            if (!File.Exists(a_filename))
                File.Create(a_filename);

            try
            {
                FileStream fs = new FileStream(a_filename, FileMode.Open, FileAccess.ReadWrite);
                fs.Seek(0, SeekOrigin.Begin);
                fs.Position = Len - 4;

                for (byte i = 0; i < 4; i++)
                {
                    fs.WriteByte((byte) (newChecksum >> ((3 - i) * 8)));
                }
                fs.Close();
            }

            catch (Exception E)
            {
                logger.Debug(E.Message);
            }
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
        /// Determine if it's even possible to extract the last used address from the footer area.
        /// </summary>
        /// <param name="Bufr"></param>
        /// <param name="Len"></param>
        /// <returns>Last used address or 0 if it can't be determined</returns>
        private static uint retLastAddress(byte[] Bufr, uint Len)
        {
            uint Loc = Len - 5; // Current location in file.
            uint End = 0x00000; // Last used address.  

            // Attempt to find the 0xFE container.
            while (Bufr[Loc - 1] != 0xFE)
            {
                Loc -= ((uint)(Bufr[Loc]) + 2);

                // No binary has this little data; Abort!
                if (Loc < (Len / 2))
                {
                    logger.Debug("Could not find container!");
                    return 0;
                }
            }

            // Store length of end-marker string.
            int MrkL = Bufr[Loc];

            // Abort if string is longer than 8 chars.
            if (MrkL > 8)
            {
                logger.Debug("Marker too long!");
                return 0;
            }

            // Convert ASCII string into usable data.
            for (int i = MrkL; i > 0; i--)
            {
                int Nibble = ToNibble((char)Bufr[(Loc - (MrkL - i)) - 2]);

                // Abort if non-ASCII characters are read!
                if (Nibble == -1)
                {
                    logger.Debug("Read invalid data!");
                    return 0;
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
                return 0;
            }

            return End;
        }

        /// <summary>
        /// Verifies whether a binary has the correct checksum or not.
        /// </summary>
        /// <param name="Bufr">A buffered copy of the binary</param>
        /// <param name="Len">Length of passed buffer</param>
        /// <returns>Checksum correct: true or false</returns>
        private static bool ChecksumMatch(byte[] Bufr, uint Len, out uint End, out uint Checksum)
        {
            Checksum = 0;

            // Last four bytes contains a Checksum. Store those for further processing.
            uint StoredChecksum = (uint)(
                Bufr[Len - 4] << 24 | Bufr[Len - 3] << 16 |
                Bufr[Len - 2] <<  8 | Bufr[Len - 1]);

            // Extract last used address from footer
            End = retLastAddress(Bufr, Len);

            // There is nothing that can be done if the footer is broken.
            if (End == 0)
            {
                return false;
            }

            // Calculate actual checksum
            for (uint i = 0; i < End; i++)
            {
                Checksum += (byte)Bufr[i];
            }

            return StoredChecksum == Checksum ? true : false;
        }
        
        /// <summary>
        /// Validates a dumped binary.
        /// </summary>
        /// <param name="Bufr">buffered copy of current binary</param>
        /// <param name="IsT55">true for trionic 5.5, false for 5.2</param>
        /// <returns>Checksum match: true or false</returns>
        public static bool ValidateDump(byte[] Bufr, bool IsT55)
        {
            uint csum;
            uint end;
            
            return ChecksumMatch(Bufr, IsT55 ? FileT5.LengthT55 : FileT5.LengthT52, out end, out csum);
        }
    }
}
