using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using TrionicCANLib.CAN;
using TrionicCANLib.KWP;
using TrionicCANLib.Flasher;
using CommonSuite;
using NLog;

namespace TrionicCANLib.API
{
    public class Trionic7 : ITrionic
    {   
        private IKWPDevice kwpDevice;
        private KWPHandler kwpHandler;
        private IFlasher flash;
        private Logger logger = LogManager.GetCurrentClassLogger();

        private bool m_UseFlasherOnDevice = false;

        public bool UseFlasherOnDevice
        {
            get { return m_UseFlasherOnDevice; }
            set { m_UseFlasherOnDevice = value; }
        }

        private bool m_ELM327Kline = false;

        public bool ELM327Kline
        {
            get { return m_ELM327Kline; }
            set { m_ELM327Kline = value; }
        }


        private readonly System.Timers.Timer tmrReadProcessChecker = new System.Timers.Timer(1000);
        private readonly System.Timers.Timer tmrWriteProcessChecker = new System.Timers.Timer(1000);

        public Trionic7()
        {
            tmrReadProcessChecker.Elapsed += new System.Timers.ElapsedEventHandler(tmrReadProcessChecker_Tick);
            tmrWriteProcessChecker.Elapsed += new System.Timers.ElapsedEventHandler(tmrWriteProcessChecker_Tick);
        }

        override public void setCANDevice(CANBusAdapter adapterType)
        {
            if (adapterType == CANBusAdapter.LAWICEL)
            {
                canUsbDevice = new CANUSBDevice();
            }
            else if (adapterType == CANBusAdapter.ELM327 && !m_ELM327Kline)
            {
                Sleeptime = SleepTime.ELM327;
                canUsbDevice = new CANELM327Device() { ForcedBaudrate = m_forcedBaudrate };
            }
            else if (adapterType == CANBusAdapter.JUST4TRIONIC)
            {
                canUsbDevice = new Just4TrionicDevice() { ForcedBaudrate = m_forcedBaudrate };
            }
            else if (adapterType == CANBusAdapter.COMBI)
            {
                canUsbDevice = new LPCCANDevice();
            }
            else if (adapterType == CANBusAdapter.KVASER)
            {
                canUsbDevice = new KvaserCANDevice();
            }
            else if (adapterType == CANBusAdapter.J2534)
            {
                canUsbDevice = new J2534CANDevice();
            }

            if (canUsbDevice != null)
            {
                canUsbDevice.UseOnlyPBus = m_OnlyPBus;
                canUsbDevice.TrionicECU = ECU.TRIONIC7;
                canUsbDevice.onReceivedAdditionalInformation += new ICANDevice.ReceivedAdditionalInformation(canUsbDevice_onReceivedAdditionalInformation);
                canUsbDevice.onReceivedAdditionalInformationFrame += new ICANDevice.ReceivedAdditionalInformationFrame(canUsbDevice_onReceivedAdditionalInformationFrame);
                canUsbDevice.AcceptOnlyMessageIds = new List<uint> { 0x258,0x238 }; //t7suite
            }

            if (adapterType == CANBusAdapter.ELM327 && m_ELM327Kline)
            {
                kwpDevice = new ELM327Device() { ForcedBaudrate = m_forcedBaudrate };
                setFlasher();
            }
            else if (adapterType != CANBusAdapter.COMBI || !m_UseFlasherOnDevice)
            {
                kwpDevice = new KWPCANDevice() { Latency = m_Latency };
                kwpDevice.setCANDevice(canUsbDevice);
                setFlasher();
            }
        }

        private void setFlasher()
        {
            KWPHandler.setKWPDevice(kwpDevice);
            kwpHandler = KWPHandler.getInstance();

            T7Flasher.setKWPHandler(kwpHandler);
            flash = new T7Flasher();
            flash.onStatusChanged += flash_onStatusChanged;
        }

        override public void SetSelectedAdapter(string adapter)
        {
            if (m_ELM327Kline)
            {
                kwpDevice.ForcedComport = adapter;
            }
            else
            {
                canUsbDevice.SetSelectedAdapter(adapter);
            }
        }

