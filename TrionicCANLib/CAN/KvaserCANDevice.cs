using System;
using System.Collections.Generic;
using System.Threading;
using canlibCLSNET;
using NLog;
using TrionicCANLib.API;

namespace TrionicCANLib.CAN
{
    /// <summary>
    /// All incomming messages are published to registered ICANListeners.
    /// </summary>
    /// 
    public class KvaserCANDevice : ICANDevice
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        int handleWrite = -1;
        int handleRead = -1;

        Canlib.canStatus writeStatus;

        public const byte CAN_BAUD_BTR_33K_btr0 = 0x8B; // 33 kbit/s SAAB GMLAN
        public const byte CAN_BAUD_BTR_33K_btr1 = 0x2F;
        public const byte CAN_BAUD_BTR_47K_btr0 = 0xcb; // 47,6 kbit/s SAAB T7 I-bus
        public const byte CAN_BAUD_BTR_47K_btr1 = 0x9a;
        public const byte CAN_BAUD_BTR_615K_btr0 = 0x40; // 615 kbit/s SAAB T5
        public const byte CAN_BAUD_BTR_615K_btr1 = 0x37;

        Thread m_readThread;
        readonly Object m_synchObject = new Object();
        bool m_endThread;

        private int m_forcedBaudrate = 38400;

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

        public int ChannelNumber { get; set; }

        // not supported by kvaser
        public override float GetADCValue(uint channel)
        {
            return 0F;
        }

        // not supported by kvaser
        public override float GetThermoValue()
        {
            return 0F;
        }

        public static new string[] GetAdapterNames()
        {
            Canlib.canInitializeLibrary();

            int nrOfChannels;
            Canlib.canGetNumberOfChannels(out nrOfChannels);
            List<string> names = new List<string>();
            object channelName = new object();
            object channelCapabilities = new object();
            for (int i = 0; i < nrOfChannels; i++)
            {
                Canlib.canGetChannelData(i, Canlib.canCHANNELDATA_CHANNEL_NAME, out channelName);
                Canlib.canGetChannelData(i, Canlib.canCHANNELDATA_CHANNEL_CAP, out channelCapabilities);

                uint capability = (uint)channelCapabilities;
                if ((capability & Canlib.canCHANNEL_CAP_VIRTUAL) != Canlib.canCHANNEL_CAP_VIRTUAL)
                {
                    names.Add(channelName.ToString());
                    logger.Debug(String.Format("Found channel {0}", channelName));
                }
                else
                {
                    logger.Debug(String.Format("Skipped channel {0}", channelName));
                }
            }
            return names.ToArray();
        }

        public override void SetSelectedAdapter(string adapter)
        {
            int nrOfChannels;
            Canlib.canGetNumberOfChannels(out nrOfChannels);
            object o = new object();
            for (int i = 0; i < nrOfChannels; i++)
            {
                Canlib.canGetChannelData(i, Canlib.canCHANNELDATA_CHANNEL_NAME, out o);
                if(adapter.Equals(o.ToString()))
                {
                    ChannelNumber = i;
                    logger.Debug(string.Format("canlibCLSNET.Canlib.canGetChannelData({0}, canlibCLSNET.Canlib.canCHANNELDATA_CHANNEL_NAME, {1})", i, o));
                    return;
                }
            }

            throw new Exception(String.Format("Channel {0} cannot be selected", adapter));
        }

