using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace TrionicCANLib
{
    public class BootloaderFromFile
    {
        private byte[] m_Bootloader;
        const string filename = "bootloader";

        public BootloaderFromFile()
        {
            FileInfo fi = new FileInfo(filename);
            if (fi.Length == 0x4000)
            {
                m_Bootloader = File.ReadAllBytes(filename);
            }
        }

        public byte[] BootloaderBytes
        {
            get { return m_Bootloader; }
            set { m_Bootloader = value; }
        }

        public byte[] BootloaderProgBytes
        {
            get { return m_Bootloader; }
            set { m_Bootloader = value; }
        }

        

    }
}
