//-----------------------------------------------------------------------------
//  Mictronics CAN<->USB interface driver
//  $Id$
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace TrionicCANLib.CAN
{

//-----------------------------------------------------------------------------
/**
    Wrapper for Mictronics CAN<->USB interface (see http://www.mictronics.de).
*/
public class MictronicsCanDevice : ICANDevice
{
    // driver imports
    [DllImport("mct_can.dll", EntryPoint = "MctAdapter_Create")]
    static extern void MctAdapter_Create();
    [DllImport("mct_can.dll", EntryPoint = "MctAdapter_Release")]
    static extern void MctAdapter_Release();
    [DllImport("mct_can.dll", EntryPoint = "MctAdapter_Open")]
    static extern bool MctAdapter_Open(string bitrate);
    [DllImport("mct_can.dll", EntryPoint = "MctAdapter_IsOpen")]
    static extern bool MctAdapter_IsOpen();
    [DllImport("mct_can.dll", EntryPoint = "MctAdapter_SendMessage")]
    static extern bool MctAdapter_SendMessage(uint id, byte length, ulong data);
    [DllImport("mct_can.dll", EntryPoint = "MctAdapter_ReceiveMessage")]
    static extern bool MctAdapter_ReceiveMessage(out uint id, out byte length, 
        out ulong data);
    [DllImport("mct_can.dll", EntryPoint = "MctAdapter_Close")]
    static extern bool MctAdapter_Close();

    // internal state
    private Thread read_thread;                     ///< reader thread
    private bool term_requested = false;            ///< thread termination flag
    private Object term_mutex = new Object();       ///< mutex for termination flag

    //-------------------------------------------------------------------------
    /**
        Default constructor.
    */
    public MictronicsCanDevice()
    {
        // create adapter object
        MctAdapter_Create();

        // create reader thread
        this.read_thread = new Thread(this.read_messages);
        this.read_thread.Name = "MictronicsCanDevice.read_thread";
        Debug.Assert(read_thread != null);
    }

    //-------------------------------------------------------------------------
    /**
        Destructor.
    */
    ~MictronicsCanDevice()
    {
        MctAdapter_Release();
    }

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

    public override float GetADCValue(uint channel)
    {
        return 0F;
    }

    // not supported by mct
    public override float GetThermoValue()
    {
        return 0F;
    }

    //-------------------------------------------------------------------------
    /**
        Opens a connection to CAN interface. 

        @return             result 
    */
    public override OpenResult open()
    {
        // connect to bus
        if (!MctAdapter_Open("4037"))
        {
            return OpenResult.OpenError;
        }

        // start reader thread
        Debug.Assert(this.read_thread != null);
        if (this.read_thread.ThreadState != System.Threading.ThreadState.Running)
        {
            this.read_thread.Start();
        }

        return OpenResult.OK;
    }

    //-------------------------------------------------------------------------
    /**
        Determines if connection to CAN device is open.
    
        return          open (true/false)
    */
    public override bool isOpen()
    {
        return MctAdapter_IsOpen();
    }

    //-------------------------------------------------------------------------
    /**
        Closes the connection to CAN interface.
     
        return          success (true/false)
    */
    public override CloseResult close()
    {
        return MctAdapter_Close() ? CloseResult.OK : CloseResult.CloseError;
    
        // terminate thread?
    }

    //-------------------------------------------------------------------------
    /**
        Sends a 11 bit CAN data frame.
     
        @param      message     CAN message
      
        @return                 success (true/false) 
    */
    public override bool sendMessage(CANMessage message)
    {
        return MctAdapter_SendMessage(message.getID(), message.getLength(),
            message.getData());
    }

    //-------------------------------------------------------------------------
    /**    
        Handles incoming messages.
    */
    private void read_messages()
    {
        uint id;
        byte length;
        ulong data;

        CANMessage msg = new CANMessage();
        Debug.Assert(msg != null);

        // main loop
        while (true)
        {
            // check for thread termination request
            Debug.Assert(this.term_mutex != null);
            lock (this.term_mutex)
            {
                if (this.term_requested)
                {
                    return;
                }
            }

            // receive messages
            while (MctAdapter_ReceiveMessage(out id, out length, out data))
            {
                if (acceptMessageId(id))
                {
                    // convert message
                    msg.setID(id);
                    msg.setLength(length);
                    msg.setData(data);

                    // pass message to listeners
                    lock (this.m_listeners)
                    {
                        AddToCanTrace("RX: " + id.ToString("X4") + " " + data.ToString("X16"));
                        foreach (ICANListener listener in this.m_listeners)
                        {
                            listener.handleMessage(msg);
                        }
                    }
                }
            }

            // give up CPU for a moment
            Thread.Sleep(1);          
        }
    }

    /// <summary>
    /// waitForMessage waits for a specific CAN message give by a CAN id.
    /// </summary>
    /// <param name="a_canID">The CAN id to listen for</param>
    /// <param name="timeout">Listen timeout</param>
    /// <param name="r_canMsg">The CAN message with a_canID that we where listening for.</param>
    /// <returns>The CAN id for the message we where listening for, otherwise 0.</returns>
    public override uint waitForMessage(uint a_canID, uint timeout, out CANMessage canMessage)
    {
        canMessage = new CANMessage();
        Debug.Assert(canMessage != null);

        int wait_cnt = 0;
        uint id;
        byte length;
        ulong data;
        while (wait_cnt < timeout)
        {
            if (MctAdapter_ReceiveMessage(out id, out length, out data))
            {
                // message received
                canMessage.setID(id);
                canMessage.setLength(length);
                canMessage.setData(data);

                if (canMessage.getID() != a_canID)
                    continue;
                return (uint)canMessage.getID();
            }

            // wait a bit
            Thread.Sleep(1);
            ++wait_cnt;
        }

        // nothing was received
        return 0;
    }

    /// <summary>
    /// waitAnyMessage waits for any message to be received.
    /// </summary>
    /// <param name="timeout">Listen timeout</param>
    /// <param name="r_canMsg">The CAN message that was first received</param>
    /// <returns>The CAN id for the message received, otherwise 0.</returns>
    private uint waitAnyMessage(uint timeout, out CANMessage canMessage)
    {
        canMessage = new CANMessage();
        Debug.Assert(canMessage != null);

        int wait_cnt = 0;
        uint id;
        byte length;
        ulong data;
        while (wait_cnt < timeout)
        {
            if (MctAdapter_ReceiveMessage(out id, out length, out data))
            {
                // message received
                canMessage.setID(id);
                canMessage.setLength(length);
                canMessage.setData(data);

                return id;
            }

            // wait a bit
            Thread.Sleep(1);
            ++wait_cnt;
        }

        // nothing was received
        return 0;
    }

    public override void Flush()
    {
        // empty
    }
};

}   // end namespace
//-----------------------------------------------------------------------------
//  EOF
//-----------------------------------------------------------------------------
