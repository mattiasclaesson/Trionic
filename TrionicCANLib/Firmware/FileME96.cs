using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace TrionicCANLib.Firmware
{
    public class FileME96 : IBaseFile
    {
        static public uint Length = 0x200000;
        static public uint LengthComplete = 0x280000;

        static private byte[] expectedBegin = {0x48, 0x01, 0x10, 0xF2, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x03, 0xC6, 0x00, 0x00, 0x00, 0x00};

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

        static public bool hasFirmwareContent(string filename)
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
    }
}