        void flash_onStatusChanged(object sender, IFlasher.StatusEventArgs e)
        {
            CastInfoEvent(e.Info, ActivityType.ConvertingFile);
        }

        void canUsbDevice_onReceivedAdditionalInformation(object sender, ICANDevice.InformationEventArgs e)
        {
            CastInfoEvent(e.Info, ActivityType.ConvertingFile);
        }

        void canUsbDevice_onReceivedAdditionalInformationFrame(object sender, ICANDevice.InformationFrameEventArgs e)
        {
            CastFrameEvent(e.Message);
        }

        public bool openDevice()
        {
            bool opened = true;
            CastInfoEvent("Open called in Trionic7", ActivityType.ConvertingFile);
            MM_BeginPeriod(1);

            if (canUsbDevice is LPCCANDevice && m_UseFlasherOnDevice)
            {
                // connect to adapter
                LPCCANDevice lpc = (LPCCANDevice)canUsbDevice;

                if (lpc.connect())
                {
                    // get flasher object
                    flash = lpc.createFlasher();
                    logger.Debug("T7CombiFlasher object created");
                    CastInfoEvent("CombiAdapter ready", ActivityType.ConvertingFile);
                }
                else
                {
                    opened = false;
                }
            }
            else
            {
                if (kwpHandler.openDevice())
                {
                    CastInfoEvent("Canbus channel opened", ActivityType.ConvertingFile);

                    if (kwpHandler.startSession())
                    {
                        CastInfoEvent("Session started", ActivityType.ConvertingFile);
                    }
                    else
                    {
                        CastInfoEvent("Unable to start session. Wait for previous session to timeout (10 seconds) and try again!", ActivityType.ConvertingFile);
                        kwpHandler.closeDevice();
                        opened = false;
                    }
                }
                else
                {
                    CastInfoEvent("Unable to open canbus channel", ActivityType.ConvertingFile);
                    kwpHandler.closeDevice();
                    opened = false;
                }
            }

            if (!opened)
            {
                CastInfoEvent("Open failed in Trionic7", ActivityType.ConvertingFile);
                if (canUsbDevice != null)
                {
                    canUsbDevice.close();
                }
                MM_EndPeriod(1);
            }
            return opened;
        }

        private bool CheckFlashStatus()
        {
            logger.Debug("Start CheckFlashStatus");
            T7Flasher.FlashStatus stat = flash.getStatus();
            logger.Debug("Status retrieved");
            switch (stat)
            {
                case T7Flasher.FlashStatus.Completed:
                    logger.Debug("Status = T7Flasher.FlashStatus.Completed");
                    break;
                case T7Flasher.FlashStatus.DoinNuthin:
                    logger.Debug("Status = T7Flasher.FlashStatus.DoinNuthin");
                    break;
                case T7Flasher.FlashStatus.EraseError:
                    logger.Debug("Status = T7Flasher.FlashStatus.EraseError");
                    break;
                case T7Flasher.FlashStatus.Eraseing:
                    logger.Debug("Status = T7Flasher.FlashStatus.Eraseing");
                    break;
                case T7Flasher.FlashStatus.NoSequrityAccess:
                    logger.Debug("Status = TrionicFlasher.FlashStatus.NoSequrityAccess");
                    flash.stopFlasher();
                    break;
                case T7Flasher.FlashStatus.NoSuchFile:
                    logger.Debug("Status = T7Flasher.FlashStatus.NoSuchFile");
                    break;
                case T7Flasher.FlashStatus.ReadError:
                    logger.Debug("Status = T7Flasher.FlashStatus.ReadError");
                    break;
                case T7Flasher.FlashStatus.Reading:
                    logger.Debug("Status = T7Flasher.FlashStatus.Reading");
                    break;
                case T7Flasher.FlashStatus.WriteError:
                    logger.Debug("Status = T7Flasher.FlashStatus.WriteError");
                    break;
                case T7Flasher.FlashStatus.Writing:
                    logger.Debug("Status = T7Flasher.FlashStatus.Writing");
                    break;
                default:
                    logger.Debug("Status = " + stat);
                    break;
            }
            bool retval;
            if (stat == T7Flasher.FlashStatus.Eraseing || stat == T7Flasher.FlashStatus.Reading || stat == T7Flasher.FlashStatus.Writing)
                retval = false;
            else
                retval = true;
            return retval;
        }

