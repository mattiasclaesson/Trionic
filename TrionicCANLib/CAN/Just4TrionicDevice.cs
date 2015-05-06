using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Threading;
using Microsoft.Win32;
using NLog;
using TrionicCANLib.API;

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

        private const char ESC = '\x1B';

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
                                canMessage.setCanData(Convert.ToByte(rxMessage.Substring(5 + (2 * (int)i), 2), 16), i);

                            lock (m_listeners)
                            {
                                logger.Trace("rx: " + canMessage.getID().ToString("X3") + " " + canMessage.getLength().ToString("X1") + " " + canMessage.getData().ToString("X16"));
                                //Console.WriteLine("MSG: " + rxMessage);
                                foreach (ICANListener listener in m_listeners)
                                {
                                    listener.handleMessage(canMessage);
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("MSG: " + rxMessage);
                }
            }
        }

        public override uint waitForMessage(uint a_canID, uint timeout, out CANMessage canMsg)
        {
            canMsg = new CANMessage();
            return 0;
            
            //CANMessage canMessage = new CANMessage();
            //string rxMessage = string.Empty;

            //canMsg = new CANMessage();
            //int nrOfWait = 0;
            
            //while (nrOfWait < timeout)
            //{
            //    rxMessage = m_serialPort.ReadLine();
            //    rxMessage = rxMessage.Replace("\r", ""); // remove prompt characters... we don't need that stuff
            //    rxMessage = rxMessage.Replace("\n", ""); // remove prompt characters... we don't need that stuff
            //    if (rxMessage.StartsWith("w") == false)
            //    {
            //        Thread.Sleep(1);
            //        nrOfWait++;
            //        continue;
            //    }
            //    uint id = Convert.ToUInt32(rxMessage.Substring(1, 3), 16);
            //    if (id != a_canID && a_canID != 0)
            //    {
            //        Thread.Sleep(1);
            //        nrOfWait++;
            //        continue;
            //    }

            //    canMessage.setID(id);
            //    canMessage.setLength(8);
            //    canMessage.setData(0x0000000000000000);
            //    for (uint i = 0; i < 8; i++)
            //        canMessage.setCanData(Convert.ToByte(rxMessage.Substring(5 + (2 * (int)i), 2), 16), i);
            //    return (uint)id;
            //}
            //return 0;
        }

        public override bool sendMessage(CANMessage a_message)
        {
            string sendString = "t";
            sendString += a_message.getID().ToString("X3");
            sendString += a_message.getLength().ToString("X1");
            for (uint i = 0; i < a_message.getLength(); i++) // leave out the length field, the ELM chip assigns that for us
            {
                sendString += a_message.getCanData(i).ToString("X2");
            }
            sendString += "\r";
            if (m_serialPort.IsOpen)
            {
                logger.Trace("tx: " + a_message.getID().ToString("X3") + " " + a_message.getLength().ToString("X1") + " " + a_message.getData().ToString("X16"));
                m_serialPort.Write(sendString);
                //Console.WriteLine("TX: " + sendString);
            }

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
            //string port = MineRegistryForJust4TrionicPortName("SYSTEM\\CurrentControlSet\\Enum\\USB\\VID_0D28&PID_0204&MI_01");
            string port = MineRegistryForJust4TrionicPortName("SYSTEM\\CurrentControlSet\\Enum\\USB");

            m_serialPort.BaudRate = m_forcedBaudrate;
            m_serialPort.Handshake = Handshake.None;
            //m_serialPort.ReadBufferSize = 0x10000;
            m_serialPort.ReadTimeout = 100;
            if (port != null)
            {
                // only check this comport
                Console.WriteLine("Opening com: " + port);

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
                    this.Flush();                       // Flush 'junk' in serial port buffers

                    try
                    {
                        m_serialPort.ReadLine();
                        Console.WriteLine("Connected to CAN at 47,619 speed");
                        CastInformationEvent("Connected to CAN I-BUS using " + port);

                        if (m_readThread != null)
                        {
                            Console.WriteLine(m_readThread.ThreadState.ToString());
                        }
                        m_readThread = new Thread(readMessages);
                        m_readThread.Name = "Just4TrionicDevice.m_readThread";
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
                        this.Flush();                       // Flush 'junk' in serial port buffers
                        return OpenResult.OK;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Unable to connect to the I-BUS");
                    }
                }

                m_serialPort.Write("s1\r");         // Set Just4trionic CAN speed to 500 kbits (P-BUS)
                Thread.Sleep(10);
                this.Flush();                       // Flush 'junk' in serial port buffers

                try
                {
                    m_serialPort.ReadLine();
                    Console.WriteLine("Connected to CAN at 500 kbits speed");
                    CastInformationEvent("Connected to CAN P-BUS using " + port);

                    if (m_readThread != null)
                    {
                        Console.WriteLine(m_readThread.ThreadState.ToString());
                    }

                    m_readThread = new Thread(readMessages);
                    m_readThread.Name = "Just4TrionicDevice.m_readThread";
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
                    this.Flush();                       // Flush 'junk' in serial port buffers
                    return OpenResult.OK;
                }
                catch (Exception)
                {
                    Console.WriteLine("Unable to connect to the P-BUS"); 
                }

                CastInformationEvent("Oh dear :-( Just4Trionic cannot connect to a CAN bus.");
                this.close();
                return OpenResult.OpenError;
            }
            CastInformationEvent("Oh dear :-( Just4Trionic doesn't seem to be connected to your computer.");
            this.close();
            return OpenResult.OpenError;
        }

        /// <summary>
        /// Recursively enumerates registry subkeys starting with strStartKey looking for 
        /// "Device Parameters" subkey. If key is present, friendly port name is extracted.
        /// </summary>
        /// <param name="strStartKey">the start key from which to begin the enumeration</param>
        private string MineRegistryForJust4TrionicPortName(string strStartKey)
        {
            //            string strStartKey = "SYSTEM\\CurrentControlSet\\Enum";
            string[] oPortNamesToMatch = System.IO.Ports.SerialPort.GetPortNames();
            Microsoft.Win32.RegistryKey oCurrentKey = Registry.LocalMachine.OpenSubKey(strStartKey);
            string[] oSubKeyNames = oCurrentKey.GetSubKeyNames();

            object oFriendlyName = Registry.GetValue("HKEY_LOCAL_MACHINE\\" + strStartKey, "FriendlyName", null);
            string strFriendlyName = (oFriendlyName != null) ? oFriendlyName.ToString() : "N/A";

            if (strFriendlyName.StartsWith("mbed Serial Port"))
            {
                object oPortNameValue = Registry.GetValue("HKEY_LOCAL_MACHINE\\" + strStartKey + "\\Device Parameters", "PortName", null);
                return new List<string>(oPortNamesToMatch).Contains(oPortNameValue.ToString()) ? oPortNameValue.ToString() : null;
            }
            else
            {
                foreach (string strSubKey in oSubKeyNames)
                    if (MineRegistryForJust4TrionicPortName(strStartKey + "\\" + strSubKey) != null)
                        return MineRegistryForJust4TrionicPortName(strStartKey + "\\" + strSubKey);
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
