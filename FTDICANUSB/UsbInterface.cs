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
using System.Collections.Generic;
using System.Text;
using FTD2XX_NET;
using CanUsbComponent;
using System.Threading;
using System.ComponentModel;
using System.IO;
// Links
// http://stackoverflow.com/questions/2439122/problem-with-two-net-threads-and-hardware-access
// Performance could be inproved using???
// http://blog.liranchen.com/2010/08/reducing-autoresetevents.html
// Research MVVM lite
// http://mvvmlight.codeplex.com/

// MVVM for application
// http://jmorrill.hjtcentral.com/Home/tabid/428/EntryId/432/MVVM-for-Tarded-Folks-Like-Me-or-MVVM-and-What-it-Means-to-Me.aspx
// http://blog.vuscode.com/malovicn/archive/2010/11/07/naked-mvvm-simplest-possible-mvvm-approach.aspx
// I was never sure if the library should expose a queue (via get/put) and semaphore (as in IXXAT c# library)
//   -OR- use a dispatcher / callback to pass data to the application (as in SerialPort) or a combination of the two?!?
//   > Answers on a postcard!
// Good ideas for programming patterns
// http://www.shymbra.com/en/documents/


namespace CanUsbInterface
{
    /*
     How it works..
     * RECEIVED
     * ========
     * The FTDI DLL unblocks the parserTask (BackgroundWorker) when data arrives.
     * When unblocked the parserTask dequeues the string data from the FTDI driver. The string is parsed byte by byte to reassemble fragmented messages.
     * Messages are split into 'streams'. These streams are:
     *  a) RECEIVED (asynchronous) are converted into a CANMsg datatype and placed on a queue. An msgRxvd_Semaphore is given as notification.
     *  b) RESPONSE messages are written to a string. A RspRxcd_Semaphore is given as notification.
     *  
     * TRANSMITTED
     * ===========
     * Sending will block the caller until the response is received. 
     * 
     * Here is quick reference of message strings:
     * SENT COMMANDS and their RESPONSES
     *  Sn\r, O\r, C\r, Miiiiiiii\r, miiiiiiii\r, Zn\r -> \r or \b  (Baud, Open, Close, Acceptance Code, Acceptance Mask, Timestamps)
     *  tiiin[dd...dd]\r, Tiiiiiiiin[dd..ddvvv]\r        -> Z\r or \b (Transmit standard, Transmit extended)
     *  riiin\r,          Riiiiiiiin\r                -> Z\r or \b (Transmit standard RTR, Transmit extended RTR)
     *  F\r -> Fxx\r   (STATUS{b0:RX Q Full, b1:TX Q Full, b2:ErrWrn, b3:DatOvRn, b4:na, b5:ErrPassiv, b6:ArbLost, b7:BussErr})
     *  V\r -> Vxx\r   (Version: 4 x nibbles in BCD)
     *  N\r -> Nxxxx\r (Serial Number)
     * RECEIVED (ansynchronous)
     * tiiin[dd...dd]\r, Tiiiiiiiin[dd..dd]\r  (Received standard, Received extended)
     * 
     * // Other messages seen on the CANBUS -> But NOT documented are:
     * When reading back the serial number..
     * N\r -> N****\r   (Serial number could not be read? Dont know why. Changed parser to accept '*' chars)
     */
    // Adapted from
    // http://stackoverflow.com/questions/530211/creating-a-blocking-queuet-in-net/530228#530228
    public class SizeQueue<T>
    {
        private readonly Queue<T> queue = new Queue<T>();
        private readonly int maxSize;
        public SizeQueue(int maxSize) { this.maxSize = maxSize; }

        public void Enqueue(T item)
        {
            lock (queue)
            {
                while (queue.Count >= maxSize)
                {
                    //Monitor.Wait(queue);
                    queue.Clear();
                }
                queue.Enqueue(item);
                if (queue.Count == 1)
                {
                    // wake up any blocked dequeue 
                    Monitor.PulseAll(queue);
                }
            }
        }
        public bool Dequeue(out T item, int timeout)
        {
            item = default(T);
            lock (queue)
            {
                while (queue.Count == 0)
                {
                    if (Monitor.Wait(queue, timeout) == false) return false;
                }
                item = queue.Dequeue();
                if (queue.Count == maxSize - 1)
                {
                    // wake up any blocked enqueue 
                    Monitor.PulseAll(queue);
                }
                return true;
            }
        }

        public int Count()
        {
            int count;
            lock (queue)
            {
                count = queue.Count;
            }
            return (count);
        }

        public void Clear()
        {
            lock (queue)
            {
                queue.Clear();
            }
        }

    }

