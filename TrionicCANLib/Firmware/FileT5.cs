using System;
using System.Collections.Generic;
using System.Linq;

namespace TrionicCANLib.Firmware
{
    public class FileT5 : BaseFile
    {
        static public uint LengthT52 = 0x20000;
        static public uint LengthT55 = 0x40000;

        static public new bool VerifyFileSize(long size)
        {
            return size == LengthT52 || size == LengthT55;
        }
    }
}