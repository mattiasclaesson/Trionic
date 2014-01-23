using System;
using System.Collections.Generic;
using System.Linq;
using System.IO.Ports;
using System.Threading;
using Microsoft.Win32;
using System.Text;

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
        private int m_transmissionRepeats = 5;
        private int m_canRetries = 10; //bluetooth device or faulty devices sometimes have transmission errors. This indicates how many can message retries are allowed
        private int m_canRetryCounter = 0;
        private bool m_isVirtualCom = true;
        CANMessage lastSentCanMessage = null;
        private string m_cr_sequence = "&CR";
        private long lastSentTimestamp = 0;
        private int timeoutWithoutReadyChar = 3000;
        private bool interfaceBusy = false;

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
            StringBuilder receiveText = new StringBuilder();

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
                            if (rxString.Contains(">"))
                                interfaceBusy = false;
                            rxString = rxString.Replace("\n", "").Replace("NO DATA", "").Replace(">", "");// remove prompt characters... we don't need that stuff

                            if (rxString.Length > 0)
                            {
                                AddToSerialTrace("SERRX: " + rxString);
                                rxString = rxString.Replace("\r", m_cr_sequence); //replace , because stringbuilder cannot handle \r

                                receiveText.Append(rxString);
                                System.Diagnostics.Debug.WriteLine("SERMSG: " + receiveText);
                                var lines = ExtractLines(ref receiveText);
                                foreach (var rxMessage in lines)
                                {
                                    if (rxMessage.StartsWith("STOPPED")) { } //skip it
                                    else if (rxMessage.StartsWith("CAN ERROR"))
                                    {

                                    }
                                    else if (rxMessage.StartsWith("?"))//need to repeat command
                                    {
                                        if (receiveText.ToString().Contains(">"))
                                        {
                                            if (m_canRetryCounter > 0)
                                            {
                                                m_canRetryCounter--;
                                                Thread.Sleep(100);
                                                sendMessage(lastSentCanMessage);
                                            }
                                        }
                                        else
                                        {
                                            //receiveText = new StringBuilder(rxMessage + "\r");
                                        }
                                    }
                                    else if (rxMessage.Length >= 6) // is it a valid line
                                    {
                                        try
                                        {
                                            rxMessage.Replace(" ", "");//remove all whitespaces
                                            uint id = Convert.ToUInt32(rxMessage.Substring(0, 3), 16);
                                            if (acceptMessageId(id))
                                            {
                                                canMessage.setID(id);
                                                canMessage.setLength(8); // TODO: alter to match data
                                                //canMessage.setData(0x0000000000000000); // reset message content
                                                canMessage.setData(ExtractDataFromString(rxMessage));

                                                lock (m_listeners)
                                                {
                                                    AddToCanTrace("RX: " + canMessage.getData().ToString("X16"));
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
            while (interfaceBusy)
            {
                if (lastSentTimestamp < Environment.TickCount - timeoutWithoutReadyChar)
                    break;
            }
            lastSentTimestamp = Environment.TickCount;
            interfaceBusy = true;

            lastSentCanMessage = a_message.Clone();
            m_canRetryCounter = m_canRetries;
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
                WriteToSerialAndWait(command + "\r"); //this should handle the response
                //WriteToSerialWithTrace();    //Set header
                //AddToSerialTrace("SERTX: " + command);
                CastInformationEvent("Switching to ID: " + a_message.getID().ToString("X3"));
                // Need to pause a bit here because we need to wait for "OK" back.
                // But the readThread is allready active, so we cannot wait for it here
                //Thread.Sleep(2);
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

                WriteToSerialWithTrace(sendString);
                //AddToSerialTrace("SERTX: " + sendString);

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
                //WriteToSerialWithTrace("ATMA\r");
                //AddToSerialTrace("SERTX: ATMA");
            }

            //            Thread.Sleep(10);
            //receiveString = "49 01 01 00 00 00 31 \n\r49 02 02 44 34 47 50 \n\r49 02 03 30 30 52 35 \n\r49 02 04 25 42";// ReadFromSerialToWithLog(">");
            /*receiveString = ReadFromSerialToWithLog(">");
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
            m_serialPort.BaudRate = BaseBaudrate;
            m_serialPort.Handshake = Handshake.None;
            m_serialPort.ReadTimeout = 3000;
            m_serialPort.WriteTimeout = 3000;
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

                try
                {
                    //Try to set up ELM327
                    WriteToSerialAndWait("ATZ\r", 1);    //Reset all               

                }
                catch (Exception)
                {
                    readException = true;
                }

                //WriteToSerialAndWait("ATL1\r");   //Linefeeds On //<GS-18052011> turned off for now
                WriteToSerialAndWait("ATE0\r");   //Echo off
                WriteToSerialAndWait("ATS0\r");     //disable whitespace, should speed up the transmission by eliminating 8 whitespaces for every 19 chars received
                //WriteToSerialAndWait("ATST40\r"); //set timeout to 256ms

                #region setBaudRate
                if (m_forcedBaudrate != 0)
                {

                    WriteToSerialAndWait("ATBRT00\r");   //Set baudrate timeout
                    //AddToSerialTrace("SERTX: ATBRT00");

                    int divider = (int)(Math.Round(4000000.0 / m_forcedBaudrate));

                    WriteToSerialWithTrace(String.Format("ATBRD{0}\r", divider.ToString("X2")));

                    Thread.Sleep(50);
                    string ok = m_serialPort.ReadExisting();

                    Console.WriteLine("change baudrateresponse: " + ok);
                    AddToSerialTrace("SERRX: change baudrateresponse" + ok);
                    AddToSerialTrace("bytestoread:" + m_serialPort.BytesToRead.ToString());

                    try
                    {
                        m_serialPort.Close();
                        m_serialPort.BaudRate = m_forcedBaudrate;
                        m_serialPort.Open();
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        AddToSerialTrace("exception" + e.ToString());
                        return OpenResult.OpenError;
                    }

                    //Thread.Sleep(300);
                    bool gotVersion = false;
                    while (!gotVersion)
                    {
                        ok = WriteToSerialAndWait("\r");

                        AddToSerialTrace("SERRX: ReadExisting2() len:" + ok.Length + " " + ok);
                        Console.WriteLine("wait: " + ok);
                        if (ok.Length > 5)
                            gotVersion = true;


                        //AddToSerialTrace("SERTX: newline");
                        Thread.Sleep(100);
                    }
                }
                Console.WriteLine("Baud: " + m_serialPort.BaudRate);
                #endregion

                //do some benchmark
                long start = Environment.TickCount;
                int tries = 100;
                while (tries-- > 0)
                    WriteToSerialAndWait("ATI\r");

                start = Environment.TickCount - start;
                AddToSerialTrace(string.Format("Got {0} responses in {1} ms", 100, start));


                string answer = WriteToSerialAndWait("ATI\r");    //Print version
                Console.WriteLine("Version ELM: " + answer);

                if (ELMVersionCorrect(answer))
                {
                    CastInformationEvent("Connected on " + m_forcedComport);

                    answer = WriteToSerialAndWait("ATSP6\r");    //Set protocol type ISO 15765-4 CAR (11 bit ID, 500kb/s)

                    Console.WriteLine("Protocol select response: " + answer);
                    if (answer.StartsWith("OK"))
                    {
                        m_deviceIsOpen = true;

                        answer = WriteToSerialAndWait("ATH1\r");    //ATH1 = Headers ON, so we can see who's talking

                        Console.WriteLine("ATH1 response: " + answer);

                        string command = "ATSH" + _ECUAddress.ToString("X3"); // Set header
                        answer = WriteToSerialAndWait(command + "\r");
                        //AddToSerialTrace("SERTX: " + command);
                        Console.WriteLine(command + "response: " + answer);

                        //WriteToSerialWithTrace("ATR0\r");    //Auto response = OFF 31082011
                        //AddToSerialTrace("SERTX: ATR0");

                        //WriteToSerialWithTrace("ATAL\r");    //Allow messages with length > 7
                        //Console.WriteLine("ATAL response: " + answer);
                        //answer = ReadFromSerialToWithLog(">");

                        answer = WriteToSerialAndWait("ATCAF0\r");   //Can formatting OFF (don't automatically send response codes, we will do this!)
                        Console.WriteLine("ATCAF0:" + answer);
                        //WriteToSerialWithTrace("ATR0\r");     //Don't wait for response from the ECU
                        //ReadFromSerialToWithLog(">");

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
            if (answer.ToUpper().StartsWith("ELM327"))
            {
                if (answer.Length >= 10)
                {
                    string version = answer.Substring(answer.Length - 3, 3).Replace(".", "");
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
                //Thread.Sleep(1000);
                WriteToSerialWithTrace("ATZ\r");    //Reset all
                Thread.Sleep(100);
                m_serialPort.Close();
            }

            m_deviceIsOpen = false;
            return CloseResult.OK;
        }

        protected void WriteToSerialWithTrace(string line)
        {
            m_serialPort.Write(line);
            AddToSerialTrace("SERTX: " + line);
        }

        protected string ReadFromSerialToWithTrace(string readTo)
        {
            string answer = m_serialPort.ReadTo(readTo);
            AddToSerialTrace("SERRX: " + answer + readTo);
            return answer;
        }

        protected string WriteToSerialAndWait(string line)
        {
            return WriteToSerialAndWait(line, m_transmissionRepeats);
        }

        protected string WriteToSerialAndWait(string line, int repeats)
        {
            while (repeats > 0)
            {
                WriteToSerialWithTrace(line);
                try
                {
                    string result = ReadFromSerialToWithTrace(">");
                    if (result.Contains("?"))
                        repeats--;
                    else
                        return result;
                }
                catch (TimeoutException x)
                {
                    repeats--;
                    AddToSerialTrace("SERTIMOUT: left tries " + repeats);
                }
                catch (Exception x)
                {
                    AddToSerialTrace("Exception" + x);
                }
            }

            if (repeats == 0)
                throw new Exception("Failed to read from interface");
            return "";
        }

        private string[] ExtractLines(ref StringBuilder input)
        {
            List<string> output = new List<string>();
            string inputStr = input.ToString();
            while (inputStr.Contains(m_cr_sequence))
            {
                int index = inputStr.IndexOf(m_cr_sequence);
                string line = inputStr.Substring(0, index);
                inputStr = inputStr.Substring(index + m_cr_sequence.Length, inputStr.Length - index - m_cr_sequence.Length);
                output.Add(line);
            }

            input = new StringBuilder(inputStr);
            return output.ToArray();
        }

        /// <summary>
        /// Extracts data from string. String can conatin whitespaces,it will be trimmed internally
        /// </summary>
        /// <param name="rxMessage">String message, i.e. 7E8 10 15 41 00 BE 3F B8 13  (to be verified)</param>
        /// <returns></returns>
        private static ulong ExtractDataFromString(string rxMessage)
        {
            rxMessage.Replace(" ", "");
            byte bytesToRead = Convert.ToByte(rxMessage.Substring(3, 2), 16);
            ulong data = bytesToRead;
            bytesToRead = Math.Min(bytesToRead, (byte)7);
            for (int i = 0; i < bytesToRead; i++)
            {
                ulong tmp = Convert.ToByte(rxMessage.Substring(5 + i * 2, 2), 16);
                tmp <<= ((i + 1) * 8);
                data |= tmp;
            }
            return data;
        }
    }
}
