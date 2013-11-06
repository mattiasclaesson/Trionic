//-------------------------------------------------------------------------------------------------
//	Universal Trionic adapter library
//	(C) Janis Silins, 2010-2013
//  $Id$
//-------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Combi;

namespace TrionicCANLib.CAN
{

//-------------------------------------------------------------------------------------------------
/**
    CAN library driver for LPC17xx based devices.
*/
public class LPCCANDevice : ICANDevice
{
    // dynamic state
    private Thread read_thread;                     ///< reader thread
    private bool term_requested = false;            ///< thread termination flag
    private Object term_mutex = new Object();       ///< mutex for termination flag

    private caCombiAdapter combi;                   ///< adapter object
    private CANMessage in_msg = new CANMessage();   ///< incoming message

    //---------------------------------------------------------------------------------------------
    /**
        Default constructor.
    */
    public LPCCANDevice()
    {  
        // create adapter
        this.combi = new caCombiAdapter();
        Debug.Assert(this.combi != null);
    }

    //---------------------------------------------------------------------------------------------
    /**
        Destructor.
    */
    ~LPCCANDevice()
    {
        // release adapter
        this.close();
        this.combi = null;
    }

    //---------------------------------------------------------------------------------------------
    /**
        Connects to adapter over USB.
      
        @return             succ / fail 
    */
    public bool connect()
    {
        try
        {
            // connect to adapter
            this.combi.Open();
            uint fw_ver = this.combi.GetFirmwareVersion();

            return true;
        }

        catch (Exception e)
        {
            this.AddToCanTrace("Failed to connect to adapter: " + e.Message);
            return false;
        }
    }

