using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace TrionicCANLib.Firmware
{
    public class FileME96 : BaseFile
    {
        public const uint Length = 0x200000;
        public const uint LengthComplete = 0x280000;

        public const uint MainOSAddress = 0x40000;
        public const uint EngineCalibrationAddress = 0x1C2000;
        public const uint EngineCalibrationAddressEnd = 0x1E0000;
        public const uint VersionOffset = 5;

        private const byte[] expectedBegin = {0x48, 0x01, 0x10, 0xF2, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x03, 0xC6, 0x00, 0x00, 0x00, 0x00};
        private const byte[] filled = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

        static public string getFileInfo(string filename)
        {
            byte[] filebytes = File.ReadAllBytes(filename);
            string damosinfo = string.Empty;

            // find damos info indicator
            for (int i = 0; i < filebytes.Length; i++)
            {
                if (i < filebytes.Length - 8)
                {
                    if (filebytes[i] == 'M' && filebytes[i + 1] == 'E' && filebytes[i + 2] == '9' && filebytes[i + 3] == '.' && filebytes[i + 4] == '6')
                    {
                        for (int j = 0; j < 44; j++) damosinfo += Convert.ToChar(filebytes[i - 5 + j]);
                        break;
                    }
                }
            }
            return damosinfo;
        }

        static public bool hasBootloader(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    byte[] read = br.ReadBytes(16);
                    if (read.AsEnumerable().SequenceEqual(expectedBegin))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static public string getMainOSVersion(string filename)
        {
            string version = string.Empty;
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(MainOSAddress + VersionOffset, SeekOrigin.Begin); //Example 55566563
                using (BinaryReader br = new BinaryReader(fs))
                {
                    byte[] read = br.ReadBytes(8);
                    if (read.AsEnumerable().SequenceEqual(filled))
                    {
                        return string.Empty;
                    }
                    version = System.Text.Encoding.Default.GetString(read);
                }
            }

            return version;
        }

        static public string getEngineCalibrationVersion(string filename)
        {
            string version = string.Empty;
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(EngineCalibrationAddress + VersionOffset, SeekOrigin.Begin); //Example 55569071
                using (BinaryReader br = new BinaryReader(fs))
                {
                    byte[] read = br.ReadBytes(8);
                    if (read.AsEnumerable().SequenceEqual(filled))
                    {
                        return string.Empty;
                    }
                    version = System.Text.Encoding.Default.GetString(read);
                }
            }

            return version;
        }

        static public new bool VerifyFileSize(long size)
        {
            return size == Length || size == LengthComplete;
        }
    }
}
