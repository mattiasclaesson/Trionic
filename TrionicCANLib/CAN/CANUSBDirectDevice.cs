using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Ports;

namespace TrionicCANLib.CAN
{

    /// <summary>
    /// CANUSBDirectDevice is an implementation of ICANDevice for the Lawicel CANUSB device
    /// (www.canusb.com) using the direct serial port approach
    /// 
    /// All incomming messages are published to registered ICANListeners.
    /// </summary>
    public class CANUSBDirectDevice : ICANDevice
    {
        SerialPort m_serialPort = new SerialPort();
        Thread m_readThread;
        Object m_synchObject = new Object();
        bool m_endThread = false;
        private int m_forcedBaudrate = 38400;
        object lockObj = new object();
        private long lastSentTimestamp = 0;
        private int timeoutWithoutReadyChar = 3000;
        private bool interfaceBusy = false;
        private TimeSpan delayTimespan = new TimeSpan(5000);//500us should be enough

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

        private int m_baseBaudrate = 38400;
        public int BaseBaudrate
        {
            get
            {
                return m_baseBaudrate;
            }
            set
            {
                m_baseBaudrate = value;
            }
        }

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
        /// Constructor for CANUSBDirectDevice.
        /// </summary>
        public CANUSBDirectDevice()
        {
        }

        /// <summary>
        /// Destructor for CANUSBDirectDevice.
        /// </summary>
        ~CANUSBDirectDevice()
        {
            lock (m_synchObject)
            {
                m_endThread = true;
            }
            close();
        }

        override public void Flush()
        {
            if (m_serialPort.IsOpen)
            {
                m_serialPort.DiscardInBuffer();
                m_serialPort.DiscardOutBuffer();
            }
        }


        private Lawicel.CANUSB.CANMsg r_canMsg = new Lawicel.CANUSB.CANMsg();
        private CANMessage canMessage = new CANMessage();

        // int thrdcnt = 0;
        /// <summary>
        /// readMessages is the "run" method of this class. It reads all incomming messages
        /// and publishes them to registered ICANListeners.
        /// </summary>
        public void readMessages()
        {
            while (true)
            {
                lock (m_synchObject)
                {
                    if (m_endThread)
                    {
                        m_endThread = false;
                        return;
                    }
                }
                if (m_serialPort.IsOpen)
                {
                    // read the status?
                    string line = string.Empty;

                    try
                    {
                        line = m_serialPort.ReadLine();
                        if (line.Length > 0)
                        {
                            if (line.Length == 25)
                            {
                                Lawicel.CANUSB.CANMsg r_canMsg = new Lawicel.CANUSB.CANMsg();
                                canMessage = new CANMessage();
                                // three bytes identifier
                                r_canMsg.id = (uint)Convert.ToInt32(line.Substring(1, 3), 16);
                                r_canMsg.len = (byte)Convert.ToInt32(line.Substring(4, 1), 16);
                                ulong data = 0;
                                // add all the bytes
                                data |= (ulong)(byte)Convert.ToInt32(line.Substring(5, 2), 16);
                                data |= (ulong)(byte)Convert.ToInt32(line.Substring(7, 2), 16) << 1 * 8;
                                data |= (ulong)(byte)Convert.ToInt32(line.Substring(9, 2), 16) << 2 * 8;
                                data |= (ulong)(byte)Convert.ToInt32(line.Substring(11, 2), 16) << 3 * 8;
                                data |= (ulong)(byte)Convert.ToInt32(line.Substring(13, 2), 16) << 4 * 8;
                                data |= (ulong)(byte)Convert.ToInt32(line.Substring(15, 2), 16) << 5 * 8;
                                data |= (ulong)(byte)Convert.ToInt32(line.Substring(17, 2), 16) << 6 * 8;
                                data |= (ulong)(byte)Convert.ToInt32(line.Substring(19, 2), 16) << 7 * 8;
                                r_canMsg.data = data;
                                canMessage.setID(r_canMsg.id);
                                canMessage.setLength(r_canMsg.len);
                                canMessage.setFlags(r_canMsg.flags);
                                canMessage.setData(r_canMsg.data);

                                lock (m_listeners)
                                {
                                    AddToCanTrace(string.Format("RX: {0} {1}", canMessage.getID().ToString("X3"), canMessage.getData().ToString("X16")));
                                    foreach (ICANListener listener in m_listeners)
                                    {
                                        listener.handleMessage(canMessage);
                                    }
                                }
                            }
                            else if(line.Contains("z"))
                            {
                                interfaceBusy = false;
                                Console.WriteLine("Got message sent ACK" + line);
                            }
                            else
                            {
                                Console.WriteLine("Unknown message: " + line);
                            }
                        }
                    }
                    catch (Exception E)
                    {
                        Console.WriteLine("Failed to read frames from CANbus: " + E.Message);
                    }
                }
                Thread.Sleep(0);
            }
        }

