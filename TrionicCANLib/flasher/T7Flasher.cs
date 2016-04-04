using System;
using System.IO;
using System.Collections.Generic;
using TrionicCANLib.KWP;
using System.Threading;
using NLog;
using System.Security.Cryptography;
using System.Text;

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

        private Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// This method returns the number of bytes that has been read or written so far.
        /// 0 is returned if there is no read or write session ongoing.
        /// </summary>
        /// <returns>Number of bytes that has been read or written.</returns>
        /// 
        public override int getNrOfBytesRead() { return m_nrOfBytesRead; }

        public static void setKWPHandler(KWPHandler a_kwpHandler)
        {
            m_kwpHandler = a_kwpHandler;
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

        public override void cleanup()
        {
            lock (m_synchObject)
            {
                m_endThread = true;
            }
            m_resetEvent.Set();
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~T7Flasher()
        {
            cleanup();
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
                logger.Debug("Running T7Flasher");
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
                NotifyStatusChanged(this, new StatusEventArgs("Starting session..."));
                
                m_kwpHandler.startSession();
                logger.Debug("Session started");
                NotifyStatusChanged(this, new StatusEventArgs("Session started, requesting security access to ECU"));
                if (!gotSequrityAccess)
                {
                    logger.Debug("No security access");

                    for (int nrOfSequrityTries = 0; nrOfSequrityTries < 5; nrOfSequrityTries++)
                    {

                        if (!KWPHandler.getInstance().requestSequrityAccess(true))
                        {
                            logger.Debug("No security access granted");
                            
                        }
                        else
                        {
                            gotSequrityAccess = true;
                            logger.Debug("Security access granted");

                            break;
                        }
                    }
                }
                if (!gotSequrityAccess)
                {
                    SetFlashStatus(FlashStatus.NoSequrityAccess);
                    logger.Debug("No security access granted after 5 retries");
                    NotifyStatusChanged(this, new StatusEventArgs("Failed to get security access after 5 retries"));
                }
                //Here it would make sense to stop if we didn't ge security access but
                //let's try anyway. It could be that we don't get a possitive reply from the 
                //ECU if we alredy have security access (from a previous, interrupted, session).
                if (m_command == FlashCommand.ReadCommand)
                {
                    ReadCommand();
                }
                else if (m_command == FlashCommand.ReadMemoryCommand)
                {
                    ReadMemoryCommand();
                }
                else if (m_command == FlashCommand.ReadSymbolMapCommand)
                {
                    ReadSymbolMapCommand();  
                }
                else if (m_command == FlashCommand.WriteCommand)
                {
                    WriteCommand();
                }

                if (m_endThread)
                    return;
                NotifyStatusChanged(this, new StatusEventArgs("Flasing procedure completed"));
                logger.Debug("T7Flasher completed");
                SetFlashStatus(FlashStatus.Completed);
            }
        }


        private void ReadCommand()
        {
            const int nrOfBytes = 64;
            byte[] data;
            logger.Debug("Reading flash content to file: " + m_fileName);
            NotifyStatusChanged(this, new StatusEventArgs("Reading data from ECU..."));

            using (MD5 md5Hash = MD5.Create())
            {
                if (File.Exists(m_fileName))
                    File.Delete(m_fileName);
                FileStream fileStream = File.Create(m_fileName, 1024);
                logger.Debug("File created");
                SetFlashStatus(FlashStatus.Reading);
                logger.Debug("Flash status is reading");

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
                    md5Hash.TransformBlock(data, 0, nrOfBytes, data, 0);
                    m_nrOfBytesRead += nrOfBytes;
                }
                fileStream.Close();
                logger.Debug("Closed file");
                Md5Tools.WriteMd5Hash(md5Hash, m_fileName);
            }

            m_kwpHandler.sendDataTransferExitRequest();
            logger.Debug("Done reading");
        }

        private void ReadMemoryCommand()
        {
            int nrOfBytes = 64;
            byte[] data;
            NotifyStatusChanged(this, new StatusEventArgs("Reading data from ECU..."));

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

        private void ReadSymbolMapCommand()
        {
            byte[] data;
            string swVersion = "";
            m_nrOfBytesRead = 0;
            NotifyStatusChanged(this, new StatusEventArgs("Reading symbol map from ECU..."));

            if (File.Exists(m_fileName))
                File.Delete(m_fileName);
            FileStream fileStream = File.Create(m_fileName, 1024);
            if (m_kwpHandler.sendUnknownRequest() != KWPResult.OK)
            {
                NotifyStatusChanged(this, new StatusEventArgs("Failed to read data from ECU..."));
                SetFlashStatus(FlashStatus.ReadError);
                return;
            }
            SetFlashStatus(FlashStatus.Reading);
            m_kwpHandler.getSwVersionFromDR51(out swVersion);

            if (m_kwpHandler.sendReadSymbolMapRequest() != KWPResult.OK)
            {
                NotifyStatusChanged(this, new StatusEventArgs("Failed to read data from ECU..."));

                SetFlashStatus(FlashStatus.ReadError);
                return;
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

        private void WriteCommand()
        {
            logger.Debug("Write command seen");
            const int nrOfBytes = 128;
            int i = 0;
            byte[] data = new byte[nrOfBytes];
            if (!gotSequrityAccess)
            {
                SetFlashStatus(FlashStatus.Completed);
                return;
            }
            if (!File.Exists(m_fileName))
            {
                SetFlashStatus(FlashStatus.NoSuchFile);
                logger.Debug("No such file found: " + m_fileName);
                NotifyStatusChanged(this, new StatusEventArgs("Failed to find file to flash..."));

                return;
            }
            logger.Debug("Start erasing");
            NotifyStatusChanged(this, new StatusEventArgs("Erasing flash..."));

            SetFlashStatus(FlashStatus.Eraseing);
            if (m_kwpHandler.sendEraseRequest() != KWPResult.OK)
            {
                NotifyStatusChanged(this, new StatusEventArgs("Failed to erase flash..."));
                SetFlashStatus(FlashStatus.EraseError);
                logger.Debug("Erase error occured");
                // break;
            }
            logger.Debug("Opening file for reading");

            FileStream fs = new FileStream(m_fileName, FileMode.Open, FileAccess.Read);

            SetFlashStatus(FlashStatus.Writing);
            logger.Debug("Set flash status to writing");
            NotifyStatusChanged(this, new StatusEventArgs("Writing flash... 0x00000-0x7B000"));

            //Write 0x0-0x7B000
            logger.Debug("0x0-0x7B000");
            Thread.Sleep(100);
            if (m_kwpHandler.sendWriteRequest(0x0, 0x7B000) != KWPResult.OK)
            {
                NotifyStatusChanged(this, new StatusEventArgs("Failed to write data to flash..."));

                SetFlashStatus(FlashStatus.WriteError);
                logger.Debug("Write error occured");

                return;
            }
            for (i = 0; i < 0x7B000 / nrOfBytes; i++)
            {
                fs.Read(data, 0, nrOfBytes);
                m_nrOfBytesRead = i * nrOfBytes;
                logger.Debug("sendWriteDataRequest " + m_nrOfBytesRead);
                if (m_kwpHandler.sendWriteDataRequest(data) != KWPResult.OK)
                {
                    NotifyStatusChanged(this, new StatusEventArgs("Failed to write data to flash..."));
                    SetFlashStatus(FlashStatus.WriteError);
                    logger.Debug("Write error occured " + m_nrOfBytesRead);

                    continue;
                }
                lock (m_synchObject)
                {
                    if (m_command == FlashCommand.StopCommand)
                    {
                        logger.Debug("Stop command seen");
                        continue;
                    }
                    if (m_endThread)
                    {
                        logger.Debug("Thread ended");
                        return;
                    }
                }
            }

            //Write 0x7FE00-0x7FFFF
            logger.Debug("Write 0x7FE00-0x7FFFF");
            NotifyStatusChanged(this, new StatusEventArgs("Writing flash... 0x7FE00-0x7FFFF"));

            if (m_kwpHandler.sendWriteRequest(0x7FE00, 0x200) != KWPResult.OK)
            {
                NotifyStatusChanged(this, new StatusEventArgs("Failed to write data to flash..."));
                SetFlashStatus(FlashStatus.WriteError);
                logger.Debug("Write error occured");
                return;
            }
            fs.Seek(0x7FE00, System.IO.SeekOrigin.Begin);
            for (i = 0x7FE00 / nrOfBytes; i < 0x80000 / nrOfBytes; i++)
            {
                fs.Read(data, 0, nrOfBytes);
                m_nrOfBytesRead = i * nrOfBytes;
                logger.Debug("sendWriteDataRequest " + m_nrOfBytesRead.ToString());

                if (m_kwpHandler.sendWriteDataRequest(data) != KWPResult.OK)
                {
                    NotifyStatusChanged(this, new StatusEventArgs("Failed to write data to flash..."));
                    SetFlashStatus(FlashStatus.WriteError);
                    logger.Debug("Write error occured " + m_nrOfBytesRead);
                    continue;
                }
                lock (m_synchObject)
                {
                    if (m_command == FlashCommand.StopCommand)
                    {
                        logger.Debug("Stop command seen");
                        continue;
                    }
                    if (m_endThread)
                    {
                        logger.Debug("Thread ended");
                        return;
                    }
                }
            }
        }

        private void NotifyStatusChanged(T7Flasher t7Flasher, StatusEventArgs statusEventArgs)
        {
            if (onStatusChanged != null)
                onStatusChanged(t7Flasher, statusEventArgs);
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
    }
}