        /// <summary>
        /// Cleans up connections and resources
        /// </summary>
        override public void Cleanup()
        {
            try
            {
                MM_EndPeriod(1);
                logger.Debug("Cleanup called in Trionic7");
                if (flash != null)
                {
                    flash.onStatusChanged -= flash_onStatusChanged;
                    flash.stopFlasher();
                    flash.cleanup();
                    flash = null;
                }
                if (kwpHandler != null)
                {
                    kwpHandler.EnableLog = false;
                    kwpHandler.closeDevice();
                }
                if (canUsbDevice != null)
                {
                    if (canUsbDevice is LPCCANDevice)
                    {
                        LPCCANDevice lpc = (LPCCANDevice)canUsbDevice;
                        lpc.disconnect();
                        canUsbDevice.close();
                        canUsbDevice = null;
                        logger.Debug("Closed LPCCANDevice in Trionic7");
                    }
                    else
                    {
                        canUsbDevice.close();
                        canUsbDevice = null;
                    }
                }
            }
            catch (Exception e)
            {
                logger.Debug(e.Message);
            }

            LogManager.Flush();
        }

        public void GetECUInfo()
        {
            string vin;
            string immo;
            string engineType;
            string swVersion;
            float e85level;

            KWPResult res = kwpHandler.getVIN(out vin);
            if (res == KWPResult.OK)
                CastInfoEvent("VIN: " + vin, ActivityType.ConvertingFile);
            else if (res == KWPResult.DeviceNotConnected)
                CastInfoEvent("VIN: not connected", ActivityType.ConvertingFile);
            else
                CastInfoEvent("VIN: timeout", ActivityType.ConvertingFile);
            res = kwpHandler.getImmo(out immo);
            if (res == KWPResult.OK)
                CastInfoEvent("Immo: " + immo, ActivityType.ConvertingFile);
            res = kwpHandler.getEngineType(out engineType);
            if (res == KWPResult.OK)
                CastInfoEvent("Engine type: :" + engineType, ActivityType.ConvertingFile);
            res = kwpHandler.getSwVersion(out swVersion);
            if (res == KWPResult.OK)
                CastInfoEvent("Software version: " + swVersion, ActivityType.ConvertingFile);
            res = kwpHandler.getE85Level(out e85level);
            if (res == KWPResult.OK)
                CastInfoEvent("E85 : " + e85level + "%", ActivityType.ConvertingFile);
        }

        public void ReadFlash(string a_fileName)
        {
            if (CheckFlashStatus())
            {
                CastInfoEvent("Starting download of FLASH", ActivityType.ConvertingFile);
                tmrReadProcessChecker.Enabled = true;
                flash.readFlash(a_fileName);
            }
        }

        public void WriteFlash(string a_fileName)
        {
            if (!tmrReadProcessChecker.Enabled)
            {
                // check reading status periodically
                logger.Debug("Starting FLASH procedure, checking FLASHing process status");
                if (CheckFlashStatus())
                {
                    tmrWriteProcessChecker.Enabled = true;
                    CastInfoEvent("FLASHing: " + a_fileName, ActivityType.ConvertingFile);
                    logger.Debug("Calling flash.writeFlash with filename: " + a_fileName);
                    flash.writeFlash(a_fileName);
                }
            }
        }