    //---------------------------------------------------------------------------------------------
    /**
        Disconnects from adapter.
      
        @return             succ / fail 
    */
    public void disconnect()
    {
        this.combi.Close();
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

    //---------------------------------------------------------------------------------------------
    /**
        Connects to the adapter and activates CAN bus. 

        @return             result 
    */
    public override OpenResult open()
    {
        try
        {
            // connect to adapter
            Console.WriteLine("Connecting LPCCanDevice");
            this.connect();
            Console.WriteLine("Connected LPCCanDevice");

            // try listening on I-bus first
            if (!UseOnlyPBus && this.try_bitrate(47619, !DisableCanConnectionCheck))
            {
                // got traffic
                Console.WriteLine("I-bus connected");

                return OpenResult.OK;
            }
 
            Console.WriteLine("Trying P-bus connection");

            // try P-bus next
            if (!this.try_bitrate(500000, !DisableCanConnectionCheck))
            {
                // give up
                Console.WriteLine("Failed to open canchannel");
                this.combi.Close();
                return OpenResult.OpenError;
            }

            Console.WriteLine("Canchannel opened");
            if(read_thread != null) Console.WriteLine("Threadstate: " + read_thread.ThreadState.ToString());
            // start reader thread
            try
            {
                if (this.read_thread != null) this.read_thread.Abort();
            }
            catch (Exception tE)
            {
                Console.WriteLine("Failed to abort thread: " + tE.Message);
            }
            term_requested = false;
            this.read_thread = new Thread(this.read_messages); // move here to ensure a new thread is started
            this.read_thread.Name = "LPCCANDevice.read_thread";
            this.read_thread.Start();
            return OpenResult.OK;        
        }

        catch (Exception E)
        {
            Console.WriteLine("Failed to open LPCCanDevice: " + E.Message);
            // cleanup
            this.close();

            // adapter not present
            return OpenResult.OpenError;            
        }
    }

    //---------------------------------------------------------------------------------------------
    /**
        Determines if connection to CAN bus is open.
    
        return          open (true/false)
    */
    public override bool isOpen()
    {
        return this.combi.IsOpen();
    }

    //---------------------------------------------------------------------------------------------
    /**
        Closes the connection to CAN bus and adapter.
     
        return          success (true/false)
    */
    public override CloseResult close()
    {
        try
        {
            // terminate worker thread
            Debug.Assert(this.term_mutex != null);
            lock (this.term_mutex)
            {
                this.term_requested = true;
            }
            
            // close connection
            Console.WriteLine("Disconnected from LPCCANDevice");
            this.disconnect();
            return CloseResult.OK;
        }

        catch
        {
            // ignore errors
            return CloseResult.OK;
        }
    }

    //---------------------------------------------------------------------------------------------
    /**
        Flushes communications queue.
    */
    public override void Flush()
    {
        // empty
    }

    //---------------------------------------------------------------------------------------------
    /**
	    Checks if ADC low-pass filter is active.

	    @param		channel		A/D channel number [0...4]

	    @return					active (yes / no)
    */
    public bool GetADCFiltering(uint channel)
    {
        Debug.Assert(this.combi != null);
        return this.combi.GetADCFiltering(channel);
    }

    //---------------------------------------------------------------------------------------------
    /**
	    Enables / disables low-pass filtering for all ADC channels and stores
	    the setting in EEPROM.

	    @param			channel			A/D channel number [0...4]
	    @param			enable			filtering enabled (yes / no)
    */
    public void SetADCFiltering(uint channel, bool enable)
    {
        Debug.Assert(this.combi != null);
        this.combi.SetADCFiltering(channel, enable);
    }

    //---------------------------------------------------------------------------------------------
    /**
        Returns momentary voltage from A/D converter; works in all modes.

        @param		channel		A/D channel number [0...4]

        @return					analog value, V					
    */
    public override float GetADCValue(uint channel)
    {
        Debug.Assert(this.combi != null);
        return this.combi.GetADCValue(channel);
    }

    //---------------------------------------------------------------------------------------------
    /**
        Returns current temperature from K-type thermocouple.

        @param		value		temperature, DegC			
    */
    public override float GetThermoValue()
    {
        Debug.Assert(this.combi != null);
        return this.combi.GetThermoValue();
    }

    //---------------------------------------------------------------------------------------------
    /**
        Creates a new CAN flasher object.
      
        @return             flasher 
    */
    public TrionicCANLib.Flasher.T7CombiFlasher createFlasher()
    {
        Debug.Assert(this.combi != null);
        if (!this.combi.IsOpen())
        {
            this.AddToCanTrace("Failed to create flasher: not connected to adapter");
            return null;
        }

        return new TrionicCANLib.Flasher.T7CombiFlasher(this.combi);
    }

    //---------------------------------------------------------------------------------------------
    /**
        Sends a 11 bit CAN data frame.
     
        @param      msg         CAN message
      
        @return                 success (true/false) 
    */
    public override bool sendMessage(CANMessage msg)
    {
        this.AddToCanTrace("Sending message: " + msg.getID().ToString("X4") + " " + msg.getData().ToString("X16") + " " + msg.getLength().ToString("X2"));

        try
        {
            Combi.caCombiAdapter.caCANFrame frame;
            frame.id = msg.getID(); 
            frame.length = msg.getLength();
            frame.data = msg.getData();
            frame.is_extended = 0;
            frame.is_remote = 0;

            this.combi.CAN_SendMessage(ref frame);

            this.AddToCanTrace("Message sent successfully");
            return true;
        }

        catch (Exception e)
        {
            this.AddToCanTrace("Message failed to send: " + e.Message);
            return false;
        }
    }

    //---------------------------------------------------------------------------------------------
    /**
        Waits for arrival of a specific CAN message or any message if ID = 0.
      
        @param      a_canID     message ID
        @param      timeout     timeout, ms
        @param      canMsg      message
     
        @return                 message ID 
    */
    public override uint waitForMessage(uint a_canID, uint timeout,
        out CANMessage canMsg)
    {
        canMsg = new CANMessage();
        Debug.Assert(canMsg != null);
        canMsg.setID(0);

        caCombiAdapter.caCANFrame frame = new caCombiAdapter.caCANFrame();
        if (this.combi.CAN_GetMessage(ref frame, timeout) && 
            (frame.id == a_canID || a_canID == 0))
        {
            // message received
            canMsg.setID(frame.id);
            canMsg.setLength(frame.length);
            canMsg.setData(frame.data);

            return frame.id;
        }

        // timed out
        return 0;
    }

    //---------------------------------------------------------------------------------------------
    /**
        Tries to connect to CAN bus using the specified bitrate.
    
        @param      bitrate             bitrate
        @param      check_traffic       check for CAN traffic
     
        @return                         succ / fail
    */
    private bool try_bitrate(uint bitrate, bool check_traffic)
    {
        try
        {
            // try connecting
            this.combi.CAN_SetBitrate(bitrate);
            this.combi.CAN_Open(true);

            if (check_traffic)
            {
                // look for bus activity
                CANMessage msg = new CANMessage();
                Debug.Assert(msg != null);

                if (this.waitForMessage(0, 1000, out msg) < 1)
                {
                    throw new Exception("No traffic at given bitrate");
                }
            }

            return true;
        }
       
        catch 
        {
            // failed
            this.combi.CAN_Open(false);
            return false;
        }
    }

    //---------------------------------------------------------------------------------------------
    /**    
        Handles incoming messages.
    */
    private void read_messages()
    {
        caCombiAdapter.caCANFrame frame = new caCombiAdapter.caCANFrame();

        // main loop
        while (true)
        {
            // check for thread termination request
            Debug.Assert(this.term_mutex != null);
            lock (this.term_mutex)
            {
                if (this.term_requested)
                {
                    // exit
                    Console.WriteLine("Reader thread ended");
                    return;
                }
            }

            // receive messages
            if (this.combi.CAN_GetMessage(ref frame, 1000))
            {
                if (acceptMessageId(frame.id))
                {
                    // convert message
                    this.in_msg.setID(frame.id);
                    this.in_msg.setLength(frame.length);
                    this.in_msg.setData(frame.data);

                    // pass message to listeners
                    lock (this.m_listeners)
                    {
                        AddToCanTrace("RX: " + frame.id.ToString("X4") + " " + frame.data.ToString("X16"));
                        foreach (ICANListener listener in this.m_listeners)
                        {
                            listener.handleMessage(this.in_msg);
                        }
                    }
                }
            }
        }
    }
};

}   // end namespace
//-------------------------------------------------------------------------------------------------
//  EOF
//-------------------------------------------------------------------------------------------------