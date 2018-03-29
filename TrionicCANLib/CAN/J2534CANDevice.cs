using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using NLog;
using J2534DotNet;

namespace TrionicCANLib.CAN
{
    /// <summary>
    /// All incomming messages are published to registered ICANListeners.
    /// </summary>
    /// 
    public class J2534CANDevice : ICANDevice
    {
        private readonly static Logger logger = LogManager.GetCurrentClassLogger();

        bool m_deviceIsOpen = false;
        Thread m_readThread;
        readonly Object m_synchObject = new Object();
        bool m_endThread;

        private int m_forcedBaudrate = 38400;

        readonly J2534Extended passThru = new J2534Extended();
        static List<J2534Device> availableJ2534Devices;
        int m_deviceId;
        int m_channelId;
        J2534Err m_status;

        public override int ForcedBaudrate
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

        private bool m_filterBypass = false;
        public override bool bypassCANfilters
        {
            get
            {
                return m_filterBypass;
            }
            set
            {
                m_filterBypass = value;
            }
        }

        // not supported by J2534
        public override float GetADCValue(uint channel)
        {
            return 0F;
        }

        // not supported by J2534
        public override float GetThermoValue()
        {
            return 0F;
        }

        public static new string[] GetAdapterNames()
        {
            // Find all of the installed J2534 passthru devices
            availableJ2534Devices = J2534Detect.ListDevices();

            // List available devices
            string[] all = new string[availableJ2534Devices.Count];
            List<string> names = new List<string>();
            for (int i = 0; i < availableJ2534Devices.Count; i++)
            {
                if (availableJ2534Devices[i].IsCANSupported)
                {
                    names.Add(availableJ2534Devices[i].Name);
                    logger.Debug(String.Format("Found device with CAN support {0}", availableJ2534Devices[i].Name));
                }
                else
                {
                    logger.Debug(String.Format("Skipped device without CAN support {0}", availableJ2534Devices[i].Name));
                }
            }
            return names.ToArray();
        }

        public override void SetSelectedAdapter(string adapter)
        {
            J2534Device selected = availableJ2534Devices.Find(x => x.Name == adapter);
            passThru.LoadLibrary(selected);
        }

        /// <summary>
        /// readMessages is the "run" method of this class. It reads all incomming messages
        /// and publishes them to registered ICANListeners.
        /// </summary>
        public void readMessages()
        {
            uint id;
            int numMsgs = 1;
            const int timeout = 1000;
            CANMessage canMessage = new CANMessage();
            logger.Debug("readMessages started");
            while (true)
            {
                lock (m_synchObject)
                {
                    if (m_endThread)
                    {
                        logger.Debug("readMessages thread ended");
                        return;
                    }
                }
                IntPtr rxMsgs = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(PassThruMsg)));
                m_status = passThru.PassThruReadMsgs(m_channelId, rxMsgs, ref numMsgs, timeout);
                if (m_status == J2534Err.STATUS_NOERROR)
                {
                    if (numMsgs > 0)
                    {
                        PassThruMsg msg = rxMsgs.AsMsgList(numMsgs)[0];

                        byte[] all = msg.GetBytes();
                        id = (uint)(all[2] * 0x100 + all[3]);
                        uint length = msg.DataSize-4;
                        byte[] data = new byte[length];
                        Array.Copy(all, 4, data, 0, length);
                        
                        if (acceptMessageId(id))
                        {
                            canMessage.setID(id);
                            canMessage.setTimeStamp(msg.Timestamp);
                            canMessage.setCanData(data, (byte)(length));

                            receivedMessage(canMessage);
                        }
                    }
                }
                else
                {
                    logger.Debug(String.Format("PassThruReadMsgs, status:{0}", m_status));
                }
                Marshal.FreeHGlobal(rxMsgs);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns>OpenResult.OK is returned on success. Otherwise OpenResult.OpenError is
        /// returned.</returns>
        override public OpenResult open()
        {
            if (isOpen())
            {
                close();
            }

            m_deviceId = 0;
            m_status = passThru.PassThruOpen(IntPtr.Zero, ref m_deviceId);
            if (m_status != J2534Err.STATUS_NOERROR)
            {
                return OpenResult.OpenError;
            }

            m_readThread = new Thread(readMessages) { Name = "J2534CANDevice.m_readThread" };
            m_endThread = false;

            if (TrionicECU == API.ECU.TRIONIC5)
            {
                m_status = passThru.PassThruConnect(m_deviceId, ProtocolID.CAN, ConnectFlag.NONE, BaudRate.CAN_615000, ref m_channelId);
            }
            else
            {
                m_status = passThru.PassThruConnect(m_deviceId, ProtocolID.CAN, ConnectFlag.NONE, BaudRate.CAN_500000, ref m_channelId);
            }
            if (J2534Err.STATUS_NOERROR != m_status)
            {
                return OpenResult.OpenError;
            }

            uint acpFilt = 0xFFFF;
            uint acpMask = 0x0000;

            foreach (var id in AcceptOnlyMessageIds)
            {
                acpFilt &= id;
                acpMask |= id;
            }
            acpMask = (~acpMask & 0x7FF) | acpFilt;

            logger.Debug("Filter: " + acpFilt.ToString("X8"));
            logger.Debug("Mask:   " + acpMask.ToString("X8"));

            byte[] maskBytes = new byte[4];
            byte[] patternBytes = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                maskBytes[i] = (byte)(acpMask >> (i * 8));
                patternBytes[i] = (byte)(acpFilt >> (i * 8));
            }

