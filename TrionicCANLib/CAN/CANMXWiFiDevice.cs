using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using NLog;
using TrionicCANLib.API;
using PIEBALD.Types;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace TrionicCANLib.CAN
{
    public class CANMXWiFiDevice : ICANDevice
    {
        bool m_deviceIsOpen;

        readonly TelnetSocket m_socket = new TelnetSocket();

        readonly Thread m_readThread;
        readonly Object m_synchObject = new Object();
        bool m_endThread;
        private uint _ECUAddress;
        private const int m_transmissionRepeats = 5;
        CANMessage lastSentCanMessage;
        private const string m_cr_sequence = "&CR";
        private bool interfaceBusy;

        private long lastSentTimestamp = 0;
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        readonly object lockObj = new object();

        private int m_forcedBaudrate = 35000;
        private string m_socketAddress = string.Empty;

        public override int ForcedBaudrate  // 'Hi-jacking' ForcedBaudrate to use for telnet port number
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

        public override void SetSelectedAdapter(string address)  // 'Hi-jacking' SetSelectedAdapter to use for telnet IP address
        {
            m_socketAddress = address;
        }

        static CountdownEvent countdown;
        readonly static List<string> IPAddresses = new List<string>();

        // Adapted from
        // stackoverflow.com/questions/4042789/how-toget-ip-of-all-hosts-in-lan
        public static new string[] GetAdapterNames()
        {
            countdown = new CountdownEvent(1);
            List<string> networks = netBaseAddresses();
            if (!networks.Contains("192.168.0.")) { networks.Add("192.168.0."); }
            if (!networks.Contains("192.168.222.")) { networks.Add("192.168.222."); }
            foreach (string network in networks)
            {
                for (int i = 1; i < 255; i++)
                {
                    string ip = String.Format("{0}{1}", network, i);
                    Ping p = new Ping();
                    p.PingCompleted += p_PingCompleted;
                    countdown.AddCount();
                    var t = Task.Factory.StartNew(() => p.SendAsync(ip, 100, ip));
                    t.Wait();
                }
            }
            countdown.Signal();
            countdown.Wait();
            IPAddresses.Sort();
            return IPAddresses.ToArray();
        }

        // Adapted from
        // www.codeproject.com/Questions/616599/Prob-in-Finding-IPAddress-Default-gate-way-Subnet
        // stackoverflow.com/questions/901165/why-does-unicastipaddressinformation-ipv4mask-return-null-on-an-ipv4-address
        static List<string> netBaseAddresses()
        {
            List<string> BaseAddresses = new List<string>();
            NetworkInterface[] Interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface Interface in Interfaces)
            {
                if (Interface.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                Console.WriteLine(Interface.Description);
                Console.WriteLine(Interface.Name);
                UnicastIPAddressInformationCollection addresses = Interface.GetIPProperties().UnicastAddresses;
                foreach (UnicastIPAddressInformation address in addresses)
                {
                    if (address.Address.AddressFamily.ToString() == "InterNetwork")
                    {
                        byte[] ipAdBytes = address.Address.GetAddressBytes();
                        byte[] maskBytes = address.IPv4Mask.GetAddressBytes();
                        StringBuilder baseAddress = new StringBuilder();
                        int i = 0;
                        while (maskBytes[i] != 0)
                        {
                            baseAddress.Append((ipAdBytes[i] & maskBytes[i]) + ".");
                            i++;
                        }
                        BaseAddresses.Add(baseAddress.ToString());
                    }
                }
            }
            return BaseAddresses;
        }

        // Adapted from
        // stackoverflow.com/questions/4042789/how-toget-ip-of-all-hosts-in-lan
                static void p_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            string ip = (string)e.UserState;
            if (e.Reply != null && e.Reply.Status == IPStatus.Success)
            {
                lock (IPAddresses)
                {
                    if (!IPAddresses.Contains(ip))
                    {
                        IPAddresses.Add(ip);
                    }
                }
            }
            countdown.Signal();
        }

        public CANMXWiFiDevice()
        {
            m_socket.OnDataReceived += DataReceived;
            m_socket.OnExceptionCaught += ExceptionCaught;
            m_readThread = new Thread(readMessages) { Name = "CANNXWiFiDevice.m_readThread", IsBackground = true };
            m_endThread = false; // reset for next tries :)
        }

        ~CANMXWiFiDevice()
        {
            lock (m_synchObject)
            {
                m_endThread = true;
            }
            close();
        }

        string rawString;

        public void readMessages()
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
                if (rawString != null)
                {
                    logger.Trace(String.Format("Raw Data: {0}", rawString));
                    //AddToSerialTrace("RAW RX: " + rawString.Replace("\r",m_cr_sequence));
                    string rxString = rawString.Replace("\r", m_cr_sequence); //replace , because stringbuilder cannot handle \r
                    bool isStopped = false;

                    if (rxString.Length > 0)
                    {
                        //AddToSerialTrace("RECEIVE TEXT: " + rxString);                                
                        //System.Diagnostics.Debug.WriteLine("SERMSG: " + rxString);
                        var lines = ExtractLines(rxString);
                        foreach (var rxMessage in lines)
                        {
                            if (rxMessage.StartsWith("STOPPED")) { isStopped = true; }
                            else if (rxMessage.StartsWith("NO (MORE?) DATA")) { } //skip it
                            else if (rxMessage.StartsWith("CAN ERROR")) { } //handle error?
                            else if (rxMessage.StartsWith("ELM")) { isStopped = false; } //skip it, this is a trick to stop ELM from listening to more messages and send ready char
                            else if (rxMessage.StartsWith("?")) { isStopped = false; }
                            else if (rxMessage.Length == 19) // is it a valid line
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

                                        receivedMessage(canMessage);
                                    }
                                }
                                catch (Exception)
                                {
                                    //Console.WriteLine("MSG: " + rxMessage);
                                }
                            }
                            //disable whitespace logging
                            if (rxMessage.Length > 0)
                            {
                                logger.Debug("TELNET: " + rxMessage);
                            }
                        }
                    }
                    if (!isStopped)
                    {
                        logger.Debug("TELNET READY");
                        interfaceBusy = false;
                    }
                    rawString = null;
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
            lock (lockObj)
            {
                interfaceBusy = true;
                if (a_message.getID() != _ECUAddress)
                {
                    _ECUAddress = a_message.getID();

                    string command = String.Format("ATSH{0:X3}\r", a_message.getID());
                    SendControlMessage(command, false);
                }

                lastSentCanMessage = a_message.Clone();
                string sendString = GetELMRequest(a_message);

                //add expected responses, but this has to be one char only :(
                if (a_message.elmExpectedResponses != -1 && a_message.elmExpectedResponses < 16)
                    sendString += " " + a_message.elmExpectedResponses.ToString("X1");

                sendString += "\r";

                lastSentTimestamp = Environment.TickCount;
                rawString = WriteToTelnetAndWait(sendString);

                return true; // remove after implementation
            }
        }

        /// <summary>
        /// Creates valid request string for ELM device. Calculates data size and formats it automatically
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string GetELMRequest(CANMessage msg)
        {
            ulong reversed = BitTools.ReverseOrder(msg.getData());
            //var length = BitTools.GetDataSize(reversed);
            return reversed.ToString("X16").Substring(0, msg.getLength() * 2);
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
            if (m_socket.Connected() == false)
                close();
            return m_deviceIsOpen;
        }

        private void Connect()
        {
            if (m_socket.Connected())
                m_socket.Close();
            m_socket.Connect(m_socketAddress, m_forcedBaudrate);   // Typically 192.168.0.10 and port 35000
        }

        //[System.STAThreadAttribute()]
        public override OpenResult open()
        {
            //receiveData = false;

            if (TrionicECU == ECU.TRIONIC7)
                _ECUAddress = 0x220;
            else if (TrionicECU == ECU.TRIONIC8)
                _ECUAddress = 0x7E0;

            try
            {
                logger.Debug("OPEN: Starting MX WiFi Telnet Session: ");
                Connect();

                CastInformationEvent("Reset OBDLink " + WriteToTelnetAndWait("ATZ\r"));            // Reset all
                CastInformationEvent("Show ELM Version " + WriteToTelnetAndWait("ATI\r"));            // ELM Version
                CastInformationEvent("Show STN Version " + WriteToTelnetAndWait("STI\r"));            // STN Version
                //CastInformationEvent("Set 2Mbps rate " + WriteToTelnetAndWait("STBR2000000\r"));    // STN 2Mbps
                //CastInformationEvent("Accept 2Mbps " + WriteToTelnetAndWait("\r"));               // Store new bitrate
                CastInformationEvent("Turn Echo Off " + WriteToTelnetAndWait("ATE0\r"));     //Echo off
                CastInformationEvent("Turn Spaces Off " + WriteToTelnetAndWait("ATS0\r"));     //disable whitespace, should speed up the transmission by eliminating 8 whitespaces for every 19 chars received                

                CastInformationEvent("OPEN: Connected to MX WiFi Telnet");

                string answer = WriteToTelnetAndWait("ATI\r");    //Print version
                logger.Debug("OPEN: Version ELM: " + answer);

                answer = WriteToTelnetAndWait("ATSP6\r");   //Set protocol type ISO 15765-4 CAN (11 bit ID, 500kb/s)
                logger.Debug("OPEN: Protocol select response: " + answer);
                if (answer.StartsWith("OK"))
                {
                    m_deviceIsOpen = true;

                    answer = WriteToTelnetAndWait("ATH1\r");    //ATH1 = Headers ON, so we can see who's talking
                    CastInformationEvent("Turn Headers On " + answer);

                    logger.Debug("OPEN: ATH1 response: " + answer);

                    string command = "ATSH" + _ECUAddress.ToString("X3"); // Set header
                    answer = WriteToTelnetAndWait(command + "\r");
                    CastInformationEvent("Set Header " + answer);

                    /* flow control
                    WriteToSerialAndWait("AT FC SH " + _ECUAddress.ToString("X3") + "\r"); //set the flow control message recipient
                    WriteToSerialAndWait("AT FC SD 30 00 01\r"); //set the flow control content
                    WriteToSerialAndWait("AT FC SM 1\r"); //enable custom flow control
                    */

                    //WriteToSerialAndWait("AT PPS\r"); //display all programmed parameters                    
                    CastInformationEvent("Aggresive Timing On " + WriteToTelnetAndWait("ATAT2\r"));  //aggresive timing adoption, should reduce time wasted for not coming response
                    answer = WriteToTelnetAndWait("ATCAF0\r");   //Can formatting OFF (custom generated PCI byte - SingleFrame, FirstFrame, ConsecutiveFrame, FlowControl)
                    CastInformationEvent("Turn CAN Formating Off " + answer);
                    logger.Debug("OPEN: ATCAF0 response: " + answer);

                    CastInformationEvent("Starting readMessages");
                    if ((m_readThread.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted)) == 0)
                    {
                        CastInformationEvent(m_readThread.Name + " is already running");
                    }
                    else
                    {
                        m_readThread.Start();
                    }
                    if ((m_readThread.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted)) == 0)
                    {
                        CastInformationEvent(m_readThread.Name + " has started");
                    }

                    return OpenResult.OK;
                }
            }
            catch (TimeoutException e)
            {
                CastInformationEvent("System Timeout Exception");
            }
            m_socket.Close();

            return OpenResult.OpenError;
        }


        ///// <summary>
        ///// Calculates message filters and sets it to ELM device. Due to ELM limitations, the filters sometimes might match more than supposed.
        ///// </summary>
        //private void InitializeMessageFilters()
        //{
        //    uint filter = 0xFFF;
        //    foreach (var id in AcceptOnlyMessageIds)
        //    {
        //        filter &= id;
        //    }
        //    SetupCANFilter(AcceptOnlyMessageIds[0].ToString("X3"), filter.ToString("X3"));
        //}

        public override CloseResult close()
        {
            if (m_socket.Connected() == true)
            {
                WriteToTelnetAndWait("ATSP00\r");   // Reset to automatic protocol
                WriteToTelnetAndWait("ATZ\r");      // Reset all
                m_socket.Close();
            }
            m_endThread = true;
            m_deviceIsOpen = false;
            return CloseResult.OK;
        }

        private static readonly StringBuilder response = new StringBuilder();

        private static void DataReceived(string Data)
        {
            lock (response)
                response.Append(Data);
            return;
        }

        private static void ExceptionCaught(Exception Exception)
        {
            throw (Exception);
        }

        private static string WaitFor(string Prompt)
        {
            string reply = null;
            while (response.ToString().IndexOf(Prompt) == -1)
                Thread.Sleep(5);
            lock (response)
            {
                reply = response.ToString();
                response.Length = 0;
            }
            return reply;
        }
        protected void WriteToTelnetWithTrace(string line)
        {
            if (m_socket.Connected() == false)
                logger.Debug(String.Format("TELNET ERROR: Socket closed {0}", line));
            else
            {
                m_socket.Write(line);
                logger.Debug(String.Format("TELNET TX: {0}", line));
            }
        }

        protected string ReadFromTelnetWithTrace(string readTo)
        {
            string result = null;
            if (m_socket.Connected() == false)
                logger.Debug("TELNET ERROR: Socket closed");
            else
            {
                result = WaitFor(readTo);
                int lastlocation = result.LastIndexOf(readTo);
                if (lastlocation > 0)
                {
                    result = result.Substring(0, lastlocation);
                    logger.Debug(String.Format("TELNET RX: {0}{1}", result, readTo));
                }
                else
                    logger.Debug(String.Format("TELNET RX: did not receive {0}", readTo));
            }
            return result;
        }


        protected string WriteToTelnetAndWait(string line)
        {
            return WriteToTelnetAndWait(line, m_transmissionRepeats,">");
        }

        protected string WriteToTelnetAndWait(string line, int tries, string waitToSequence)
        {
            while (tries > 0)
            {
                WriteToTelnetWithTrace(line);
                try
                {
                    string result = ReadFromTelnetWithTrace(waitToSequence);
                    if (result.Contains("?"))
                        tries--;
                    else
                        return result;
                }
                catch (TimeoutException)
                {
                    tries--;
                    logger.Debug(String.Format("TELNET TIMEOUT: left tries {0}", tries));
                }
                catch (Exception x)
                {
                    logger.Debug("Exception" + x);
                }
            }

            return (tries == 0) ? "RETRY ERROR" : "EXCEPTION";
            //if (tries == 0)
            //    return null;
            //return "";
        }

        private static string[] ExtractLines(String inputStr)
        {
            List<string> output = new List<string>();
            while (inputStr.Contains(m_cr_sequence))
            {
                int index = inputStr.IndexOf(m_cr_sequence);
                string line = inputStr.Substring(0, index);
                inputStr = inputStr.Substring(index + m_cr_sequence.Length, inputStr.Length - index - m_cr_sequence.Length);
                output.Add(line);
            }
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

        public override void RequestDeviceReady()
        {
        }

        public override bool IsBusy
        {
            get
            {
                return interfaceBusy;
            }
        }

        public override void SetupCANFilter(string canAddress, string canMask)
        {
            SendControlCommand(string.Format("AT CF {0} \r", canAddress));
            SendControlCommand(string.Format("AT CM {0} \r", canMask));
        }

        public override List<uint> AcceptOnlyMessageIds
        {
            get { return m_AcceptedMessageIds; }
            set
            {
                m_AcceptedMessageIds = value;
                //InitializeMessageFilters(); //this should setup the ELM to monitor all possible these addresses
            }
        }

        public override void SendControlCommand(string msg)
        {
            SendControlMessage(msg, true);
        }

        private void SendControlMessage(string msg, bool waitForReady)
        {
            if (waitForReady)
            {
                while (interfaceBusy)
                    Thread.Sleep(1);
            }
            //read all left bytes
            lock (response)
            {
                response.Length = 0;
            }
            //this should handle the response
            WriteToTelnetAndWait(msg,2,"OK\r\r>");
        }
    }
}