        private void tmrReadProcessChecker_Tick(object sender, EventArgs e)
        {
            logger.Debug("tmrReadProcessChecker_Tick");
            if (flash != null)
            {
                float numberkb = (float)flash.getNrOfBytesRead() / 1024F;
                int percentage = ((int)numberkb * 100) / 512;
                CastProgressReadEvent(percentage);

                if (flash.getStatus() == T7Flasher.FlashStatus.Completed)
                {
                    flash.stopFlasher();
                    tmrReadProcessChecker.Enabled = false;
                    CastInfoEvent("Finished download of FLASH", ActivityType.FinishedDownloadingFlash);
                }
            }
        }

        private void tmrWriteProcessChecker_Tick(object sender, EventArgs e)
        {
            logger.Debug("tmrWriteProcessChecker_Tick");
            if (flash != null)
            {
                float numberkb = (float)flash.getNrOfBytesRead() / 1024F;
                int percentage = ((int)numberkb * 100) / 512;
                CastProgressWriteEvent(percentage);

                T7Flasher.FlashStatus stat = flash.getStatus();
                logger.Debug("tmrWriteProcessChecker_Tick: " + stat.ToString());

                if (stat == T7Flasher.FlashStatus.Completed)
                {
                    flash.stopFlasher();
                    tmrWriteProcessChecker.Enabled = false;
                    CastInfoEvent("Finished FLASH session", ActivityType.FinishedFlashing);
                }
                else if (stat == T7Flasher.FlashStatus.NoSequrityAccess)
                {
                    flash.stopFlasher();
                    tmrWriteProcessChecker.Enabled = false;
                    CastInfoEvent("No security access granted", ActivityType.FinishedFlashing);
                }
                else if (stat == T7Flasher.FlashStatus.EraseError)
                {
                    flash.stopFlasher();
                    tmrWriteProcessChecker.Enabled = false;
                    CastInfoEvent("An erase error occured", ActivityType.FinishedFlashing);
                }
                else if (stat == T7Flasher.FlashStatus.NoSuchFile)
                {
                    flash.stopFlasher();
                    tmrWriteProcessChecker.Enabled = false;
                    CastInfoEvent("File not found", ActivityType.FinishedFlashing);
                }
                else if (stat == T7Flasher.FlashStatus.WriteError)
                {
                    flash.stopFlasher();
                    tmrWriteProcessChecker.Enabled = false;
                    CastInfoEvent("A write error occured, please retry to FLASH without cutting power to the ECU", ActivityType.FinishedFlashing);
                }
            }
        }

        public string GetE85AdaptionStatus()
        {
            string status;
            KWPHandler.getInstance().getE85AdaptionStatus(out status);
            return status;
        }

        public bool ForceE85Adaption()
        {
            return KWPHandler.getInstance().forceE85Adaption() == KWPResult.OK;
        }

        public bool SetE85Percentage(int level)
        {
            return KWPHandler.getInstance().setE85Level(level) == KWPResult.OK;
        }

        public float GetE85Percentage()
        {
            float level;
            KWPHandler.getInstance().getE85Level(out level);
            return level;
        }

        public string[] ReadDTC()
        {
            List<string> list;
            KWPHandler.getInstance().requestSequrityAccess(false);
            KWPHandler.getInstance().ReadDTCCodes(out list);
            return list.ToArray();
        }

        public bool ClearDTCCode(int dtccode)
        {
            KWPHandler.getInstance().requestSequrityAccess(false);
            return KWPHandler.getInstance().ClearDTCCode(dtccode);
        }

        public bool ClearDTCCodes()
        {
            KWPHandler.getInstance().requestSequrityAccess(false);
            return KWPHandler.getInstance().ClearDTCCodes();
        }

        public void SuspendAlivePolling()
        {
            KWPHandler.getInstance().SuspendAlivePolling();
        }

        public void ResumeAlivePolling()
        {
            KWPHandler.getInstance().ResumeAlivePolling();
        }


