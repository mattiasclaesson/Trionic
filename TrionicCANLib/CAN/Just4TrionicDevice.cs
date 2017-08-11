using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using NLog;
using TrionicCANLib.API;
using TrionicCANLib.WMI;

namespace TrionicCANLib.CAN
{
    public class Just4TrionicDevice : ICANDevice
    {
        bool m_deviceIsOpen = false;
        SerialPort m_serialPort = new SerialPort();
        Thread m_readThread;
        Object m_synchObject = new Object();
        bool m_endThread = false;
        private Logger logger = LogManager.GetCurrentClassLogger();

        private int m_forcedBaudrate = 115200;

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
            return SerialPort.GetPortNames();
        }

        private string m_forcedComport = string.Empty;

        public override void SetSelectedAdapter(string adapter)
        {
            m_forcedComport = adapter;
        }

        public Just4TrionicDevice()
        {
          
        }

        ~Just4TrionicDevice()
        {
            lock (m_synchObject)
            {
                m_endThread = true;
            }
            close();
        }

        public void readMessages()
        {
            CANMessage canMessage = new CANMessage();
            string rxMessage = string.Empty;

            logger.Debug("readMessages started");
            while (true)
            {
                lock (m_synchObject)
                {
                    if (m_endThread)
                    {
                        logger.Debug("readMessages ended");
                        return;
                    }
                }

                try
                {
                    if (m_serialPort.IsOpen)
                    {
                        do
                        {
                            rxMessage = m_serialPort.ReadLine();
                            rxMessage = rxMessage.Replace("\r", ""); // remove prompt characters... we don't need that stuff
                            rxMessage = rxMessage.Replace("\n", ""); // remove prompt characters... we don't need that stuff
                        } while (rxMessage.StartsWith("w") == false);

                        uint id = Convert.ToUInt32(rxMessage.Substring(1, 3), 16);
                        if (acceptMessageId(id))
                        {
                            canMessage.setID(id);
                            canMessage.setLength(8);
                            canMessage.setData(0x0000000000000000);
                            for (uint i = 0; i < 8; i++)
                            {
                                canMessage.setCanData(Convert.ToByte(rxMessage.Substring(5 + (2 * (int)i), 2), 16), i);
                            }
                            receivedMessage(canMessage);
                        }
                    }
                }
                catch (Exception)
                {
                    logger.Debug("MSG: " + rxMessage);
                }
            }
        }

        public override uint waitForMessage(uint a_canID, uint timeout, out CANMessage canMsg)
        {
            canMsg = new CANMessage();
            return 0;
        }


        protected override bool sendMessageDevice(CANMessage a_message)
        {
            if (!m_serialPort.IsOpen)
            {
                return false;
            }

            string sendString = "t";
            sendString += a_message.getID().ToString("X3");
            sendString += a_message.getLength().ToString("X1");
            for (uint i = 0; i < a_message.getLength(); i++) // leave out the length field, the ELM chip assigns that for us
            {
                sendString += a_message.getCanData(i).ToString("X2");
            }
            sendString += "\r";

            m_serialPort.Write(sendString);
            //logger.Debug("TX: " + sendString);

            // bitrate = 38400bps -> 3840 bytes per second
            // sending each byte will take 0.2 ms approx
            //Thread.Sleep(a_message.getLength()); // sleep length ms
            //            Thread.Sleep(10);
            Thread.Sleep(1);

            return true; // remove after implementation
        }


        public override float GetThermoValue()
        {
            return 0F;
        }

        public override float GetADCValue(uint channel)
        {
            return 0F;
        }


        public override bool isOpen()
        {
            return m_deviceIsOpen;
        }

