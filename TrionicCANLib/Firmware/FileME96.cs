using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace TrionicCANLib.Firmware
{
    public class FileME96 : IBaseFile
    {
        static public uint Length = 0x200000;

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
    }
}
