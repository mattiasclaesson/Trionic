using System;
using System.Collections.Generic;
using System.Threading;
using NLog;
using TrionicCANLib.API;

namespace TrionicCANLib.CAN
{
    /// <summary>
    /// CANUSBDevice is an implementation of ICANDevice for the Lawicel CANUSB device
    /// (www.canusb.com). 
    /// In this implementation the open method autmatically detects if the device is connected
    /// to a T7 or T8, I-bus or P-bus. The autodetection is primarily done by listening for the 0x280
    /// message (sent on both busses) but if the device is started after an interrupted flashing
    /// session there is no such message available on the bus. There fore the open method sends
    /// a message to set address and length for flashing. If there is a reply there is connection.
    /// 
    /// All incomming messages are published to registered ICANListeners.
    /// </summary>
    /// 
    public class CANUSBDevice : ICANDevice
    {
        public const string CAN_BAUD_BTR_33K  = "0x8B:0x2F"; //  33,333 kbit/s SAAB GMLAN
        public const string CAN_BAUD_BTR_47K  = "0xcb:0x9a"; //  47,619 kbit/s SAAB T7 I-bus
        public const string CAN_BAUD_BTR_615K = "0x40:0x37"; // 615,384 kbit/s SAAB Trionic 5 P-bus (69% Sampling)
//      public const string CAN_BAUD_BTR_615K = "0x00:0x28"; // 615,384 kbit/s SAAB Trionic 5 P-bus (75% Sampling)
        

        static uint m_deviceHandle = 0;
        Thread m_readThread;
        Object m_synchObject = new Object();
        bool m_endThread = false;
        private static Logger logger = LogManager.GetCurrentClassLogger();

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

        public static new string[] GetAdapterNames()
        {
            
            System.Text.StringBuilder adapter = new System.Text.StringBuilder(10);
            List<string> names = new List<string>();
            int number;
            try
            {
                number = Lawicel.CANUSB.canusb_getFirstAdapter(adapter, 10);
            }
            catch (Exception e)
            {
                logger.Debug("Trouble with Lawicel CANUSB. Check drivers are installed: " + e);
                return names.ToArray();
            }
            logger.Debug("Lawicel.CANUSB.canusb_getFirstAdapter() name=" + adapter + " number=" + number);
            //string[] names = new string[number];
            if(number > 0)
            {
                names.Add(adapter.ToString());
            }

            for (int i = 1; i < number; i++ )
            {
                System.Text.StringBuilder next = new System.Text.StringBuilder(10);
                int num2 = Lawicel.CANUSB.canusb_getNextAdapter(next, 10);
                logger.Debug("Lawicel.CANUSB.canusb_getNextAdapter() name=" + next + " number=" + num2);
                names.Add(next.ToString());
            }
            return names.ToArray();
        }

        private string SelectedAdapter = null;

        public override void SetSelectedAdapter(string adapter)
        {
            SelectedAdapter = adapter;
        }

        /// <summary>
        /// Constructor for CANUSBDevice.
        /// </summary>
        public CANUSBDevice()
        {
        }

        // not supported by lawicel
        public override float GetADCValue(uint channel)
        {
            return 0F;
        }

        // not supported by lawicel
        public override float GetThermoValue()
        {
            return 0F;
        }

        public void Flush()
        {
            Lawicel.CANUSB.canusb_Flush(m_deviceHandle, 0x01);
        }