        public override OpenResult open()
        {
            //Automatically find port with Just4Trionic
            string port = UseWMIForCOMPortByFriendlyName("mbed Serial Port");

            m_serialPort.BaudRate = m_forcedBaudrate;
            m_serialPort.Handshake = Handshake.None;
            //m_serialPort.ReadBufferSize = 0x10000;
            m_serialPort.ReadTimeout = 100;
            if (port != null)
            {
                // only check this comport
                logger.Debug("Opening com: " + port);

                if (m_serialPort.IsOpen)
                    m_serialPort.Close();
                m_serialPort.PortName = port;

                try
                {
                    m_serialPort.Open();
                }
                catch (UnauthorizedAccessException)
                {
                    return OpenResult.OpenError;
                }

                m_deviceIsOpen = true;

                m_serialPort.BreakState = true;     //Reset mbed / Just4trionic
                m_serialPort.BreakState = false;     //
                Thread.Sleep(1000);
                m_serialPort.Write("o\r");          // 'open' Just4trionic CAN interface
                Thread.Sleep(10);

                if (!UseOnlyPBus)
                {
                    m_serialPort.Write("s0\r");         // Set Just4trionic CAN speed to 47,619 bits (I-BUS)
                    Thread.Sleep(10);
                    Flush();                       // Flush 'junk' in serial port buffers

                    try
                    {
                        m_serialPort.ReadLine();
                        logger.Debug("Connected to CAN at 47,619 speed");
                        CastInformationEvent("Connected to CAN I-BUS using " + port);

                        if (m_readThread != null)
                        {
                            logger.Debug(m_readThread.ThreadState.ToString());
                        }
                        m_readThread = new Thread(readMessages) { Name = "Just4TrionicDevice.m_readThread" };
                        m_endThread = false; // reset for next tries :)
                        if (m_readThread.ThreadState == ThreadState.Unstarted)
                            m_readThread.Start();

                        if (TrionicECU == ECU.TRIONIC7)
                        {
                            m_serialPort.Write("f7\r");         // Set Just4trionic filter to allow only Trionic 7 messages
                        }
                        else if (TrionicECU == ECU.TRIONIC8)
                        {
                            m_serialPort.Write("f8\r");         // Set Just4trionic filter to allow only Trionic 8 messages
                        }
                        
                        Thread.Sleep(10);
                        Flush();                       // Flush 'junk' in serial port buffers
                        return OpenResult.OK;
                    }
                    catch (Exception)
                    {
                        logger.Debug("Unable to connect to the I-BUS");
                    }
                }

                m_serialPort.Write("s1\r");         // Set Just4trionic CAN speed to 500 kbits (P-BUS)
                Thread.Sleep(10);
                Flush();                       // Flush 'junk' in serial port buffers

                try
                {
                    m_serialPort.ReadLine();
                    logger.Debug("Connected to CAN at 500 kbits speed");
                    CastInformationEvent("Connected to CAN P-BUS using " + port);

                    if (m_readThread != null)
                    {
                        logger.Debug(m_readThread.ThreadState.ToString());
                    }

                    m_readThread = new Thread(readMessages) { Name = "Just4TrionicDevice.m_readThread" };
                    m_endThread = false; // reset for next tries :)
                    if (m_readThread.ThreadState == ThreadState.Unstarted)
                        m_readThread.Start();
                    
                    if (TrionicECU == ECU.TRIONIC7)
                    {
                        m_serialPort.Write("f7\r");         // Set Just4trionic filter to allow only Trionic 7 messages
                    }
                    else if (TrionicECU == ECU.TRIONIC8)
                    {
                        m_serialPort.Write("f8\r");         // Set Just4trionic filter to allow only Trionic 8 messages
                    }

                    Thread.Sleep(10);
                    Flush();                       // Flush 'junk' in serial port buffers
                    return OpenResult.OK;
                }
                catch (Exception)
                {
                    logger.Debug("Unable to connect to the P-BUS"); 
                }

                CastInformationEvent("Oh dear :-( Just4Trionic cannot connect to a CAN bus.");
                close();
                return OpenResult.OpenError;
            }
            CastInformationEvent("Oh dear :-( Just4Trionic doesn't seem to be connected to your computer.");
            close();
            return OpenResult.OpenError;
        }

        /// <summary>
        /// Use WMI to search for a device by its Frindly Name and returns a COM port string if found or NULL
        /// </summary>
        /// <param name="strFriendlyName">Friendly Name of device to find a COM port for</param>
        /// <returns>COMn where n is the COM port number</returns>
        private static string UseWMIForCOMPortByFriendlyName(string strFriendlyName)
        {
            foreach (COMPortInfo comPort in COMPortInfo.GetCOMPortsInfo())
            {
                if (comPort.Description.StartsWith(strFriendlyName))
                    return comPort.Name;
            }
            return null;
        }

        public void Flush()
        {
            if (m_deviceIsOpen)
            {
                m_serialPort.DiscardInBuffer();
                m_serialPort.DiscardOutBuffer();
            }
        }

        public override CloseResult close()
        {
            if (m_deviceIsOpen) 
                m_serialPort.Write("\x1B");          // mbed ESCape CAN interface
            m_endThread = true;
            m_serialPort.Close();
            m_deviceIsOpen = false;
            
            return CloseResult.OK;
        }
    }
}
