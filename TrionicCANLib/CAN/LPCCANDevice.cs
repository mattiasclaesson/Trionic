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
using NLog;

// Trionic 5 patch
using TrionicCANLib.API;


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
    private Logger logger = LogManager.GetCurrentClassLogger();

    //---------------------------------------------------------------------------------------------
    /**
        Default constructor.
    */
    public LPCCANDevice()
    {  
        // create adapter
        combi = new caCombiAdapter();
        Debug.Assert(combi != null);
    }

    //---------------------------------------------------------------------------------------------
    /**
        Destructor.
    */
    ~LPCCANDevice()
    {
        // release adapter
        close();
        combi = null;
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
            combi.Open();
            uint fw_ver = combi.GetFirmwareVersion();

            return true;
        }

        catch (Exception e)
        {
            logger.Debug("Failed to connect to adapter: " + e.Message);
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
        combi.Close();
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

    public override void SetSelectedAdapter(string adapter)
    {
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
            Boolean open = true;

            // connect to adapter
            logger.Debug("Connecting LPCCanDevice");
            connect();
            logger.Debug("Connected LPCCanDevice");

            if (TrionicECU == ECU.TRIONIC5)
            {
                logger.Debug("Trying P-bus connection @615000");
                open = try_bitrate(615000, false);
            }
            else if (TrionicECU == ECU.TRIONIC7)
            {
                if (!UseOnlyPBus)
                {
                    logger.Debug("Trying I-bus connection @47619");
                    if (!try_bitrate(47619, false))
                    {
                        open = false;
                        logger.Debug("Failed to open I-bus canchannel");

                        logger.Debug("Trying P-bus connection @500000");
                        open = try_bitrate(500000, false);
                    }
                }
                else
                {
                    logger.Debug("Trying P-bus connection @500000");
                    open = try_bitrate(500000, false);
                }
            }
            else
            {
                logger.Debug("Trying P-bus connection @500000");
                open = try_bitrate(500000, false);
            }

            if (!open)
            {
                logger.Debug("Failed to open canchannel");
                combi.Close();
                return OpenResult.OpenError;
            }

            PrepareThread();
            return OpenResult.OK;
        }

        catch (Exception E)
        {
            logger.Debug("Failed to open LPCCanDevice: " + E.Message);
            // cleanup
            close();

            // adapter not present
            return OpenResult.OpenError;
        }
    }

    private void PrepareThread()
    {
        logger.Debug("Canchannel opened");
        if (read_thread != null)
        {
            try
            {
                read_thread.Abort();
            }
            catch (Exception tE)
            {
                logger.Debug("Failed to abort thread: " + tE.Message);
            }
        }
        term_requested = false;
        read_thread = new Thread(read_messages); // move here to ensure a new thread is started
        read_thread.Name = "LPCCANDevice.read_thread";
        read_thread.Start();
    }

    //---------------------------------------------------------------------------------------------
    /**
        Determines if connection to CAN bus is open.
    
        return          open (true/false)
    */
    public override bool isOpen()
    {
        return combi.IsOpen();
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
            Debug.Assert(term_mutex != null);
            lock (term_mutex)
            {
                term_requested = true;
            }
            
            // close connection
            logger.Debug("Disconnected from LPCCANDevice");
            disconnect();
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
	    Checks if ADC low-pass filter is active.

	    @param		channel		A/D channel number [0...4]

	    @return					active (yes / no)
    */
    public bool GetADCFiltering(uint channel)
    {
        Debug.Assert(combi != null);
        return combi.GetADCFiltering(channel);
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
        Debug.Assert(combi != null);
        combi.SetADCFiltering(channel, enable);
    }

    //---------------------------------------------------------------------------------------------
    /**
        Returns momentary voltage from A/D converter; works in all modes.

        @param		channel		A/D channel number [0...4]

        @return					analog value, V					
    */
    public override float GetADCValue(uint channel)
    {
        Debug.Assert(combi != null);
        return combi.GetADCValue(channel);
    }

    //---------------------------------------------------------------------------------------------
    /**
        Returns current temperature from K-type thermocouple.

        @param		value		temperature, DegC			
    */
    public override float GetThermoValue()
    {
        Debug.Assert(combi != null);
        return combi.GetThermoValue();
    }

    //---------------------------------------------------------------------------------------------
    /**
        Creates a new CAN flasher object.
      
        @return             flasher 
    */
    public TrionicCANLib.Flasher.T7CombiFlasher createFlasher()
    {
        Debug.Assert(combi != null);
        if (!combi.IsOpen())
        {
            logger.Debug("Failed to create flasher: not connected to adapter");
            return null;
        }

        return new TrionicCANLib.Flasher.T7CombiFlasher(combi);
    }

    //---------------------------------------------------------------------------------------------
    /**
        Sends a 11 bit CAN data frame.
     
        @param      msg         CAN message
      
        @return                 success (true/false) 
    */
    protected override bool sendMessageDevice(CANMessage msg)
    {
        try
        {
            caCombiAdapter.caCANFrame frame;
            frame.id = msg.getID(); 
            frame.length = msg.getLength();
            frame.data = msg.getData();
            frame.is_extended = 0;
            frame.is_remote = 0;

            combi.CAN_SendMessage(ref frame);
            return true;
        }
        catch (Exception e)
        {
            logger.Debug("tx failed with Exception.Message: " + e.Message);
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
        if (combi.CAN_GetMessage(ref frame, timeout) && 
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
            combi.CAN_SetBitrate(bitrate);
            combi.CAN_Open(true);

            if (check_traffic)
            {
                // look for bus activity
                CANMessage msg = new CANMessage();
                Debug.Assert(msg != null);

                if (waitForMessage(0, 1000, out msg) < 1)
                {
                    throw new Exception("No traffic at given bitrate");
                }
            }

            return true;
        }
       
        catch 
        {
            // failed
            combi.CAN_Open(false);
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
            Debug.Assert(term_mutex != null);
            lock (term_mutex)
            {
                if (term_requested)
                {
                    // exit
                    logger.Debug("Reader thread ended");
                    return;
                }
            }

            // receive messages
            if (combi.CAN_GetMessage(ref frame, 1000))
            {
                if (acceptMessageId(frame.id))
                {
                    // convert message
                    in_msg.setID(frame.id);
                    in_msg.setLength(frame.length);
                    in_msg.setData(frame.data);

                    receivedMessage(in_msg);
                }
            }
            else
            {
                Thread.Sleep(1);
            }
        }
    }
};

}   // end namespace
//-------------------------------------------------------------------------------------------------
//  EOF
//-------------------------------------------------------------------------------------------------