using System;
using System.Collections.Generic;
using System.Diagnostics;
using Combi;

namespace TrionicCANLib.Flasher
{
    //-------------------------------------------------------------------------------------------------
    public class T7CombiFlasher : IFlasher
    {
        // dynamic state
        private readonly caCombiAdapter combi;       ///< adapter object

        // events
        public override event IFlasher.StatusChanged onStatusChanged;

        //---------------------------------------------------------------------------------------------
        /**
            Constructor.
          
            @param      _combi      adapter object
        */
        public T7CombiFlasher(caCombiAdapter _combi)
        {
            Debug.Assert(_combi != null);
            this.combi = _combi;
        }

        //---------------------------------------------------------------------------------------------
        /**
            Returns the current status of flasher.
         
            @return                 status 
        */
        public override FlashStatus getStatus()
        {
            if (this.combi.OperationRunning())
            {
                // in progress
                return this.m_flashStatus;
            }

            if (!this.combi.OperationSucceeded())
            {
                // failed (no way to know the real reason)
                switch (this.m_command)
                {
                    case FlashCommand.ReadCommand:
                    case FlashCommand.ReadMemoryCommand:
                    case FlashCommand.ReadSymbolMapCommand:
                    case FlashCommand.ReadSymbolNameCommand:
                        return FlashStatus.ReadError;

                    case FlashCommand.WriteCommand:
                        return FlashStatus.WriteError;
                };
            }

            return FlashStatus.Completed;
        }

        //---------------------------------------------------------------------------------------------
        /**
            Returns the number of bytes that has been read or written so far; 0 is 
            returned if there is no read or write session ongoing.
         
            @return                 number of bytes 
        */
        public override int getNrOfBytesRead()
        {
            return (int)this.combi.GetOperationProgress();
        }

        //---------------------------------------------------------------------------------------------
        /**
            Interrupts ongoing read or write session.
        */
        public override void stopFlasher()
        {
            this.endSession();

            base.stopFlasher();
            this.m_flashStatus = FlashStatus.Completed;
        }

        //---------------------------------------------------------------------------------------------
        /**
            Starts a flash reading session.
            
            @param      a_fileName      name of target file
        */
        public override void readFlash(string a_fileName)
        {
            base.readFlash(a_fileName);

            try
            {
                // connect to ECU; this may take some time as both
                // P-Bus and I-bus are checked for traffic
                if (!this.beginSession())
                {
                    throw new Exception("Failed to start session");
                }

                // read flash    
                this.combi.CAN_ReadFlash(a_fileName);
                this.m_flashStatus = FlashStatus.Reading;
            }

            catch (Exception e)
            {
                this.AddToFlasherTrace("Read error: " + e.Message);
                this.m_flashStatus = FlashStatus.ReadError;
            }
        }

        //---------------------------------------------------------------------------------------------
        /**
            Starts a flash writing session.
            
            @param      a_fileName      name of target file
        */
        public override void writeFlash(string a_fileName)
        {
            base.writeFlash(a_fileName);

            try
            {
                // connect to ECU; this may take some time as both
                // P-Bus and I-bus are checked for traffic
                if (!this.beginSession())
                {
                    throw new Exception("Failed to start session");
                }

                // read flash    
                this.combi.CAN_WriteFlash(a_fileName, 0);
                this.m_flashStatus = FlashStatus.Writing;
            }

            catch (Exception e)
            {
                this.AddToFlasherTrace("Write error: " + e.Message);
                this.m_flashStatus = FlashStatus.WriteError;
            }
        }

        //---------------------------------------------------------------------------------------------
        /**
            Begins a communication session with ECU.
          
            @return             succ / fail
        */
        public bool beginSession()
        {
            try
            {
                this.combi.CAN_ConnectECU(3);
                return true;
            }

            catch (Exception e)
            {
                this.AddToFlasherTrace("Session error: " + e.Message);
                return false;
            }
        }

        //---------------------------------------------------------------------------------------------
        /**
            Ends a communication session with ECU.     
        */
        public void endSession()
        {
            this.AddToFlasherTrace("End communication session");
            this.combi.CAN_DisconnectECU(false);
        }
    };
}
