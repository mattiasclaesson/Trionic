using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using log4net;
using System.IO;
using log4net.Config;
using TrionicCANLib.Properties;
using System.Threading;
using System.Collections;

namespace TrionicCANLib.Log
{
    internal class LogHelper
    {
        static Thread logThread;
        static ILog canLog;
        static ILog serialLog;
        static LogQueue<LogEntry> logQueue;
        static bool isRunning = false;

        static LogHelper()
        {   
            logQueue = new LogQueue<LogEntry>();
            logThread = new Thread(LogMain);
            logThread.Priority = ThreadPriority.BelowNormal;
            logThread.Start();
        }

        static void LogMain()
        {
            while (true)
            {
                var logItem = logQueue.Dequeue();
                if (logItem != null)
                {
                    if (!isRunning)
                    {
                        lock (logThread)
                        {
                            XmlConfigurator.Configure(new MemoryStream(Resources.log4net_config));
                            isRunning = true;
                        }
                    }
                    if (logItem.type == LogType.Can)
                        GetCanLog().Info(logItem);

                    else if (logItem.type == LogType.Serial)
                        GetSerialLog().Info(logItem);
                }
            }
        }

        private static ILog GetCanLog()
        {
            if (canLog == null)
                canLog = LogManager.GetLogger("CanLog");
            return canLog;
        }

        private static ILog GetSerialLog()
        {
            if (serialLog == null)
                serialLog = LogManager.GetLogger("SerialLog");
            return serialLog;
        }

        internal static void LogDebug(string info)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debug.WriteLine(info);
            }
        }

        internal static void LogCan(string info)
        {
            logQueue.Enqueue(new LogEntry { type = LogType.Can, msg = info });
        }

        internal static void LogSerial(string info)
        {
            logQueue.Enqueue(new LogEntry { type = LogType.Serial, msg = info });
        }

        internal static void Flush()
        {
            lock (logThread)
            {
                LogManager.Shutdown();
                isRunning = false;
            }
        }

        private class LogEntry
        {
            public LogType type;
            public string msg;
            DateTime time;

            public LogEntry()
            {
                time = DateTime.Now;
            }

            public override string ToString()
            {
                return string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} - {1}", time, msg);
            }
        }

        private enum LogType
        {
            Can, Serial
        }
    }
}