        private string GetCharString(int value)
        {
            char c = Convert.ToChar(value);
            string charstr = c.ToString();
            if (c == 0x0d) charstr = "<CR>";
            else if (c == 0x0a) charstr = "<LF>";
            else if (c == 0x00) charstr = "<NULL>";
            else if (c == 0x01) charstr = "<SOH>";
            else if (c == 0x02) charstr = "<STX>";
            else if (c == 0x03) charstr = "<ETX>";
            else if (c == 0x04) charstr = "<EOT>";
            else if (c == 0x05) charstr = "<ENQ>";
            else if (c == 0x06) charstr = "<ACK>";
            else if (c == 0x07) charstr = "<BEL>";
            else if (c == 0x08) charstr = "<BS>";
            else if (c == 0x09) charstr = "<TAB>";
            else if (c == 0x0B) charstr = "<VT>";
            else if (c == 0x0C) charstr = "<FF>";
            else if (c == 0x0E) charstr = "<SO>";
            else if (c == 0x0F) charstr = "<SI>";
            else if (c == 0x10) charstr = "<DLE>";
            else if (c == 0x11) charstr = "<DC1>";
            else if (c == 0x12) charstr = "<DC2>";
            else if (c == 0x13) charstr = "<DC3>";
            else if (c == 0x14) charstr = "<DC4>";
            else if (c == 0x15) charstr = "<NACK>";
            else if (c == 0x16) charstr = "<SYN>";
            else if (c == 0x17) charstr = "<ETB>";
            else if (c == 0x18) charstr = "<CAN>";
            else if (c == 0x19) charstr = "<EM>";
            else if (c == 0x1A) charstr = "<SUB>";
            else if (c == 0x1B) charstr = "<ESC>";
            else if (c == 0x1C) charstr = "<FS>";
            else if (c == 0x1D) charstr = "<GS>";
            else if (c == 0x1E) charstr = "<RS>";
            else if (c == 0x1F) charstr = "<US>";
            else if (c == 0x7F) charstr = "<DEL>";
            return charstr;
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
            if (m_forcedComport != string.Empty)
            {
                // only check this comport
                Console.WriteLine("Opening com: " + m_forcedComport);

                if (m_serialPort.IsOpen)
                    m_serialPort.Close();

                m_serialPort.PortName = m_forcedComport;
                if (m_forcedBaudrate != BaseBaudrate && m_forcedBaudrate != 0)
                {
                    m_serialPort.BaudRate = m_forcedBaudrate;
                }
                else
                {
                    m_serialPort.BaudRate = BaseBaudrate;
                }
                m_serialPort.Handshake = Handshake.None;
                m_serialPort.WriteTimeout = 100;
                m_serialPort.ReadTimeout = 100;
                m_serialPort.NewLine = "\r";

                CastInformationEvent("Connected on com: " + m_forcedComport + " with baudrate: " + m_serialPort.BaudRate);

                try
                {
                    m_serialPort.Open();
                    if (m_serialPort.IsOpen)
                    {
                        try
                        {
                            Console.WriteLine("Setting CAN bitrate");
                            m_serialPort.Write("\r");
                            Thread.Sleep(1);
                            
                            m_serialPort.Write("S6\r"); // 500 kb/s
                            //m_serialPort.Write("scb9a\r"); // 47,619 kb/s I-bus
                            //m_serialPort.Write("s4037\r"); // 615 kb/s

                            // now open the CAN channel
                            Thread.Sleep(100);
                            if ((byte)m_serialPort.ReadByte() == 0x07) // error
                            {
                                Console.WriteLine("Failed to set CAN bitrate");
                            }
                            Console.WriteLine("Opening CAN channel");

                            m_serialPort.Write("\r");
                            Thread.Sleep(1);
                            m_serialPort.Write("O\r");
                            Thread.Sleep(100);
                            if ((byte)m_serialPort.ReadByte() == 0x07) // error
                            {
                                Console.WriteLine("Failed to open CAN channel");
                                return OpenResult.OpenError;
                            }
                            //need to check is channel opened!!! 
                            Console.WriteLine("Creating new reader thread");
                            m_readThread = new Thread(readMessages);
                            try
                            {
                                m_readThread.Priority = ThreadPriority.Normal; // realtime enough
                            }
                            catch (Exception E)
                            {
                                Console.WriteLine(E.Message);
                            }
                            if (m_readThread.ThreadState == ThreadState.Unstarted)
                                m_readThread.Start();
                            return OpenResult.OK;
                        }
                        catch (Exception E)
                        {
                            Console.WriteLine("Failed to set canbaudrate: " + E.Message);

                        }
                        try
                        {
                            m_serialPort.Close();
                        }
                        catch (Exception cE)
                        {
                            Console.WriteLine("Failed to close comport: " + cE.Message);
                        }
                        return OpenResult.OpenError;
                    }
                }
                catch (Exception oE)
                {
                    Console.WriteLine("Failed to open comport: " + oE.Message);
                }  
            }
            return OpenResult.OpenError;
        }

