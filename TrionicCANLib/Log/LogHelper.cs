using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using log4net;
using System.IO;
using log4net.Config;
using TrionicCANLib.Properties;
using System.Threading;
using System.Collections;
using System.Diagnostics;

namespace TrionicCANLib.Log
{
    internal class LogHelper
    {
        static Thread logThread;
        static ILog canLog;
        static ILog deviceLog;
        static ILog flasherLog;
        static ILog kwpLog;
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
                    {
                        GetCanLog().Info(logItem);
                    }
                    else if (logItem.type == LogType.Device)
                    {
                        GetDeviceLog().Info(logItem);
                    }
                    else if (logItem.type == LogType.Flasher)
                    {
                        GetFlasherLog().Info(logItem);
                    }
                    else if (logItem.type == LogType.Kwp)
                    {
                        GetKwpLog().Info(logItem);
                    }

                }
            }
        }

        private static ILog GetCanLog()
        {
            if (canLog == null)
                canLog = LogManager.GetLogger("CanLog");
            return canLog;
        }

        private static ILog GetDeviceLog()
        {
            if (deviceLog == null)
                deviceLog = LogManager.GetLogger("DeviceLog");
            return deviceLog;
        }

        private static ILog GetFlasherLog()
        {
            if (flasherLog == null)
                flasherLog = LogManager.GetLogger("FlasherLog");
            return flasherLog;
        }

        private static ILog GetKwpLog()
        {
            if (kwpLog == null)
                kwpLog = LogManager.GetLogger("KwpLog");
            return kwpLog;
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

        internal static void LogDevice(string info)
        {
            logQueue.Enqueue(new LogEntry { type = LogType.Device, msg = info });
        }

        internal static void LogFlasher(string info)
        {
            logQueue.Enqueue(new LogEntry { type = LogType.Flasher, msg = info });
        }

        internal static void LogKwp(string info)
        {
            logQueue.Enqueue(new LogEntry { type = LogType.Kwp, msg = info });
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
            private static Stopwatch sw = new Stopwatch();
            static LogEntry()
            {
                sw.Start();
            }

            public LogType type;
            public string msg;
            DateTime time;
            TimeSpan ts;

            public LogEntry()
            {
                ts = sw.Elapsed;
                time = DateTime.Now;
            }

            public override string ToString()
            {
                return string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} - {1}", time, msg);
                //return string.Format("{0} - {1}",ts,msg);
            }
        }

        private enum LogType
        {
            Can, Device, Flasher, Kwp
        }
    }
}
