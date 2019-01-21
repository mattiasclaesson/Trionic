using System;
using System.Collections.Generic;
using System.Linq;

namespace TrionicCANLib.Firmware
{
    public class FileT8mcp : BaseFile
    {
        static public uint Length = 0x40100;

        static public new bool VerifyFileSize(long size)
        {
            return size == Length;
        }
    }
}
