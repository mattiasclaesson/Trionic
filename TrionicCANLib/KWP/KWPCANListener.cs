using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TrionicCANLib.CAN;

namespace TrionicCANLib.KWP
{
    /// <summary>
    /// KWPCANListener is used by the KWPCANDevice for listening for CAN messages.
    /// </summary>
    class KWPCANListener : ICANListener
    {
        private CANMessage m_canMessage = new CANMessage();
        private CANMessage m_EmptyMessage = new CANMessage();
        private uint m_waitMsgID = 0;
        private AutoResetEvent m_resetEvent = new AutoResetEvent(false);
        private bool messageReceived = false;

        public override void FlushQueue()
        {
            
        }

        public override void dumpQueue()
        {
            
        }

        public override bool messagePending()
        {
            return messageReceived;
        }

        //---------------------------------------------------------------------
        /**
        */
        public void setupWaitMessage(uint can_id)
        {
            this.m_canMessage.setID(0); 
            lock (this.m_canMessage)
            {
                this.m_waitMsgID = can_id;
            }   
        }

        //---------------------------------------------------------------------
        /**
        */
        public CANMessage waitMessage(int a_timeout)
        {          
            CANMessage retMsg;

            if(m_resetEvent.WaitOne(a_timeout, true))
            {
                lock (m_canMessage)
                {
                    retMsg = m_canMessage;
                }
            }
            else
            {
                retMsg = m_EmptyMessage;
            }
            messageReceived = false;
            return retMsg;
        }

        override public void handleMessage(CANMessage a_message)
        {
            lock (m_canMessage)
            {
                if (a_message.getID() == m_waitMsgID)
                {
                    m_canMessage.setData(a_message.getData());
                    m_canMessage.setFlags(a_message.getFlags());
                    m_canMessage.setID(a_message.getID());
                    m_canMessage.setLength(a_message.getLength());
                    m_canMessage.setTimeStamp(a_message.getTimeStamp());
                    messageReceived = true;
                    m_resetEvent.Set();
                }
            }
        }
    }
}
