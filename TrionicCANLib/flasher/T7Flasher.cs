using System;
using System.IO;
using System.Collections.Generic;
using TrionicCANLib.KWP;
using System.Threading;

namespace TrionicCANLib.Flasher
{
    //-----------------------------------------------------------------------------
    /// <summary>
    /// T7Flasher handles reading and writing of flash in Trionic 7 ECUs.
    /// 
    /// To use this class a KWPHandler must be set for the communication.
    /// </summary>
    public class T7Flasher : IFlasher
    {

        public override event IFlasher.StatusChanged onStatusChanged;

        /// <summary>
        /// This method returns the number of bytes that has been read or written so far.
        /// 0 is returned if there is no read or write session ongoing.
        /// </summary>
        /// <returns>Number of bytes that has been read or written.</returns>
        /// 
        public override int getNrOfBytesRead() { return m_nrOfBytesRead; }

        public static void setKWPHandler(KWPHandler a_kwpHandler)
        {
            if (m_kwpHandler != null)
                throw new Exception("KWPHandler already set");

            m_kwpHandler = a_kwpHandler;
        }

        public static T7Flasher getInstance()
        {
            if (m_kwpHandler == null)
                throw new Exception("KWPHandler not set");
            if (m_instance == null)
                m_instance = new T7Flasher();
            return m_instance;
        }

