using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TrionicCANLib.CAN;
using NLog;

namespace TrionicCANLib.API
{
    abstract public class ITrionic
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        protected ICANDevice canUsbDevice;

        public delegate void WriteProgress(object sender, WriteProgressEventArgs e);
        public event ITrionic.WriteProgress onWriteProgress;

        public delegate void ReadProgress(object sender, ReadProgressEventArgs e);
        public event ITrionic.ReadProgress onReadProgress;

        public delegate void CanInfo(object sender, CanInfoEventArgs e);
        public event ITrionic.CanInfo onCanInfo;

        public delegate void CanFrame(object sender, CanFrameEventArgs e);
        public event ITrionic.CanFrame onCanFrame;

        // implements functions for canbus access for Trionic 8
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint MM_BeginPeriod(uint uMilliseconds);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint MM_EndPeriod(uint uMilliseconds);

        protected int m_sleepTime = (int)SleepTime.Default;

        public SleepTime Sleeptime
        {
            get { return (SleepTime)m_sleepTime; }
            set { m_sleepTime = (int)value; }
        }

        protected int m_forcedBaudrate = 0;
        public int ForcedBaudrate
        {
            get
            {
                return m_forcedBaudrate;
            }
            set
            {
                m_forcedBaudrate = value;
            }
        }

        public bool isOpen()
        {
            if (canUsbDevice != null)
            {
                return canUsbDevice.isOpen();
            }
            return false;
        }

        protected bool m_OnlyPBus = false;

        public bool OnlyPBus
        {
            get { return m_OnlyPBus; }
            set { m_OnlyPBus = value; }
        }

        protected bool m_DisableCanConnectionCheck = false;

        public bool DisableCanConnectionCheck
        {
            get { return m_DisableCanConnectionCheck; }
            set { m_DisableCanConnectionCheck = value; }
        }

        abstract public void setCANDevice(CANBusAdapter adapterType);

        abstract public void Cleanup();

        public float GetADCValue(uint channel)
        {
            return canUsbDevice.GetADCValue(channel);
        }

        public float GetThermoValue()
        {
            return canUsbDevice.GetThermoValue();
        }

        public static string[] GetAdapterNames(CANBusAdapter adapterType)
        {
            try
            {
                if (adapterType == CANBusAdapter.LAWICEL)
                {
                    return CANUSBDevice.GetAdapterNames();
                }
                else if (adapterType == CANBusAdapter.ELM327)
                {
                    return CANELM327Device.GetAdapterNames();
                }
                else if (adapterType == CANBusAdapter.JUST4TRIONIC)
                {
                    return Just4TrionicDevice.GetAdapterNames();
                }
                else if (adapterType == CANBusAdapter.KVASER)
                {
                    return KvaserCANDevice.GetAdapterNames();
                }
                else if (adapterType == CANBusAdapter.MXWIFI)
                {
                    return CANMXWiFiDevice.GetAdapterNames();
                }
            }
            catch(Exception ex)
            {
                logger.Debug("Failed to get adapternames", ex);
            }
            return new string[0];
        }

        abstract public void SetSelectedAdapter(string adapter);

        protected void CastProgressWriteEvent(int percentage)
        {
            if (onWriteProgress != null)
            {
                onWriteProgress(this, new WriteProgressEventArgs(percentage));
            }
        }

        protected void CastProgressReadEvent(int percentage)
        {
            if (onReadProgress != null)
            {
                onReadProgress(this, new ReadProgressEventArgs(percentage));
            }
        }

        protected void CastInfoEvent(string info, ActivityType type)
        {
            Console.WriteLine(info);
            if (onCanInfo != null)
            {
                onCanInfo(this, new CanInfoEventArgs(info, type));
            }
        }

        protected void CastFrameEvent(CANMessage message)
        {
            Console.WriteLine(message);
            if (onCanFrame != null)
            {
                onCanFrame(this, new CanFrameEventArgs(message));
            }
        }

        public class CanInfoEventArgs : System.EventArgs
        {
            private ActivityType _type;

            public ActivityType Type
            {
                get { return _type; }
                set { _type = value; }
            }

            private string _info;

            public string Info
            {
                get { return _info; }
                set { _info = value; }
            }

            public CanInfoEventArgs(string info, ActivityType type)
            {
                _info = info;
                _type = type;
            }
        }

        public class CanFrameEventArgs : System.EventArgs
        {
            private CANMessage _message;

            public CANMessage Message
            {
                get { return _message; }
                set { _message = value; }
            }

            public CanFrameEventArgs(CANMessage message)
            {
                _message = message;
            }
        }

        public class WriteProgressEventArgs : System.EventArgs
        {
            private int _percentage;

            private int _bytestowrite;

            public int Bytestowrite
            {
                get { return _bytestowrite; }
                set { _bytestowrite = value; }
            }

            private int _byteswritten;

            public int Byteswritten
            {
                get { return _byteswritten; }
                set { _byteswritten = value; }
            }

            public int Percentage
            {
                get { return _percentage; }
                set { _percentage = value; }
            }

            public WriteProgressEventArgs(int percentage)
            {
                _percentage = percentage;
            }

            public WriteProgressEventArgs(int percentage, int bytestowrite, int byteswritten)
            {
                _bytestowrite = bytestowrite;
                _byteswritten = byteswritten;
                _percentage = percentage;
            }
        }

        public class ReadProgressEventArgs : System.EventArgs
        {
            private int _percentage;

            public int Percentage
            {
                get { return _percentage; }
                set { _percentage = value; }
            }

            public ReadProgressEventArgs(int percentage)
            {
                _percentage = percentage;
            }
        }
    }
}
