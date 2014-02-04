using System;
using System.Collections.Generic;
using System.Threading;
using CanUsbInterface;
using CanUsbComponent;

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
    public class CANUSBFTDIDevice : ICANDevice
    {
        public const string CAN_BAUD_BTR_33K = "0x8B:0x2F"; // 33 kbit/s SAAB GMLAN
        public const string CAN_BAUD_BTR_47K = "0xcb:0x9a"; // 47,6 kbit/s SAAB T7 I-bus

        CanUsbComponentClass canPort;
        Thread m_readThread;
        Object m_synchObject = new Object();
        bool m_endThread = false;

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

        private string m_forcedComport = string.Empty;

        public override string ForcedComport
        {
            get
            {
                return m_forcedComport;
            }
            set
            {
                m_forcedComport = value;
            }
        }

        /// <summary>
        /// Constructor for CANUSBFTDIDevice.
        /// </summary>
        public CANUSBFTDIDevice()
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

        /// <summary>
        /// Destructor for CANUSBFTDIDevice.
        /// </summary>
        ~CANUSBFTDIDevice()
        {
            lock (m_synchObject)
            {
                m_endThread = true;
            }
            close();
        }

        public override void Flush()
        {
            
        }

        /// <summary>
        /// readMessages is the "run" method of this class. It reads all incomming messages
        /// and publishes them to registered ICANListeners.
        /// </summary>
        public void readMessages()
        {
            if (canPort.State == AdaptorState.CanBusOpen)
            {
                CANMessage canMessage = new CANMessage();
                Console.WriteLine("readMessages started");
                while (true)
                {
                    lock (m_synchObject)
                    {
                        if (m_endThread)
                        {
                            Console.WriteLine("readMessages ended");
                            return;
                        }
                    }

                    CanMessage msgcan;
                    if (canPort.GetCanMessage(out msgcan, 0))
                    {
                        if (acceptMessageId(msgcan.Id))
                        {
                            canMessage.setID(msgcan.Id);
                            canMessage.setLength((byte)msgcan.Length);
                            canMessage.setData(BitTools.ReverseOrder(msgcan.Data));
                            
                            lock (m_listeners)
                            {
                                AddToCanTrace(string.Format("RX: {0} {1}", canMessage.getID().ToString("X3"), canMessage.getData().ToString("X16")));
                                foreach (ICANListener listener in m_listeners)
                                {
                                    listener.handleMessage(canMessage);
                                }
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
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
            canPort = new CanUsbComponentClass();
            if (canPort.State == AdaptorState.Closed)
            {
                try
                {
                    //if (canPort.OpenCanBus(serialnum, baud, 0xF000, 0x0FFF))
                    if (canPort.OpenCanBus("", ECanBps.Baud500kBps))
                    {
                        // Print the adaptor information
                        Console.WriteLine("Connected to FtdiDevice " + canPort.FtdiSerialNumber + ", Version "
                            + canPort.AdaptorVersion + ", CanUsb Serialnumber " + canPort.CanUsbSerialnumber);

                        Thread.Sleep(200);
                        m_readThread = new Thread(readMessages);
                        m_readThread.Name = "CANUSBDevice.m_readThread";
                        m_endThread = false;

                        if (m_readThread.ThreadState == ThreadState.Unstarted)
                            m_readThread.Start();

                        return OpenResult.OK;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            else
            {
                canPort.CloseCanBus();
            }


            /*
            //Check if I bus is connected
            if (m_deviceHandle != 0)
            {
                close();
            }


            if (!UseOnlyPBus)
            {
                Console.WriteLine("Getting handle");
                if (TrionicECU == ECU.TRIONIC7)
                {
                    m_deviceHandle = Lawicel.CANUSB.canusb_Open(IntPtr.Zero,
                        CAN_BAUD_BTR_47K, // T7 i-bus
                        Lawicel.CANUSB.CANUSB_ACCEPTANCE_CODE_ALL,
                        Lawicel.CANUSB.CANUSB_ACCEPTANCE_MASK_ALL,
                        Lawicel.CANUSB.CANUSB_FLAG_TIMESTAMP);
                }
                else if (TrionicECU == ECU.TRIONIC8)
                {
                    m_deviceHandle = Lawicel.CANUSB.canusb_Open(IntPtr.Zero,
                        CAN_BAUD_BTR_33K, // GMLAN
                        Lawicel.CANUSB.CANUSB_ACCEPTANCE_CODE_ALL,
                        Lawicel.CANUSB.CANUSB_ACCEPTANCE_MASK_ALL,
                        Lawicel.CANUSB.CANUSB_FLAG_TIMESTAMP);
                }

                if (m_deviceHandle != 0)
                {
                    if (waitAnyMessage(1000, out msg) != 0)
                    {
                        if (m_readThread.ThreadState == ThreadState.Unstarted)
                            m_readThread.Start();
                        return OpenResult.OK;
                    }
                }
                if (m_deviceHandle != 0)
                {
                    close();
                }
                Thread.Sleep(200);
            }
            if (m_deviceHandle != 0)
            {
                close();
            }
            m_endThread = false;

            //I bus wasn't connected.
            //Check if P bus is connected
            m_deviceHandle = Lawicel.CANUSB.canusb_Open(IntPtr.Zero,
            Lawicel.CANUSB.CAN_BAUD_500K,
            Lawicel.CANUSB.CANUSB_ACCEPTANCE_CODE_ALL,
            Lawicel.CANUSB.CANUSB_ACCEPTANCE_MASK_ALL,
            Lawicel.CANUSB.CANUSB_FLAG_TIMESTAMP);
            if (m_deviceHandle == 0x00000000)
            {
                return OpenResult.OpenError;
            }
            if(DisableCanConnectionCheck || boxIsThere())
            {
                Console.WriteLine("Box is there, starting thread");
                if (m_readThread.ThreadState == ThreadState.Unstarted)
                    m_readThread.Start();
                return OpenResult.OK;
            }
            Console.WriteLine("Box not there");
            close();*/
            return OpenResult.OpenError;
        }

        /// <summary>
        /// The close method closes the CANUSB device.
        /// </summary>
        /// <returns>CloseResult.OK on success, otherwise CloseResult.CloseError.</returns>
        override public CloseResult close()
        {
            canPort.CloseCanBus();
            return CloseResult.OK;
        }

        /// <summary>
        /// isOpen checks if the device is open.
        /// </summary>
        /// <returns>true if the device is open, otherwise false.</returns>
        override public bool isOpen()
        {
            return canPort.State == AdaptorState.CanBusOpen;
        }

        /// <summary>
        /// sendMessage send a CANMessage.
        /// </summary>
        /// <param name="a_message">A CANMessage.</param>
        /// <returns>true on success, othewise false.</returns>
        override public bool sendMessage(CANMessage a_message)
        {
            if (canPort.State == AdaptorState.CanBusOpen)
            {
                CanMessage canMsg = new CanMessage();
                canMsg.Id = a_message.getID();
                canMsg.Length = a_message.getLength();
                //canMsg.flags = a_message.getFlags();
                canMsg.Data = BitTools.ReverseOrder(a_message.getData());

                AddToCanTrace(string.Format("TX: {0} {1}", a_message.getID().ToString("X3"), canMsg.Data.ToString("X16")));
                if (canPort.SendCanMessage(canMsg))
                {
                    AddToCanTrace("Message sent successfully");
                    return true;
                }
                else
                {
                    AddToCanTrace("Message failed to send");
                    return false;
                }
            }
            return false;
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
        /// waitForMessage waits for a specific CAN message give by a CAN id.
        /// </summary>
        /// <param name="a_canID">The CAN id to listen for</param>
        /// <param name="timeout">Listen timeout</param>
        /// <param name="r_canMsg">The CAN message with a_canID that we where listening for.</param>
        /// <returns>The CAN id for the message we where listening for, otherwise 0.</returns>
        private uint waitForMessage(uint a_canID, uint timeout, out Lawicel.CANUSB.CANMsg r_canMsg)
        {
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
            r_canMsg = new Lawicel.CANUSB.CANMsg();
            return 0;
        }

        /// <summary>
        /// Check if there is connection with a CAN bus.
        /// </summary>
        /// <returns>true on connection, otherwise false</returns>
        private bool boxIsThere()
        {
			// TODO T8 disabled this method, always returned true
            Lawicel.CANUSB.CANMsg msg = new Lawicel.CANUSB.CANMsg();
            if (waitAnyMessage(2000, out msg) != 0)
            {
                Console.WriteLine("A message was seen");
                return true;
            }
            if (sendSessionRequest())
            {
                Console.WriteLine("Session request success");

                return true;
            }
            Console.WriteLine("Box not there");

            return false;
        }

        /// <summary>
        /// Send a message that starts a session. This is used to test if there is 
        /// a connection.
        /// </summary>
        /// <returns></returns>
        private bool sendSessionRequest()
        {
            Console.WriteLine("Sending session request");
            // 0x220 is for T7
            // 0x7E0 is for T8
            CANMessage msg1 = new CANMessage(0x220, 0, 8);
            Lawicel.CANUSB.CANMsg msg = new Lawicel.CANUSB.CANMsg();
            msg1.setData(0x000040021100813f);

            if (!sendMessage(msg1))
            {
                Console.WriteLine("Unable to send session request");
                return false;
            }
            if (waitForMessage(0x238, 1000, out msg) == 0x238)
            {
                //Ok, there seems to be a ECU somewhere out there.
                //Now, sleep for 10 seconds to get a session timeout. This is needed for
                //applications on higher level. Otherwise there will be no reply when the
                //higher level application tries to start a session.
                Thread.Sleep(10000);
                Console.WriteLine("sendSessionRequest: TRUE");

                return true;
            }
            Console.WriteLine("sendSessionRequest: FALSE");
            return false;
        }
    }
}