        /// <summary>
        /// The close method closes the CANUSBDirect device.
        /// </summary>
        /// <returns>CloseResult.OK on success, otherwise CloseResult.CloseError.</returns>
        override public CloseResult close()
        {
            Console.WriteLine("Close called in CANUSBDirectDevice");

            lock (m_synchObject)
            {
                m_endThread = true;
            }

            Console.WriteLine("Thread ended");
            if (m_serialPort.IsOpen)
            {
                Console.WriteLine("Thread Closing port (1)");
                m_serialPort.Write("\r");
                m_serialPort.Write("C\r");
                Thread.Sleep(100);
                Console.WriteLine("Thread Closing port (2)");
                m_serialPort.Close();
                Console.WriteLine("Thread Closing port (3)");
                return CloseResult.OK;
            }
            return CloseResult.CloseError;
        }

        /// <summary>
        /// isOpen checks if the device is open.
        /// </summary>
        /// <returns>true if the device is open, otherwise false.</returns>
        override public bool isOpen()
        {
            return m_serialPort.IsOpen;
        }


        /// <summary>
        /// sendMessage send a CANMessage.
        /// </summary>
        /// <param name="a_message">A CANMessage.</param>
        /// <returns>true on success, othewise false.</returns>
        override public bool sendMessage(CANMessage a_message)
        {
            lock (lockObj)
            {
                while (interfaceBusy)
                {
                    if (lastSentTimestamp < Environment.TickCount - timeoutWithoutReadyChar)
                        break;
                }

                lastSentTimestamp = Environment.TickCount;
                interfaceBusy = true;

                Lawicel.CANUSB.CANMsg msg = new Lawicel.CANUSB.CANMsg();
                msg.id = a_message.getID();
                msg.len = a_message.getLength();
                msg.flags = a_message.getFlags();
                msg.data = a_message.getData();

                if (m_serialPort.IsOpen)
                {
                    //m_serialPort.Write("\r");
                    string txstring = "t";
                    txstring += msg.id.ToString("X3");
                    txstring += "8"; // always 8 bytes to transmit
                    for (int t = 0; t < 8; t++)
                    {
                        byte b = (byte)(((msg.data >> t * 8) & 0x0000000000000000FF));
                        txstring += b.ToString("X2");
                    }
                    txstring += "\r";
                    m_serialPort.Write(txstring);
                    Console.WriteLine("Send: " + txstring);
                    return true;
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
            r_canMsg = new Lawicel.CANUSB.CANMsg();
            canMsg = new CANMessage();
            string line = string.Empty;
            int readResult = 0;
            int nrOfWait = 0;
            while (nrOfWait < timeout)
            {
                m_serialPort.Write("\r");
                m_serialPort.Write("P\r");
                bool endofFrames = false;
                while (!endofFrames)
                {
                    Console.WriteLine("reading line");
                    line = m_serialPort.ReadLine();
                    Console.WriteLine("line: " + line + " len: " + line.Length.ToString());
                    if (line[0] == '\x07' || line[0] == '\r' || line[0] == 'A')
                    {
                        endofFrames = true;
                    }
                    else
                    {

                        if (line.Length == 14)
                        {
                            // three bytes identifier
                            r_canMsg = new Lawicel.CANUSB.CANMsg();
                            r_canMsg.id = (uint)Convert.ToInt32(line.Substring(1, 3), 16);
                            r_canMsg.len = (byte)Convert.ToInt32(line.Substring(4, 1), 16);
                            ulong data = 0;
                            // add all the bytes
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(5, 2), 16) << 7 * 8;
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(7, 2), 16) << 6 * 8;
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(9, 2), 16) << 5 * 8;
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(11, 2), 16) << 4 * 8;
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(13, 2), 16) << 3 * 8;
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(15, 2), 16) << 2 * 8;
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(17, 2), 16) << 1 * 8;
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(19, 2), 16);
                            r_canMsg.data = data;
                            canMsg.setID(r_canMsg.id);
                            canMsg.setLength(r_canMsg.len);
                            canMsg.setFlags(0);
                            canMsg.setData(r_canMsg.data);

