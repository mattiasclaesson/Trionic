using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using log4net;
using System.IO;
using log4net.Config;
using TrionicCANLib.Properties;

namespace TrionicCANFlasher
{
    internal class LogHelper
    {
        static LogHelper()
        {
            XmlConfigurator.Configure(new MemoryStream(TrionicCANFlasher.Properties.Resources.log4net_config));
        }

        internal static ILog GetUILog()
        {
            return LogManager.GetLogger("UILog");
        }
    }
}
