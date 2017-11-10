using System;
using System.Collections.Generic;
using System.Linq;

namespace TrionicCANLib.Checksum
{
    public enum ChecksumResult : int
    {
        Ok,
        InvalidFileLength,
        Layer1Failed,
        Layer2Failed,
        Invalid,
        UpdateFailed
    };
}