                            if (r_canMsg.id != a_canID)
                                continue;

                            return (uint)r_canMsg.id;
                        }
                    }

                }
                //Thread.Sleep(0);
                nrOfWait++;
            }
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
            CANMessage canMessage = new CANMessage();

            string line = string.Empty;

            int readResult = 0;
            int nrOfWait = 0;
            while (nrOfWait < timeout)
            {
                m_serialPort.Write("\r");
                m_serialPort.Write("P\r");
                bool endofFrames = false;
                while (!endofFrames)
                {
                    Console.WriteLine("reading line");
                    line = m_serialPort.ReadLine();
                    Console.WriteLine("line: " + line + " len: " + line.Length.ToString());
                    if (line[0] == '\x07' || line[0] == '\r' || line[0] == 'A')
                    {
                        endofFrames = true;
                    }
                    else
                    {

                        if (line.Length == 14)
                        {
                            // three bytes identifier
                            r_canMsg = new Lawicel.CANUSB.CANMsg();
                            r_canMsg.id = (uint)Convert.ToInt32(line.Substring(1, 3), 16);
                            r_canMsg.len = (byte)Convert.ToInt32(line.Substring(4, 1), 16);
                            ulong data = 0;
                            // add all the bytes
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(5, 2), 16) << 7 * 8;
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(7, 2), 16) << 6 * 8;
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(9, 2), 16) << 5 * 8;
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(11, 2), 16) << 4 * 8;
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(13, 2), 16) << 3 * 8;
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(15, 2), 16) << 2 * 8;
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(17, 2), 16) << 1 * 8;
                            data |= (ulong)(byte)Convert.ToInt32(line.Substring(19, 2), 16);
                            r_canMsg.data = data;
                            canMessage.setID(r_canMsg.id);
                            canMessage.setLength(r_canMsg.len);
                            canMessage.setFlags(0);
                            canMessage.setData(r_canMsg.data);

                            return (uint)r_canMsg.id;

                        }
                    }

                }
                //Thread.Sleep(0);
                nrOfWait++;
            }
            r_canMsg = new Lawicel.CANUSB.CANMsg();
            return 0;
        }


    }



}
