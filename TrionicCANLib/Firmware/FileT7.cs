using System;
using System.Collections.Generic;
using System.Linq;

namespace TrionicCANLib.Firmware
{
    public class FileT7 : BaseFile
    {
        public const uint Length = 0x80000;

        static public new bool VerifyFileSize(long size)
        {
            return size == Length;
        }
    }
}
