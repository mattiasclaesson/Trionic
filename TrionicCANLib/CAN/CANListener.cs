using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace TrionicCANLib.CAN
{
    /// <summary>
    /// CANListener is used by the CANDevice for listening for CAN messages.
    /// </summary>
    class CANListener : ICANListener
    {
        private CANMessage m_canMessage = new CANMessage();
        private uint m_waitMsgID = 0;
        private AutoResetEvent m_resetEvent = new AutoResetEvent(false);

        public override bool messagePending()
        {
            return messageReceived;
        }

        //---------------------------------------------------------------------
        /**
        */
        public void setupWaitMessage(uint can_id)
        {
            this.m_waitMsgID = can_id;

            /*this.m_canMessage.setID(0);
            lock (this.m_canMessage)
            {
                this.m_waitMsgID = can_id;
            }*/
        }

        //---------------------------------------------------------------------
        /**
        */

        private Stopwatch sw = new Stopwatch();

        public CANMessage waitMessage(int a_timeout, uint can_id)
        {
            setupWaitMessage(can_id);
            return waitMessage(a_timeout);
        }

        public CANMessage waitMessage(int a_timeout)
        {
            sw.Reset();
            sw.Start();
            CANMessage retMsg = new CANMessage();
            while (sw.ElapsedMilliseconds < a_timeout)
            {
                // search queue for the desired message
                if (_receiveMessageIndex < _readMessageIndex)
                {
                    // first upto (_queue.Length - 1)
                    for (int idx = _readMessageIndex; idx < _queue.Length; idx++)
                    {
                        if (_queue[idx].getID() == this.m_waitMsgID)
                        {
                            retMsg = _queue[idx];
                            _readMessageIndex = idx + 1;
                            if (_readMessageIndex > _queue.Length - 1) _readMessageIndex = 0; // make it circular

                            sw.Stop();
                            return retMsg;
                        }
                    }
                    for (int idx = 0; idx < _receiveMessageIndex; idx++)
                    {
                        if (_queue[idx].getID() == this.m_waitMsgID)
                        {
                            retMsg = _queue[idx];
                            _readMessageIndex = idx + 1;
                            if (_readMessageIndex > _queue.Length - 1) _readMessageIndex = 0; // make it circular
                            sw.Stop();
                            return retMsg;
                        }
                    }
                }
                else
                {
                    for (int idx = _readMessageIndex; idx < _receiveMessageIndex; idx++)
                    {
                        if (_queue[idx].getID() == this.m_waitMsgID)
                        {
                            retMsg = _queue[idx];
                            _readMessageIndex = idx + 1;
                            if (_readMessageIndex > _queue.Length - 1) _readMessageIndex = 0; // make it circular
                            sw.Stop();
                            return retMsg;
                        }
                    }
                }
                Thread.Sleep(1);
            }
            sw.Stop();
            return retMsg;

            /*
            CANMessage retMsg;

            m_resetEvent.WaitOne(a_timeout, true);
            lock (m_canMessage)
            {
                retMsg = m_canMessage;
            }
            messageReceived = false;
            return retMsg;*/
        }

        private bool messageReceived = false;


        private int _receiveMessageIndex = 0;
        private int _readMessageIndex = 0;
        private CANMessage[] _queue;

        public override void dumpQueue()
        {
            
            for (int idx = 0; idx < _queue.Length; idx ++)
            {
                if (_receiveMessageIndex == idx && _readMessageIndex == idx)
                {
                    Console.WriteLine(_queue[idx].getID().ToString("X3") + " " + _queue[idx].getData().ToString("X16") + " RX RD");
                }
                else if (_receiveMessageIndex == idx)
                {
                    Console.WriteLine(_queue[idx].getID().ToString("X3") + " " + _queue[idx].getData().ToString("X16") + " RX");
                }
                else if (_readMessageIndex == idx)
                {
                    Console.WriteLine(_queue[idx].getID().ToString("X3") + " " + _queue[idx].getData().ToString("X16") + "    RD");
                }
                else
                {
                    Console.WriteLine(_queue[idx].getID().ToString("X3") + " " + _queue[idx].getData().ToString("X16"));

                }
            }
        }

        public override void FlushQueue()
        {
            _queue = new CANMessage[128]; //16 might be a bit too little when some thread stops (i.e. the one that writes the log). 
            _receiveMessageIndex = 0;
            _readMessageIndex = 0;
        }

        override public void handleMessage(CANMessage a_message)
        {
            if (_queue == null)
            {
                _queue = new CANMessage[16];
                _receiveMessageIndex = 0;
                _readMessageIndex = 0;
            }

            // add the message to a queue for later processing ... 
            // the queue is a ringbuffer for CANMessage objects.
            // X objects are supported
            // we need a receive and a read pointer for this to work properly
            messageReceived = false;
            //_queue[_receiveMessageIndex] = a_message;
            _queue[_receiveMessageIndex] = new CANMessage();
            _queue[_receiveMessageIndex].setData(a_message.getData());
            _queue[_receiveMessageIndex].setID(a_message.getID());
            _queue[_receiveMessageIndex].setLength(a_message.getLength());

            _receiveMessageIndex++;
            if(_receiveMessageIndex > _queue.Length - 1) _receiveMessageIndex = 0; // make it circular

            //DetermineSize();


            /*
            lock (m_canMessage)
            {
                if (a_message.getID() == m_waitMsgID)
                {
                    m_canMessage = a_message;
                    messageReceived = true;
                }
            }
            if (messageReceived)
            {
                m_resetEvent.Set();
            }*/
        }

        private void DetermineSize()
        {

            int size = 0;
            if (_receiveMessageIndex > _readMessageIndex)
            {
                size = _receiveMessageIndex - _readMessageIndex;
            }
            else
            {
                size = (_queue.Length - 1) - _readMessageIndex + _receiveMessageIndex;
            }
            if (size > 1)
            {
                Console.WriteLine("Buffering: " + size.ToString() + " messages");
                dumpQueue();
            }
        }

        /// <summary>
        /// Clears the queue (advances internal indexes) sp that all old messages will be ignored
        /// </summary>
        public void ClearQueue()
        {
            _readMessageIndex = _receiveMessageIndex;
        }
    }
}