        /// <summary>
        /// readMessages is the "run" method of this class. It reads all incomming messages
        /// and publishes them to registered ICANListeners.
        /// </summary>
        public void readMessages()
        {
            byte[] msg = new byte[8];
            int dlc;
            int flag, id;
            long time;
            Canlib.canStatus status;
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
                status = Canlib.canReadWait(handleRead, out id, msg, out dlc, out flag, out time, 250);
                if ((flag & Canlib.canMSG_ERROR_FRAME) == 0)
                {
                    if (status == Canlib.canStatus.canOK)
                    {
                        if (acceptMessageId((uint)id))
                        {
                            canMessage.setID((uint)id);
                            canMessage.setTimeStamp((uint)time);
                            canMessage.setFlags((byte)flag);
                            canMessage.setCanData(msg, (byte)dlc);

                            receivedMessage(canMessage);
                        }
                    }
                    else if (status == Canlib.canStatus.canERR_NOMSG)
                    {
                        Thread.Sleep(1);
                    }
                    else
                    {
                        logger.Debug("error" + status);
                    }
                }
                else
                {
                    logger.Debug("error frame");
                }
            }
        }

        /// <summary>
        /// The open method tries to connect to both busses to see if one of them is connected and
        /// active. The first strategy is to listen for any CAN message. If this fails there is a
        /// check to see if the application is started after an interrupted flash session. This is
        /// done by sending a message to set address and length (only for P-bus).
        /// </summary>
        /// <returns>OpenResult.OK is returned on success. Otherwise OpenResult.OpenError is
        /// returned.</returns>
        override public OpenResult open()
        {
            Canlib.canInitializeLibrary();

            if (isOpen())
            {
                close();
            }

            // Default to "Allow all"
            uint code = 0xFFF;
            uint mask = 0x000;

            // ID 0x000 will thrash the filter calculation so bypass the whole filter if it's found
            foreach (var id in AcceptOnlyMessageIds)
            {
                if (id == 0) { m_filterBypass = true; }
            }

            if (m_filterBypass == false)
            {
                foreach (var id in AcceptOnlyMessageIds)
                {
                    code &= id;
                    mask |= id;
                }

                mask = (~mask & 0x7FF) | code;

                logger.Debug("code: " + code.ToString("X8"));
                logger.Debug("mask:   " + mask.ToString("X8"));
            }

            VerifyFilterIntegrity(code, mask);

            Thread.Sleep(200);
            m_readThread = new Thread(readMessages) { Name = "KvaserCANDevice.m_readThread" };

            if (TrionicECU == ECU.TRIONIC5)
            {
                OpenChannelWithParamsC200(out handleWrite, CAN_BAUD_BTR_615K_btr0, CAN_BAUD_BTR_615K_btr1);
                OpenChannelWithParamsC200(out handleRead, CAN_BAUD_BTR_615K_btr0, CAN_BAUD_BTR_615K_btr1);
                Canlib.canSetAcceptanceFilter(handleRead, (int)code, (int)mask, 0);

                if (handleWrite < 0 || handleRead < 0)
                {
                    return OpenResult.OpenError;
                }

                logger.Debug("P bus connected");
                m_endThread = false;
                if (m_readThread.ThreadState == ThreadState.Unstarted)
                {
                    m_readThread.Start();
                }
                return OpenResult.OK;
            }

            if (!UseOnlyPBus)
            {
                if (TrionicECU == ECU.TRIONIC7)
                {
                    OpenChannelWithParamsC200(out handleWrite, CAN_BAUD_BTR_47K_btr0, CAN_BAUD_BTR_47K_btr1);
                    OpenChannelWithParamsC200(out handleRead, CAN_BAUD_BTR_47K_btr0, CAN_BAUD_BTR_47K_btr1);
                    Canlib.canSetAcceptanceFilter(handleRead, (int)code, (int)mask, 0);
                }
                else if (TrionicECU == ECU.TRIONIC8)
                {
                    OpenChannelWithParamsC200(out handleWrite, CAN_BAUD_BTR_33K_btr0, CAN_BAUD_BTR_33K_btr1);
                    OpenChannelWithParamsC200(out handleRead, CAN_BAUD_BTR_33K_btr0, CAN_BAUD_BTR_33K_btr1);
                    Canlib.canSetAcceptanceFilter(handleRead, (int)code, (int)mask, 0);
                }

                if (handleWrite < 0 || handleRead < 0)
                {
                    return OpenResult.OpenError;
                }

                logger.Debug("I bus connected");
                m_endThread = false;
                if (m_readThread.ThreadState == ThreadState.Unstarted)
                {
                    m_readThread.Start();
                }
                return OpenResult.OK;
            }

            OpenChannel(out handleWrite, Canlib.canBITRATE_500K);
            OpenChannel(out handleRead, Canlib.canBITRATE_500K);
            Canlib.canSetAcceptanceFilter(handleRead, (int)code, (int)mask, 0);
            
            if (handleWrite < 0 || handleRead < 0)
            {
                return OpenResult.OpenError;
            }

            logger.Debug("P bus connected");
            m_endThread = false;
            if (m_readThread.ThreadState == ThreadState.Unstarted)
            {
                m_readThread.Start();
            }
            return OpenResult.OK;
        }

        private void OpenChannelWithParamsC200(out int hnd, byte btr0, byte btr1)
        {
            logger.Debug("hnd = canlibCLSNET.Canlib.canOpenChannel()");
            hnd = Canlib.canOpenChannel(ChannelNumber, 0);
            logger.Debug("canlibCLSNET.Canlib.canSetBusParams(hnd)");
            Canlib.canStatus statusSetParam = Canlib.canSetBusParamsC200(hnd, btr0, btr1);
            logger.Debug("canlibCLSNET.Canlib.canBusOn(hnd)");
            Canlib.canStatus statusOn = Canlib.canBusOn(hnd);
            Canlib.canIoCtl(hnd, Canlib.canIOCTL_SET_LOCAL_TXECHO, 0);
        }

        private void OpenChannel(out int hnd, int bitrate)
        {
            logger.Debug("hnd = canlibCLSNET.Canlib.canOpenChannel()");
            hnd = Canlib.canOpenChannel(ChannelNumber, 0);
            logger.Debug("canlibCLSNET.Canlib.canSetBusParams(hnd)");
            Canlib.canStatus statusSetParam = Canlib.canSetBusParams(hnd, bitrate, 0, 0, 0, 0, 0);
            logger.Debug("canlibCLSNET.Canlib.canBusOn(hnd)");
            Canlib.canStatus statusOn = Canlib.canBusOn(hnd);
            Canlib.canIoCtl(hnd, Canlib.canIOCTL_SET_LOCAL_TXECHO, 0);
        }

        /// <summary>
        /// The close method closes the device.
        /// </summary>
        /// <returns>CloseResult.OK on success, otherwise CloseResult.CloseError.</returns>
        override public CloseResult close()
        {
            m_endThread = true;

            Canlib.canStatus statusBusOff1 = Canlib.canStatus.canOK;
            Canlib.canStatus statusBusOff2 = Canlib.canStatus.canOK;
            Canlib.canStatus statusCanClose1 = Canlib.canStatus.canOK;
            Canlib.canStatus statusCanClose2 = Canlib.canStatus.canOK;

            if (handleWrite >= 0)
            {
                statusBusOff1 = Canlib.canBusOff(handleWrite);
                logger.Debug("canlibCLSNET.Canlib.canBusOff(handleWrite)");
                statusCanClose1 = Canlib.canClose(handleWrite);
                logger.Debug("canlibCLSNET.Canlib.canClose(handleWrite)");
            }

            if (handleRead >= 0)
            {
                statusBusOff2 = Canlib.canBusOff(handleRead);
                logger.Debug("canlibCLSNET.Canlib.canBusOff(handleRead)");
                statusCanClose2 = Canlib.canClose(handleRead);
                logger.Debug("canlibCLSNET.Canlib.canClose(handleRead)");
            }

            handleWrite = -1;
            handleRead = -1;
            if (Canlib.canStatus.canOK == statusBusOff1 && Canlib.canStatus.canOK == statusBusOff2 &&
                Canlib.canStatus.canOK == statusCanClose1 && Canlib.canStatus.canOK == statusCanClose2)
            {
                return CloseResult.OK;
            }
            else
            {
                return CloseResult.CloseError;
            }
        }

        /// <summary>
        /// isOpen checks if the device is open.
        /// </summary>
        /// <returns>true if the device is open, otherwise false.</returns>
        override public bool isOpen()
        {
            if (handleWrite >= 0 && handleRead >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// sendMessage send a CANMessage.
        /// </summary>
        /// <param name="a_message">A CANMessage.</param>
        /// <returns>true on success, othewise false.</returns>
        override protected bool sendMessageDevice(CANMessage a_message)
        {
            byte[] msg = a_message.getDataAsByteArray();

            writeStatus = Canlib.canWrite(handleWrite, (int)a_message.getID(), msg, a_message.getLength(), 0);

            if (writeStatus == Canlib.canStatus.canOK)
            {
                return true;
            }
            else
            {
                logger.Debug(String.Format("tx failed with status {0}", writeStatus));
                return false;
            }
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

        /// <summary>
        /// Debug; count number of IDs that gets through
        /// </summary>
        /// <returns></returns>
        private void VerifyFilterIntegrity(uint code, uint mask)
        {
            uint cnt = 0;

            for (uint i = 0; i < 0x800; i++)
            {
                if (((code ^ i) & mask) == 0)
                {
                    cnt++;
                    logger.Debug(String.Format("Currently letting through ID: {0:X3}", i));
                }
            }
            logger.Debug("Currently letting through: " + cnt + " IDs");
        }
    }
}