        /// <summary>
        /// readMessages is the "run" method of this class. It reads all incomming messages
        /// and publishes them to registered ICANListeners.
        /// </summary>
        public void readMessages()
        {
            int readResult = 0;
            Lawicel.CANUSB.CANMsg r_canMsg = new Lawicel.CANUSB.CANMsg();
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
                readResult = Lawicel.CANUSB.canusb_Read(m_deviceHandle, out r_canMsg);
                if (readResult == Lawicel.CANUSB.ERROR_CANUSB_OK)
                {
                    if (acceptMessageId(r_canMsg.id))
                    {
                        canMessage.setID(r_canMsg.id);
                        canMessage.setLength(r_canMsg.len);
                        canMessage.setTimeStamp(r_canMsg.timestamp);
                        canMessage.setFlags(r_canMsg.flags);
                        canMessage.setData(r_canMsg.data);

                        receivedMessage(canMessage);
                    }
                }
                else if (readResult == Lawicel.CANUSB.ERROR_CANUSB_NO_MESSAGE)
                {
                    Thread.Sleep(1);
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
            Lawicel.CANUSB.CANMsg msg = new Lawicel.CANUSB.CANMsg();
            //Check if I bus is connected
            if (m_deviceHandle != 0)
            {
                close();
            }
            Thread.Sleep(200);


            // Note: setting these parameters in here means that tcanflash becomes unable to monitor all traffic with the logging feature.
            // Unfortunately canusb requires these parameters to be set AFTER the channel has been initialized but BEFORE it is open so setting them the same way as elm does is probably not possible..?

            // Default to "Allow all"
            uint AcceptanceCode = 0x00000000;
            uint AcceptanceMask = 0xFFFFFFFF;

            // if (!Monitor)
            {
                CalcAcceptanceFilters(out AcceptanceCode, out AcceptanceMask);
            }


            if (!UseOnlyPBus && TrionicECU != ECU.TRIONIC5)
            {
                logger.Debug("Lawicel.CANUSB.canusb_Open()");
                if (TrionicECU == ECU.TRIONIC7)
                {
                    m_deviceHandle = Lawicel.CANUSB.canusb_Open(SelectedAdapter,
                        CAN_BAUD_BTR_47K, // T7 i-bus
                        AcceptanceCode,
                        AcceptanceMask,
                        Lawicel.CANUSB.CANUSB_FLAG_TIMESTAMP);
                }
                else if (TrionicECU == ECU.TRIONIC8)
                {
                    m_deviceHandle = Lawicel.CANUSB.canusb_Open(SelectedAdapter,
                        CAN_BAUD_BTR_33K, // GMLAN
                        AcceptanceCode,
                        AcceptanceMask,
                        Lawicel.CANUSB.CANUSB_FLAG_TIMESTAMP);
                }

                if (m_deviceHandle != 0)
                {
                    if (waitAnyMessage(1000, out msg) != 0)
                    {
                        logger.Debug("I bus connected");
                        m_readThread = new Thread(readMessages) { Name = "CANUSBDevice.m_readThread" };
                        if (m_readThread.ThreadState == ThreadState.Unstarted)
                            m_readThread.Start();
                        return OpenResult.OK;
                    }
                }
                Thread.Sleep(200);
            }
            close();
            m_endThread = false;


            if (TrionicECU == ECU.TRIONIC5)
            {
                logger.Debug("Lawicel.CANUSB.canusb_Open()");
                m_deviceHandle = Lawicel.CANUSB.canusb_Open(SelectedAdapter,
                    CAN_BAUD_BTR_615K, // T5, P-BUS
                    AcceptanceCode,
                    AcceptanceMask,
                    Lawicel.CANUSB.CANUSB_FLAG_TIMESTAMP);
            }
            else
            {
                //I bus wasn't connected.
                //Check if P bus is connected
                logger.Debug("Lawicel.CANUSB.canusb_Open()");
                m_deviceHandle = Lawicel.CANUSB.canusb_Open(SelectedAdapter,
                Lawicel.CANUSB.CAN_BAUD_500K,
                AcceptanceCode,
                AcceptanceMask,
                Lawicel.CANUSB.CANUSB_FLAG_TIMESTAMP);
            }


            if (m_deviceHandle == 0x00000000)
            {
                return OpenResult.OpenError;
            }

            logger.Debug("P bus connected");
            m_readThread = new Thread(readMessages) { Name = "CANUSBDevice.m_readThread" };
            if (m_readThread.ThreadState == ThreadState.Unstarted)
                m_readThread.Start();
            return OpenResult.OK;
        }

        /// <summary>
        /// The close method closes the CANUSB device.
        /// </summary>
        /// <returns>CloseResult.OK on success, otherwise CloseResult.CloseError.</returns>
        override public CloseResult close()
        {
            m_endThread = true;

            int res = 0;
            try
            {
                if (m_deviceHandle != 0)
                {
                    logger.Debug("Lawicel.CANUSB.canusb_Close()");
                    res = Lawicel.CANUSB.canusb_Close(m_deviceHandle);
                }
            }
            catch(DllNotFoundException e)
            {
                logger.Debug("Dll exception" + e);
                return CloseResult.CloseError;
            }

            m_deviceHandle = 0;
            if (Lawicel.CANUSB.ERROR_CANUSB_OK == res)
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
            if (m_deviceHandle > 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// sendMessage send a CANMessage.
        /// </summary>
        /// <param name="a_message">A CANMessage.</param>
        /// <returns>true on success, othewise false.</returns>
        override protected bool sendMessageDevice(CANMessage a_message)
        {
            Lawicel.CANUSB.CANMsg msg = new Lawicel.CANUSB.CANMsg();
            msg.id = a_message.getID();
            msg.len = a_message.getLength();
            msg.flags = a_message.getFlags();
            msg.data = a_message.getData();
            int writeResult;
            writeResult = Lawicel.CANUSB.canusb_Write(m_deviceHandle, ref msg);
            if (writeResult == Lawicel.CANUSB.ERROR_CANUSB_OK)
            {
                return true;
            }
            else
            {
                logger.Debug("tx failed writeResult: " + writeResult);
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
            Lawicel.CANUSB.CANMsg r_canMsg;
            canMsg = new CANMessage();
            int readResult = 0;
            int nrOfWait = 0;
            while (nrOfWait < timeout)
            {
                r_canMsg = new Lawicel.CANUSB.CANMsg();
                readResult = Lawicel.CANUSB.canusb_Read(m_deviceHandle, out r_canMsg);
                if (readResult == Lawicel.CANUSB.ERROR_CANUSB_OK)
                {
                    Thread.Sleep(1);
                    logger.Trace("rx: 0x" + r_canMsg.id.ToString("X3") + r_canMsg.data.ToString("X16"));
                    if (r_canMsg.id == 0x00)
                    {
                        nrOfWait++;
                    }
                    else if (r_canMsg.id != a_canID)
                        continue;
                    canMsg.setData(r_canMsg.data);
                    canMsg.setID(r_canMsg.id);
                    canMsg.setLength(r_canMsg.len);
                    return (uint)r_canMsg.id;
                }
                else if (readResult == Lawicel.CANUSB.ERROR_CANUSB_NO_MESSAGE)
                {
                    Thread.Sleep(1);
                    nrOfWait++;
                }
            }
            r_canMsg = new Lawicel.CANUSB.CANMsg();
            return 0;
        }

        /// <summary>
        /// waitForMessage waits for a specific CAN message give by a CAN id.
        /// </summary>
        /// <param name="a_canID">The CAN id to listen for</param>
        /// <param name="timeout">Listen timeout</param>
        /// <param name="r_canMsg">The CAN message with a_canID that we where listening for.</param>
        /// <returns>The CAN id for the message we where listening for, otherwise 0.</returns>
        private uint waitForMessage(uint a_canID, uint timeout, out Lawicel.CANUSB.CANMsg r_canMsg)
        {
            int readResult = 0;
            int nrOfWait = 0;
            while (nrOfWait < timeout)
            {
                readResult = Lawicel.CANUSB.canusb_Read(m_deviceHandle, out r_canMsg);
                if (readResult == Lawicel.CANUSB.ERROR_CANUSB_OK)
                {
                    if (r_canMsg.id == 0x00)
                    {
                        nrOfWait++;
                    }
                    else if (r_canMsg.id != a_canID)
                        continue;
                    return (uint)r_canMsg.id;
                }
                else if (readResult == Lawicel.CANUSB.ERROR_CANUSB_NO_MESSAGE)
                {
                    Thread.Sleep(1);
                    nrOfWait++;
                }
            }
            r_canMsg = new Lawicel.CANUSB.CANMsg(); 
            return 0;
        }

        /// <summary>
        /// waitAnyMessage waits for any message to be received.
        /// </summary>
        /// <param name="timeout">Listen timeout</param>
        /// <param name="r_canMsg">The CAN message that was first received</param>
        /// <returns>The CAN id for the message received, otherwise 0.</returns>
        private uint waitAnyMessage(uint timeout, out Lawicel.CANUSB.CANMsg r_canMsg)
        {
            int readResult = 0;
            int nrOfWait = 0;
            while (nrOfWait < timeout)
            {
                readResult = Lawicel.CANUSB.canusb_Read(m_deviceHandle, out r_canMsg);
                if (readResult == Lawicel.CANUSB.ERROR_CANUSB_OK)
                {
                    return (uint)r_canMsg.id;
                }
                else if (readResult == Lawicel.CANUSB.ERROR_CANUSB_NO_MESSAGE)
                {
                    Thread.Sleep(1);
                    nrOfWait++;
                }
            }
            r_canMsg = new Lawicel.CANUSB.CANMsg();
            return 0;
        }

        /// <summary>
        /// Send a message that starts a session. This is used to test if there is 
        /// a connection.
        /// </summary>
        /// <returns></returns>
        private bool sendSessionRequest()
        {
            logger.Debug("Sending session request");
            // 0x220 is for T7
            // 0x7E0 is for T8
            CANMessage msg1 = new CANMessage(0x220, 0, 8);
            Lawicel.CANUSB.CANMsg msg = new Lawicel.CANUSB.CANMsg();
            msg1.setData(0x000040021100813f);

            if (!sendMessage(msg1))
            {
                logger.Debug("Unable to send session request");
                return false;
            }
            if (waitForMessage(0x238, 1000, out msg) == 0x238)
            {
                //Ok, there seems to be a ECU somewhere out there.
                //Now, sleep for 10 seconds to get a session timeout. This is needed for
                //applications on higher level. Otherwise there will be no reply when the
                //higher level application tries to start a session.
                Thread.Sleep(10000);
                logger.Debug("sendSessionRequest: TRUE");

                return true;
            }
            logger.Debug("sendSessionRequest: FALSE");
            return false;
        }

        /// <summary>
        /// Calculates desired acceptance settings.
        /// </summary>
        /// <returns></returns>
        private void CalcAcceptanceFilters(out uint code, out uint mask)
        {
            code = 0xFFFFFFFF;
            mask = 0x00000000;

            // Remote frames.. Are they used?
            bool RTR = false;

            foreach (var id in AcceptOnlyMessageIds)
            {
                code &= (id << 5)&0xFFE0;
                mask |=  id << 5;
                logger.Debug("Adding id: " + id.ToString("X4") + " to acceptance filters");
            }

            // Store setting of RTR in mask
            mask |= (uint)(RTR ? 1 : 0) << 4;

            // Remove bits set to ignore by AcceptanceCode
            mask &= ~code;

            logger.Debug("Configured acceptance code: " + code.ToString("X8"));
            logger.Debug("Configured acceptance mask: " + mask.ToString("X8"));

            // TODO: Remove when done!
            // VerifyFilterIntegrity(code, mask);

        }

        /// <summary>
        /// Debug; count number of IDs that gets through
        /// </summary>
        /// <returns></returns>
        private void VerifyFilterIntegrity(uint code, uint mask)
        {
            uint AM  = ~(mask >> 5);
            uint AC  =   code >> 5;
            uint cnt = 0;

            for (uint i = 0; i <= 0x7FF; i++)
            {
                if ((i & AM) == AC)
                {
                    cnt++;
                }
            }
            logger.Debug("Currently letting through: " + cnt + " IDs");
        }
    }
}
