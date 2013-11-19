using System;
using System.Collections.Generic;
using System.Linq;
using System.IO.Ports;
using System.Threading;
using Microsoft.Win32;

namespace TrionicCANLib.CAN
{
    public class CANELM327Device : ICANDevice
    {
        bool m_deviceIsOpen = false;
        SerialPort m_serialPort = new SerialPort();
        Thread m_readThread;
        Object m_synchObject = new Object();
        bool m_endThread = false;
        private uint _ECUAddress;
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

        ~CANELM327Device()
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
            string receiveString = string.Empty;
            

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
                if (m_serialPort != null)
                {
                    if (m_serialPort.IsOpen)
                    {
                        if (m_serialPort.BytesToRead > 0)
                        {
                            string rxString = m_serialPort.ReadExisting();
                            if (rxString.Length > 0) AddToSerialTrace("SERRX: " + rxString);
                            receiveString += rxString;
                            //Console.WriteLine("BUF1: " + receiveString);
                            receiveString = receiveString.Replace(">", ""); // remove prompt characters... we don't need that stuff
                            receiveString = receiveString.Replace("NO DATA", ""); // remove prompt characters... we don't need that stuff
                            while (receiveString.StartsWith("\n") || receiveString.StartsWith("\r"))
                            {
                                receiveString = receiveString.Substring(1, receiveString.Length - 1);
                            }

                            while (receiveString.Contains('\r'))
                            {
                                // process the line
                                int idx = receiveString.IndexOf('\r');
                                string rxMessage = receiveString.Substring(0, idx);
                                receiveString = receiveString.Substring(idx + 1, receiveString.Length - idx - 1);
                                while (receiveString.StartsWith("\n") || receiveString.StartsWith("\r"))
                                {
                                    receiveString = receiveString.Substring(1, receiveString.Length - 1);
                                }
                                //Console.WriteLine("BUF2: " + receiveString);

                                if (rxMessage.Equals("STOPPED") || rxMessage.Equals("OK"))
                                {
                                }
                                else if (rxMessage.Length >= 6) // is it a valid line
                                {
                                    try
                                    {
                                        uint id = Convert.ToUInt32(rxMessage.Substring(0, 3), 16);
                                        if (acceptMessageId(id))
                                        {
                                            canMessage.setID(id);
                                            canMessage.setLength(8); // TODO: alter to match data
                                            canMessage.setData(0x0000000000000000); // reset message content
                                            byte b1 = Convert.ToByte(rxMessage.Substring(4, 2), 16);
                                            if (b1 < 7)
                                            {
                                                canMessage.setCanData(b1, 0);
                                                //Console.WriteLine("Byte 1: " + Convert.ToByte(rxMessage.Substring(4, 2), 16).ToString("X2"));
                                                if (b1 >= 1) canMessage.setCanData(Convert.ToByte(rxMessage.Substring(7, 2), 16), 1);
                                                if (b1 >= 2) canMessage.setCanData(Convert.ToByte(rxMessage.Substring(10, 2), 16), 2);
                                                if (b1 >= 3) canMessage.setCanData(Convert.ToByte(rxMessage.Substring(13, 2), 16), 3);
                                                if (b1 >= 4) canMessage.setCanData(Convert.ToByte(rxMessage.Substring(16, 2), 16), 4);
                                                if (b1 >= 5) canMessage.setCanData(Convert.ToByte(rxMessage.Substring(19, 2), 16), 5);
                                                if (b1 >= 6) canMessage.setCanData(Convert.ToByte(rxMessage.Substring(22, 2), 16), 6);
                                                if (b1 >= 7) canMessage.setCanData(Convert.ToByte(rxMessage.Substring(25, 2), 16), 7);
                                            }
                                            else
                                            {
                                                canMessage.setCanData(b1, 0);
                                                //Console.WriteLine("Byte 1: " + Convert.ToByte(rxMessage.Substring(4, 2), 16).ToString("X2"));
                                                canMessage.setCanData(Convert.ToByte(rxMessage.Substring(7, 2), 16), 1);
                                                canMessage.setCanData(Convert.ToByte(rxMessage.Substring(10, 2), 16), 2);
                                                canMessage.setCanData(Convert.ToByte(rxMessage.Substring(13, 2), 16), 3);
                                                canMessage.setCanData(Convert.ToByte(rxMessage.Substring(16, 2), 16), 4);
                                                canMessage.setCanData(Convert.ToByte(rxMessage.Substring(19, 2), 16), 5);
                                                canMessage.setCanData(Convert.ToByte(rxMessage.Substring(22, 2), 16), 6);
                                                canMessage.setCanData(Convert.ToByte(rxMessage.Substring(25, 2), 16), 7);
                                            }

                                            lock (m_listeners)
                                            {
                                                AddToCanTrace("RX: " + canMessage.getData().ToString("X16"));
                                                //Console.WriteLine("MSG: " + rxMessage);
                                                foreach (ICANListener listener in m_listeners)
                                                {
                                                    listener.handleMessage(canMessage);
                                                }
                                            }
                                        }

                                    }
                                    catch (Exception)
                                    {
                                        //Console.WriteLine("MSG: " + rxMessage);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Thread.Sleep(1); // give others some air
                        }
                    }
                }

            }
        }

        public override uint waitForMessage(uint a_canID, uint timeout, out CANMessage canMsg)
        {
            canMsg = new CANMessage();
            return 0;
        }

        public override bool sendMessage(CANMessage a_message)
        {
            string sendString = "  ";
            /*if (a_message.getID() == 0x11)
            {
                // try to insert length ourselves
                sendString = " ";
                sendString += a_message.getLength().ToString("X2");
            }*/
            //Console.WriteLine("send: " + a_message.getID().ToString("X3") + " " + a_message.getData().ToString("X16"));

            if (a_message.getID() != _ECUAddress)
            {
                _ECUAddress = a_message.getID();

                string command = "ATSH" + a_message.getID().ToString("X3");
                /*
                if (_ECUAddress == 0x11)
                {
                    command = "ATSH0000" + a_message.getID().ToString("X3");
                }*/
                m_serialPort.Write(command + "\r");    //Set header
                AddToSerialTrace("SERTX: " + command);
                CastInformationEvent("Switching to ID: " + a_message.getID().ToString("X3"));
                // Need to pause a bit here because we need to wait for "OK" back.
                // But the readThread is allready active, so we cannot wait for it here
                Thread.Sleep(2);
            }


            for (uint i = 0; i < a_message.getLength(); i++) // leave out the length field, the ELM chip assigns that for us
            {
                //if (i <= 7)
                {
                        sendString += a_message.getCanData(i).ToString("X2");
                }
                /*else
                {
                    sendString += "00"; // fill with zeros
                }*/
            }
            //sendString = sendString.Trim();
            sendString += "\r";
            
            
            if (m_serialPort.IsOpen)
            {

                m_serialPort.Write(sendString);
                AddToSerialTrace("SERTX: " + sendString);

                //Console.WriteLine("TX: " + sendString);
                AddToCanTrace("TX: " + a_message.getID().ToString("X3") + " " + sendString);
            }

            // bitrate = 38400bps -> 4800 bytes per second
            // sending each byte will take 0.2 ms approx
            // bitrate = 115200bps -> 14400 bytes per second
            // sending each byte will take 0,07 ms approx
            Thread.Sleep(2); // sleep length ms

             //07E0 0000000000003E01
            if (a_message.getID() == 0x7E0 && a_message.getCanData(0) == 0x01 && a_message.getCanData(1) == 0x3E)
            {
                //m_serialPort.Write("ATMA\r");
                //AddToSerialTrace("SERTX: ATMA");
            }

            //            Thread.Sleep(10);
            //receiveString = "49 01 01 00 00 00 31 \n\r49 02 02 44 34 47 50 \n\r49 02 03 30 30 52 35 \n\r49 02 04 25 42";// m_serialPort.ReadTo(">");
            /*receiveString = m_serialPort.ReadTo(">");
            char[] chrArray = receiveString.ToCharArray();
            byte[] reply = new byte[0xFF];
            int insertPos = 1;
            int index = 0;
            string subString = "";
            while (receiveString.Length > 4)
            {

                //Remove first three bytes

                //TODO. Remove Mode and PIDs
                for (int i = 0; i < 3; i++)
                {
                    index = receiveString.IndexOf(" ");
                    receiveString = receiveString.Remove(0, index + 1);
                }
                //Read data for the rest of the row.
                for (int i = 0; i < 4; i++)
                {
                    index = receiveString.IndexOf(" ");
                    if (index == 0) //Last row not 4 bytes of data.
                    {
                        continue;
                    }
                    subString = receiveString.Substring(0, index);
                    reply[insertPos] = (byte)Convert.ToInt16("0x" + subString, 16);
                    insertPos++;
                    receiveString = receiveString.Remove(0, index + 1);
                }

            }

            reply[0] = (byte)insertPos; //Length

            r_reply = new KWPReply(reply, a_request.getNrOfPID());
            return RequestResult.NoError;*/
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

        private string FilterString(string port)
        {
            string retval = string.Empty;
            foreach (char c in port)
            {
                if (c >= 0x30 && c <= 'Z') retval += c;
            }
            return retval.Trim();
        }

        public override OpenResult open()
        {
            m_serialPort.BaudRate = m_forcedBaudrate;
            m_serialPort.Handshake = Handshake.None;
            m_serialPort.ReadTimeout = 10000;
            m_serialPort.WriteTimeout = 10000;
            m_serialPort.ReadBufferSize = 1024;
            m_serialPort.WriteBufferSize = 1024;
            bool readException = false;

            if (TrionicECU == ECU.TRIONIC7)
                _ECUAddress = 0x220;
            else if (TrionicECU == ECU.TRIONIC8)
                _ECUAddress = 0x7E0;

            if (m_forcedComport != string.Empty)
            {
                // only check this comport
                Console.WriteLine("Opening com: " + m_forcedComport);

                readException = false;
                if (m_serialPort.IsOpen)
                    m_serialPort.Close();
                m_serialPort.PortName = m_forcedComport;

                try
                {
                    m_serialPort.Open();
                }
                catch (UnauthorizedAccessException)
                {
                    return OpenResult.OpenError;
                }

                m_serialPort.Write("ATZ\r");    //Reset all
                AddToSerialTrace("SERTX: ATZ");

                Thread.Sleep(1000);

                //Try to set up ELM327
                try
                {
                    m_serialPort.ReadTo(">");
                }
                catch (Exception)
                {
                    readException = true;
                }
                if (readException)
                {
                    // baudrate might be set to 115200 baud
                    m_serialPort.Close();
                    m_serialPort.BaudRate = 115200;
                    m_serialPort.Open();
                    m_serialPort.Write("ATZ\r");    //Reset all
                    AddToSerialTrace("SERTX: ATZ");

                    Thread.Sleep(1000);

                    readException = false;
                    //Try to set up ELM327
                    try
                    {
                        m_serialPort.ReadTo(">");
                    }
                    catch (Exception)
                    {
                        readException = true;
                    }
                    if (readException)
                    {
                        m_serialPort.Close();
                        return OpenResult.OpenError;
                    }
                }

                m_serialPort.Write("ATL1\r");   //Linefeeds On //<GS-18052011> turned off for now
                AddToSerialTrace("SERTX: ATL1");

                m_serialPort.ReadTo(">");
                m_serialPort.Write("ATE0\r");   //Echo off
                AddToSerialTrace("SERTX: ATE0");

                if (m_serialPort.BaudRate != 115200)
                {
                    m_serialPort.ReadTo(">");
                    m_serialPort.Write("ATBRT00\r");   //Set baudrate timeout
                    AddToSerialTrace("SERTX: ATBRT00");

                    m_serialPort.ReadTo(">");
                    m_serialPort.Write("ATBRD23\r"); // Attempt to change baudrate to 23=115.2kbps
                    AddToSerialTrace("SERTX: ATBRD23");
                    Thread.Sleep(10);

                    string ok = m_serialPort.ReadExisting();
                    Console.WriteLine("change baudrateresponse: " + ok);
                    AddToSerialTrace("SERRX: change baudrateresponse" + ok);
                    AddToSerialTrace("bytestoread:" + m_serialPort.BytesToRead.ToString());

                    try
                    {
                        m_serialPort.Close();
                        m_serialPort.BaudRate = 115200;
                        m_serialPort.Open();
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        AddToSerialTrace("exception" + e.ToString());
                        return OpenResult.OpenError;
                    }
                    Thread.Sleep(300);

                    bool gotVersion = false;
                    while (!gotVersion)
                    {
                        string test2 = m_serialPort.ReadExisting();
                        AddToSerialTrace("SERRX: ReadExisting2() len:" + test2.Length + " " + test2);
                        Console.WriteLine("Version ELM: " + test2);
                        if (test2.Length > 5)
                            gotVersion = true;

                        m_serialPort.Write("\r");
                        AddToSerialTrace("SERTX: newline");
                        Thread.Sleep(100);
                    }
                    // OK received here?

                    m_serialPort.ReadTo(">");

                }
                m_serialPort.Write("ATI\r");    //Print version
                AddToSerialTrace("SERTX: ATI");

                string answer = m_serialPort.ReadTo(">");
                AddToSerialTrace("SERRX: " + answer);
                Console.WriteLine("Version ELM: " + answer);
                if(ELMVersionCorrect(answer))
                //if (answer.StartsWith("ELM327 v1.2") || answer.StartsWith("ELM327 v1.3"))
                {
                    CastInformationEvent("Connected on " + m_forcedComport);

                    m_serialPort.Write("ATSP6\r");    //Set protocol type ISO 15765-4 CAR (11 bit ID, 500kb/s)
                    AddToSerialTrace("SERTX: ATSP6");

                    answer = m_serialPort.ReadTo(">");
                    Console.WriteLine("Protocol select response: " + answer);
                    if (answer.StartsWith("OK"))
                    {
                        m_deviceIsOpen = true;

                        m_serialPort.Write("ATH1\r");    //ATH1 = Headers ON, so we can see who's talking
                        AddToSerialTrace("SERTX: ATH1");

                        answer = m_serialPort.ReadTo(">");
                        Console.WriteLine("ATH1 response: " + answer);

                        string command = "ATSH" + _ECUAddress.ToString("X3"); // Set header
                        m_serialPort.Write(command + "\r");
                        AddToSerialTrace("SERTX: " + command);
                        answer = m_serialPort.ReadTo(">");
                        Console.WriteLine(command + "response: " + answer);

                        //m_serialPort.Write("ATR0\r");    //Auto response = OFF 31082011
                        //AddToSerialTrace("SERTX: ATR0");

                        //m_serialPort.Write("ATAL\r");    //Allow messages with length > 7
                        //Console.WriteLine("ATAL response: " + answer);
                        //answer = m_serialPort.ReadTo(">");

                        m_serialPort.Write("ATCAF0\r");   //Can formatting OFF (don't automatically send response codes, we will do this!)
                        AddToSerialTrace("SERTX: ATCAF0");

                        Console.WriteLine("ATCAF0:" + m_serialPort.ReadTo(">"));
                        //m_serialPort.Write("ATR0\r");     //Don't wait for response from the ECU
                        //m_serialPort.ReadTo(">");

                        if (m_readThread != null)
                        {
                            Console.WriteLine(m_readThread.ThreadState.ToString());
                        }
                        m_readThread = new Thread(readMessages);
                        m_readThread.Name = "CANELM327Device.m_readThread";
                        m_endThread = false; // reset for next tries :)
                        if (m_readThread.ThreadState == ThreadState.Unstarted)
                            m_readThread.Start();
                        return OpenResult.OK;
                    }
                    m_serialPort.Close();
                }
            }
            
            return OpenResult.OpenError;
        }

        private bool ELMVersionCorrect(string answer)
        {
            bool retval = true; // assume user knows what he's doing
            //ELM327 v1.2
            if (answer.ToUpper().StartsWith("ELM327 V"))
            {
                if (answer.Length >= 11)
                {
                    string version = answer.Substring(8, 3);
                    version = version.Replace(".", "");
                    try
                    {
                        int versionNumber = Convert.ToInt32(version);
                        if (versionNumber < 12) retval = false;
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            return retval;
        }

        public override void Flush()
        {
            if (m_deviceIsOpen)
            {
                m_serialPort.DiscardInBuffer();
                m_serialPort.DiscardOutBuffer();
            }
        }

        public override CloseResult close()
        {
            lock (m_synchObject)
            {
                m_endThread = true;
            }
            if (m_serialPort.IsOpen)
            {
                Thread.Sleep(1000);
                m_serialPort.Write("ATZ\r");    //Reset all
                AddToSerialTrace("SERTX: ATZ");
                Thread.Sleep(2000);
                m_serialPort.Close();
            }
            m_deviceIsOpen = false;
            return CloseResult.OK;
        }
    }
}
