using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrionicCANLib.Checksum
{
    public class ChecksumDelegate
    {
        public delegate bool ChecksumUpdate(string layer, string filechecksum, string realchecksum);
    }
}
