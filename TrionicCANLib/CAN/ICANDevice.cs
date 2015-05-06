using System;
using System.Collections.Generic;
using System.IO;
using TrionicCANLib.API;

namespace TrionicCANLib.CAN
{
    /// <summary>
    /// OpenResult is returned by the open method to report the status of the opening.
    /// </summary>
    public enum OpenResult
    {
        OK,
        OpenError
    }
    /// <summary>
    /// CloseResult is returned by the close method to report the status of the closening.
    /// </summary>
    public enum CloseResult
    {
        OK,
        CloseError
    }

    /// <summary>
    /// ICANDevice is an interface class for CAN devices. It is used to hide the differences 
    /// there are in the CAN drivers from different manufactureres (since there is no 
    /// standardised driver model for CAN devices). 
    /// For each new CAN device there must be a class that inherits from this and all
    /// the abstract methods must be implemented in the sub class.
    /// </summary>
    public abstract class ICANDevice
    {
        public class InformationFrameEventArgs : System.EventArgs
        {
            private CANMessage _message;

            public CANMessage Message
            {
                get
                {
                    return _message;
                }
                set
                {
                    _message = value;
                }
            }

            public InformationFrameEventArgs(CANMessage message)
            {
                this._message = message;
            }
        }

        public class InformationEventArgs : System.EventArgs
        {
            private string _info;

            public string Info
            {
                get
                {
                    return _info;
                }
                set
                {
                    _info = value;
                }
            }

            public InformationEventArgs(string info)
            {
                this._info = info;
            }
        }

        public delegate void ReceivedAdditionalInformation(object sender, InformationEventArgs e);
        public event ReceivedAdditionalInformation onReceivedAdditionalInformation;


        public delegate void ReceivedAdditionalInformationFrame(object sender, InformationFrameEventArgs e);
        public event ReceivedAdditionalInformationFrame onReceivedAdditionalInformationFrame;

        public void CastInformationEvent(CANMessage message)
        {
            if (onReceivedAdditionalInformationFrame != null)
            {
                onReceivedAdditionalInformationFrame(this, new InformationFrameEventArgs(message));
            }
        }

        public void CastInformationEvent(string info)
        {
            if (onReceivedAdditionalInformation != null)
            {
                onReceivedAdditionalInformation(this, new InformationEventArgs(info));
            }
        }

        private bool m_OnlyPBus = true;

        public bool UseOnlyPBus
        {
            get { return m_OnlyPBus; }
            set { m_OnlyPBus = value; }
        }

        private bool _DisableCanConnectionCheck = false;

        public bool DisableCanConnectionCheck
        {
            get { return _DisableCanConnectionCheck; }
            set { _DisableCanConnectionCheck = value; }
        }

        protected List<uint> m_AcceptedMessageIds;

        public virtual List<uint> AcceptOnlyMessageIds
        {
            get { return m_AcceptedMessageIds; }
            set { m_AcceptedMessageIds = value; }
        }

        private ECU m_ECU = ECU.TRIONIC7;

        public ECU TrionicECU
        {
            get { return m_ECU; }
            set { m_ECU = value; }
        }

        /// <summary>
        /// This method opens the device for reading and writing.
        /// There is no mechanism for setting the bus speed so this method must
        /// detect this.
        /// </summary>
        /// <returns>OpenResult</returns>
        abstract public OpenResult open();

        /// <summary>
        /// This method closes the device for reading and writing.
        /// </summary>
        /// <returns>CloseResult</returns>
        abstract public CloseResult close();

        /// <summary>
        /// This method checks if the CAN device is opened or closed.
        /// </summary>
        /// <returns>true if device is open, otherwise false</returns>
        abstract public bool isOpen();

        /// <summary>
        /// This message sends a CANMessage to the CAN device.
        /// The open method must have been called and returned possitive result
        /// before this method is called.
        /// </summary>
        /// <param name="a_message">The CANMessage</param>
        /// <returns>true on success, otherwise false.</returns>
        abstract public bool sendMessage(CANMessage a_message);
        abstract public uint waitForMessage(uint a_canID, uint timeout, out CANMessage canMsg);
        abstract public float GetThermoValue();
        abstract public float GetADCValue(uint channel);

        static public string[] GetAdapterNames(){return new string[0];}
        abstract public void SetSelectedAdapter(string adapter);

        /// <summary>
        /// This method adds a ICANListener. Any number of ICANListeners can be added (well,
        /// it's limited to processor speed and memory).
        /// </summary>
        /// <param name="a_listener">The ICANListener to be added.</param>
        /// <returns>true on success, otherwise false.</returns>
        public bool addListener(ICANListener a_listener)
        {
            lock (m_listeners)
            {
                m_listeners.Add(a_listener);
            }
            return true;
        }

        /// <summary>
        /// This method removes a ICANListener.
        /// </summary>
        /// <param name="a_listener">The ICANListener to remove.</param>
        /// <returns>true on success, otherwise false</returns>
        public bool removeListener(ICANListener a_listener)
        {
            lock (m_listeners)
            {
                m_listeners.Remove(a_listener);
            }
            return true;
        }

        protected bool acceptMessageId(uint msgId)
        {
            return m_AcceptedMessageIds == null ? true : m_AcceptedMessageIds.Contains(msgId);
        }

        abstract public int ForcedBaudrate
        {
            get;
            set;
        }

        protected List<ICANListener> m_listeners = new List<ICANListener>();

        #region ELM327 specific

        /// <summary>
        /// Used for ELM327 only to stop waiting for messages. This is because when getting lots of responses (i.e. 19 for getting flash)
        /// ELM will wait for next message for some time (30-50ms), and this slows down the flash download
        /// </summary>
        public virtual void RequestDeviceReady()
        {
        }

        /// <summary>
        /// This indicates that interface is busy. Used only by ELM327 for now
        /// </summary>
        public virtual bool IsBusy
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Setups can filter. Used by ELM327 implementation only
        /// </summary>
        /// <param name="canAddress">Can address(i.e. 7E8)</param>
        /// <param name="canMask">Can mask (i.e. DFF with 7E8 should allow to get both 7E8 and 5E8 responses). 
        /// If set to 000, then all the can traffic will be visible to ELM</param>
        public virtual void SetupCANFilter(string canAddress, string canMask) { }

        /// <summary>
        /// sends control command (like ATST)
        /// </summary>
        /// <param name="cmd">The command with \r like "ATST01\r"</param>
        public virtual void SendControlCommand(string cmd)
        {

        }

        #endregion ELM327 specific
    }
}