        /// <summary>
        /// Constructor for T7Flasher.
        /// </summary>
        /// <param name="a_kwpHandler">The KWPHandler to be used for the communication.</param>
        public T7Flasher()
        {
            m_thread = new Thread(run);
            m_thread.Name = "T7Flasher.m_thread";
            m_thread.Start();
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~T7Flasher()
        {
            lock (m_synchObject)
            {
                m_endThread = true;
            }
            m_resetEvent.Set();
        }

        /// <summary>
        /// This method starts a reading session.
        /// </summary>
        /// <param name="a_fileName">Name of the file where the flash contents is saved.</param>
        public override void readFlash(string a_fileName)
        {
            base.readFlash(a_fileName);
            lock (m_synchObject)
            {
                m_fileName = a_fileName;
            }
            m_resetEvent.Set();
        }

        /// <summary>
        /// This method starts a reading session for reading memory.
        /// </summary>
        /// <param name="a_fileName">Name of the file where the flash contents is saved.</param>
        /// <param name="a_offset">Starting address to read from.</param>
        /// <param name="a_length">Length to read.</param>
        public override void readMemory(string a_fileName, UInt32 a_offset, UInt32 a_length)
        {
            base.readMemory(a_fileName, a_offset, a_length);
            lock (m_synchObject)
            {
                m_fileName = a_fileName;
                m_offset = a_offset;
                m_length = a_length;
            }
            m_resetEvent.Set();
        }

        /// <summary>
        /// This method starts symbol map.
        /// </summary>
        /// <param name="a_fileName">Name of the file where the flash contents is saved.</param>
        /// <param name="a_offset">Starting address to read from.</param>
        /// <param name="a_length">Length to read.</param>
        public override void readSymbolMap(string a_fileName)
        {
            base.readSymbolMap(a_fileName);
            lock (m_synchObject)
            {
                m_fileName = a_fileName;
            }
            m_resetEvent.Set();
        }

        /// <summary>
        /// This method starts writing to flash.
        /// </summary>
        /// <param name="a_fileName">The name of the file from where to read the data from.</param>
        public override void writeFlash(string a_fileName)
        {
            base.writeFlash(a_fileName);
            lock (m_synchObject)
            {
                m_fileName = a_fileName;
            }
            m_resetEvent.Set();
        }

        bool gotSequrityAccess = false;

        private void SetFlashStatus(FlashStatus status)
        {
            m_flashStatus = status;
            
        }
        /// <summary>
        /// The run method handles writing and reading. It waits for a command to start read
        /// or write and handles this command until it's completed, stopped or until there is 
        /// a failure.
        /// </summary>
        void run()
        {
            while (true)
            {
                AddToCanTrace("Running T7Flasher");
                m_nrOfRetries = 0;
                m_nrOfBytesRead = 0;
                m_resetEvent.WaitOne(-1, true);
                gotSequrityAccess = false;
                lock (m_synchObject)
                {
                    if (m_endThread)
                    {
                        return;
                    }
                }
                if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Starting session..."));
                
                m_kwpHandler.startSession();
                AddToCanTrace("Session started");
                if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Session started, requesting security access to ECU"));
                if (!gotSequrityAccess)
                {
                    AddToCanTrace("No security access");

                    for (int nrOfSequrityTries = 0; nrOfSequrityTries < 5; nrOfSequrityTries++)
                    {

                        if (!KWPHandler.getInstance().requestSequrityAccess(true))
                        {
                            AddToCanTrace("No security access granted");
                            
                        }
                        else
                        {
                            gotSequrityAccess = true;
                            AddToCanTrace("Security access granted");

                            break;
                        }
                    }
                }
                if (!gotSequrityAccess)
                {
                    SetFlashStatus(FlashStatus.NoSequrityAccess);
                    AddToCanTrace("No security access granted after 5 retries");
                    if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Failed to get security access after 5 retries"));
                }
                //Here it would make sense to stop if we didn't ge security access but
                //let's try anyway. It could be that we don't get a possitive reply from the 
                //ECU if we alredy have security access (from a previous, interrupted, session).
                if (m_command == FlashCommand.ReadCommand)
                {
                    const int nrOfBytes = 64;
                    byte[] data;
                    AddToCanTrace("Reading flash content to file: " + m_fileName);
                    if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Reading data from ECU..."));


                    if (File.Exists(m_fileName))
                        File.Delete(m_fileName);
                    FileStream fileStream = File.Create(m_fileName, 1024);
                    AddToCanTrace("File created");
                    SetFlashStatus(FlashStatus.Reading);
                    AddToCanTrace("Flash status is reading");

                    for (int i = 0; i < 512 * 1024 / nrOfBytes; i++)
                    {
                        lock (m_synchObject)
                        {
                            if (m_command == FlashCommand.StopCommand)
                                continue;
                            if (m_endThread)
                                return;
                        }

                        while (!m_kwpHandler.sendReadRequest((uint)(nrOfBytes * i), (uint)nrOfBytes))
                        {
                            m_nrOfRetries++;
                        }

                        while (!m_kwpHandler.sendRequestDataByOffset(out data))
                        {
                            m_nrOfRetries++;
                        }
                        fileStream.Write(data, 0, nrOfBytes);
                        m_nrOfBytesRead += nrOfBytes;
                    }
                    fileStream.Close();
                    AddToCanTrace("Closed file");

                    m_kwpHandler.sendDataTransferExitRequest();
                    AddToCanTrace("Done reading");

                }
                else if (m_command == FlashCommand.ReadMemoryCommand)
                {
                    int nrOfBytes = 64;
                    byte[] data;
                    if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Reading data from ECU..."));

                    if (File.Exists(m_fileName))
                        File.Delete(m_fileName);
                    FileStream fileStream = File.Create(m_fileName, 1024);
                    int nrOfReads = (int)m_length / nrOfBytes;
                    for (int i = 0; i < nrOfReads; i++)
                    {
                        lock (m_synchObject)
                        {
                            if (m_command == FlashCommand.StopCommand)
                                continue;
                            if (m_endThread)
                                return;
                        }
                        SetFlashStatus(FlashStatus.Reading);

                        if (i == nrOfReads - 1)
                            nrOfBytes = (int)m_length - nrOfBytes * i;
                        while (!m_kwpHandler.sendReadRequest((uint)m_offset + (uint)(nrOfBytes * i), (uint)nrOfBytes))
                        {
                            m_nrOfRetries++;
                        }

                        while (!m_kwpHandler.sendRequestDataByOffset(out data))
                        {
                            m_nrOfRetries++;
                        }
                        Console.WriteLine("Writing data to file: " + m_length + " bytes");
                        fileStream.Write(data, 0, nrOfBytes);
                        m_nrOfBytesRead += nrOfBytes;
                    }
                    fileStream.Close();
                    Console.WriteLine("Done reading");
                    m_kwpHandler.sendDataTransferExitRequest();
                }
                else if (m_command == FlashCommand.ReadSymbolMapCommand)
                {
                    byte[] data;
                    string swVersion = "";
                    m_nrOfBytesRead = 0;
                    if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Reading symbol map from ECU..."));

                    if (File.Exists(m_fileName))
                        File.Delete(m_fileName);
                    FileStream fileStream = File.Create(m_fileName, 1024);
                    if (m_kwpHandler.sendUnknownRequest() != KWPResult.OK)
                    {
                        if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Failed to read data from ECU..."));
                        SetFlashStatus(FlashStatus.ReadError);
                        continue;
                    }
                    SetFlashStatus(FlashStatus.Reading);
                    m_kwpHandler.getSwVersionFromDR51(out swVersion);

                    if (m_kwpHandler.sendReadSymbolMapRequest() != KWPResult.OK)
                    {
                        if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Failed to read data from ECU..."));

                        SetFlashStatus(FlashStatus.ReadError);
                        continue;
                    }
                    m_kwpHandler.sendDataTransferRequest(out data);
                    while (data.Length > 0x10)
                    {
                        fileStream.Write(data, 1, data.Length - 3);
                        m_nrOfBytesRead += data.Length - 3;
                        lock (m_synchObject)
                        {
                            if (m_command == FlashCommand.StopCommand)
                                continue;
                            if (m_endThread)
                                return;
                        }
                        m_kwpHandler.sendDataTransferRequest(out data);
                    }
                    fileStream.Flush();
                    fileStream.Close();
                }
                else if (m_command == FlashCommand.WriteCommand)
                {
                    AddToCanTrace("Write command seen");
                    const int nrOfBytes = 128;
                    int i = 0;
                    byte[] data = new byte[nrOfBytes];
                    if (!gotSequrityAccess)
                    {
                        SetFlashStatus(FlashStatus.Completed);
                        continue;
                    }
                    if (!File.Exists(m_fileName))
                    {
                        SetFlashStatus(FlashStatus.NoSuchFile);
                        AddToCanTrace("No such file found: " + m_fileName);
                        if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Failed to find file to flash..."));

                        continue;
                    }
                    AddToCanTrace("Start erasing");
                    if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Erasing flash..."));

                    SetFlashStatus(FlashStatus.Eraseing);
                    if (m_kwpHandler.sendEraseRequest() != KWPResult.OK)
                    {
                        if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Failed to erase flash..."));
                        SetFlashStatus(FlashStatus.EraseError);
                        AddToCanTrace("Erase error occured");
                        // break;
                    }
                    AddToCanTrace("Opening file for reading");

                    FileStream fs = new FileStream(m_fileName, FileMode.Open, FileAccess.Read);

                    SetFlashStatus(FlashStatus.Writing);
                    AddToCanTrace("Set flash status to writing");
                    if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Writing flash... 0x00000-0x7B000"));

                    //Write 0x0-0x7B000
                    AddToCanTrace("0x0-0x7B000");
                    Thread.Sleep(100);
                    if (m_kwpHandler.sendWriteRequest(0x0, 0x7B000) != KWPResult.OK)
                    {
                        if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Failed to write data to flash..."));

                        SetFlashStatus(FlashStatus.WriteError);
                        AddToCanTrace("Write error occured");

                        continue;
                    }
                    for (i = 0; i < 0x7B000 / nrOfBytes; i++)
                    {
                        fs.Read(data, 0, nrOfBytes);
                        m_nrOfBytesRead = i * nrOfBytes;
                        AddToCanTrace("sendWriteDataRequest " + m_nrOfBytesRead);
                        if (m_kwpHandler.sendWriteDataRequest(data) != KWPResult.OK)
                        {
                            if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Failed to write data to flash..."));
                            SetFlashStatus(FlashStatus.WriteError);
                            AddToCanTrace("Write error occured " + m_nrOfBytesRead);

                            continue;
                        }
                        lock (m_synchObject)
                        {
                            if (m_command == FlashCommand.StopCommand)
                            {
                                AddToCanTrace("Stop command seen");
                                continue;
                            }
                            if (m_endThread)
                            {
                                AddToCanTrace("Thread ended");
                                return;
                            }
                        }
                    }

                    //Write 0x7FE00-0x7FFFF
                    AddToCanTrace("Write 0x7FE00-0x7FFFF");
                    if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Writing flash... 0x7FE00-0x7FFFF"));

                    if (m_kwpHandler.sendWriteRequest(0x7FE00, 0x200) != KWPResult.OK)
                    {
                        if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Failed to write data to flash..."));
                        SetFlashStatus(FlashStatus.WriteError);
                        AddToCanTrace("Write error occured");
                        continue;
                    }
                    fs.Seek(0x7FE00, System.IO.SeekOrigin.Begin);
                    for (i = 0x7FE00 / nrOfBytes; i < 0x80000 / nrOfBytes; i++)
                    {
                        fs.Read(data, 0, nrOfBytes);
                        m_nrOfBytesRead = i * nrOfBytes;
                        AddToCanTrace("sendWriteDataRequest " + m_nrOfBytesRead.ToString());

                        if (m_kwpHandler.sendWriteDataRequest(data) != KWPResult.OK)
                        {
                            if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Failed to write data to flash..."));
                            SetFlashStatus(FlashStatus.WriteError);
                            AddToCanTrace("Write error occured " + m_nrOfBytesRead);
                            continue;
                        }
                        lock (m_synchObject)
                        {
                            if (m_command == FlashCommand.StopCommand)
                            {
                                AddToCanTrace("Stop command seen");
                                continue;
                            }
                            if (m_endThread)
                            {
                                AddToCanTrace("Thread ended");
                                return;
                            }
                        }
                    }
                }
                if (onStatusChanged != null) onStatusChanged(this, new StatusEventArgs("Flasing procedure completed"));
                AddToCanTrace("T7Flasher completed");
                SetFlashStatus(FlashStatus.Completed);
            }
        }


        private readonly Thread m_thread;
        private readonly AutoResetEvent m_resetEvent = new AutoResetEvent(false);
        private string m_fileName;
        private static KWPHandler m_kwpHandler;
        private int m_nrOfRetries;
        private int m_nrOfBytesRead;
        private bool m_endThread = false;
        private UInt32 m_offset;
        private UInt32 m_length;
        private static T7Flasher m_instance;
    }
}
