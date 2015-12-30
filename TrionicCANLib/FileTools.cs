using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NLog;

namespace TrionicCANLib
{
    class FileTools
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static byte[] readdatafromfile(string filename, int address, int length)
        {
            if (length <= 0) return new byte[1];
            byte[] retval = new byte[length];
            try
            {
                using (FileStream fsi1 = File.OpenRead(filename))
                {
                    while (address > fsi1.Length) address -= (int)fsi1.Length;
                    BinaryReader br1 = new BinaryReader(fsi1);
                    fsi1.Position = address;
                    string temp = string.Empty;
                    for (int i = 0; i < length; i++)
                    {
                        retval.SetValue(br1.ReadByte(), i);
                    }
                    fsi1.Flush();
                    br1.Close();
                    fsi1.Close();
                    fsi1.Dispose();
                }
            }
            catch (Exception E)
            {
                logger.Debug(E.Message);
            }
            return retval;
        }
    }
}
