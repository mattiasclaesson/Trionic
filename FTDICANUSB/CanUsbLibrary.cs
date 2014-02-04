/*
    CanUsbComponent library for CAN-USB device
    Copyright (C) 2012 J Newcomb / http://www.jnewcomb.com/cv

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>
*/
using System;
using System.Text;
using System.Threading;
using CanUsbInterface;
using CanUsbComponent;

namespace CanUsbComponent
{
    /// <summary>
    /// Can communication speed
    /// </summary>
    public enum ECanBps : int
    {
        Baud10kBps = 0,
        Baud20kBps = 1,
        Baud50kBps = 2,
        Baud100kBps = 3,
        Baud125kBps = 4,
        Baud250kBps = 5,
        Baud500kBps = 6,
        Baud800kBps = 7,
        Baud1MBps = 8,
        BaudNotSetup = 9,
    }

    public struct CanStatusFlags
    {
        // Bit 0 CAN receive FIFO queue full
        public bool RxFifoFull;
        // Bit 1 CAN transmit FIFO queue full
        public bool TxFifoFull;
        // Bit 2 Error warning (EI), see SJA1000 datasheet
        public bool ErrorWarning;
        // Bit 3 Data Overrun (DOI), see SJA1000 datasheet
        public bool DataOverrun;
        // Bit 4 Not used.
        // Bit Error Passive (EPI), see SJA1000 datasheet
        public bool ErrorPassive;
        // Bit 6 Arbitration Lost (ALI), see SJA1000 datasheet *
        public bool ArbitrationLost;
        // Bit 7 Bus Error (BEI), see SJA1000 datasheet **
        public bool BusError;


        public CanStatusFlags(byte flags)
        {
            if ((flags & (Convert.ToByte(1 << 0))) == 0)
                RxFifoFull = false;
            else
                RxFifoFull = true;

            if ((flags & (Convert.ToByte(1 << 1))) == 0)
                TxFifoFull = false;
            else
                TxFifoFull = true;

            if ((flags & (Convert.ToByte(1 << 2))) == 0)
                ErrorWarning = false;
            else
                ErrorWarning = true;

            if ((flags & (Convert.ToByte(1 << 3))) == 0)
                DataOverrun = false;
            else
                DataOverrun = true;

            //if ((flags & (Convert.ToByte(1 << 4))) != 0) - Not used

            if ((flags & (Convert.ToByte(1 << 5))) == 0)
                ErrorPassive = false;
            else
                ErrorPassive = true;

            if ((flags & (Convert.ToByte(1 << 6))) == 0)
                ArbitrationLost = false;
            else
                ArbitrationLost = true;

            if ((flags & (Convert.ToByte(1 << 7))) == 0)
                BusError = false;
            else
                BusError = true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("RxFifoFull=");
            sb.Append(RxFifoFull);
            sb.Append(", ");
            sb.Append("TxFifoFull=");
            sb.Append(TxFifoFull);
            sb.Append(", ");
            sb.Append("ErrorWarning=");
            sb.Append(ErrorWarning);
            sb.Append(", ");
            sb.Append("DataOverrun=");
            sb.Append(DataOverrun);
            sb.Append(", ");
            sb.Append("ErrorPassive=");
            sb.Append(ErrorPassive);
            sb.Append(", ");
            sb.Append("ArbitrationLost=");
            sb.Append(ArbitrationLost);
            sb.Append(", ");
            sb.Append("BusError=");
            sb.Append(BusError);
            return (sb.ToString());
        }
    }

    public enum AdaptorState : int
    {
        Closed,
        ConnectedToInterface,
        CanBusOpen,
    }
    /// <summary>
    /// Can analyzer component main
    /// </summary>
    public class CanUsbComponentClass
    {
        private CanUsbAdaptor _canUsbAdaptor;
        private AdaptorState _connectionState;
        private string _version;
        //private string _ftdiSerial;
        private string _canUsbSerial;

