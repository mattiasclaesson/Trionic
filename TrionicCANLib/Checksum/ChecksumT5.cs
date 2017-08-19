using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NLog;
using TrionicCANLib;
using TrionicCANLib.Firmware;
using System.Windows.Forms;

namespace TrionicCANLib.Checksum
{
    public class ChecksumT5
    {
        static private Logger logger = LogManager.GetCurrentClassLogger();

        public static ChecksumResult VerifyChecksum(string filename)
        {
            // Implement me!
            return ChecksumResult.Ok;
        }
    }
}
