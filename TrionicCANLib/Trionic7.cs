using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using TrionicCANLib.CAN;
using TrionicCANLib.KWP;
using TrionicCANLib.Flasher;
using TrionicCANLib.Log;

namespace TrionicCANLib
{
    public class Trionic7 : ITrionic
    {
        private readonly CANListener m_canListener;
        
        private KWPCANDevice kwpCanDevice;
        private KWPHandler kwpHandler;
        private IFlasher flash;

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
                kwpCanDevice = new KWPCANDevice();
                kwpCanDevice.setCANDevice(canUsbDevice);
                kwpCanDevice.EnableKwpLog = m_EnableLog;
                KWPHandler.setKWPDevice(kwpCanDevice);
                if (m_EnableLog)
                {
                    KWPHandler.startLogging();
                }
                kwpHandler = KWPHandler.getInstance();
                try
                {
                    T7Flasher.setKWPHandler(kwpHandler);
                }
                catch (Exception E)
                {
                    Console.WriteLine(E.Message);
                    AddToFlasherLog("Failed to set FLASHer object to KWPHandler");
                }
                flash = T7Flasher.getInstance();
                flash.onStatusChanged += flash_onStatusChanged;
                flash.EnableFlasherLog = m_EnableLog;
            }
            else if (adapterType == CANBusAdapter.ELM327)
            {
                Sleeptime = SleepTime.ELM327;
                canUsbDevice = new CANELM327Device() { ForcedComport = m_forcedComport, ForcedBaudrate = m_forcedBaudrate, BaseBaudrate = BaseBaudrate };
                kwpCanDevice = new KWPCANDevice();
                kwpCanDevice.setCANDevice(canUsbDevice);
                kwpCanDevice.EnableKwpLog = m_EnableLog;
                KWPHandler.setKWPDevice(kwpCanDevice);
                if (m_EnableLog)
                {
                    KWPHandler.startLogging();
                }
                kwpHandler = KWPHandler.getInstance();
                try
                {
                    T7Flasher.setKWPHandler(kwpHandler);
                }
                catch (Exception E)
                {
                    Console.WriteLine(E.Message);
                    AddToFlasherLog("Failed to set FLASHer object to KWPHandler");
                }
                flash = T7Flasher.getInstance();
                flash.onStatusChanged += flash_onStatusChanged;
                flash.EnableFlasherLog = m_EnableLog;
            }
            else if (adapterType == CANBusAdapter.JUST4TRIONIC)
            {
                canUsbDevice = new Just4TrionicDevice() { ForcedComport = m_forcedComport, ForcedBaudrate = m_forcedBaudrate };
                kwpCanDevice = new KWPCANDevice();
                kwpCanDevice.setCANDevice(canUsbDevice);
                kwpCanDevice.EnableKwpLog = m_EnableLog;
                KWPHandler.setKWPDevice(kwpCanDevice);
                if (m_EnableLog)
                {
                    KWPHandler.startLogging();
                }
                kwpHandler = KWPHandler.getInstance();
                kwpHandler.ResumeAlivePolling();
                try
                {
                    T7Flasher.setKWPHandler(kwpHandler);
                }
                catch (Exception E)
                {
                    Console.WriteLine(E.Message);
                    AddToFlasherLog("Failed to set FLASHer object to KWPHandler");
                }
                flash = T7Flasher.getInstance();
                flash.onStatusChanged += flash_onStatusChanged;
                flash.EnableFlasherLog = m_EnableLog;
            }
            else if (adapterType == CANBusAdapter.COMBI)
            {
                canUsbDevice = new LPCCANDevice();
            }