    public class CanUsbAdaptor
    {
        // CAN messages received by the CANUSB adaptor
        SizeQueue<CanMessage> AsyncMsgRxvdQueue;
        // Responses from the Adaptor
        SizeQueue<string> AckNackRspQueue;
        //////////////////////
        // This thread parses the data coming from the CANUSB device (via the FTDI DLL) and places
        // it on the relevant queue. It then signals and continues to process incoming data.
        private BackgroundWorker parserTask;
        //////////////////////
        // FTDI DLL Related
        private FTDI _ftdi;
        // Vendor ID of the CanUsb device
        private const UInt32 CanUsbPIDVID = 0x403FFA8;
        private string _ftdiSerial;
        // The FTDI DLL thread uses the AutoResetEvent to signal the backround worker that data has arrived.
        // When signalled the background worker parses the string and places individual messages into a queue 
        private AutoResetEvent fdtiRxvdDataEvent;
        // We have to start this interface with events that the caller can block on
        public CanUsbAdaptor()
        {
            // Setup events / queues
            AsyncMsgRxvdQueue = new SizeQueue<CanMessage>(1000);
            AckNackRspQueue = new SizeQueue<string>(20);
            ///////////////////////
            // FTDI related setup
            _ftdi = new FTDI();
            fdtiRxvdDataEvent = new AutoResetEvent(false);
            // Parser - task that takes data from FTDI driver and parses data.
            parserTask = new BackgroundWorker();
            parserTask.DoWork += ReadData;
            if (!parserTask.IsBusy)
            {
                parserTask.RunWorkerAsync();
            }
        }

        public string FtdiSerialNumber
        {
            get { return _ftdiSerial; }
        }

        public bool IsOpen
        {
            get { return _ftdi.IsOpen; }
        }