        // Properties
        public AdaptorState State
        {
            get
            {
                // Check the state of the underlying connection
                if (_canUsbAdaptor.IsOpen == false)
                {
                    _connectionState = AdaptorState.Closed;
                }
                return _connectionState;
            }
        }

        // BaudRate
        private ECanBps _BaudrateCurrent;
        public ECanBps BaudrateCurrent
        {
            get
            {
                ECanBps retval;
                if (_canUsbAdaptor.IsOpen == false)
                {
                    retval = ECanBps.BaudNotSetup;
                }
                else
                {
                    retval = _BaudrateCurrent;
                }
                return (_BaudrateCurrent);
            }
        }

        public string[] AdaptorSerialNumbers
        {
            get { return _canUsbAdaptor.GetFtdiSerialnumbers(); }
        }

        public string FtdiSerialNumber
        {
            get
            {
                // Check the state of the underlying connection
                string serial = "";
                if (_canUsbAdaptor.IsOpen == true)
                {
                    serial = _canUsbAdaptor.FtdiSerialNumber;
                }
                return serial;
            }
        }

        public string AdaptorVersion
        {
            get { return (_version); }
        }

        public string CanUsbSerialnumber
        {
            get { return (_canUsbSerial); }
        }

        // Constructor
        public CanUsbComponentClass()
        {
            _connectionState = AdaptorState.Closed;
            _canUsbAdaptor = new CanUsbAdaptor();
            _BaudrateCurrent = ECanBps.BaudNotSetup;
        }

        public bool OpenCanBus(string serialnumber, ECanBps baud, UInt16 CodeMaskA, UInt16 FilerMaskA)
        {
            return (OpenCanBus(serialnumber, baud, CodeMaskA, FilerMaskA, CodeMaskA, FilerMaskA));
        }

        public bool OpenCanBus(string serialnumber, ECanBps baud)
        {
            return (OpenCanBus(serialnumber, baud, 0x0000, 0xFFFF, 0x0000, 0xFFFF));
        }

