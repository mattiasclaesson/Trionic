using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using log4net;
using System.IO;
using log4net.Config;
using TrionicCANLib.Properties;

namespace TrionicCANLib.Log
{
    internal class LogHelper
    {
        static LogHelper()
        {
            XmlConfigurator.Configure(new MemoryStream(Resources.log4net_config));
        }

        internal static ILog GetCanLog()
        {
            return LogManager.GetLogger("CanLog");
        }

        internal static ILog GetSerialLog()
        {
            return LogManager.GetLogger("SerialLog");
        }
    }
}