            //PassThruMsg maskMsg    = new PassThruMsg(ProtocolID.CAN, TxFlag.NONE, maskBytes);
            //PassThruMsg patternMsg = new PassThruMsg(ProtocolID.CAN, TxFlag.NONE, patternBytes);
            PassThruMsg maskMsg = new PassThruMsg(ProtocolID.CAN, TxFlag.NONE, new byte[] { 0x00, 0x00, 0x00, 0x00 });
            PassThruMsg patternMsg = new PassThruMsg(ProtocolID.CAN, TxFlag.NONE, new byte[] { 0x00, 0x00, 0x00, 0x00 });
            int filterId = 0;
            m_status = passThru.PassThruStartMsgFilter(
                m_channelId,
                FilterType.PASS_FILTER,
                maskMsg.ToIntPtr(),
                patternMsg.ToIntPtr(),
                IntPtr.Zero,
                ref filterId);
            if (J2534Err.STATUS_NOERROR != m_status)
            {
                return OpenResult.OpenError;
            }

            m_status = passThru.PassThruIoctl(m_channelId, (int)Ioctl.CLEAR_RX_BUFFER, IntPtr.Zero, IntPtr.Zero);
            if (J2534Err.STATUS_NOERROR != m_status)
            {
                return OpenResult.OpenError;
            }

            logger.Debug("P bus connected");
            if (m_readThread.ThreadState == ThreadState.Unstarted)
            {
                m_readThread.Start();
            }
            m_deviceIsOpen = true;
            return OpenResult.OK;
        }

        /// <summary>
        /// The close method closes the device.
        /// </summary>
        /// <returns>CloseResult.OK on success, otherwise CloseResult.CloseError.</returns>
        override public CloseResult close()
        {
            if (m_deviceIsOpen)
            {
                m_deviceIsOpen = false;
                m_endThread = true;

                Thread.Sleep(1200);

                m_status = passThru.PassThruDisconnect(m_channelId);
                m_status = passThru.PassThruClose(m_deviceId);
                if (m_status != J2534Err.STATUS_NOERROR)
                {
                    return CloseResult.CloseError;
                }

                passThru.FreeLibrary();
            }
            return CloseResult.OK;
        }

        /// <summary>
        /// isOpen checks if the device is open.
        /// </summary>
        /// <returns>true if the device is open, otherwise false.</returns>
        override public bool isOpen()
        {
            return m_deviceIsOpen;
        }

        /// <summary>
        /// sendMessage send a CANMessage.
        /// </summary>
        /// <param name="a_message">A CANMessage.</param>
        /// <returns>true on success, othewise false.</returns>
        override protected bool sendMessageDevice(CANMessage a_message)
        {
            if (m_endThread)
            {
                return false;
            }
            byte[] msg = a_message.getHeaderAndData();
            
            PassThruMsg txMsg = new PassThruMsg();
            txMsg.ProtocolID = ProtocolID.CAN;
            txMsg.TxFlags = TxFlag.NONE;
            txMsg.SetBytes(msg);

            int numMsgs = 1;
            const int timeout = 0;
            m_status = passThru.PassThruWriteMsgs(m_channelId, txMsg.ToIntPtr(), ref numMsgs, timeout);

            if (J2534Err.STATUS_NOERROR != m_status)
            {
                logger.Debug(String.Format("tx failed with status {0} {1}", m_status, BitConverter.ToString(msg)));
                return false;
            }
            return true;
        }

        /// <summary>
        /// waitForMessage waits for a specific CAN message give by a CAN id.
        /// </summary>
        /// <param name="a_canID">The CAN id to listen for</param>
        /// <param name="timeout">Listen timeout</param>
        /// <param name="r_canMsg">The CAN message with a_canID that we where listening for.</param>
        /// <returns>The CAN id for the message we where listening for, otherwise 0.</returns>
        public override uint waitForMessage(uint a_canID, uint timeout, out CANMessage canMsg)
        {
            canMsg = new CANMessage();
            return 0;
        }
    }
}