        public bool OpenCanBus(string serialnumber, ECanBps baud, UInt16 CodeMaskA, UInt16 FilerMaskA, UInt16 CodeMaskB, UInt16 FilerMaskB)
        {
            bool retval = false;
            // If we have not selected a FTDI serial number, choose the first in the list.
            // Firstly, check if the adaptor is closed
            if (_canUsbAdaptor.IsOpen == false)
            {
                if (serialnumber == "")
                {
                    try
                    {
                        string[] adaptors = _canUsbAdaptor.GetFtdiSerialnumbers();
                        if (adaptors.Length >= 1)
                        {
                            // We have one or more adaptors. Open the first one
                            serialnumber = adaptors[0];
                        }
                        else
                        {
                            Console.WriteLine("No adaptors are connected. Open aborted");
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Failed to readback the list of adaptors connected");
                    }
                }
                // Set the state to 'closed'
                _connectionState = AdaptorState.Closed;
                // It is closed. Now try to open it.
                if (_canUsbAdaptor.Open(serialnumber))
                {
                    // Then we successully opened the adaptor. Flush any pending messages
                    if (FlushCanUsb() == false)
                    {
                        Console.WriteLine("Flush failed");
                    }
                    // Read the version
                    // We could already be connected to the CAN bus if it was not previously closed.
                    // To check if this is the case, poll with a status message. if we get a NACK, we are
                    // not connected.
                    CanStatusFlags statspoll;
                    if (RequestStatus(out statspoll))
                    {
                        Console.WriteLine("CANBUS port is already open on this adaptor. Closing and re-opening.");
                        if (StartReceiving(false) == false)
                        {
                            Console.WriteLine("Failed to close CANBUS port");
                        }
                    }
                    // We have connected to the adaptor.. and at this point the CAN port is closed.
                    _connectionState = AdaptorState.ConnectedToInterface;
                    // Read the adaptor version number
                    if (GetVersionInformation(out _version) == false)
                    {
                        Console.WriteLine("Can't read adaptor version");
                    }
                    else
                    {
                        Console.WriteLine("Version = " + AdaptorVersion);
                    }
                    // Read the adaptor serial number written on the adaptor #nnnn
                    if (GetCanUsbSerialNumber(out _canUsbSerial) == false)
                    {
                        Console.WriteLine("Can't read adaptor serial number");
                    }
                    else
                    {
                        Console.WriteLine("Serial = " + FtdiSerialNumber);
                    }
                    // Set the Adaptor to generate timestamp information
                    if (SetTimeStamp(true) == false)
                    {
                        Console.WriteLine("Can't set the adaptor to generate timestamps");
                    }
                    // The first thing we must do is set the baud rate.
                    // If we dont set the baud rate, then setting the mask will fail.
                    if (baud != ECanBps.BaudNotSetup)
                    {
                        if (SetCanSpeed(baud))
                        {
                            // AFTER baudrate setup, now we can set the masks.
                            if (SetAcceptanceCodeMask(CodeMaskA, FilerMaskA, CodeMaskB, FilerMaskB))
                            {
                                // After baudrate and masks, we can no open the CANBUS connection
                                if (StartReceiving(true))
                                {
                                    // Flush the CAN buffers of pending messages. We dont know how long they
                                    // have been sitting in the hardware RX bufferes
                                    FlushCanUsb();
                                    // Set the state to 'open'
                                    _connectionState = AdaptorState.CanBusOpen;
                                    // Return true.. the only path that signals all is well!
                                    retval = true;
                                }
                                else
                                {
                                    Console.WriteLine("Adaptor is open, but unable to open a connection on the CANBUS" + baud.ToString());
                                }
                            }
                            else
                            {
                                Console.WriteLine("Unable to set Code / Mask");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Unable to set baud rate" + baud.ToString());
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid baud rate");
                    }
                }
                 else
                {
                    Console.WriteLine("Unable to open serialnumber=" + serialnumber);
                }
            }
            else
            {
                Console.WriteLine("The CANUSB adaptor is already open. Closing now..");
                CloseCanBus();
            }
            return (retval);
        }

        public bool CloseCanBus()
        {
            bool retval = false;
            // Firstly, check if the adaptor is open
            if (_canUsbAdaptor.IsOpen)
            {
                // Close the CANBUS connection
                if (StartReceiving(false) == true)
                {
                    _connectionState = AdaptorState.ConnectedToInterface;
                }
                else
                {
                    Console.WriteLine("Unable to close the CANBUS connection");
                }
                // Close the Adaptor
                if (_canUsbAdaptor.Close())
                {
                    _connectionState = AdaptorState.Closed;
                    // We have closed. Reset the serial numbers / verison information
                    _version = "";
                    _canUsbSerial = "";
                    _BaudrateCurrent = ECanBps.BaudNotSetup;
                    retval = true;
                }
                else
                {
                    Console.WriteLine("Unable to close Adaptor");
                }
            }
            else
            {
                Console.WriteLine("The CANUSB adaptor is already closed");
                _connectionState = AdaptorState.Closed;
            }
            return (retval);
        }

        public bool GetCanMessage(out CanMessage canmsg, int timeout)
        {
            return (_canUsbAdaptor.DequeueAsyncMsgRxvd(out canmsg, timeout));
        }

        /// <summary>
        /// Send a message to CANUSB adaptor. Wait for an ACK / NACK / RSP from the CANUSB adaptor.
        /// NOTE: When sending CAN Frame, the response we get from the adaptor indicates the
        /// frame has been sent and NOT that we have received a response to that frame sent.
        /// All receive CAN frames will arrive as an asynchronous message.
        /// </summary>
        /// <param name="msg">message string to send</param>
        /// <returns>True on response received</returns>
        private Mutex mu = new Mutex();
        private bool SendGenericBlockingMessage(string msgtx, out string msgrx, int timeoutms)
        {
            // Default data rx
            msgrx = "";
            if (_canUsbAdaptor.IsOpen == false)
            {
                Console.WriteLine("Adaptor must be open before a message can be sent");
                return (false);
            }
            // Before we send the message to the port, get the mutex to prevent more than one outstanding message
            if (mu.WaitOne(timeoutms) == false)
            {
                Console.WriteLine("Unable to access Send Mutex");
                return (false);
            }
            // Send the message
            if (_canUsbAdaptor.SendMessage(msgtx) == false)
            {
                Console.WriteLine("Failed to send");
                return (false);
            }
            bool retval = false;
            // Wait for the ACK / NACK / RSP
            if (_canUsbAdaptor.DequeueAckNakRsp(out msgrx, timeoutms))
            {
                // We have a response. If any more remain, flush them
                int count = _canUsbAdaptor.CountAckNakRsp();
                if (count > 0)
                {
                    Console.WriteLine("Flushed " + count.ToString() + " Ack Nak Rsp");
                    _canUsbAdaptor.FlushAckNakRsp();
                }
                retval = true;
            }
            else
            {
                Console.WriteLine("ACK NACK RSP Not received after " + timeoutms.ToString() + "ms timeout");
            }
            // Release the mutex
            mu.ReleaseMutex();
            // Return the result
            return (retval);
        }


        /// <summary>
        /// Sends can communication speed setting to the device when connected
        /// </summary>
        /// <param name="bps">Communication Speed</param>
        /// <returns>True on success</returns>
        private bool FlushCanUsb()
        {
            bool retval = false;
            if (_canUsbAdaptor.IsOpen)
            {
                string rxstr;
                Thread.Sleep(20);
                retval = SendGenericBlockingMessage("\r", out rxstr, 200);
                Thread.Sleep(50);
                retval = SendGenericBlockingMessage("\r", out rxstr, 200);
                Thread.Sleep(50);
                retval = SendGenericBlockingMessage("\r", out rxstr, 200);
                Thread.Sleep(50);
            }
            return (retval);
        }

        private bool GetVersionInformation(out string version)
        {
            bool retval = false;
            version = "";
            if (_canUsbAdaptor.IsOpen)
            {
                string rxstr;
                if (SendGenericBlockingMessage("V\r", out rxstr, 200) == true)
                {
                    if (rxstr.StartsWith("V"))
                    {
                        // Format is "Vnnnn\r"
                        rxstr.TrimStart('V');
                        try
                        {
                            version = rxstr.Substring(1, 4);
                        }
                        catch
                        {
                            version = "????";
                        }
                        retval = true;
                    }
                }
            }
            return (retval);
        }

        private bool GetCanUsbSerialNumber(out string serial)
        {
            bool retval = false;
            serial = "";
            if (_canUsbAdaptor.IsOpen)
            {
                string rxstr;
                if (SendGenericBlockingMessage("N\r", out rxstr, 200) == true)
                {
                    if (rxstr.StartsWith("N"))
                    {
                        // Format is "Vnnnn\r"
                        rxstr.TrimStart('N');
                        try
                        {
                            serial = "#" + rxstr.Substring(1, 4);
                        }
                        catch
                        {
                            serial = "#????";
                        }
                        retval = true;
                    }
                }
            }
            return (retval);
        }
        /// <summary>
        /// Sends can communication speed setting to the device when connected
        /// </summary>
        /// <param name="bps">Communication Speed</param>
        /// <returns>True on success</returns>
        private bool SetCanSpeed(ECanBps bps)
        {
            bool retval = false;
            if (_canUsbAdaptor.IsOpen)
            {
                string rxstr;
                if (SendGenericBlockingMessage("S" + Convert.ToString((int)bps) + "\r", out rxstr, 200) == true)
                {
                    if (rxstr == "\r")
                    {
                        // Save the baude rate
                        _BaudrateCurrent = bps;
                        retval = true;
                    }
                }
            }
            return (retval);
        }
        /// <summary>
        /// Start/Stop receiving data from CAN module
        /// </summary>
        /// <param name="yes">True: Start reading, False: Stop reading</param>
        /// <returns>Return true on success</returns>
        private bool StartReceiving(bool yes)
        {
            bool retval = false;
            string txstr;
            if (yes) txstr = "O";
            else txstr = "C";
            if (_canUsbAdaptor.IsOpen)
            {
                string rxstr;
                if (SendGenericBlockingMessage(txstr + "\r", out rxstr, 200) == true)
                {
                    if (rxstr == "\r")
                    {
                        retval = true;
                    }
                }
            }
            return (retval);
        }
        /// <summary>
        /// Sends a can message through the device
        /// </summary>
        /// <param name="msg">CAN message</param>
        /// <returns>Return true on success</returns>
        public bool SendCanMessage(CanMessage msg)
        {
            if (_canUsbAdaptor.IsOpen)
            {
                string rxstr;
                if (SendGenericBlockingMessage(msg.ToTransmitString(), out rxstr, 200) == true)
                {
                    if ((rxstr == "z") || (rxstr == "Z")) return (true);
                }
            }
            return false;
        }
        /// <summary>
        /// Sends a status request message to the device when CAN channel is open
        /// </summary>
        /// <returns>True on success</returns>
        public bool RequestStatus(out CanStatusFlags status)
        {
            status = new CanStatusFlags(0);
            // Init the status
            bool retval = false;
            if (_canUsbAdaptor.IsOpen)
            {
                string rxstr;
                if (SendGenericBlockingMessage("F\r", out rxstr, 200) == true)
                {
                    // Check the response
                    if (rxstr.StartsWith("F"))
                    {
                        // Extract the data
                        try
                        {
                            byte byteval = Convert.ToByte(rxstr.Substring(1, 2), 16);
                            status = new CanStatusFlags(byteval);
                            retval = true;
                        }
                        catch
                        {
                            Console.WriteLine("Failed to convert status string");
                        }

                    }
                }
            }
            return (retval);
        }
        /// <summary>
        /// Sets Time Stamp On/Off for received frames
        /// </summary>
        /// <param name="set">true to set/false to OFF</param>
        /// <returns>true on succesful operation</returns>
        private bool SetTimeStamp(bool set)
        {
            // Init the status
            bool retval = false;
            if (_canUsbAdaptor.IsOpen)
            {
                // Set the flag to a 1 (enabled) or 0 (disabled)
                int flagvalue = 0;
                if (set) flagvalue = 1;
                // Send the message
                string rxstr;
                if (SendGenericBlockingMessage("Z" + flagvalue.ToString("X1") + "\r", out rxstr, 200) == true)
                {
                    if (rxstr == "\r")
                    {
                        // Message send OK and ACK received
                        retval = true;
                    }
                }
            }
            return (retval);
        }
        /// <summary>
        /// Set the CAN acceptance code / acceptance mask values
        /// </summary>
        /// <returns>true on succesful operation</returns>
        /// 
        private bool SetAcceptanceCodeMask(UInt16 CodeMaskA, UInt16 FilerMaskA, UInt16 CodeMaskB, UInt16 FilerMaskB)
        {
            // Init the status
            bool retval = false;
            // Build Acceptance and filer mask
            UInt32 code = ((((UInt32)CodeMaskB << 16) & 0xFFFF0000) | ((UInt32)CodeMaskA & 0x0000FFFF));
            UInt32 mask = ((((UInt32)FilerMaskB  << 16) & 0xFFFF0000) | ((UInt32)FilerMaskA  & 0x0000FFFF));
            if (_canUsbAdaptor.IsOpen)
            {
                string rxstr;
                // Send acceptance code / acceptance mask
                if (SendGenericBlockingMessage("M" + code.ToString("X8") + "\r", out rxstr, 200))
                {
                    if (rxstr == "\r")
                    {
                        // Message sent OK and correctly ACKed
                        if (SendGenericBlockingMessage("m" + mask.ToString("X8") + "\r", out rxstr, 200))
                        {
                            if (rxstr == "\r")
                            {
                                // Both messages were send and correctly ACKed
                                retval = true;
                            }
                            else
                            {
                                Console.WriteLine("Mask NACKed");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Mask not sent");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Code flags NACKed");
                    }
                }
                else
                {
                    Console.WriteLine("Code flags not sent");
                }
            }
            return (retval);
        }

    }

}