        public bool GetSRAMSnapshot(string a_fileName)
        {
            const int blockSize = 0x80;
            byte[] data = new byte[blockSize];
            try
            {
                KWPHandler.getInstance().requestSequrityAccess(false);

                FileStream fs = new FileStream(a_fileName, FileMode.Create);
                using (BinaryWriter br = new BinaryWriter(fs))
                {
                    for (int i = 0; i < 0x10000 / blockSize; i++)
                    {
                        long curaddress = (0xF00000 + i * blockSize);
                        if (canUsbDevice is LPCCANDevice)
                        {
                            Thread.Sleep(1);
                        }
                        if (KWPHandler.getInstance().sendReadRequest((uint)(curaddress), (uint)blockSize))
                        {
                            Thread.Sleep(0);
                            if (canUsbDevice is LPCCANDevice)
                            {
                                Thread.Sleep(1);
                            }
                            if (!KWPHandler.getInstance().sendRequestDataByOffset(out data))
                            {
                                logger.Debug("Failed to read data. sendRequestDataByOffset: " + curaddress.ToString("X8"));
                                CastInfoEvent("Failed to read data. sendRequestDataByOffset: " + curaddress.ToString("X8"), ActivityType.FinishedDownloadingFlash);
                                return false;
                            }
                        }
                        else
                        {
                            logger.Debug("Failed to read data. sendReadRequest: " + curaddress.ToString("X8"));
                            CastInfoEvent("Failed to read data. sendReadRequest: " + curaddress.ToString("X8"), ActivityType.FinishedDownloadingFlash);
                            return false;
                        }
                        CastProgressReadEvent((i * 100) / (0x10000 / blockSize));
                        br.Write(data);
                    }
                }
                fs.Close();
                CastProgressReadEvent(100);
                CastInfoEvent("Snapshot downloaded", ActivityType.FinishedDownloadingFlash);
                return true;
            }
            catch (Exception E)
            {
                logger.Debug("Failed to read memory: " + E.Message);
            }
            return false;
        }

        public byte[] ReadMapfromSRAM(SymbolHelper sh, bool showProgress)
        {
            byte[] completedata = new byte[sh.Length];
            try
            {
                byte[] data;
                int m_nrBytes = 64;
                int m_nrOfReads = 0;
                int m_nrOfRetries = 0;
                m_nrOfReads = sh.Length / m_nrBytes;
                if (((sh.Length) % 64) > 0) m_nrOfReads++;
                int bytecount = 0;
                KWPHandler.getInstance().requestSequrityAccess(false);
                for (int readcount = 0; readcount < m_nrOfReads; readcount++)
                {
                    if (showProgress)
                    {
                        CastProgressReadEvent((int)(readcount * 100 / m_nrOfReads));
                    }

                    m_nrOfRetries = 0;
                    int addresstoread = (int)sh.Start_address + (readcount * m_nrBytes);
                    logger.Debug("Reading 64 bytes from address: " + addresstoread.ToString("X6"));

                    while (!KWPHandler.getInstance().sendReadRequest((uint)(addresstoread), 64) && m_nrOfRetries < 20)
                    {
                        m_nrOfRetries++;
                    }
                    logger.Debug("Send command in " + m_nrOfRetries.ToString() + " retries");
                    m_nrOfRetries = 0;
                    Thread.Sleep(1);
                    while (!KWPHandler.getInstance().sendRequestDataByOffset(out data) && m_nrOfRetries < 20)
                    {
                        m_nrOfRetries++;
                    }
                    logger.Debug("Read data in " + m_nrOfRetries.ToString() + " retries");
                    logger.Debug("Read " + data.Length.ToString() + " bytes from CAN interface");
                    foreach (byte b in data)
                    {
                        // Console.Write(b.ToString("X2") + " ");
                        if (bytecount < completedata.Length)
                        {
                            completedata[bytecount++] = b;
                        }
                    }
                }
                //logger.Debug("Reading done");
            }
            catch (Exception E)
            {
                logger.Debug("Failed to read memory: " + E.Message);
            }
            return completedata;
        }