        public string[] GetFtdiSerialnumbers()
        {
            List<string> strlist = new List<string>();
            FTDI.FT_STATUS stat;
            // Allocate storage for device info list
            UInt32 numPorts = 0;
            // Determine the number of FTDI devices connected to the machine
            stat = _ftdi.GetNumberOfDevices(ref numPorts);
            // Check status
            if (stat == FTDI.FT_STATUS.FT_OK)
            {

                Console.WriteLine("Number of FTDI devices: " + numPorts.ToString());
                Console.WriteLine("");
                // The call worked, scan through the devices and find the CAN USB devices
                if (numPorts > 0)
                {
                    // Yes... add them to the list. Provide a spare slot incase a new adaptor is inserted
                    // between GetNumberOfDevices and GetDeviceList
                    FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[numPorts+1];
                    stat = _ftdi.GetDeviceList(ftdiDeviceList);
                    if (stat == FTDI.FT_STATUS.FT_OK)
                    {
                        for (UInt32 i = 0; i < numPorts; i++)
                        {
                            // A threading issue between GetNumberOfDevices and GetDeviceList could result in
                            // null entries in ftdiDeviceList. Filter null entries here
                            if (ftdiDeviceList[i] != null)
                            {
                                // Only add CANUSB devices that are closed AND that mach the CANUSB device PID/VID
                                if (((ftdiDeviceList[i].Flags & FTDI.FT_FLAGS.FT_FLAGS_OPENED) == 0x00) &&
                                    (ftdiDeviceList[i].ID == CanUsbPIDVID))
                                {
                                    // Then it is a CAN usb device that can be opened
                                    strlist.Add(ftdiDeviceList[i].SerialNumber);
                                }
                                if (ftdiDeviceList[i].ID == CanUsbPIDVID)
                                {
                                    // debug info
                                    Console.WriteLine("Device Index: " + i.ToString());
                                    Console.WriteLine("Flags: " + String.Format("{0:x}", ftdiDeviceList[i].Flags));
                                    Console.WriteLine("Type: " + ftdiDeviceList[i].Type.ToString());
                                    Console.WriteLine("ID: " + String.Format("{0:x}", ftdiDeviceList[i].ID));
                                    Console.WriteLine("Location ID: " + String.Format("{0:x}", ftdiDeviceList[i].LocId));
                                    Console.WriteLine("Serial Number: " + ftdiDeviceList[i].SerialNumber.ToString());
                                    Console.WriteLine("Description: " + ftdiDeviceList[i].Description.ToString());
                                    Console.WriteLine("");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Null entry");
                            }

                        }
                    }
                }
            }
            return (strlist.ToArray());
        }

        public bool Open(string serialnumber)
        {
            bool retval = false;
            // If we have no string, dont even try to open the adaptor
            if (serialnumber != "")
            {
                try
                {
                    _ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
                    // Reset the parsers
                    ResetParser();
                    // Open the port.
                    _ftdi.OpenBySerialNumber(serialnumber);
                    // Is it open?
                    if (_ftdi.IsOpen)
                    {
                        // Setup the FTDI port params
                        FTDI.FT_STATUS status = _ftdi.SetEventNotification(FTDI.FT_EVENTS.FT_EVENT_RXCHAR, fdtiRxvdDataEvent);
                        // Set latency so if a single message is received, it will not wait in the buffer for long before it is processed
                        _ftdi.SetLatency(4);
                        // Force a flush every \r. Not guaranteed. 
                        _ftdi.SetCharacters((byte)'\r', true, 0x00, false);
                        _ftdi.SetBaudRate(2000000);
                        // We have successfully opened the port. Save the port name.
                        _ftdiSerial = serialnumber;
                        retval = true;
                    }
                    else
                    {
                        _ftdiSerial = "";
                    }
                }
                catch (Exception)
                {
                    _ftdiSerial = "";
                    Console.WriteLine("Call to FTDI DLL caused an exception");
                }
            }
            return (retval);
        }

        public bool Close()
        {
            bool retval = false;
            // If it is open.. Close it.
            if (_ftdi.IsOpen)
            {
                lock (_ftdi)
                {
                    _ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
                    _ftdi.Close();
                }
            }
            // If it is closed, return true,
            if (!_ftdi.IsOpen)
            {
                _ftdiSerial = "";
                retval = true;
            }
            return retval;
        }



        // This runs on the FTDI driver thread. It is supplied to the driver as a callback to 
        //  parse the incoming data and extract the data, ACKs and NACKs.
        private void ReadData(object pSender, DoWorkEventArgs pEventArgs)
        {
            // When traffic gets heavy, expect data to arrive in random chunks with no regard for the start
            // or end of the CanUsb message.
            // Don’t expect the \r to be the last char we receive.. the string often ends half way
            // through a message.. It could be the next .Read() starts with a /r at the beginning of the string.
            // If a fragment of a CanUsb message is at the end of the string, we can’t throw it away.. it
            // needs to prefixed to the next incoming message otherwise we are going to throw away data.
            // Aswell as this, when we connect to a CanUsb device that is in full flow, dont expect it
            // to start at the beginning of a CanUsb message.. 
            // - This explains some of the complexity of the code below...
            // There is the added headache that an ACK to some messages is only indicated by a \r
            // inserted between the received messages - which are themselves terminated by a \r
            //  "t12300\rt12300\r\rt12300"   - spot the ack to RTR transmit..
            // .. it could arrive like this.. "t12300\rt12300\r" then "\rt12300"
            // To avoid ugly code, I have chosen to parse incoming data byte by byte using a statemachine.
            UInt32 chars_expected = 0;
            // The number of times the timestamp has  
            while (true)
            {
                // Block until we receive a signal from the FTDI driver that data has arrived..
                this.fdtiRxvdDataEvent.WaitOne();
                // Check how much data there is
                if (_ftdi.GetRxBytesAvailable(ref chars_expected) == FTDI.FT_STATUS.FT_OK)
                {
                    // Get the data..
                    string str_incoming;
                    UInt32 chars_actual = 0;
                    if (_ftdi.Read(out str_incoming, chars_expected, ref chars_actual) == FTDI.FT_STATUS.FT_OK)
                    {
                        if (chars_expected != chars_actual) Console.WriteLine("Expected=" + chars_expected.ToString() +
                            ", Actual=" + chars_actual.ToString());
                        // Convert to stream for easy parsing..
                        if (chars_actual > 0)
                        {
                            // Split into two streams.. Incoming message - and ACKs / NAKs
                            string str = "";
                            foreach (char ch in str_incoming)
                            {
                                // Extract the incoming received CAN messages ('t' or 'T')
                                STATE messageType;
                                str = ParseChar(ch, out messageType);
                                if (messageType == STATE.CANMSG)
                                {
                                    // We have received an async CAN message
                                    CanMessage addmessage = new CanMessage(str);
                                    AsyncMsgRxvdQueue.Enqueue(addmessage);
                                }
                                else if (messageType == STATE.RESPONSE)
                                {
                                    // We have and ACK NACK or RSP. Add it to the queue
                                    AckNackRspQueue.Enqueue(str);
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool DequeueAsyncMsgRxvd(out CanMessage canmsg, int timeout)
        {
            return(AsyncMsgRxvdQueue.Dequeue(out canmsg, timeout));
        }

        // Interface function to read out async CAN messages received by the CANUSB Adaptor
        public int CountAckNakRsp()
        {
            return (AckNackRspQueue.Count());
        }
        public void FlushAckNakRsp()
        {
            AckNackRspQueue.Clear();
        }
        // Interface function to read out CANUSB Adaptor generated responses
        public bool DequeueAckNakRsp(out string msg, int timeout)
        {
            return (AckNackRspQueue.Dequeue(out msg, timeout));
        }

        // PARSER//////////////////////////////
        private enum STATE
        {
            FIRST,
            CANMSG,
            RESPONSE,
            EMPTY,
        }
        private STATE state = STATE.FIRST;
        private StringBuilder rawmsg = new StringBuilder(30);
        private string ParseChar(char ch, out STATE isCanMessage)
        {
            string retval = "";
            isCanMessage = STATE.EMPTY;
            // Process char
            switch (state)
            {
                case STATE.FIRST:
                    if (ch == '\r')
                    {
                        isCanMessage = STATE.RESPONSE;
                        retval = "\r";
                    }
                    // If there is an \a, this is taken to be a NACK
                    else if (ch == '\a')
                    {
                        isCanMessage = STATE.RESPONSE;
                        retval = "\a";
                    }
                    // Check for a CAN message received
                    else if ((ch == 't') || (ch == 'T') ||
                        (ch == 'r') || (ch == 'R'))
                    {
                        // Clear the string and add the byte..
                        rawmsg.Length = 0; // Clear string builder contents
                        rawmsg.Append(ch);
                        // Get the rest of the message
                        state = STATE.CANMSG;
                    }
                    // Check for a response to see if it a response to a messages that may have been sent
                    else if ((ch == 'F') || (ch == 'V') || (ch == 'N') ||
                        (ch == 'z') || (ch == 'Z'))
                    {
                        // Clear the string and add the byte..
                        rawmsg.Length = 0; // Clear string builder contents
                        rawmsg.Append(ch);
                        // Get the rest of the message
                        state = STATE.RESPONSE;
                    }
                    else
                    {
                        // Unrecognised char. Idle.
                        Console.WriteLine("ClassAckNackRspRxvdParser Error=" + ch.ToString());
                        state = STATE.FIRST;
                    }
                    break;
                case STATE.CANMSG:
                    // Append the received char
                    if (((ch >= '0') && (ch <= '9')) || ((ch >= 'A') && (ch <= 'F')))
                    {
                        // The message body
                        rawmsg.Append(ch);
                    }
                    else
                    {
                        state = STATE.FIRST;
                        // Not a hex value
                        if (ch == '\r')
                        {
                            // We have reached the end of the message
                            isCanMessage = STATE.CANMSG;
                            retval = rawmsg.ToString();
                        }
                        else if (ch == '\a')
                        {
                            Console.WriteLine("Unexpected NACK Rxved during reception of CAN Message" + ch.ToString());
                        }
                        else
                        {
                            // We have received an invalid char Exit
                            Console.WriteLine("Invalid char Rxved during reception of CAN Message" + ch.ToString());
                        }
                    }
                    break;
                case STATE.RESPONSE:
                    // Append the received char
                    if ((Char.IsLetterOrDigit(ch)) || (ch == '*'))
                    {
                        // The message body
                        rawmsg.Append(ch);
                    }
                    else
                    {
                        state = STATE.FIRST;
                        // Not a hex value
                        if (ch == '\r')
                        {
                            // We have reached the end of the message
                            isCanMessage = STATE.RESPONSE;
                            retval = rawmsg.ToString();
                        }
                        else if (ch == '\a')
                        {
                            Console.WriteLine("Unexpected NACK Rxved during reception of Response" + ch.ToString());
                        }
                        else
                        {
                            // We have received an invalid char Exit
                            Console.WriteLine("Invalid char Rxved during reception of Response" + ch.ToString());
                        }
                    }
                    break;
            }
            return (retval);
        }
        // If we need to reset the parser, the set to idle. This also takes
        // care of clearing the rawmsg string data.
        private void ResetParser()
        {
            state = STATE.FIRST;
        }

        // Send a message to CANUSB adaptor. Wait for a response.
        public bool SendMessage(string msgtx)
        {
            bool retval = false;
            UInt32 numBytesWritten = 0;
            FTDI.FT_STATUS status = _ftdi.Write(msgtx, msgtx.Length, ref numBytesWritten);
            if (status != FTDI.FT_STATUS.FT_OK)
            {
                Console.WriteLine("FTDI Write Status ERROR: " + status);
            }
            else if (numBytesWritten < msgtx.Length)
            {
                Console.WriteLine("FTDI Write Length ERROR: " + status + " length " + msgtx.Length +
                                " written " + numBytesWritten);
            }
            else
            {
                retval = true;
            }
            return (retval);
        }
    }
}
