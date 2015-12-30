using System;
using System.Collections.Generic;
using System.Linq;

namespace TrionicCANLib.API
{
    public class FlashReadArguments
    {
        public string FileName { get; set; }
        public int start { get; set; }
        public int end { get; set; }
    }
}
