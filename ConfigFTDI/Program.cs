using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace SetupFTDI
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                using (RegistryKey FTDIBUSKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum\\FTDIBUS"))
                {
                    if (FTDIBUSKey != null)
                    {
                        string[] vals = FTDIBUSKey.GetSubKeyNames();
                        foreach (string name in vals)
                        {
                            if (name.StartsWith("VID_0403+PID_6001"))
                            {
                                using (RegistryKey NameKey = FTDIBUSKey.OpenSubKey(name + "\\0000\\Device Parameters"))
                                {
                                    String PortName = NameKey.GetValue("PortName").ToString();
                                    if (args[0].Equals(PortName))
                                    {
                                        String Latency = NameKey.GetValue("LatencyTimer").ToString();
                                        if (!Latency.Equals("2"))
                                        {
                                            NameKey.SetValue("LatencyTimer", "2");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