            canUsbDevice.EnableCanLog = m_EnableLog;
            canUsbDevice.UseOnlyPBus = m_OnlyPBus;
            canUsbDevice.DisableCanConnectionCheck = m_DisableCanConnectionCheck;
            canUsbDevice.TrionicECU = ECU.TRIONIC7;
            canUsbDevice.onReceivedAdditionalInformation += new ICANDevice.ReceivedAdditionalInformation(canUsbDevice_onReceivedAdditionalInformation);
            //canUsbDevice.onReceivedAdditionalInformationFrame += new ICANDevice.ReceivedAdditionalInformationFrame(canUsbDevice_onReceivedAdditionalInformationFrame);
            //canUsbDevice.acceptOnlyMessageIds = new List<uint> { 0x258,0x238 }; //t7suite
        }

        void flash_onStatusChanged(object sender, IFlasher.StatusEventArgs e)
        {
            CastInfoEvent(e.Info, ActivityType.ConvertingFile);
        }

        void canUsbDevice_onReceivedAdditionalInformation(object sender, ICANDevice.InformationEventArgs e)
        {
            CastInfoEvent(e.Info, ActivityType.ConvertingFile);
        }

        override public bool openDevice(bool requestSecurityAccess)
        {
            bool opened = true;
            CastInfoEvent("Open called in Trionic7", ActivityType.ConvertingFile);
            MM_BeginPeriod(1);

            if (canUsbDevice is LPCCANDevice)
            {
                // connect to adapter                   
                LPCCANDevice lpc = (LPCCANDevice)canUsbDevice;

                if (lpc.connect())
                {
                    // get flasher object
                    flash = lpc.createFlasher();
                    flash.EnableFlasherLog = m_EnableLog;

                    AddToFlasherLog("T7CombiFlasher object created");
                    CastInfoEvent("CombiAdapter ready", ActivityType.ConvertingFile);
                }
                else
                {
                    opened = false;
                }

            }
            else if (canUsbDevice is CANUSBDevice || canUsbDevice is Just4TrionicDevice || canUsbDevice is CANELM327Device || canUsbDevice is CANUSBDirectDevice)
            {
                if (kwpHandler.openDevice())
                {
                    CastInfoEvent("Canbus channel opened", ActivityType.ConvertingFile);
                }
                else
                {
                    CastInfoEvent("Unable to open canbus channel", ActivityType.ConvertingFile);
                    kwpHandler.closeDevice();
                    opened = false;
                }

                if (kwpHandler.startSession())
                {
                    CastInfoEvent("Session started", ActivityType.ConvertingFile);
                }
                else
                {
                    CastInfoEvent("Unable to start session", ActivityType.ConvertingFile);
                    kwpHandler.closeDevice();
                    opened = false;
                }
            }

            if (!opened)
            {
                CastInfoEvent("Open failed in Trinoic7", ActivityType.ConvertingFile);
                canUsbDevice.close();
                MM_EndPeriod(1);
            }
            return opened;
        }

        private bool CheckFlashStatus()
        {
            AddToFlasherLog("Start CheckFlashStatus");
            T7Flasher.FlashStatus stat = flash.getStatus();
            AddToFlasherLog("Status retrieved");
            switch (stat)
            {
                case T7Flasher.FlashStatus.Completed:
                    AddToFlasherLog("Status = T7Flasher.FlashStatus.Completed");
                    break;
                case T7Flasher.FlashStatus.DoinNuthin:
                    AddToFlasherLog("Status = T7Flasher.FlashStatus.DoinNuthin");
                    break;
                case T7Flasher.FlashStatus.EraseError:
                    AddToFlasherLog("Status = T7Flasher.FlashStatus.EraseError");
                    break;
                case T7Flasher.FlashStatus.Eraseing:
                    AddToFlasherLog("Status = T7Flasher.FlashStatus.Eraseing");
                    break;
                case T7Flasher.FlashStatus.NoSequrityAccess:
                    AddToFlasherLog("Status = TrionicFlasher.FlashStatus.NoSequrityAccess");
                    flash.stopFlasher();
                    break;
                case T7Flasher.FlashStatus.NoSuchFile:
                    AddToFlasherLog("Status = T7Flasher.FlashStatus.NoSuchFile");
                    break;
                case T7Flasher.FlashStatus.ReadError:
                    AddToFlasherLog("Status = T7Flasher.FlashStatus.ReadError");
                    break;
                case T7Flasher.FlashStatus.Reading:
                    AddToFlasherLog("Status = T7Flasher.FlashStatus.Reading");
                    break;
                case T7Flasher.FlashStatus.WriteError:
                    AddToFlasherLog("Status = T7Flasher.FlashStatus.WriteError");
                    break;
                case T7Flasher.FlashStatus.Writing:
                    AddToFlasherLog("Status = T7Flasher.FlashStatus.Writing");
                    break;
                default:
                    AddToFlasherLog("Status = " + stat);
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
                Console.WriteLine("Cleanup called in Trionic7");
                //m_canDevice.removeListener(m_canListener);
                if (m_canListener != null)
                {
                    m_canListener.FlushQueue();
                }
                if (flash != null)
                {
                    flash.onStatusChanged -= flash_onStatusChanged;
                    flash = null;
                }
                KWPHandler.stopLogging();
                if (kwpHandler != null)
                {
                    kwpHandler.SuspendAlivePolling();
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
                        AddToFlasherLog("Closed LPCCANDevice in Trionic7");
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
                AddToFlasherLog(e.Message);
            }

            TrionicCANLib.Log.LogHelper.Flush();
        }

        private void AddToFlasherLog(string line)
        {
            if (m_EnableLog)
            {
                LogHelper.LogFlasher(line);
            }
        }

        public void GetECUInfo()
        {
            string vin;
            string immo;
            string engineType;
            string swVersion;
            float e85level;

            if (m_EnableLog)
            {
                KWPHandler.startLogging();
            }
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
            if (m_EnableLog)
            {
                KWPHandler.startLogging();
            }
            if (CheckFlashStatus())
            {
                CastInfoEvent("Starting download of FLASH", ActivityType.ConvertingFile);
                tmrReadProcessChecker.Enabled = true;
                flash.readFlash(a_fileName);
            }
        }

        public void WriteFlash(string a_fileName)
        {
            if (m_EnableLog)
            {
                KWPHandler.startLogging();
            }
            if (!tmrReadProcessChecker.Enabled)
            {
                // check reading status periodically
                AddToFlasherLog("Starting FLASH procedure, checking FLASHing process status");
                if (CheckFlashStatus())
                {
                    tmrWriteProcessChecker.Enabled = true;
                    CastInfoEvent("FLASHing: " + a_fileName, ActivityType.ConvertingFile);
                    AddToFlasherLog("Calling flash.writeFlash with filename: " + a_fileName);
                    flash.writeFlash(a_fileName);
                }
            }
        }

        private void tmrReadProcessChecker_Tick(object sender, EventArgs e)
        {
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
            if (flash != null)
            {
                float numberkb = (float)flash.getNrOfBytesRead() / 1024F;
                int percentage = ((int)numberkb * 100) / 512;
                CastProgressWriteEvent(percentage);

                T7Flasher.FlashStatus stat = flash.getStatus();
                switch (stat)
                {
                    case T7Flasher.FlashStatus.Completed:
                        AddToFlasherLog("tmrWriteProcessChecker_Tick: Completed FLASHing procedure");
                        break;
                    case T7Flasher.FlashStatus.DoinNuthin:
                        AddToFlasherLog("tmrWriteProcessChecker_Tick: DoinNuthin");
                        break;
                    case T7Flasher.FlashStatus.EraseError:
                        AddToFlasherLog("tmrWriteProcessChecker_Tick: EraseError");
                        break;
                    case T7Flasher.FlashStatus.Eraseing:
                        AddToFlasherLog("tmrWriteProcessChecker_Tick: Eraseing");
                        break;
                    case T7Flasher.FlashStatus.NoSequrityAccess:
                        AddToFlasherLog("tmrWriteProcessChecker_Tick: NoSecurityAccess");
                        break;
                    case T7Flasher.FlashStatus.NoSuchFile:
                        AddToFlasherLog("tmrWriteProcessChecker_Tick: NoSuchFile");
                        break;
                    case T7Flasher.FlashStatus.ReadError:
                        AddToFlasherLog("tmrWriteProcessChecker_Tick: ReadError");
                        break;
                    case T7Flasher.FlashStatus.Reading:
                        break;
                    case T7Flasher.FlashStatus.WriteError:
                        AddToFlasherLog("tmrWriteProcessChecker_Tick: WriteError");
                        break;
                    case T7Flasher.FlashStatus.Writing:
                        break;
                    default:
                        AddToFlasherLog("tmrWriteProcessChecker_Tick: " + stat);
                        break;
                }

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
            KWPHandler.getInstance().ReadDTCCodes(out list);
            return list.ToArray();
        }

        public void GetSRAMSnapshot(string a_fileName)
        {
            const int blockSize = 0x80;
            byte[] data = new byte[blockSize];
            try
            {
                KWPHandler.getInstance().requestSequrityAccess(false);

                FileStream fs = new FileStream(a_fileName, FileMode.Create);
                using (BinaryWriter br = new BinaryWriter(fs))
                {
                    for (int i = 0; i < /*0x800*/ 0x10000 / blockSize; i++)
                    {
                        long curaddress = (0xF00000 + i * blockSize);
                        if (KWPHandler.getInstance().sendReadRequest((uint)(curaddress), (uint)blockSize))
                        {
                            if (!KWPHandler.getInstance().sendRequestDataByOffset(out data))
                            {
                                AddToFlasherLog("Failed to read data: " + curaddress.ToString("X8"));
                            }
                        }
                        CastProgressReadEvent((i * 100) / (0x10000 / blockSize));
                        br.Write(data);
                    }
                }
                fs.Close();
                CastProgressReadEvent(100);
                CastInfoEvent("Snapshot downloaded", ActivityType.FinishedDownloadingFlash);
            }
            catch (Exception E)
            {
                AddToFlasherLog("Failed to read memory: " + E.Message);
            }
        }
    }
}
