using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NLog;
using System.Windows.Forms;

namespace TrionicCANLib
{
    class FileTools
    {
        private readonly static Logger logger = LogManager.GetCurrentClassLogger();

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

        public static bool savedatatobinary(int address, int length, byte[] data, string filename)
        {
            if (address > 0 && address < Firmware.FileT8.Length) // TODO: mattias support other firmwares
            {
                try
                {
                    using (FileStream fsi1 = File.OpenWrite(filename))
                    {
                        BinaryWriter bw1 = new BinaryWriter(fsi1);
                        fsi1.Position = address;
                        for (int i = 0; i < length; i++)
                        {
                            bw1.Write((byte)data.GetValue(i));
                        }
                        fsi1.Flush();
                        bw1.Close();
                        fsi1.Close();
                        fsi1.Dispose();
                    }

                    // TODO: mattias add TransactionEntry in projectTransactionLog
                    // implement as event?

                    return true;
                }
                catch (Exception E)
                {
                    MessageBox.Show("Failed to write to binary. Is it read-only? Details: " + E.Message);
                    logger.Debug(E.Message);
                }
            }
            return false;
        }
    }
}
