using System;
using System.IO;
using System.Collections.Generic;

namespace TrionicCANLib.Flasher
{
    //-----------------------------------------------------------------------------
    /**
        Trionic 7 CAN flasher base class.
    */
    abstract public class IFlasher
    {
        public delegate void StatusChanged(object sender, StatusEventArgs e);
        abstract public event StatusChanged onStatusChanged;

        public class StatusEventArgs : System.EventArgs
        {
            private string _info;

            public string Info
            {
                get { return _info; }
                set { _info = value; }
            }

            public StatusEventArgs(string info)
            {
                this._info = info;
            }
        }


        // flasher commands
        public enum FlashCommand
        {
            ReadCommand,
            ReadMemoryCommand,
            ReadSymbolMapCommand,
            ReadSymbolNameCommand,
            WriteCommand,
            StopCommand,
            NoCommand
        };

        // status of current flashing session    
        public enum FlashStatus
        {
            Reading,
            Writing,
            Eraseing,
            NoSequrityAccess,
            DoinNuthin,
            Completed,
            NoSuchFile,
            EraseError,
            WriteError,
            ReadError,
            StartingFlashSession,
            WritingBaseSection,
            WritingLastSection
        };

        // dynamic state
        protected FlashStatus m_flashStatus;            ///< current status
        protected FlashCommand m_command;               ///< command
        protected Object m_synchObject = new Object();  ///< state lock

        // configuration
        private bool m_EnableCanLog = false;            ///< CAN logging flag

        //-------------------------------------------------------------------------
        /**
            Default constructor.
        */
        public IFlasher()
        {
            // clear state
            this.m_flashStatus = FlashStatus.DoinNuthin;
            this.m_command = FlashCommand.NoCommand;
        }

        //-------------------------------------------------------------------------
        /**
            Sets CAN logging on/off.
        */
        public bool EnableCanLog
        {
            get { return m_EnableCanLog; }
            set { m_EnableCanLog = value; }
        }

        //-------------------------------------------------------------------------
        /**
            Adds a line to CAN log file.
         
            @param      line        line to add
        */
        protected void AddToCanTrace(string line)
        {
            Console.WriteLine(line);
            if (!this.m_EnableCanLog)
            {
                return;
            }

            DateTime dtnow = DateTime.Now;
            using (StreamWriter sw = new StreamWriter(System.Windows.Forms.Application.StartupPath + "\\CanTraceT7Flasher.txt", true))
            {
                sw.WriteLine(dtnow.ToString("dd/MM/yyyy HH:mm:ss:fff") + " - " + line);
            }
        }

        //-------------------------------------------------------------------------
        /**
            Returns the current status of flasher.
         
            @return                 status 
        */
        public virtual FlashStatus getStatus()
        {
            return this.m_flashStatus;
        }

        public abstract int getNrOfBytesRead();

        //-------------------------------------------------------------------------
        /**
            Interrupts ongoing read or write session.
        */
        public virtual void stopFlasher()
        {
            lock (this.m_synchObject)
            {
                this.m_command = FlashCommand.StopCommand;
            }
        }

        //-------------------------------------------------------------------------
        /**
            Starts a flash reading session.
            
            @param      a_fileName      name of target file
        */
        public virtual void readFlash(string a_fileName)
        {
            lock (this.m_synchObject)
            {
                this.m_command = FlashCommand.ReadCommand;
            }
        }

        //-------------------------------------------------------------------------
        /**
            Starts a SRAM reading session.
            
            @param      a_fileName      name of target file
            @param      a_offset        source offset
            @param      a_length        source length, bytes
        */
        public virtual void readMemory(string a_fileName, UInt32 a_offset,
            UInt32 a_length)
        {
            lock (this.m_synchObject)
            {
                m_command = FlashCommand.ReadMemoryCommand;
            }
        }

        //-------------------------------------------------------------------------
        /**
            Starts a symbol map reading session.
            
            @param      a_fileName      name of target file
        */
        public virtual void readSymbolMap(string a_fileName)
        {
            lock (m_synchObject)
            {
                m_command = FlashCommand.ReadSymbolMapCommand;
            }
        }

        //-------------------------------------------------------------------------
        /**
            Starts writing to flash memory.
            
            @param      a_fileName      name of source file
        */
        public virtual void writeFlash(string a_fileName)
        {
            lock (m_synchObject)
            {
                m_command = FlashCommand.WriteCommand;
            }
        }
    };
}