        public byte[] ReadValueFromSRAM(Int64 sramaddress, Int32 length, out bool _success)
        {
            _success = false;
            byte[] data = new byte[length];
            try
            {
                KWPHandler.getInstance().requestSequrityAccess(false);

                if (canUsbDevice is LPCCANDevice) // or ELM327?
                {
                    Thread.Sleep(1);
                }
                if (KWPHandler.getInstance().sendReadRequest((uint)(sramaddress), (uint)length, out data))
                {
                    Thread.Sleep(0); //<GS-11022010>
                     _success = true;
                }
            }
            
            catch (Exception E)
            {
                logger.Debug("Failed to read memory: " + E.Message);
            }
            return data;
        }

        public byte[] ReadMapFromSRAM(Int64 sramaddress, Int32 length, out bool _success)
        {
            _success = false;
            byte[] data = new byte[length];
            try
            {
                KWPHandler.getInstance().requestSequrityAccess(false);

                if (canUsbDevice is LPCCANDevice) // or ELM327?
                {
                    Thread.Sleep(1);
                }
                if (KWPHandler.getInstance().sendReadRequest((uint)(sramaddress), (uint)length))
                {
                    Thread.Sleep(0); //<GS-11022010>
                    if (canUsbDevice is LPCCANDevice) // or ELM327?
                    {
                        Thread.Sleep(1);
                    }
                    if (KWPHandler.getInstance().sendRequestDataByOffset(out data))
                    {
                        _success = true;
                    }
                }
            }
            catch (Exception E)
            {
                logger.Debug("Failed to read memory: " + E.Message);
            }
            return data;
        }

        public byte[] ReadSymbolNumber(uint symbolnumber, out bool _success)
        {
            _success = false;
            byte[] data = new byte[0];
            try
            {
                if (KWPHandler.getInstance().setSymbolRequest((uint)symbolnumber))
                {
                    Thread.Sleep(0);//<GS-11022010>
                    if (KWPHandler.getInstance().sendRequestDataByOffset(out data))
                    {
                        _success = true;
                    }
                }
            }
            catch (Exception E)
            {
                logger.Debug("Failed to read SymbolNumber: " + E.Message);
            }
            return data;
        }

        public bool WriteSymbolToSRAM(uint symbolnumber, byte[] bytes)
        {
            KWPHandler.getInstance().requestSequrityAccess(false);
            return KWPHandler.getInstance().writeSymbolRequest(symbolnumber, bytes);
        }

        public void WriteMapToSRAM(string symbolname, byte[] completedata, bool showProgress, uint sramAddress, int symbolindex)
        {
            logger.Debug("Writing " + symbolindex.ToString() + " " + symbolname + " SRAM: " + sramAddress.ToString("X8"));
            // if data length > 64 then split the messages
            const uint m_nrBytes = 64;
            uint m_nrOfWrites = 0;
            m_nrOfWrites = (uint)completedata.Length / m_nrBytes;
            if (((completedata.Length) % 64) > 0) m_nrOfWrites++;
            uint bytecount = 0;


            KWPHandler.getInstance().requestSequrityAccess(false);

            for (uint readcount = 0; readcount < m_nrOfWrites; readcount++)
            {
                if (showProgress)
                {
                    CastProgressWriteEvent((int)(readcount * 100 / m_nrOfWrites));
                }

                uint addresstowrite = sramAddress + (readcount * m_nrBytes);
                byte[] dataToSend = new byte[64];
                if (readcount == m_nrOfWrites - 1) // only the last part
                {
                    dataToSend = new byte[completedata.Length - bytecount];
                }
                for (int t = 0; t < dataToSend.Length; t++)
                {
                    dataToSend[t] = completedata[bytecount++];
                }
                if (!KWPHandler.getInstance().writeSymbolRequestAddress(addresstowrite, dataToSend))
                {
                    logger.Debug("Failed to write data to the ECU");
                }
            }
        }

        public bool WriteMapToSRAM(uint addresstowrite, byte[] dataToSend)
        {
            KWPHandler.getInstance().requestSequrityAccess(false);
            return KWPHandler.getInstance().writeSymbolRequestAddress(addresstowrite, dataToSend);
        }
    }
}
