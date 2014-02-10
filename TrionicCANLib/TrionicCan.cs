using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Diagnostics;
using TrionicCANLib.CAN;
using TrionicCANLib.KWP;
using TrionicCANLib.Flasher;

namespace TrionicCANLib
{

    public enum ActivityType : int
    {
        StartUploadingBootloader,
        UploadingBootloader,
        FinishedUploadingBootloader,
        StartFlashing,
        UploadingFlash,
        FinishedFlashing,
        StartErasingFlash,
        ErasingFlash,
        FinishedErasingFlash,
        DownloadingSRAM,
        ConvertingFile,
        StartDownloadingFlash,
        DownloadingFlash,
        FinishedDownloadingFlash,
        StartDownloadingFooter,
        DownloadingFooter,
        FinishedDownloadingFooter
    }

    public enum CANBusAdapter : int
    {
        LAWICEL,
        COMBI,
        ELM327,
        JUST4TRIONIC,
        OBDLinkSX,
        LAWICEL_VCP,
        LAWICEL_FTDI
    };

    public enum ECU : int
    {
        TRIONIC7,
        TRIONIC8
    };

    public enum SleepTime : int
    {
        Default = 1,
        ELM327 = 1
    };

    public enum ComSpeed : int
    {
        DEFAULT,
        S38400,
        S57600,
        S115200,
        S230400,
        S1Mbit,
        S2Mbit
    };

    public class TrionicCan
    {
        ICANDevice canUsbDevice;

        AccessLevel _securityLevel = AccessLevel.AccessLevelFD; // by default 0xFD

        public AccessLevel SecurityLevel
        {
            get { return _securityLevel; }
            set { _securityLevel = value; }
        }

        public delegate void WriteProgress(object sender, WriteProgressEventArgs e);
        public event TrionicCan.WriteProgress onWriteProgress;

        public delegate void ReadProgress(object sender, ReadProgressEventArgs e);
        public event TrionicCan.ReadProgress onReadProgress;


        public delegate void CanInfo(object sender, CanInfoEventArgs e);
        public event TrionicCan.CanInfo onCanInfo;

        // implements functions for canbus access for Trionic 8
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint MM_BeginPeriod(uint uMilliseconds);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint MM_EndPeriod(uint uMilliseconds);

        private CANListener m_canListener;
        private bool _stallKeepAlive;
        private float _oilQualityRead = 0;

        private const int maxRetries = 100;

        private KWPCANDevice kwpCanDevice;
        private KWPHandler kwpHandler;
        private IFlasher flash;

        public bool StallKeepAlive
        {
            get { return _stallKeepAlive; }
            set { _stallKeepAlive = value; }
        }

        private System.Timers.Timer tmr = new System.Timers.Timer(3000);

        private System.Timers.Timer tmrReadProcessChecker = new System.Timers.Timer(1000);
        private System.Timers.Timer tmrWriteProcessChecker = new System.Timers.Timer(1000);

        private bool m_EnableCanLog = false;

        public bool EnableCanLog
        {
            get { return m_EnableCanLog; }
            set
            {
                m_EnableCanLog = value;
                if (canUsbDevice != null)
                {
                    canUsbDevice.EnableCanLog = m_EnableCanLog;
                }
            }
        }

        private int m_sleepTime = (int)SleepTime.Default;

        public SleepTime Sleeptime
        {
            get { return (SleepTime)m_sleepTime; }
            set { m_sleepTime = (int)value; }
        }

        private int m_forcedBaudrate = 0;
        public int ForcedBaudrate
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

        public int BaseBaudrate { get; set; }

        public TrionicCan()
        {
            tmr.Elapsed += new System.Timers.ElapsedEventHandler(tmr_Elapsed);
            tmrReadProcessChecker.Elapsed += new System.Timers.ElapsedEventHandler(tmrReadProcessChecker_Tick);
            tmrWriteProcessChecker.Elapsed += new System.Timers.ElapsedEventHandler(tmrWriteProcessChecker_Tick);
        }

        public bool isOpen()
        {
            if (canUsbDevice != null)
            {
                return canUsbDevice.isOpen();
            }
            return false;
        }

        void tmr_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (canUsbDevice.isOpen())
            {
                // send keep alive
                if (!_stallKeepAlive)
                {
                    //SendMessage(0x0000000000003E01); // tester present
                    AddToCanTrace("Send KA based on timer");
                    SendKeepAlive();
                    //Console.WriteLine("KA");
                    //Console.WriteLine("KA sent");
                }
            }
        }

        private string m_forcedComport = string.Empty;

        public string ForcedComport
        {
            get { return m_forcedComport; }
            set { m_forcedComport = value; }
        }

        private bool m_OnlyPBus = false;

        public bool OnlyPBus
        {
            get { return m_OnlyPBus; }
            set { m_OnlyPBus = value; }
        }

        private bool m_DisableCanConnectionCheck = false;

        public bool DisableCanConnectionCheck
        {
            get { return m_DisableCanConnectionCheck; }
            set { m_DisableCanConnectionCheck = value; }
        }

        public void setCANDevice(CANBusAdapter adapterType)
        {
            if (adapterType == CANBusAdapter.LAWICEL)
            {
                canUsbDevice = new CANUSBDevice();
            }
            else if (adapterType == CANBusAdapter.ELM327)
            {
                Sleeptime = SleepTime.ELM327;
                canUsbDevice = new CANELM327Device() { ForcedComport = m_forcedComport, ForcedBaudrate = m_forcedBaudrate, BaseBaudrate = BaseBaudrate };
            }
            else if (adapterType == CANBusAdapter.JUST4TRIONIC)
            {
                canUsbDevice = new Just4TrionicDevice() { ForcedComport = m_forcedComport, ForcedBaudrate = m_forcedBaudrate };
            }
            else if (adapterType == CANBusAdapter.COMBI)
            {
                canUsbDevice = new LPCCANDevice();
            }
            else if (adapterType == CANBusAdapter.LAWICEL_VCP)
            {
                canUsbDevice = new CANUSBDirectDevice() { ForcedComport = m_forcedComport, ForcedBaudrate = m_forcedBaudrate, BaseBaudrate = BaseBaudrate };
            }
            canUsbDevice.EnableCanLog = m_EnableCanLog;
            canUsbDevice.UseOnlyPBus = m_OnlyPBus;
            canUsbDevice.DisableCanConnectionCheck = m_DisableCanConnectionCheck;
            canUsbDevice.TrionicECU = ECU.TRIONIC8;
            canUsbDevice.onReceivedAdditionalInformation += new ICANDevice.ReceivedAdditionalInformation(canUsbDevice_onReceivedAdditionalInformation);
            //canUsbDevice.onReceivedAdditionalInformationFrame += new ICANDevice.ReceivedAdditionalInformationFrame(canUsbDevice_onReceivedAdditionalInformationFrame);
            if (m_canListener == null)
            {
                m_canListener = new CANListener();
            }
            canUsbDevice.addListener(m_canListener);
            canUsbDevice.AcceptOnlyMessageIds = new List<uint> { 0x645, 0x7E0, 0x7E8, 0x311, 0x5E8 };
        }

        public void setT7CANDevice(CANBusAdapter adapterType)
        {
            if (adapterType == CANBusAdapter.LAWICEL)
            {
                canUsbDevice = new CANUSBDevice();
                kwpCanDevice = new KWPCANDevice();
                kwpCanDevice.setCANDevice(canUsbDevice);
                kwpCanDevice.EnableCanLog = m_EnableCanLog;
                KWPHandler.setKWPDevice(kwpCanDevice);
                if (m_EnableCanLog)
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
                    AddToCanTrace("Failed to set flasher object to KWPHandler");
                }
                flash = T7Flasher.getInstance();
                flash.onStatusChanged += flash_onStatusChanged;
                flash.EnableCanLog = m_EnableCanLog;
            }
            else if (adapterType == CANBusAdapter.ELM327)
            {
                Sleeptime = SleepTime.ELM327;
                canUsbDevice = new CANELM327Device() { ForcedComport = m_forcedComport, ForcedBaudrate = m_forcedBaudrate, BaseBaudrate = BaseBaudrate };
                kwpCanDevice = new KWPCANDevice();
                kwpCanDevice.setCANDevice(canUsbDevice);
                kwpCanDevice.EnableCanLog = m_EnableCanLog;
                KWPHandler.setKWPDevice(kwpCanDevice);
                if (m_EnableCanLog)
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
                    AddToCanTrace("Failed to set flasher object to KWPHandler");
                }
                flash = T7Flasher.getInstance();
                flash.onStatusChanged += flash_onStatusChanged;
                flash.EnableCanLog = m_EnableCanLog;
            }
            else if (adapterType == CANBusAdapter.JUST4TRIONIC)
            {
                canUsbDevice = new Just4TrionicDevice() { ForcedComport = m_forcedComport, ForcedBaudrate = m_forcedBaudrate };
                kwpCanDevice = new KWPCANDevice();
                kwpCanDevice.setCANDevice(canUsbDevice);
                kwpCanDevice.EnableCanLog = m_EnableCanLog;
                KWPHandler.setKWPDevice(kwpCanDevice);
                if (m_EnableCanLog)
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
                    AddToCanTrace("Failed to set flasher object to KWPHandler");
                }
                flash = T7Flasher.getInstance();
                flash.onStatusChanged += flash_onStatusChanged;
                flash.EnableCanLog = m_EnableCanLog;
            }
            else if (adapterType == CANBusAdapter.COMBI)
            {
                canUsbDevice = new LPCCANDevice();
            }
            else if (adapterType == CANBusAdapter.LAWICEL_VCP)
            {
                canUsbDevice = new CANUSBDirectDevice() { ForcedComport = m_forcedComport, ForcedBaudrate = m_forcedBaudrate, BaseBaudrate = BaseBaudrate };
                kwpCanDevice = new KWPCANDevice();
                kwpCanDevice.setCANDevice(canUsbDevice);
                kwpCanDevice.EnableCanLog = m_EnableCanLog;
                KWPHandler.setKWPDevice(kwpCanDevice);
                if (m_EnableCanLog)
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
                    AddToCanTrace("Failed to set flasher object to KWPHandler");
                }
                flash = T7Flasher.getInstance();
                flash.onStatusChanged += flash_onStatusChanged;
                flash.EnableCanLog = m_EnableCanLog;
            }

            canUsbDevice.EnableCanLog = m_EnableCanLog;
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

        public bool openDevice(bool requestSecurityAccess)
        {
            CastInfoEvent("Open called in trionicCan", ActivityType.ConvertingFile);
            MM_BeginPeriod(1);
            OpenResult openResult = OpenResult.OpenError;
            try
            {
                openResult = canUsbDevice.open();
            }
            catch (Exception x)
            {
                CastInfoEvent("Exception opening device " + x.ToString(), ActivityType.ConvertingFile);
            }

            if (openResult != OpenResult.OK)
            {
                CastInfoEvent("Open failed in trionicCan", ActivityType.ConvertingFile);
                canUsbDevice.close();
                MM_EndPeriod(1);
                return false;
            }

            // read some data ... 
            for (int i = 0; i < 10; i++)
            {
                CANMessage response = new CANMessage();
                response = m_canListener.waitMessage(50);
            }

            if (requestSecurityAccess)
            {
                CastInfoEvent("Open succeeded in trionicCan", ActivityType.ConvertingFile);
                InitializeSession();
                CastInfoEvent("Session initialized", ActivityType.ConvertingFile);
                // read some data ... 
                for (int i = 0; i < 10; i++)
                {
                    CANMessage response = new CANMessage();
                    response = m_canListener.waitMessage(50);
                }
                bool _securityAccessOk = false;
                for (int i = 0; i < 3; i++)
                {
                    if (RequestSecurityAccess(0))
                    {
                        _securityAccessOk = true;
                        tmr.Start();
                        Console.WriteLine("Timer started");
                        break;
                    }
                }
                if (!_securityAccessOk)
                {
                    CastInfoEvent("Failed to get security access", ActivityType.ConvertingFile);
                    canUsbDevice.close();
                    MM_EndPeriod(1);
                    return false;
                }
                CastInfoEvent("Open successful", ActivityType.ConvertingFile);
            }
            return true;
        }

        public bool openT7Device()
        {
            bool opened = true;
            CastInfoEvent("Open called in T7CAN", ActivityType.ConvertingFile);
            MM_BeginPeriod(1);

            if (canUsbDevice is LPCCANDevice)
            {
                // connect to adapter                   
                LPCCANDevice lpc = (LPCCANDevice)canUsbDevice;

                if (lpc.connect())
                {
                    // get flasher object
                    flash = lpc.createFlasher();
                    flash.EnableCanLog = m_EnableCanLog;

                    AddToCanTrace("T7CombiFlasher object created");
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
                CastInfoEvent("Open failed in T7CAN", ActivityType.ConvertingFile);
                canUsbDevice.close();
                MM_EndPeriod(1);
            }
            return opened;
        }

        private bool CheckStatusT7Flasher()
        {
            AddToCanTrace("Start CheckFlashStatus");
            T7Flasher.FlashStatus stat = flash.getStatus();
            AddToCanTrace("Status retrieved");
            switch (stat)
            {
                case T7Flasher.FlashStatus.Completed:
                    AddToCanTrace("Status = T7Flasher.FlashStatus.Completed");
                    break;
                case T7Flasher.FlashStatus.DoinNuthin:
                    AddToCanTrace("Status = T7Flasher.FlashStatus.DoinNuthin");
                    break;
                case T7Flasher.FlashStatus.EraseError:
                    AddToCanTrace("Status = T7Flasher.FlashStatus.EraseError");
                    break;
                case T7Flasher.FlashStatus.Eraseing:
                    AddToCanTrace("Status = T7Flasher.FlashStatus.Eraseing");
                    break;
                case T7Flasher.FlashStatus.NoSequrityAccess:
                    AddToCanTrace("Status = TrionicFlasher.FlashStatus.NoSequrityAccess");
                    flash.stopFlasher();
                    break;
                case T7Flasher.FlashStatus.NoSuchFile:
                    AddToCanTrace("Status = T7Flasher.FlashStatus.NoSuchFile");
                    break;
                case T7Flasher.FlashStatus.ReadError:
                    AddToCanTrace("Status = T7Flasher.FlashStatus.ReadError");
                    break;
                case T7Flasher.FlashStatus.Reading:
                    AddToCanTrace("Status = T7Flasher.FlashStatus.Reading");
                    break;
                case T7Flasher.FlashStatus.WriteError:
                    AddToCanTrace("Status = T7Flasher.FlashStatus.WriteError");
                    break;
                case T7Flasher.FlashStatus.Writing:
                    AddToCanTrace("Status = T7Flasher.FlashStatus.Writing");
                    break;
                default:
                    AddToCanTrace("Status = " + stat);
                    break;
            }
            bool retval;
            if (stat == T7Flasher.FlashStatus.Eraseing || stat == T7Flasher.FlashStatus.Reading || stat == T7Flasher.FlashStatus.Writing)
                retval = false;
            else
                retval = true;
            return retval;
        }

        public bool SendTestMessage(CANMessage msg)
        {
            if (canUsbDevice.isOpen())
            {
                return canUsbDevice.sendMessage(msg);
            }
            return false;
        }

        private bool RequestSecurityAccessCIM(int millisecondsToWaitWithResponse)
        {
            int secondsToWait = millisecondsToWaitWithResponse / 1000;
            ulong cmd = 0x0000000000012702; // request security access
            CANMessage msg = new CANMessage(0x245, 0, 8);
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x645);
            CastInfoEvent("Requesting security access to CIM", ActivityType.ConvertingFile);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            //ulong data = response.getData();
            Console.WriteLine("---" + response.getData().ToString("X16"));
            if (response.getCanData(1) == 0x67)
            {
                if (response.getCanData(2) == 0x01)
                {
                    CastInfoEvent("Got seed value from CIM", ActivityType.ConvertingFile);
                    while (secondsToWait > 0)
                    {
                        CastInfoEvent("Waiting for " + secondsToWait.ToString() + " seconds...", ActivityType.UploadingBootloader);
                        Thread.Sleep(1000);
                        SendKeepAlive();
                        secondsToWait--;

                    }
                    byte[] seed = new byte[2];
                    seed[0] = response.getCanData(3);
                    seed[1] = response.getCanData(4);
                    if (seed[0] == 0x00 && seed[1] == 0x00)
                    {
                        return true; // security access was already granted
                    }
                    else
                    {
                        SeedToKey s2k = new SeedToKey();
                        byte[] key = s2k.calculateKeyForCIM(seed);
                        CastInfoEvent("Security access CIM : Key (" + key[0].ToString("X2") + key[1].ToString("X2") + ") calculated from seed (" + seed[0].ToString("X2") + seed[1].ToString("X2") + ")", ActivityType.ConvertingFile);

                        ulong keydata = 0x0000000000022704;
                        ulong key1 = key[1];
                        key1 *= 0x100000000;
                        keydata ^= key1;
                        ulong key2 = key[0];
                        key2 *= 0x1000000;
                        keydata ^= key2;
                        msg = new CANMessage(0x245, 0, 8);
                        msg.setData(keydata);
                        m_canListener.setupWaitMessage(0x645);
                        if (!canUsbDevice.sendMessage(msg))
                        {
                            Console.WriteLine("Couldn't send message");
                        }
                        response = new CANMessage();
                        response = m_canListener.waitMessage(1000);
                        // is it ok or not
                        if (response.getCanData(1) == 0x67 && response.getCanData(2) == 0x02)
                        {
                            CastInfoEvent("Security access to CIM granted", ActivityType.ConvertingFile);
                            return true;
                        }
                    }

                }
                else if (response.getCanData(2) == 0x02)
                {
                    CastInfoEvent("Security access to CIM granted", ActivityType.ConvertingFile);
                    return true;
                }
            }
            else if (response.getCanData(1) == 0x7F && response.getCanData(2) == 0x27)
            {
                Console.WriteLine("Casting error");
                string info = TranslateErrorCode(response.getCanData(3));
                Console.WriteLine("Casting error: " + info);
                CastInfoEvent("Error: " + info, ActivityType.ConvertingFile);
            }
            return false;
        }

        private bool RequestSecurityAccess(int millisecondsToWaitWithResponse)
        {
            int secondsToWait = millisecondsToWaitWithResponse / 1000;
            ulong cmd = 0x0000000000FD2702; // request security access
            if (_securityLevel == AccessLevel.AccessLevel01)
            {
                cmd = 0x0000000000012702; // request security access
            }
            else if (_securityLevel == AccessLevel.AccessLevelFB)
            {
                cmd = 0x0000000000FB2702; // request security access
            }
            CANMessage msg = new CANMessage(0x7E0, 0, 3); //<GS-18052011> ELM327 support requires the length byte
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            CastInfoEvent("Requesting security access", ActivityType.ConvertingFile);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            //ulong data = response.getData();
            Console.WriteLine("---" + response.getData().ToString("X16"));
            if (response.getCanData(1) == 0x67)
            {
                if (response.getCanData(2) == 0xFD || response.getCanData(2) == 0xFB || response.getCanData(2) == 0x01)
                {
                    CastInfoEvent("Got seed value from ECU", ActivityType.ConvertingFile);

                    while (secondsToWait > 0)
                    {
                        CastInfoEvent("Waiting for " + secondsToWait.ToString() + " seconds...", ActivityType.UploadingBootloader);
                        Thread.Sleep(1000);
                        SendKeepAlive();
                        secondsToWait--;

                    }

                    byte[] seed = new byte[2];
                    seed[0] = response.getCanData(3);
                    seed[1] = response.getCanData(4);
                    if (seed[0] == 0x00 && seed[1] == 0x00)
                    {
                        return true; // security access was already granted
                    }
                    else
                    {
                        SeedToKey s2k = new SeedToKey();
                        byte[] key = s2k.calculateKey(seed, _securityLevel);
                        CastInfoEvent("Security access : Key (" + key[0].ToString("X2") + key[1].ToString("X2") + ") calculated from seed (" + seed[0].ToString("X2") + seed[1].ToString("X2") + ")", ActivityType.ConvertingFile);

                        ulong keydata = 0x0000000000FE2704;
                        if (_securityLevel == AccessLevel.AccessLevel01)
                        {
                            keydata = 0x0000000000022704;
                        }
                        else if (_securityLevel == AccessLevel.AccessLevelFB)
                        {
                            keydata = 0x0000000000FC2704;
                        }
                        ulong key1 = key[1];
                        key1 *= 0x100000000;
                        keydata ^= key1;
                        ulong key2 = key[0];
                        key2 *= 0x1000000;
                        keydata ^= key2;
                        msg = new CANMessage(0x7E0, 0, 5);//<GS-18052011> ELM327 support requires the length byte
                        msg.setData(keydata);
                        m_canListener.setupWaitMessage(0x7E8);
                        if (!canUsbDevice.sendMessage(msg))
                        {
                            Console.WriteLine("Couldn't send message");
                        }
                        response = new CANMessage();
                        response = m_canListener.waitMessage(1000);
                        // is it ok or not
                        if (response.getCanData(1) == 0x67 && (response.getCanData(2) == 0xFE || response.getCanData(2) == 0xFC || response.getCanData(2) == 0x02))
                        {
                            CastInfoEvent("Security access granted", ActivityType.ConvertingFile);
                            return true;
                        }
                    }

                }
                else if (response.getCanData(2) == 0xFE || response.getCanData(2) == 0x02)
                {
                    CastInfoEvent("Security access granted", ActivityType.ConvertingFile);
                    return true;
                }
            }
            else if (response.getCanData(1) == 0x7F && response.getCanData(2) == 0x27)
            {
                Console.WriteLine("Casting error");
                string info = TranslateErrorCode(response.getCanData(3));
                Console.WriteLine("Casting error: " + info);
                CastInfoEvent("Error: " + info, ActivityType.ConvertingFile);
            }
            return false;
        }

        private string TranslateErrorCode(byte p)
        {
            string retval = "code " + p.ToString("X2");
            switch (p)
            {
                case 0x10:
                    retval = "General reject";
                    break;
                case 0x11:
                    retval = "Service not supported";
                    break;
                case 0x12:
                    retval = "subFunction not supported - invalid format";
                    break;
                case 0x21:
                    retval = "Busy, repeat request";
                    break;
                case 0x22:
                    retval = "conditions not correct or request sequence error";
                    break;
                case 0x23:
                    retval = "Routine not completed or service in progress";
                    break;
                case 0x31:
                    retval = "Request out of range or session dropped";
                    break;
                case 0x33:
                    retval = "Security access denied";
                    break;
                case 0x35:
                    retval = "Invalid key supplied";
                    break;
                case 0x36:
                    retval = "Exceeded number of attempts to get security access";
                    break;
                case 0x37:
                    retval = "Required time delay not expired, you cannot gain security access at this moment";
                    break;
                case 0x40:
                    retval = "Download (PC -> ECU) not accepted";
                    break;
                case 0x41:
                    retval = "Improper download (PC -> ECU) type";
                    break;
                case 0x42:
                    retval = "Unable to download (PC -> ECU) to specified address";
                    break;
                case 0x43:
                    retval = "Unable to download (PC -> ECU) number of bytes requested";
                    break;
                case 0x50:
                    retval = "Upload (ECU -> PC) not accepted";
                    break;
                case 0x51:
                    retval = "Improper upload (ECU -> PC) type";
                    break;
                case 0x52:
                    retval = "Unable to upload (ECU -> PC) for specified address";
                    break;
                case 0x53:
                    retval = "Unable to upload (ECU -> PC) number of bytes requested";
                    break;
                case 0x71:
                    retval = "Transfer suspended";
                    break;
                case 0x72:
                    retval = "Transfer aborted";
                    break;
                case 0x74:
                    retval = "Illegal address in block transfer";
                    break;
                case 0x75:
                    retval = "Illegal byte count in block transfer";
                    break;
                case 0x76:
                    retval = "Illegal block transfer type";
                    break;
                case 0x77:
                    retval = "Block transfer data checksum error";
                    break;
                case 0x78:
                    retval = "Response pending";
                    break;
                case 0x79:
                    retval = "Incorrect byte count during block transfer";
                    break;
                case 0x80:
                    retval = "Service not supported in current diagnostics session";
                    break;
            }
            return retval;
        }

        /// <summary>
        /// Cleans up connections and resources in use by the TrionicCAN DLL
        /// </summary>
        public void Cleanup()
        {
            try
            {
                tmr.Stop();
                MM_EndPeriod(1);
                Console.WriteLine("Cleanup called in TrionicCAN");
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
                        Console.WriteLine("Closed m_canDevice in TrionicCAN");
                    }
                    else
                    {
                        canUsbDevice.close();
                        canUsbDevice = null;
                    }
                }
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }

            TrionicCANLib.Log.LogHelper.Flush();
        }

        public float GetADCValue(uint channel)
        {
            return canUsbDevice.GetADCValue(channel);
        }

        public float GetThermoValue()
        {
            return canUsbDevice.GetThermoValue();
        }

        public string RequestECUInfo(uint _pid, string description)
        {
            return RequestECUInfo(_pid, description, -1);
        }

        public string RequestECUInfo(uint _pid, string description, int expectedResponses)
        {
            string retval = string.Empty;
            byte[] rx_buffer = new byte[128];
            int rx_pnt = 0;

            if (canUsbDevice.isOpen())
            {
                ulong cmd = 0x0000000000001A02 | _pid << 16;
                //SendMessage(data);  // software version
                CANMessage msg = new CANMessage(0x7E0, 0, 3); // test GS was 8
                msg.setData(cmd);
                msg.elmExpectedResponses = expectedResponses;
                m_canListener.setupWaitMessage(0x7E8);
                if (!canUsbDevice.sendMessage(msg))
                {
                    Console.WriteLine("Couldn't send message");
                }

                int msgcnt = 0;
                bool _success = false;
                CANMessage response = new CANMessage();
                ulong data = 0;
                while (!_success && msgcnt < 2)
                {
                    response = new CANMessage();
                    response = m_canListener.waitMessage(1000);
                    data = response.getData();
                    if (response.getCanData(1) != 0x7E) _success = true;
                    msgcnt++;
                }

                //CANMessage response = new CANMessage();
                //response = m_canListener.waitMessage(1000);
                //ulong data = response.getData();
                if (response.getCanData(1) == 0x5A)
                {
                    // only one frame in this repsonse

                    for (uint fi = 3; fi < 8; fi++) rx_buffer[rx_pnt++] = response.getCanData(fi);
                    retval = Encoding.ASCII.GetString(rx_buffer, 0, rx_pnt - 1);
                }
                else if (response.getCanData(2) == 0x5A)
                {
                    SendAckMessageT8();
                    byte len = response.getCanData(1);
                    int m_nrFrameToReceive = ((len - 4) / 8);
                    if ((len - 4) % 8 > 0) m_nrFrameToReceive++;
                    int lenthisFrame = len;
                    if (lenthisFrame > 4) lenthisFrame = 4;
                    for (uint fi = 4; fi < 4 + lenthisFrame; fi++) rx_buffer[rx_pnt++] = response.getCanData(fi);
                    // wait for more records now

                    while (m_nrFrameToReceive > 0)
                    {
                        m_canListener.setupWaitMessage(0x7E8);
                        response = new CANMessage();
                        response = m_canListener.waitMessage(1000);
                        if (response.getCanData(1) != 0x7E)
                        {
                            m_nrFrameToReceive--;
                            data = response.getData();
                            // add the bytes to the receive buffer
                            for (uint fi = 1; fi < 8; fi++)
                            {
                                if (rx_pnt < rx_buffer.Length) // prevent overrun
                                {
                                    rx_buffer[rx_pnt++] = response.getCanData(fi);
                                }
                            }
                        }
                    }
                    retval = Encoding.ASCII.GetString(rx_buffer, 0, rx_pnt - 1);
                }
                else if (response.getCanData(1) == 0x7F && response.getCanData(1) == 0x27)
                {
                    string info = TranslateErrorCode(response.getCanData(3));
                    CastInfoEvent("Error: " + info, ActivityType.ConvertingFile);
                }
            }
            Thread.Sleep(25);

            return retval;
        }

        public string RequestCIMInfo(uint _pid)
        {
            string retval = string.Empty;
            byte[] rx_buffer = new byte[128];
            int rx_pnt = 0;

            if (canUsbDevice.isOpen())
            {
                // pid=2
                // 17-12-2012 17:15:51.239 - TX: 0245 0000000000021A02
                // 17-12-2012 17:15:51.298 - RX: 0645 00000000311A7F03
                // pid=3
                // 17-12-2012 17:16:41.190 - TX: 0245 0000000000031A02
                // 17-12-2012 17:16:41.238 - RX: 0645 00000000311A7F03

                ulong cmd = 0x0000000000001A02 | _pid << 16;
                //SendMessage(data);  // software version
                CANMessage msg = new CANMessage(0x245, 0, 8);
                msg.setData(cmd);
                m_canListener.setupWaitMessage(0x645);
                if (!canUsbDevice.sendMessage(msg))
                {
                    Console.WriteLine("Couldn't send message");
                }

                int msgcnt = 0;
                bool _success = false;
                CANMessage response = new CANMessage();
                ulong data = 0;
                while (!_success && msgcnt < 2)
                {
                    response = new CANMessage();
                    response = m_canListener.waitMessage(1000);
                    data = response.getData();
                    if (response.getCanData(1) != 0x7E) _success = true;
                    msgcnt++;
                }

                //CANMessage response = new CANMessage();
                //response = m_canListener.waitMessage(1000);
                //ulong data = response.getData();
                if (response.getCanData(1) == 0x5A)
                {
                    // only one frame in this repsonse

                    for (uint fi = 3; fi < 8; fi++) rx_buffer[rx_pnt++] = response.getCanData(fi);
                    retval = Encoding.ASCII.GetString(rx_buffer, 0, rx_pnt);
                }
                else if (response.getCanData(2) == 0x5A)
                {
                    SendAckMessageT7();
                    byte len = response.getCanData(1);
                    int m_nrFrameToReceive = ((len - 4) / 8);
                    if ((len - 4) % 8 > 0) m_nrFrameToReceive++;
                    int lenthisFrame = len;
                    if (lenthisFrame > 4) lenthisFrame = 4;
                    for (uint fi = 4; fi < 4 + lenthisFrame; fi++) rx_buffer[rx_pnt++] = response.getCanData(fi);
                    // wait for more records now

                    while (m_nrFrameToReceive > 0)
                    {
                        m_canListener.setupWaitMessage(0x645);
                        response = new CANMessage();
                        response = m_canListener.waitMessage(1000);
                        if (response.getCanData(1) != 0x7E)
                        {
                            m_nrFrameToReceive--;
                            data = response.getData();
                            // add the bytes to the receive buffer
                            for (uint fi = 1; fi < 8; fi++)
                            {
                                if (rx_pnt < rx_buffer.Length) // prevent overrun
                                {
                                    rx_buffer[rx_pnt++] = response.getCanData(fi);
                                }
                            }
                        }
                    }
                    retval = Encoding.ASCII.GetString(rx_buffer, 0, rx_pnt);
                }
                else if (response.getCanData(1) == 0x7F && response.getCanData(1) == 0x27)
                {
                    string info = TranslateErrorCode(response.getCanData(3));
                    CastInfoEvent("Error: " + info, ActivityType.ConvertingFile);
                }
            }
            Thread.Sleep(25);

            return retval;
        }

        public byte[] RequestECUInfo(uint _pid)
        {
            byte[] retval = new byte[2];
            byte[] rx_buffer = new byte[128];
            int rx_pnt = 0;

            if (canUsbDevice.isOpen())
            {
                ulong cmd = 0x0000000000001A02 | _pid<<16;
                //ulong lpid = _pid;
                //lpid *= 256;
                //lpid *= 256;
                //cmd ^= lpid;
                //SendMessage(data);  // software version
                CANMessage msg = new CANMessage(0x7E0, 0, 3); //<GS-18052011> support for ELM requires length byte
                msg.setData(cmd);
                m_canListener.setupWaitMessage(0x7E8);
                if (!canUsbDevice.sendMessage(msg))
                {
                    Console.WriteLine("Couldn't send message");
                }

                int msgcnt = 0;
                bool _success = false;
                CANMessage response = new CANMessage();
                ulong data = 0;
                while (!_success && msgcnt < 2)
                {
                    response = new CANMessage();
                    response = m_canListener.waitMessage(1000);
                    data = response.getData();
                    if (response.getCanData(1) != 0x7E) _success = true;
                    msgcnt++;
                }

                //CANMessage response = new CANMessage();
                //response = m_canListener.waitMessage(1000);
                //ulong data = response.getData();
                if (response.getCanData(1) == 0x5A)
                {
                    // only one frame in this repsonse

                    for (uint fi = 3; fi < 8; fi++) rx_buffer[rx_pnt++] = response.getCanData(fi);
                    retval = new byte[rx_pnt];
                    for (int i = 0; i < rx_pnt; i++) retval[i] = rx_buffer[i];
                }
                else if (response.getCanData(2) == 0x5A)
                {
                    SendAckMessageT8();
                    byte len = response.getCanData(1);
                    int m_nrFrameToReceive = ((len - 4) / 8);
                    if ((len - 4) % 8 > 0) m_nrFrameToReceive++;
                    int lenthisFrame = len;
                    if (lenthisFrame > 4) lenthisFrame = 4;
                    for (uint fi = 4; fi < 4 + lenthisFrame; fi++) rx_buffer[rx_pnt++] = response.getCanData(fi);
                    // wait for more records now

                    while (m_nrFrameToReceive > 0)
                    {
                        m_canListener.setupWaitMessage(0x7E8);
                        response = new CANMessage();
                        response = m_canListener.waitMessage(1000);
                        if (response.getCanData(1) != 0x7E)
                        {
                            m_nrFrameToReceive--;
                            data = response.getData();
                            // add the bytes to the receive buffer
                            for (uint fi = 1; fi < 8; fi++)
                            {
                                if (rx_pnt < rx_buffer.Length) // prevent overrun
                                {
                                    rx_buffer[rx_pnt++] = response.getCanData(fi);
                                }
                            }
                        }
                    }
                    retval = new byte[rx_pnt];
                    for (int i = 0; i < rx_pnt; i++) retval[i] = rx_buffer[i];

                }
                else if (response.getCanData(1) == 0x7F && response.getCanData(1) == 0x27)
                {
                    string info = TranslateErrorCode(response.getCanData(3));
                    CastInfoEvent("Error: " + info, ActivityType.ConvertingFile);
                }
            }
            Thread.Sleep(5);

            return retval;
        }

        private void SendAckMessageT7()
        {
            if (canUsbDevice is CANELM327Device) return;
            SendMessage(0x245, 0x0000000000000030);
        }

        private void SendAckMessageT8()
        {
            if (canUsbDevice is CANELM327Device) return;
            SendMessage(0x7E0, 0x0000000000000030);
        }

        private void SendMessage(uint id, ulong data)
        {
            CANMessage msg = new CANMessage();
            msg.setID(id);
            msg.setLength(8);
            msg.setData(data);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Failed to send message");
            }
        }

        public float GetOilQualityPercentage()
        {
            float retval = 0;
            byte[] data = RequestECUInfo(0x25);
            if (data.Length >= 4)
            {
                retval = Convert.ToInt32(data[2]) * 256;
                retval += Convert.ToInt32(data[3]);
                retval /= 256;
            }
            return retval;
        }

        public int GetTopSpeed()
        {
            int retval = 0;
            byte[] data = RequestECUInfo(0x02);
            if (data.Length >= 2)
            {
                retval = Convert.ToInt32(data[0]) * 256;
                retval += Convert.ToInt32(data[1]);
                retval /= 10;
            }
            return retval;
        }

        public string GetDiagnosticDataIdentifier()
        {
            //9A = 01 10 0
            string retval = string.Empty;
            byte[] data = RequestECUInfo(0x9A);
            Console.WriteLine("data: " + data[0].ToString("X2") + " " + data[1].ToString("X2"));
            if (data[0] == 0x00 && data[1] == 0x00) return string.Empty;
            if (data.Length >= 2)
            {
                retval = "0x" + data[0].ToString("X2") + " " + "0x" + data[1].ToString("X2");
            }
            return retval;
        }

        public string GetSaabPartnumber()
        {
            ulong retval = 0;
            byte[] data = RequestECUInfo(0x7C);
            if (data.Length >= 4)
            {
                retval = Convert.ToUInt64(data[0]) * 256 * 256 * 256;
                retval += Convert.ToUInt64(data[1]) * 256 * 256;
                retval += Convert.ToUInt64(data[2]) * 256;
                retval += Convert.ToUInt64(data[3]);
            }
            return retval.ToString();
        }

        public string GetInt64FromID(uint id)
        {
            ulong retval = 0;
            byte[] data = RequestECUInfo(id);
            if (data.Length >= 4)
            {
                retval = Convert.ToUInt64(data[0]) * 256 * 256 * 256;
                retval += Convert.ToUInt64(data[1]) * 256 * 256;
                retval += Convert.ToUInt64(data[2]) * 256;
                retval += Convert.ToUInt64(data[3]);
            }
            return retval.ToString();
        }


        public string GetVehicleVIN()
        {
            // read and wait for sequence of acks
            return RequestECUInfo(0x90, "VINNumber", 3);
        }

        /*
            RequestECUInfo(0x08, "Software version:");
            RequestECUInfo(0x0A, "Build date      :");  // build date
            RequestECUInfo(0x0C, "Engine type     :");  // build date
            RequestECUInfo(0x2C, "Odometer counter:");
            RequestECUInfo(0x71, "ECU hardware    :");
            RequestECUInfo(0x72, "ECU description :");
            RequestECUInfo(0x73, "Codefile version:");
            RequestECUInfo(0x74, "Calibration set :");
            RequestECUInfo(0x90, "VIN number      :");
            RequestECUInfo(0x92, "SystemsupplierID:");
            RequestECUInfo(0x95, "ECU SW VersionNr:");
            RequestECUInfo(0x97, "System/engine   :");
            RequestECUInfo(0x99, "Programming date:");
            RequestECUInfo(0x9A, "Diag data ID    :");
            RequestECUInfo(0xA0, "Unknown type    :");
            RequestECUInfo(0xA2, "Unknown type    :");
            RequestECUInfo(0xB4, "ECU serialnumber:");
            RequestECUInfo(0xC1, "SW module ID 1  :");
            RequestECUInfo(0xC2, "SW module ID 2  :");
            RequestECUInfo(0xC3, "SW module ID 3  :");
            RequestECUInfo(0xC4, "SW module ID 4  :");
            RequestECUInfo(0xC5, "SW module ID 5  :");
            RequestECUInfo(0xCB, "Endmodel partnr :");
            RequestECUInfo(0xCB, "Basemodel partnr:");         */


        public string GetBuildDate()
        {
            // read and wait for sequence of acks
            return RequestECUInfo(0x0A, "Build date");
        }

        public string GetECUSWVersionNumber()
        {
            // read and wait for sequence of acks
            return RequestECUInfo(0x95, "ECUSWNumber");
        }


        public string GetProgrammingDate()
        {
            // read and wait for sequence of acks
            return RequestECUInfo(0x99, "Programming date");
        }


        public string GetSerialNumber()
        {
            // read and wait for sequence of acks
            return RequestECUInfo(0xB4, "Serial number");
        }

        public string GetCalibrationSet()
        {
            // read and wait for sequence of acks
            return RequestECUInfo(0x74, "Calibration set");
        }


        public string GetCodefileVersion()
        {
            // read and wait for sequence of acks
            return RequestECUInfo(0x73, "Codefile version");
        }

        public string GetECUDescription()
        {
            // read and wait for sequence of acks
            return RequestECUInfo(0x72, "ECU description");
        }
        public string GetECUHardware()
        {
            // read and wait for sequence of acks
            return RequestECUInfo(0x71, "ECU hardware");
        }

        public string GetSoftwareVersion()
        {
            // read and wait for sequence of acks
            string retval = RequestECUInfo(0x08, "Software version");
            retval = retval.Replace("\x00", "");
            return retval.Trim();
        }

        public string getSWVersion()
        {
            //TODO: Implement
            string r_swVersion = "";

            return r_swVersion;
        }

        public float GetE85Percentage()
        {
            float retval = 0;
            GetDiagnosticDataIdentifier();

            CastInfoEvent("Request 0xAA", ActivityType.ConvertingFile);
            CANMessage msg = new CANMessage(0x7E0, 0, 3);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x000000007A01AA03;// <dpid=7A> <level=sendOneResponse> <service=AA> <length>
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x5E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            // 7A 00 52 04 00 16 DC 00
            // <dpid><6 datavalues>
            if (response.getCanData(0) == 0x7A)
            {
                retval = Convert.ToInt32(response.getCanData(2));
            }
            // Negative Response 0x7F Service <nrsi> <service> <returncode>
            // Bug: this is never handled because its sent with id=0x7E8
            else if (response.getCanData(1) == 0x7F && response.getCanData(2) == 0xAA)
            {
                string info = TranslateErrorCode(response.getCanData(3));
                CastInfoEvent("Error: " + info, ActivityType.ConvertingFile);
            }
            return retval;
        }

        public int GetManufacturersEnableCounter()
        {
            int retval = 0;
            byte[] data = RequestECUInfo(0xA0);
            if (data.Length == 1)
            {
                retval = Convert.ToInt32(data[0]);
            }
            return retval;
        }

        public byte[] readDataByLocalIdentifier(int address, int length, out bool success)
        {
            success = false;
            byte[] buffer = this.sendReadDataByLocalIdentifier(address, length, out success);

            Thread.Sleep(1); //was 1 <GS-05052011>
            return buffer;
        }

        public byte[] readMemory(int address, int length, out bool success)
        {
            //lock (this)
            {
                success = false;
                byte[] buffer = this.sendReadCommand(address, length, out success);
                //AddToCanTrace("sendReadCommand returned: " + buffer[0].ToString("X2") + " " + success.ToString());
                Thread.Sleep(1); //was 1 <GS-05052011>
                return buffer;
            }
        }

        public void InitializeSession()
        {
            CANMessage response = new CANMessage();

            //101      8 FE 01 3E 00 00 00 00 00 
            CANMessage msg = new CANMessage(0x11, 0, 2);
            ulong cmd = 0x0000000000003E01;
            msg.setData(cmd);
            if (!canUsbDevice.sendMessage(msg))
            {
                AddToCanTrace("Couldn't send message");
            }

            /*
            // first send 
            ulong data = 0;
            CANMessage msg = new CANMessage(0x7E0, 0, 8);
            ulong cmd = 0x0000000000005001;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!m_canDevice.sendMessage(msg))
            {
                AddToCanTrace("Couldn't send message");
            }
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            data = response.getData();
            Console.WriteLine("Received1: " + data.ToString("X8"));

            cmd = 0x0000000000006801;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!m_canDevice.sendMessage(msg))
            {
                AddToCanTrace("Couldn't send message");
            }
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            data = response.getData();
            Console.WriteLine("Received2: " + data.ToString("X8"));
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            data = response.getData();
            Console.WriteLine("Received3: " + data.ToString("X8"));

            cmd = 0x000000000000E501;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!m_canDevice.sendMessage(msg))
            {
                AddToCanTrace("Couldn't send message");
            }
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            data = response.getData();
            Console.WriteLine("Received4: " + data.ToString("X8"));*/
        }

        private bool UploadBootloader()
        {
            int startAddress = 0x102400;
            Bootloader btloaderdata = new Bootloader();

            int txpnt = 0;
            byte iFrameNumber = 0x21;
            if (requestDownload())
            {
                for (int i = 0; i < 0x46; i++)
                {
                    iFrameNumber = 0x21;
                    //10 F0 36 00 00 10 24 00
                    //Console.WriteLine("Sending bootloader: " + startAddress.ToString("X8"));
                    // cast event
                    float percentage = ((float)i * 100) / 70F;
                    CastProgressWriteEvent(percentage);
                    
                    if (SendTransferData(0xF0, startAddress, 0x7E8))
                    {
                        // send 0x22 (34) frames with data from bootloader
                        CANMessage msg = new CANMessage(0x7E0, 0, 8);
                        for (int j = 0; j < 0x22; j++)
                        {
                            ulong cmd = 0x0000000000000000; // 0x34 = upload data to ECU
                            msg.setData(cmd);
                            msg.setCanData(iFrameNumber, 0);
                            msg.setCanData(btloaderdata.BootloaderBytes[txpnt++], 1);
                            msg.setCanData(btloaderdata.BootloaderBytes[txpnt++], 2);
                            msg.setCanData(btloaderdata.BootloaderBytes[txpnt++], 3);
                            msg.setCanData(btloaderdata.BootloaderBytes[txpnt++], 4);
                            msg.setCanData(btloaderdata.BootloaderBytes[txpnt++], 5);
                            msg.setCanData(btloaderdata.BootloaderBytes[txpnt++], 6);
                            msg.setCanData(btloaderdata.BootloaderBytes[txpnt++], 7);
                            iFrameNumber++;
                            if (iFrameNumber > 0x2F) iFrameNumber = 0x20;
                            msg.elmExpectedResponses = j == 0x21 ? 1 : 0;//on last command (iFrameNumber 22 expect 1 message)
                            if (!canUsbDevice.sendMessage(msg))
                            {
                                AddToCanTrace("Couldn't send message");
                            }
                            Thread.Sleep(m_sleepTime); 
                        }
                        // send the remaining data
                        m_canListener.setupWaitMessage(0x7E8);
                        // now wait for 01 76 00 00 00 00 00 00 
                        CANMessage response = new CANMessage();
                        response = new CANMessage();
                        response = m_canListener.waitMessage(1000);
                        ulong data = response.getData();
                        if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x76)
                        {
                            return false;
                        }
                        SendKeepAlive();
                        startAddress += 0xEA;

                    }
                    else
                    {
                        Console.WriteLine("Did not receive correct response from SendTransferData");
                    }
                }

                iFrameNumber = 0x21;
                if (SendTransferData(0x0A, startAddress, 0x7E8))
                {
                    // send 0x22 (34) frames with data from bootloader
                    CANMessage msg = new CANMessage(0x7E0, 0, 8);

                    ulong cmd = 0x0000000000000000; // 0x34 = upload data to ECU
                    msg.setData(cmd);
                    msg.setCanData(iFrameNumber, 0);
                    msg.setCanData(btloaderdata.BootloaderBytes[txpnt++], 1);
                    msg.setCanData(btloaderdata.BootloaderBytes[txpnt++], 2);
                    msg.setCanData(btloaderdata.BootloaderBytes[txpnt++], 3);
                    msg.setCanData(btloaderdata.BootloaderBytes[txpnt++], 4);
                    msg.setCanData(btloaderdata.BootloaderBytes[txpnt++], 5);
                    msg.setCanData(btloaderdata.BootloaderBytes[txpnt++], 6);
                    msg.setCanData(btloaderdata.BootloaderBytes[txpnt++], 7);
                    iFrameNumber++;
                    if (iFrameNumber > 0x2F) iFrameNumber = 0x20;
                    if (!canUsbDevice.sendMessage(msg))
                    {
                        AddToCanTrace("Couldn't send message");
                    }
                    Thread.Sleep(m_sleepTime);

                    // send the remaining data
                    m_canListener.setupWaitMessage(0x7E8);
                    // now wait for 01 76 00 00 00 00 00 00 
                    CANMessage response = new CANMessage();
                    response = new CANMessage();
                    response = m_canListener.waitMessage(1000);
                    ulong data = response.getData();
                    if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x76)
                    {
                        return false;
                    }
                    SendKeepAlive();
                    startAddress += 0x06;
                }
                else
                {
                    Console.WriteLine("Did not receive correct response from SendTransferData");
                }

                CastProgressWriteEvent(100);
            }
            return true;
        }

        private bool UploadBootloaderProg()
        {
            int startAddress = 0x102400;
            Bootloader btloaderdata = new Bootloader();
            int txpnt = 0;
            byte iFrameNumber = 0x21;
            if (requestDownload())
            {
                for (int i = 0; i < 0x46; i++)
                {
                    iFrameNumber = 0x21;
                    //10 F0 36 00 00 10 24 00
                    //Console.WriteLine("Sending bootloader: " + startAddress.ToString("X8"));
                    // cast event
                    float percentage = ((float)i * 100) / 70F;
                    CastProgressWriteEvent(percentage);


                    if (SendTransferData(0xF0, startAddress, 0x7E8))
                    {
                        // send 0x22 (34) frames with data from bootloader
                        CANMessage msg = new CANMessage(0x7E0, 0, 8);
                        for (int j = 0; j < 0x22; j++)
                        {
                            ulong cmd = 0x0000000000000000; // 0x34 = upload data to ECU
                            msg.setData(cmd);
                            msg.setCanData(iFrameNumber, 0);
                            msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 1);
                            msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 2);
                            msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 3);
                            msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 4);
                            msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 5);
                            msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 6);
                            msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 7);
                            iFrameNumber++;
                            if (iFrameNumber > 0x2F) iFrameNumber = 0x20;
                            msg.elmExpectedResponses = j == 0x21 ? 1 : 0;//on last command (iFrameNumber 22 expect 1 message)
                            if (!canUsbDevice.sendMessage(msg))
                            {
                                AddToCanTrace("Couldn't send message");
                            }
                            Thread.Sleep(m_sleepTime);
                        }
                        // send the remaining data
                        m_canListener.setupWaitMessage(0x7E8);
                        // now wait for 01 76 00 00 00 00 00 00 
                        CANMessage response = new CANMessage();
                        response = new CANMessage();
                        response = m_canListener.waitMessage(1000);
                        ulong data = response.getData();
                        if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x76)
                        {
                            return false;
                        }
                        SendKeepAlive();
                        startAddress += 0xEA;

                    }
                    else
                    {
                        Console.WriteLine("Did not receive correct response from SendTransferData");
                    }
                }

                iFrameNumber = 0x21;
                if (SendTransferData(0x0A, startAddress, 0x7E8))
                {
                    // send 0x22 (34) frames with data from bootloader
                    CANMessage msg = new CANMessage(0x7E0, 0, 8);

                    ulong cmd = 0x0000000000000000; // 0x34 = upload data to ECU
                    msg.setData(cmd);
                    msg.setCanData(iFrameNumber, 0);
                    msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 1);
                    msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 2);
                    msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 3);
                    msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 4);
                    msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 5);
                    msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 6);
                    msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 7);
                    iFrameNumber++;
                    if (iFrameNumber > 0x2F) iFrameNumber = 0x20;                    
                    if (!canUsbDevice.sendMessage(msg))
                    {
                        AddToCanTrace("Couldn't send message");
                    }
                    Thread.Sleep(m_sleepTime);

                    // send the remaining data
                    m_canListener.setupWaitMessage(0x7E8);
                    // now wait for 01 76 00 00 00 00 00 00 
                    CANMessage response = new CANMessage();
                    response = new CANMessage();
                    response = m_canListener.waitMessage(1000);
                    ulong data = response.getData();
                    if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x76)
                    {
                        return false;
                    }
                    SendKeepAlive();
                    startAddress += 0x06;
                }
                else
                {
                    Console.WriteLine("Did not receive correct response from SendTransferData");
                }

                CastProgressWriteEvent(100);
            }
            else
            {
                Console.WriteLine("requestDownload() failed");
                return false;
            }
            return true;
        }

        private bool UploadBootloaderProg011()
        {
            int startAddress = 0x102400;
            Bootloader btloaderdata = new Bootloader();
            int txpnt = 0;
            byte iFrameNumber = 0x21;
            if (requestDownload011())
            {
                for (int i = 0; i < 0x46; i++)
                {
                    iFrameNumber = 0x21;
                    //10 F0 36 00 00 10 24 00
                    //Console.WriteLine("Sending bootloader: " + startAddress.ToString("X8"));
                    // cast event
                    float percentage = ((float)i * 100) / 70F;
                    CastProgressWriteEvent(percentage);

                    if (SendTransferData011(0xF0, startAddress, 0x311))
                    {
                        // send 0x22 (34) frames with data from bootloader
                        CANMessage msg = new CANMessage(0x11, 0, 8);
                        for (int j = 0; j < 0x22; j++)
                        {
                            ulong cmd = 0x0000000000000000; // 0x34 = upload data to ECU
                            msg.setData(cmd);
                            msg.setCanData(iFrameNumber, 0);
                            msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 1);
                            msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 2);
                            msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 3);
                            msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 4);
                            msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 5);
                            msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 6);
                            msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 7);
                            iFrameNumber++;
                            if (iFrameNumber > 0x2F) iFrameNumber = 0x20;
                            msg.elmExpectedResponses = j == 0x21 ? 1 : 0;//on last command (iFrameNumber 22 expect 1 message)
                            if (!canUsbDevice.sendMessage(msg))
                            {
                                AddToCanTrace("Couldn't send message");
                            }
                            Thread.Sleep(m_sleepTime);
                        }
                        // send the remaining data
                        m_canListener.setupWaitMessage(0x311);
                        // now wait for 01 76 00 00 00 00 00 00 
                        CANMessage response = new CANMessage();
                        response = new CANMessage();
                        response = m_canListener.waitMessage(1000);
                        ulong data = response.getData();
                        if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x76)
                        {
                            return false;
                        }
                        BroadcastKeepAlive();
                        startAddress += 0xEA;

                    }
                    else
                    {
                        Console.WriteLine("Did not receive correct response from SendTransferData");
                    }
                }

                iFrameNumber = 0x21;
                if (SendTransferData011(0x0A, startAddress, 0x311))
                {
                    // send 0x22 (34) frames with data from bootloader
                    CANMessage msg = new CANMessage(0x11, 0, 8);

                    ulong cmd = 0x0000000000000000; // 0x34 = upload data to ECU
                    msg.setData(cmd);
                    msg.setCanData(iFrameNumber, 0);
                    msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 1);
                    msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 2);
                    msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 3);
                    msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 4);
                    msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 5);
                    msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 6);
                    msg.setCanData(btloaderdata.BootloaderProgBytes[txpnt++], 7);
                    iFrameNumber++;
                    if (iFrameNumber > 0x2F) iFrameNumber = 0x20;
                    if (!canUsbDevice.sendMessage(msg))
                    {
                        AddToCanTrace("Couldn't send message");
                    }
                    Thread.Sleep(m_sleepTime);

                    // send the remaining data
                    m_canListener.setupWaitMessage(0x311);
                    // now wait for 01 76 00 00 00 00 00 00 
                    CANMessage response = new CANMessage();
                    response = new CANMessage();
                    response = m_canListener.waitMessage(1000);
                    ulong data = response.getData();
                    if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x76)
                    {
                        return false;
                    }
                    BroadcastKeepAlive();
                    startAddress += 0x06;
                }
                else
                {
                    Console.WriteLine("Did not receive correct response from SendTransferData");
                }

                CastProgressWriteEvent(100);
            }
            return true;
        }
        
        private bool SendTransferData(int length, int address, uint waitforResponseID)
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 8); // <GS-24052011> test for ELM327, set length to 16 (0x10)
            ulong cmd = 0x0000000000360010; // 0x36 = transferData
            ulong addressHigh = (uint)address & 0x0000000000FF0000;
            addressHigh /= 0x10000;
            ulong addressMiddle = (uint)address & 0x000000000000FF00;
            addressMiddle /= 0x100;
            ulong addressLow = (uint)address & 0x00000000000000FF;
            ulong len = (ulong)length;

            cmd |= (addressLow * 0x100000000000000);
            cmd |= (addressMiddle * 0x1000000000000);
            cmd |= (addressHigh * 0x10000000000);
            cmd |= (len * 0x100);
            //Console.WriteLine("send: " + cmd.ToString("X16"));

            msg.setData(cmd);
            msg.elmExpectedResponses = 1;
            m_canListener.setupWaitMessage(waitforResponseID);
            if (!canUsbDevice.sendMessage(msg))
            {
                AddToCanTrace("Couldn't send message");
            }

            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            //Console.WriteLine("Received in SendTransferData: " + data.ToString("X16"));
            if (getCanData(data, 0) != 0x30 || getCanData(data, 1) != 0x00)
            {
                return false;
            }
            return true;
        }

        private bool requestDownload()
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 7);   //<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0000000000003406;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(2000);
            ulong data = response.getData();
            //CastInfoEvent("rx requestDownload: " + data.ToString("X16"), ActivityType.UploadingBootloader);
            if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x74)
            {
                return false;
            }
            return true;
        }

        private bool Send0120()
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 2); //<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0000000000002001;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x50)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// InitiateDiagnosticOperation 
        /// </summary>
        /// <returns></returns>
        private bool StartSession10()
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 3);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0000000000021002; // 0x02 0x10 0x02
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x50)
            {
                return false;
            }
            return true;
        }

        private bool StartSession1081()
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 3);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0000000000811002; // 0x02 0x10 0x02
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            if (getCanData(data, 0) != 0x03 || getCanData(data, 1) != 0x7F)
            {
                return false;
            }
            return true;
        }


        private bool StartSession20()
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 2);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0000000000002001; // 0x02 0x10 0x02
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x60)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// DisableNormalCommunication 
        /// </summary>
        /// <returns></returns>
        private bool SendShutup()
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 2);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0000000000002801; // 0x02 0x10 0x02
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x68)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// ReportProgrammedState 
        /// </summary>
        /// <returns></returns>
        private bool SendA2()
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 2);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x000000000000A201; // 0x02 0x10 0x02
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0xE2)
            {
                return false;
            }
            return true;
        }

        private bool StartBootloader()
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 7);   //<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0060241000803606;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x76)
            {
                return false;
            }
            return true;
        }

        private bool SendA5()
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 3);   //<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x000000000001A502; // 0x02 0x10 0x02
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0xE5)
            {
                return false;
            }
            return true;
        }

        private bool SendA503()
        {
            // expect no response
            CANMessage msg = new CANMessage(0x7E0, 0, 3);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x000000000003A502;
            msg.setData(cmd);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            return true;
        }

        public bool testSRAMWrite(/*int address, byte[] data*/)
        {
            StartSession10();
            CastInfoEvent("Requesting mandatory data", ActivityType.UploadingBootloader);

            RequestECUInfo(0x90);
            RequestECUInfo(0x97);
            RequestECUInfo(0x92);
            RequestECUInfo(0xB4);
            RequestECUInfo(0xC1);
            RequestECUInfo(0xC2);
            RequestECUInfo(0xC3);
            RequestECUInfo(0xC4);
            RequestECUInfo(0xC5);
            RequestECUInfo(0xC6);
            Send0120();
            Thread.Sleep(1000);

            StartSession1081();

            StartSession10();
            CastInfoEvent("Telling ECU to clear CANbus", ActivityType.UploadingBootloader);
            SendShutup();
            SendA2();
            SendA5();
            SendA503();
            Thread.Sleep(500);
            SendKeepAlive();
            _securityLevel = AccessLevel.AccessLevel01;
            CastInfoEvent("Requesting security access", ActivityType.UploadingBootloader);
            RequestSecurityAccess(2000);
            Thread.Sleep(500);
            CastInfoEvent("Uploading data", ActivityType.UploadingBootloader);

            int startAddress = 0x102400;
            Bootloader btloaderdata = new Bootloader();
            if (requestDownload())
            {
                for (int i = 0; i < 0x46; i++)
                {
                    //10 F0 36 00 00 10 24 00
                    //Console.WriteLine("Sending bootloader: " + startAddress.ToString("X8"));
                    // cast event
                    float percentage = ((float)i * 100) / 70F;
                    CastProgressWriteEvent(percentage);

                    byte iFrameNumber = 0x21;
                    if (SendTransferData(0xF0, startAddress, 0x7E8))
                    {
                        // send 0x22 (34) frames with data from bootloader
                        CANMessage msg = new CANMessage(0x7E0, 0, 8);
                        for (int j = 0; j < 0x22; j++)
                        {
                            ulong cmd = 0x0000000000000000; // 0x34 = upload data to ECU
                            msg.setData(cmd);
                            msg.setCanData(iFrameNumber, 0);
                            msg.setCanData(0x00, 1);
                            msg.setCanData(0x01, 2);
                            msg.setCanData(0x02, 3);
                            msg.setCanData(0x03, 4);
                            msg.setCanData(0x04, 5);
                            msg.setCanData(0x05, 6);
                            msg.setCanData(0x06, 7);
                            iFrameNumber++;
                            if (iFrameNumber > 0x2F) iFrameNumber = 0x20;
                            if (!canUsbDevice.sendMessage(msg))
                            {
                                AddToCanTrace("Couldn't send message");
                            }
                            Thread.Sleep(1);
                        }
                        // send the remaining data
                        m_canListener.setupWaitMessage(0x7E8);
                        // now wait for 01 76 00 00 00 00 00 00 
                        CANMessage response = new CANMessage();
                        response = new CANMessage();
                        response = m_canListener.waitMessage(1000);
                        ulong datax = response.getData();
                        if (getCanData(datax, 0) != 0x01 || getCanData(datax, 1) != 0x76)
                        {
                            return false;
                        }
                        SendKeepAlive();
                        startAddress += 0xEA;

                    }
                    else
                    {
                        Console.WriteLine("Did not receive correct response from SendTransferData");
                    }
                }
            }
            return true;
        }

        public byte[] GetFlashWithBootloader()
        {
            _stallKeepAlive = true;
            bool success = false;
            int retryCount = 0;
            int startAddress = 0x000000;
            int blockSize = 0x80; // defined in bootloader... keep it that way!
            int bufpnt = 0;
            byte[] buf = new byte[0x100000];
            int blockCount = 0;
            SendKeepAlive();
            sw.Reset();
            sw.Start();
            CastInfoEvent("Starting session", ActivityType.UploadingBootloader);

            StartSession10();
            CastInfoEvent("Requesting mandatory data", ActivityType.UploadingBootloader);

            RequestECUInfo(0x90);
            RequestECUInfo(0x97);
            RequestECUInfo(0x92);
            RequestECUInfo(0xB4);
            RequestECUInfo(0xC1);
            RequestECUInfo(0xC2);
            RequestECUInfo(0xC3);
            RequestECUInfo(0xC4);
            RequestECUInfo(0xC5);
            RequestECUInfo(0xC6);
            Send0120();
            Thread.Sleep(1000);

            StartSession1081();

            StartSession10();
            CastInfoEvent("Telling ECU to clear CANbus", ActivityType.UploadingBootloader);
            SendShutup();
            SendA2();
            SendA5();
            SendA503();
            Thread.Sleep(500);
            SendKeepAlive();
            _securityLevel = AccessLevel.AccessLevel01;
            CastInfoEvent("Requesting security access", ActivityType.UploadingBootloader);
            RequestSecurityAccess(2000);
            Thread.Sleep(500);
            CastInfoEvent("Uploading bootloader", ActivityType.UploadingBootloader);
            UploadBootloader();
            CastInfoEvent("Starting bootloader", ActivityType.UploadingBootloader);
            // start bootloader in ECU
            Thread.Sleep(500);
            StartBootloader();
            SendKeepAlive();
            Thread.Sleep(500);

            CastInfoEvent("Downloading flash", ActivityType.DownloadingFlash);



            // now start sending commands:
            //06 21 80 00 00 00 00 00 
            // response: 
            //10 82 61 80 00 10 0C 00 // 4 bytes data already

            //for (int i = 0; i < buf.Length / blockSize; i++)
            while (startAddress < buf.Length)
            {
                if (!canUsbDevice.isOpen())
                {
                    _stallKeepAlive = false;
                    return buf;
                }
                byte[] readbuf = readDataByLocalIdentifier(startAddress, blockSize, out success);
                if (success)
                {
                    if (readbuf.Length == blockSize)
                    {
                        for (int j = 0; j < blockSize; j++)
                        {
                            buf[bufpnt++] = readbuf[j];
                        }
                    }
                    //string infoStr = "Address: " + startAddress.ToString("X8"); //+ " ";
                    CastProgressReadEvent((float)(bufpnt * 100) / (float)buf.Length);
                    startAddress += blockSize;
                    retryCount = 0;
                }
                else
                {
                    CastInfoEvent("Frame dropped, retrying " + startAddress.ToString("X8") + " " + retryCount.ToString(), ActivityType.DownloadingFlash);
                    retryCount++;
                    // read all available message from the bus now

                    for (int i = 0; i < 10; i++)
                    {
                        CANMessage response = new CANMessage();
                        ulong data = 0;
                        response = new CANMessage();
                        response = m_canListener.waitMessage(10);
                        data = response.getData();
                    }



                    if (retryCount == maxRetries)
                    {
                        CastInfoEvent("Failed to download flash content", ActivityType.ConvertingFile);
                        _stallKeepAlive = false;
                        return buf;
                    }
                }
                blockCount++;
                if (sw.ElapsedMilliseconds > 3000) // once every 3 seconds
                //if ((blockCount % 10) == 0)
                {
                    sw.Stop();
                    sw.Reset();
                    SendKeepAlive();
                    sw.Start();
                }

            }
            sw.Stop();
            _stallKeepAlive = false;
            return buf;
        }

        public byte[] getSRAMSnapshotWithBootloader()
        {
            _stallKeepAlive = true;
            bool success = false;
            int retryCount = 0;
            int startAddress = 0x107000;
            int blockSize = 0x80; // defined in bootloader... keep it that way!
            int bufpnt = 0;
            byte[] buf = new byte[0x001000];
            int blockCount = 0;
            SendKeepAlive();
            sw.Reset();
            sw.Start();
            CastInfoEvent("Starting session", ActivityType.UploadingBootloader);

            StartSession10();
            CastInfoEvent("Requesting mandatory data", ActivityType.UploadingBootloader);

            RequestECUInfo(0x90);
            RequestECUInfo(0x97);
            RequestECUInfo(0x92);
            RequestECUInfo(0xB4);
            RequestECUInfo(0xC1);
            RequestECUInfo(0xC2);
            RequestECUInfo(0xC3);
            RequestECUInfo(0xC4);
            RequestECUInfo(0xC5);
            RequestECUInfo(0xC6);
            Send0120();
            Thread.Sleep(1000);

            StartSession1081();

            StartSession10();
            CastInfoEvent("Telling ECU to clear CANbus", ActivityType.UploadingBootloader);
            SendShutup();
            SendA2();
            SendA5();
            SendA503();
            Thread.Sleep(500);
            SendKeepAlive();
            _securityLevel = AccessLevel.AccessLevel01;
            CastInfoEvent("Requesting security access", ActivityType.UploadingBootloader);
            RequestSecurityAccess(500);
            Thread.Sleep(500);
            CastInfoEvent("Uploading bootloader", ActivityType.UploadingBootloader);
            UploadBootloader();
            CastInfoEvent("Starting bootloader", ActivityType.UploadingBootloader);
            // start bootloader in ECU
            Thread.Sleep(500);
            StartBootloader();
            SendKeepAlive();
            Thread.Sleep(500);

            CastInfoEvent("Downloading snapshot", ActivityType.DownloadingFlash);



            // now start sending commands:
            //06 21 80 00 00 00 00 00 
            // response: 
            //10 82 61 80 00 10 0C 00 // 4 bytes data already

            //for (int i = 0; i < buf.Length / blockSize; i++)
            while (startAddress < 0x108000)
            {
                if (!canUsbDevice.isOpen())
                {
                    _stallKeepAlive = false;
                    return buf;
                }
                byte[] readbuf = readDataByLocalIdentifier(startAddress, blockSize, out success);
                if (success)
                {
                    if (readbuf.Length == blockSize)
                    {
                        for (int j = 0; j < blockSize; j++)
                        {
                            buf[bufpnt++] = readbuf[j];
                        }
                    }
                    //string infoStr = "Address: " + startAddress.ToString("X8"); //+ " ";
                    CastProgressReadEvent((float)(bufpnt * 100) / (float)buf.Length);
                    startAddress += blockSize;
                    retryCount = 0;
                }
                else
                {
                    CastInfoEvent("Frame dropped, retrying " + startAddress.ToString("X8") + " " + retryCount.ToString(), ActivityType.DownloadingFlash);
                    retryCount++;
                    // read all available message from the bus now

                    for (int i = 0; i < 10; i++)
                    {
                        CANMessage response = new CANMessage();
                        ulong data = 0;
                        response = new CANMessage();
                        response = m_canListener.waitMessage(10);
                        data = response.getData();
                    }



                    if (retryCount == maxRetries)
                    {
                        CastInfoEvent("Failed to download flash content", ActivityType.ConvertingFile);
                        _stallKeepAlive = false;
                        return buf;
                    }
                }
                blockCount++;
                if (sw.ElapsedMilliseconds > 3000) // once every 3 seconds
                //if ((blockCount % 10) == 0)
                {
                    sw.Stop();
                    sw.Reset();
                    SendKeepAlive();
                    sw.Start();
                }

            }
            sw.Stop();
            _stallKeepAlive = false;
            return buf;
        }

        public bool WriteToSRAM(int address, byte[] memdata)
        {
            if (!canUsbDevice.isOpen()) return false;

            return false;

        }

        private bool SendrequestDownload(bool recoveryMode)
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 7);//<GS-18052011> ELM327 support requires the length byte
            //06 34 01 00 00 00 00 00
            ulong cmd = 0x0000000000013406;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            bool eraseDone = false;
            int eraseCount = 0;
            int waitCount = 0;
            while (!eraseDone)
            {   
                m_canListener.setupWaitMessage(0x7E8); // TEST ELM327 31082011
                CANMessage response = new CANMessage();
                response = m_canListener.waitMessage(500); // 1 seconds!
                ulong data = response.getData();
                if (data == 0)
                {
                    m_canListener.setupWaitMessage(0x311); // TEST ELM327 31082011
                    response = new CANMessage();
                    response = m_canListener.waitMessage(500); // 1 seconds!
                    data = response.getData();
                }
                // response will be 03 7F 34 78 00 00 00 00 a couple of times while erasing
                if (getCanData(data, 0) == 0x03 && getCanData(data, 1) == 0x7F && getCanData(data, 2) == 0x34 && getCanData(data, 3) == 0x78)
                {
                    if (recoveryMode) BroadcastKeepAlive();
                    else SendKeepAlive();
                    eraseCount++;
                    string info = "Erasing flash";
                    for (int i = 0; i < eraseCount; i++) info += ".";
                    CastInfoEvent(info, ActivityType.ErasingFlash);
                }
                else if (getCanData(data, 0) == 0x01 && getCanData(data, 1) == 0x74)
                {
                    if (recoveryMode) BroadcastKeepAlive();
                    else SendKeepAlive();
                    eraseDone = true;
                    return true;
                }
                else if (getCanData(data, 0) == 0x03 && getCanData(data, 1) == 0x7F && getCanData(data, 2) == 0x34 && getCanData(data, 3) == 0x11)
                {
                    CastInfoEvent("Erase cannot be performed", ActivityType.ErasingFlash);
                    return false;
                }
                else
                {
                    Console.WriteLine("Rx: " + data.ToString("X16"));
                    if(canUsbDevice is CANELM327Device){
                        if (recoveryMode) BroadcastKeepAlive();
                        else SendKeepAlive();
                    }                    
                }
                waitCount++;
                if (waitCount > 30)
                {
                    CastInfoEvent("Erase timed out after 30 seconds", ActivityType.ErasingFlash);
                    // ELM327 seem to be unable to wait long enough for this response
                    // Instead we assume its finnished ok after 30 seconds
                    if (canUsbDevice is CANELM327Device)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                Thread.Sleep(m_sleepTime);

            }
            return true;
        }

        private bool BroadcastSession10()
        {
            CANMessage msg = new CANMessage(0x11, 0, 3);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0000000000021002; // 0x02 0x10 0x02
            msg.setData(cmd);
            //m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            /*CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x50)
            {
                return false;
            }*/
            return true;
        }

        private bool BroadcastShutup()
        {
            CANMessage msg = new CANMessage(0x11, 0, 2);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0000000000002801; // 0x02 0x10 0x02
            msg.setData(cmd);
            //m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            /*CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x68)
            {
                return false;
            }*/
            return true;
        }

        private bool BroadcastShutup011()
        {
            CANMessage msg = new CANMessage(0x11, 0, 2);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0000000000002801; // 0x02 0x10 0x02
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x311);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x68)
            {
                return false;
            }
            return true;
        }

        private int GetProgrammingState(uint responseID)
        {
            Console.WriteLine("Get programming state");
            CANMessage msg = new CANMessage(0x11, 0, 2);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x000000000000A201; // 0x02 0x10 0x02
            msg.setData(cmd);
            m_canListener.setupWaitMessage(responseID);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            Console.WriteLine("Get programming state response: " + data.ToString("X16"));
            //\__ 00 00 03 11 02 e2 01 00 00 00 00 00 Magic reply, T8 replies with 0311 and programming state 01(recovery state?)
            if (data == 0) return -1;
            if (getCanData(data, 1) != 0xE2 || getCanData(data, 0) != 0x02)
            {
                return 0;
            }
            return Convert.ToInt32(getCanData(data, 2));
        }

        private int GetProgrammingState011()
        {
            CANMessage msg = new CANMessage(0x11, 0, 2);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x000000000000A201; // 0x02 0x10 0x02
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x311);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            //\__ 00 00 03 11 02 e2 01 00 00 00 00 00 Magic reply, T8 replies with 0311 and programming state 01(recovery state?)
            if (getCanData(data, 1) != 0xE2 || getCanData(data, 0) != 0x02)
            {
                return 0;
            }
            return Convert.ToInt32(getCanData(data, 2));
        }

        private bool SendA5011()
        {
            CANMessage msg = new CANMessage(0x11, 0, 3);   //<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x000000000001A502; // 0x02 0x10 0x02
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x311);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0xE5)
            {
                return false;
            }
            return true;
        }

        private bool SendA503011()
        {
            // expect no response
            CANMessage msg = new CANMessage(0x11, 0, 3);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x000000000003A502;
            msg.setData(cmd);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            return true;
        }

        private bool RequestSecurityAccess011(int millisecondsToWaitWithResponse)
        {
            int secondsToWait = millisecondsToWaitWithResponse / 1000;
            ulong cmd = 0x0000000000FD2702; // request security access
            if (_securityLevel == AccessLevel.AccessLevel01)
            {
                cmd = 0x0000000000012702; // request security access
            }
            else if (_securityLevel == AccessLevel.AccessLevelFB)
            {
                cmd = 0x0000000000FB2702; // request security access
            }
            CANMessage msg = new CANMessage(0x11, 0, 3); //<GS-18052011> ELM327 support requires the length byte
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x311);
            CastInfoEvent("Requesting security access", ActivityType.ConvertingFile);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            //ulong data = response.getData();
            Console.WriteLine("---" + response.getData().ToString("X16"));
            if (response.getCanData(1) == 0x67)
            {
                if (response.getCanData(2) == 0xFD || response.getCanData(2) == 0xFB || response.getCanData(2) == 0x01)
                {
                    CastInfoEvent("Got seed value from ECU", ActivityType.ConvertingFile);

                    while (secondsToWait > 0)
                    {
                        CastInfoEvent("Waiting for " + secondsToWait.ToString() + " seconds...", ActivityType.UploadingBootloader);
                        Thread.Sleep(1000);
                        SendKeepAlive();
                        secondsToWait--;

                    }

                    byte[] seed = new byte[2];
                    seed[0] = response.getCanData(3);
                    seed[1] = response.getCanData(4);
                    if (seed[0] == 0x00 && seed[1] == 0x00)
                    {
                        return true; // security access was already granted
                    }
                    else
                    {
                        SeedToKey s2k = new SeedToKey();
                        byte[] key = s2k.calculateKey(seed, _securityLevel);
                        CastInfoEvent("Security access : Key (" + key[0].ToString("X2") + key[1].ToString("X2") + ") calculated from seed (" + seed[0].ToString("X2") + seed[1].ToString("X2") + ")", ActivityType.ConvertingFile);

                        ulong keydata = 0x0000000000FE2704;
                        if (_securityLevel == AccessLevel.AccessLevel01)
                        {
                            keydata = 0x0000000000022704;
                        }
                        else if (_securityLevel == AccessLevel.AccessLevelFB)
                        {
                            keydata = 0x0000000000FC2704;
                        }
                        ulong key1 = key[1];
                        key1 *= 0x100000000;
                        keydata ^= key1;
                        ulong key2 = key[0];
                        key2 *= 0x1000000;
                        keydata ^= key2;
                        msg = new CANMessage(0x11, 0, 5);//<GS-18052011> ELM327 support requires the length byte
                        msg.setData(keydata);
                        m_canListener.setupWaitMessage(0x311);
                        if (!canUsbDevice.sendMessage(msg))
                        {
                            Console.WriteLine("Couldn't send message");
                        }
                        response = new CANMessage();
                        response = m_canListener.waitMessage(1000);
                        // is it ok or not
                        if (response.getCanData(1) == 0x67 && (response.getCanData(2) == 0xFE || response.getCanData(2) == 0xFC || response.getCanData(2) == 0x02))
                        {
                            CastInfoEvent("Security access granted", ActivityType.ConvertingFile);
                            return true;
                        }
                    }

                }
                else if (response.getCanData(2) == 0xFE || response.getCanData(2) == 0x02)
                {
                    CastInfoEvent("Security access granted", ActivityType.ConvertingFile);
                    return true;
                }
            }
            else if (response.getCanData(1) == 0x7F && response.getCanData(2) == 0x27)
            {
                Console.WriteLine("Casting error");
                string info = TranslateErrorCode(response.getCanData(3));
                Console.WriteLine("Casting error: " + info);
                CastInfoEvent("Error: " + info, ActivityType.ConvertingFile);
            }
            return false;
        }

        private bool requestDownload011()
        {
            CANMessage msg = new CANMessage(0x11, 0, 7);   //<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0000000000003406;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x311);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(2000);
            ulong data = response.getData();
            //CastInfoEvent("rx requestDownload: " + data.ToString("X16"), ActivityType.UploadingBootloader);
            if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x74)
            {
                return false;
            }
            return true;
        }
     

        private bool SendTransferData011(int length, int address, uint waitforResponseID)
        {
            CANMessage msg = new CANMessage(0x11, 0, 8); // <GS-24052011> test for ELM327, set length to 16 (0x10)
            ulong cmd = 0x0000000000360010; // 0x36 = transferData
            ulong addressHigh = (uint)address & 0x0000000000FF0000;
            addressHigh /= 0x10000;
            ulong addressMiddle = (uint)address & 0x000000000000FF00;
            addressMiddle /= 0x100;
            ulong addressLow = (uint)address & 0x00000000000000FF;
            ulong len = (ulong)length;

            cmd |= (addressLow * 0x100000000000000);
            cmd |= (addressMiddle * 0x1000000000000);
            cmd |= (addressHigh * 0x10000000000);
            cmd |= (len * 0x100);
            //Console.WriteLine("send: " + cmd.ToString("X16"));

            msg.setData(cmd);
            m_canListener.setupWaitMessage(waitforResponseID);
            if (!canUsbDevice.sendMessage(msg))
            {
                AddToCanTrace("Couldn't send message");
            }

            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            //Console.WriteLine("Received in SendTransferData: " + data.ToString("X16"));
            if (getCanData(data, 0) != 0x30 || getCanData(data, 1) != 0x00)
            {
                return false;
            }
            return true;
        }

        private bool StartBootloader011()
        {
            CANMessage msg = new CANMessage(0x11, 0, 7);   //<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0060241000803606;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x311);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x76)
            {
                return false;
            }
            return true;
        }

        public byte[] RequestECUInfo0101(uint _pid)
        {
            byte[] retval = new byte[2];
            byte[] rx_buffer = new byte[128];
            int rx_pnt = 0;

            if (canUsbDevice.isOpen())
            {
                ulong cmd = 0x0000000000001A02 | _pid<<16;                
                
                //SendMessage(data);  // software version
                CANMessage msg = new CANMessage(0x11, 0, 3); //<GS-18052011> support for ELM requires length byte
                msg.setData(cmd);
                m_canListener.setupWaitMessage(0x7E8);
                if (!canUsbDevice.sendMessage(msg))
                {
                    Console.WriteLine("Couldn't send message");
                }

                int msgcnt = 0;
                bool _success = false;
                CANMessage response = new CANMessage();
                ulong data = 0;
                while (!_success && msgcnt < 2)
                {
                    response = new CANMessage();
                    response = m_canListener.waitMessage(1000);
                    data = response.getData();
                    if (response.getCanData(1) != 0x7E) _success = true;
                    msgcnt++;
                }

                //CANMessage response = new CANMessage();
                //response = m_canListener.waitMessage(1000);
                //ulong data = response.getData();
                if (response.getCanData(1) == 0x5A)
                {
                    // only one frame in this repsonse

                    for (uint fi = 3; fi < 8; fi++) rx_buffer[rx_pnt++] = response.getCanData(fi);
                    retval = new byte[rx_pnt];
                    for (int i = 0; i < rx_pnt; i++) retval[i] = rx_buffer[i];
                }
                else if (response.getCanData(2) == 0x5A)
                {
                    SendAckMessageT8();
                    byte len = response.getCanData(1);
                    int m_nrFrameToReceive = ((len - 4) / 8);
                    if ((len - 4) % 8 > 0) m_nrFrameToReceive++;
                    int lenthisFrame = len;
                    if (lenthisFrame > 4) lenthisFrame = 4;
                    for (uint fi = 4; fi < 4 + lenthisFrame; fi++) rx_buffer[rx_pnt++] = response.getCanData(fi);
                    // wait for more records now

                    while (m_nrFrameToReceive > 0)
                    {
                        m_canListener.setupWaitMessage(0x7E8);
                        response = new CANMessage();
                        response = m_canListener.waitMessage(1000);
                        if (response.getCanData(1) != 0x7E)
                        {
                            m_nrFrameToReceive--;
                            data = response.getData();
                            // add the bytes to the receive buffer
                            for (uint fi = 1; fi < 8; fi++)
                            {
                                if (rx_pnt < rx_buffer.Length) // prevent overrun
                                {
                                    rx_buffer[rx_pnt++] = response.getCanData(fi);
                                }
                            }
                        }
                    }
                    retval = new byte[rx_pnt];
                    for (int i = 0; i < rx_pnt; i++) retval[i] = rx_buffer[i];

                }
                else if (response.getCanData(1) == 0x7F && response.getCanData(1) == 0x27)
                {
                    string info = TranslateErrorCode(response.getCanData(3));
                    CastInfoEvent("Error: " + info, ActivityType.ConvertingFile);
                }
            }
            Thread.Sleep(5);

            return retval;
        }

        public string GetDiagnosticDataIdentifier0101()
        {
            //9A = 01 10 0
            string retval = string.Empty;
            byte[] data = RequestECUInfo0101(0x9A);
            if (data[0] == 0x00 && data[1] == 0x00) return string.Empty;
            if (data.Length >= 2)
            {
                retval = "0x" + data[0].ToString("X2") + " " + "0x" + data[1].ToString("X2");
            }
            return retval;
        }

        public bool RecoverECU(string filename)
        {
            string diagDataID = GetDiagnosticDataIdentifier0101();
            Console.WriteLine("DataID: " + diagDataID);
            if (diagDataID == string.Empty)
            {
                canUsbDevice.SetupCANFilter("7E8", "000");
                //canUsbDevice.SetAutomaticFlowControl(false);
                BlockManager bm = new BlockManager();
                bm.SetFilename(filename);
                int startAddress = 0x020000;
                sw.Reset();
                sw.Start();

                _stallKeepAlive = true;

                CastInfoEvent("Recovery needed...", ActivityType.UploadingBootloader);
                BroadcastKeepAlive();
                Thread.Sleep(200);  // was 1
                BroadcastKeepAlive();
                Thread.Sleep(500);
                CastInfoEvent("Starting session", ActivityType.UploadingBootloader);
                BroadcastSession10();
                Thread.Sleep(200);  // was 1
                CastInfoEvent("Telling ECU to clear CANbus", ActivityType.UploadingBootloader);
                BroadcastShutup();
                Thread.Sleep(200);  // was 1
                int progState = GetProgrammingState(0x311);
                if (progState == 0x01)
                {
                    CastInfoEvent("Recovery needed phase 1", ActivityType.UploadingBootloader);
                    BroadcastShutup011();
                    if (GetProgrammingState011() == 0x01)
                    {
                        CastInfoEvent("Recovery needed phase 2", ActivityType.UploadingBootloader);
                        SendA5011();
                        Thread.Sleep(100);
                        SendA503011();
                        Thread.Sleep(100);
                        BroadcastKeepAlive();
                        Thread.Sleep(100);
                        CastInfoEvent("Requesting security access...", ActivityType.UploadingBootloader);
                        if (RequestSecurityAccess011(0))
                        {
                            CastInfoEvent("Security access granted, uploading bootloader", ActivityType.UploadingBootloader);
                            UploadBootloaderProg011();
                            CastInfoEvent("Starting bootloader", ActivityType.UploadingBootloader);
                            Thread.Sleep(500);
                            StartBootloader011();
                            Thread.Sleep(500);
                            CastInfoEvent("Erasing flash", ActivityType.StartErasingFlash);
                            if (SendrequestDownload(true))
                            {
                                _needRecovery = true;
                                CastInfoEvent("Programming flash", ActivityType.UploadingFlash);
                                for (int blockNumber = 0; blockNumber <= 0xF50; blockNumber++)
                                {
                                    float percentage = ((float)blockNumber * 100) / 3920F;
                                    CastProgressWriteEvent(percentage);
                                    byte[] data2Send = bm.GetNextBlock();
                                    int length = 0xF0;
                                    if (blockNumber == 0xF50) length = 0xE6;
                                    if (SendTransferData(length, startAddress + (blockNumber * 0xEA), 0x311))
                                    {
                                        // send the data from the block

                                        // calculate number of frames
                                        int numberOfFrames = (int)data2Send.Length / 7; // remnants?
                                        if (((int)data2Send.Length % 7) > 0) numberOfFrames++;
                                        byte iFrameNumber = 0x21;
                                        int txpnt = 0;
                                        CANMessage msg = new CANMessage(0x7E0, 0, 8);
                                        for (int frame = 0; frame < numberOfFrames; frame++)
                                        {
                                            ulong cmd = 0x0000000000000000; // 0x34 = upload data to ECU
                                            msg.setData(cmd);
                                            msg.setCanData(iFrameNumber, 0);
                                            msg.setCanData(data2Send[txpnt++], 1);
                                            msg.setCanData(data2Send[txpnt++], 2);
                                            msg.setCanData(data2Send[txpnt++], 3);
                                            msg.setCanData(data2Send[txpnt++], 4);
                                            msg.setCanData(data2Send[txpnt++], 5);
                                            msg.setCanData(data2Send[txpnt++], 6);
                                            msg.setCanData(data2Send[txpnt++], 7);
                                            iFrameNumber++;
                                            if (iFrameNumber > 0x2F) iFrameNumber = 0x20;
                                            msg.elmExpectedResponses = frame == numberOfFrames - 1 ? 1 : 0;
                                            if (!canUsbDevice.sendMessage(msg))
                                            {
                                                AddToCanTrace("Couldn't send message");
                                            }
                                            Thread.Sleep(1);
                                        }

                                        // send the remaining data
                                        m_canListener.setupWaitMessage(0x7E8);
                                        // now wait for 01 76 00 00 00 00 00 00 
                                        CANMessage response = new CANMessage();
                                        response = new CANMessage();
                                        response = m_canListener.waitMessage(1000);
                                        ulong data = response.getData();
                                        if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x76)
                                        {
                                            _stallKeepAlive = false;
                                            return false;
                                        }
                                        BroadcastKeepAlive();
                                    }
                                }
                                sw.Stop();
                                _needRecovery = false;
                                CastInfoEvent("Recovery completed", ActivityType.ConvertingFile);
                                // what else to do?
                                Send0120();
                                CastInfoEvent("Session ended", ActivityType.FinishedFlashing);
                                return true;
                            }
                            else
                            {
                                sw.Stop();
                                _needRecovery = false;
                                _stallKeepAlive = false;
                                CastInfoEvent("Failed to erase flash", ActivityType.ConvertingFile);
                                Send0120();
                                CastInfoEvent("Session ended", ActivityType.FinishedFlashing);
                                return false;

                            }
                        }
                    }
                    else
                    {
                        CastInfoEvent("Recovery not needed...", ActivityType.UploadingBootloader);
                    }
                }
                else if (progState == 0x00)
                {
                    CastInfoEvent("Recovery not needed...", ActivityType.UploadingBootloader);
                }
                else if (progState == -1)
                {
                    CastInfoEvent("Unable to communicate with the ECU...", ActivityType.UploadingBootloader);
                }
                sw.Stop();
            }
            else
            {
                CastInfoEvent("Recovery not needed...", ActivityType.UploadingBootloader);
            }
            return false;
        }

        /// <summary>
        /// Send ONLY the erase and write commands, ECU is already in running bootloader
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool RecoverECUOLD(string filename)
        {
            if (!canUsbDevice.isOpen()) return false;
            _stallKeepAlive = true;
            BlockManager bm = new BlockManager();
            bm.SetFilename(filename);
            int startAddress = 0x020000;

            SendKeepAlive();
            _securityLevel = AccessLevel.AccessLevel01;
            CastInfoEvent("Erasing flash", ActivityType.StartErasingFlash);
            if (SendrequestDownload(true))
            {
                CastInfoEvent("Programming flash", ActivityType.UploadingFlash);
                for (int blockNumber = 0; blockNumber <= 0xF50; blockNumber++)
                {
                    float percentage = ((float)blockNumber * 100) / 3920F;
                    CastProgressWriteEvent(percentage);
                    byte[] data2Send = bm.GetNextBlock();
                    int length = 0xF0;
                    if (blockNumber == 0xF50) length = 0xE6;
                    if (SendTransferData(length, startAddress + (blockNumber * 0xEA), 0x311))
                    {
                        // send the data from the block

                        // calculate number of frames
                        int numberOfFrames = (int)data2Send.Length / 7; // remnants?
                        if (((int)data2Send.Length % 7) > 0) numberOfFrames++;
                        byte iFrameNumber = 0x21;
                        int txpnt = 0;
                        CANMessage msg = new CANMessage(0x7E0, 0, 8);
                        for (int frame = 0; frame < numberOfFrames; frame++)
                        {
                            ulong cmd = 0x0000000000000000; // 0x34 = upload data to ECU
                            msg.setData(cmd);
                            msg.setCanData(iFrameNumber, 0);
                            msg.setCanData(data2Send[txpnt++], 1);
                            msg.setCanData(data2Send[txpnt++], 2);
                            msg.setCanData(data2Send[txpnt++], 3);
                            msg.setCanData(data2Send[txpnt++], 4);
                            msg.setCanData(data2Send[txpnt++], 5);
                            msg.setCanData(data2Send[txpnt++], 6);
                            msg.setCanData(data2Send[txpnt++], 7);
                            iFrameNumber++;
                            if (iFrameNumber > 0x2F) iFrameNumber = 0x20;
                            if (!canUsbDevice.sendMessage(msg))
                            {
                                AddToCanTrace("Couldn't send message");
                            }
                            Thread.Sleep(1);
                        }

                        // send the remaining data
                        m_canListener.setupWaitMessage(0x7E8);
                        // now wait for 01 76 00 00 00 00 00 00 
                        CANMessage response = new CANMessage();
                        response = new CANMessage();
                        response = m_canListener.waitMessage(1000);
                        ulong data = response.getData();
                        if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x76)
                        {
                            return false;
                        }
                        SendKeepAlive();
                    }
                }
                sw.Stop();
                CastInfoEvent("Flash upload completed", ActivityType.ConvertingFile);
                // what else to do?
                Send0120();
                CastInfoEvent("Session ended", ActivityType.FinishedFlashing);
            }
            else
            {
                sw.Stop();
                CastInfoEvent("Failed to erase flash", ActivityType.ConvertingFile);
                Send0120();
                CastInfoEvent("Session ended", ActivityType.FinishedFlashing);
                return false;

            }
            _stallKeepAlive = false;
            return true;

        }

        private bool _needRecovery = false;

        public bool NeedRecovery
        {
            get { return _needRecovery; }
            set { _needRecovery = value; }
        }
        
        public bool UpdateFlashT8(string filename)
        {
            if (!canUsbDevice.isOpen()) return false;
            _needRecovery = false;
            BlockManager bm = new BlockManager();
            bm.SetFilename(filename);

            _stallKeepAlive = true;
            
            SendKeepAlive();
            sw.Reset();
            sw.Start();
            CastInfoEvent("Starting session", ActivityType.UploadingBootloader);
            StartSession10();
            CastInfoEvent("Requesting mandatory data", ActivityType.UploadingBootloader);
            RequestECUInfo(0x90);
            RequestECUInfo(0x97);
            RequestECUInfo(0x92);
            RequestECUInfo(0xB4);
            RequestECUInfo(0xC1);
            RequestECUInfo(0xC2);
            RequestECUInfo(0xC3);
            RequestECUInfo(0xC4);
            RequestECUInfo(0xC5);
            RequestECUInfo(0xC6);
            Send0120();
            Thread.Sleep(1000);
            StartSession1081();
            StartSession10();
            CastInfoEvent("Telling ECU to clear CANbus", ActivityType.UploadingBootloader);
            SendShutup();
            SendA2();
            SendA5();
            SendA503();
            Thread.Sleep(500);
            SendKeepAlive();

            // verified upto here

            _securityLevel = AccessLevel.AccessLevel01;
            CastInfoEvent("Requesting security access", ActivityType.UploadingBootloader);
            if (!RequestSecurityAccess(2000))
            {
                CastInfoEvent("Failed to get security access", ActivityType.UploadingFlash);
                _stallKeepAlive = false;
                return false;
            }
            Thread.Sleep(500);
            CastInfoEvent("Uploading bootloader", ActivityType.UploadingBootloader);
            if (!UploadBootloaderProg())
            {
                CastInfoEvent("Failed to upload bootloader", ActivityType.UploadingFlash);
                _stallKeepAlive = false;
                return false;
            }
            CastInfoEvent("Starting bootloader", ActivityType.UploadingBootloader);
            // start bootloader in ECU
            //SendKeepAlive();
            Thread.Sleep(500);
            if (!StartBootloader())
            {
                CastInfoEvent("Failed to start bootloader", ActivityType.UploadingFlash);
                _stallKeepAlive = false;
                return false;
            }
            Thread.Sleep(500);
            SendKeepAlive();
            Thread.Sleep(200);

            CastInfoEvent("Erasing flash", ActivityType.StartErasingFlash);
            if (SendrequestDownload(false))
            {
                _needRecovery = true;
                CastInfoEvent("Programming flash", ActivityType.UploadingFlash);
                bool success = ProgramFlashT8(bm);
                
                if (success)
                    CastInfoEvent("Flash upload completed", ActivityType.ConvertingFile);
                else
                    CastInfoEvent("Flash upload failed", ActivityType.ConvertingFile);

                sw.Stop();
                _needRecovery = false;
                
                // what else to do?
                Send0120();
                CastInfoEvent("Session ended", ActivityType.FinishedFlashing);
            }
            else
            {
                sw.Stop();
                _needRecovery = false;
                _stallKeepAlive = false;
                CastInfoEvent("Failed to erase flash", ActivityType.ConvertingFile);
                Send0120();
                CastInfoEvent("Session ended", ActivityType.FinishedFlashing);
                return false;

            }
            _stallKeepAlive = false;
            return true;
        }

        private bool ProgramFlashT8(BlockManager bm)
        {
            int startAddress = 0x020000;

            for (int blockNumber = 0; blockNumber <= 0xF50; blockNumber++)
            {
                float percentage = ((float)blockNumber * 100) / 3920F;
                CastProgressWriteEvent(percentage);
                bool canSkip = false;// bm.CanSkipCurrentBlock();
                byte[] data2Send = bm.GetNextBlock();                
                int length = 0xF0;
                if (blockNumber == 0xF50) length = 0xE6;

                int currentAddress = startAddress + (blockNumber * 0xEA);
                if (!canSkip)
                {
                    
                    if (SendTransferData(length, currentAddress, 0x7E8))
                    {
                        // send the data from the block

                        // calculate number of frames
                        int numberOfFrames = (int)data2Send.Length / 7; // remnants?
                        if (((int)data2Send.Length % 7) > 0) numberOfFrames++;
                        byte iFrameNumber = 0x21;
                        int txpnt = 0;
                        CANMessage msg = new CANMessage(0x7E0, 0, 8);
                        for (int frame = 0; frame < numberOfFrames; frame++)
                        {
                            ulong cmd = 0x0000000000000000; // 0x34 = upload data to ECU
                            msg.setData(cmd);
                            msg.setCanData(iFrameNumber, 0);
                            msg.setCanData(data2Send[txpnt++], 1);
                            msg.setCanData(data2Send[txpnt++], 2);
                            msg.setCanData(data2Send[txpnt++], 3);
                            msg.setCanData(data2Send[txpnt++], 4);
                            msg.setCanData(data2Send[txpnt++], 5);
                            msg.setCanData(data2Send[txpnt++], 6);
                            msg.setCanData(data2Send[txpnt++], 7);
                            iFrameNumber++;
                            if (iFrameNumber > 0x2F) iFrameNumber = 0x20;
                            msg.elmExpectedResponses = (frame == numberOfFrames - 1) ? 1 : 0;
                            if (!canUsbDevice.sendMessage(msg))
                            {
                                AddToCanTrace("Couldn't send message");
                            }
                            Thread.Sleep(m_sleepTime);
                        }

                        // send the remaining data
                        m_canListener.setupWaitMessage(0x7E8);
                        // now wait for 01 76 00 00 00 00 00 00 
                        CANMessage response = new CANMessage();
                        response = new CANMessage();
                        response = m_canListener.waitMessage(1000);
                        ulong data = response.getData();
                        if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x76)
                        {
                            _stallKeepAlive = false;
                            return false;
                        }
                        SendKeepAlive();
                    }
                }
                else
                {
                    Console.WriteLine("Skipping block at " + currentAddress);
                }
            }
            return true;
        }

        /// <summary>
        /// Write a byte array to an address.
        /// </summary>
        /// <param name="address">Address. Must be greater than 0x1000</param>
        /// <param name="data">Data to be written</param>
        /// <returns></returns>
        //KWP2000 can read more than 6 bytes at a time.. but for now we are happy with this
        public bool writeMemory(int address, byte[] memdata)
        {
            if (!canUsbDevice.isOpen()) return false;
            _stallKeepAlive = true;

            /* for (int i = 0; i < 6; i++)
             {
                 InitializeSession();
                 Thread.Sleep(1000);
             }*/

            CANMessage response = new CANMessage();
            ulong data = 0;
            // first send 
            CANMessage msg = new CANMessage(0x7E0, 0, 7);//<GS-18052011> ELM327 support requires the length byte
            //Console.WriteLine("Writing " + address.ToString("X8") + " len: " + memdata.Length.ToString("X2"));
            ulong cmd = 0x0000000000003406; // 0x34 = upload data to ECU
            ulong addressHigh = (uint)address & 0x0000000000FF0000;
            addressHigh /= 0x10000;
            ulong addressMiddle = (uint)address & 0x000000000000FF00;
            addressMiddle /= 0x100;
            ulong addressLow = (uint)address & 0x00000000000000FF;
            ulong len = (ulong)memdata.Length;

            //cmd |= (addressLow * 0x100000000);
            //cmd |= (addressMiddle * 0x1000000);
            //cmd |= (addressHigh * 0x10000);
            //            cmd |= (len * 0x1000000000000);


            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                AddToCanTrace("Couldn't send message");
            }


            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            data = response.getData();
            CastInfoEvent("Waited for response: " + data.ToString("X8"), ActivityType.ConvertingFile);
            if (getCanData(data, 0) != 0x01 || getCanData(data, 1) != 0x74)
            {
                CastInfoEvent("Unable to write to ECUs memory", ActivityType.ConvertingFile);
                AddToCanTrace("Unable to write data to ECUs memory");
                //_stallKeepAlive = false;
                //return false;
            }
            //10 F0 36 00 00 10 24 00 
            cmd = 0x0000000000360010; // 0x34 = upload data to ECU


            cmd |= (addressLow * 0x100000000000000);
            cmd |= (addressMiddle * 0x1000000000000);
            cmd |= (addressHigh * 0x10000000000);
            cmd |= (len * 0x100);
            //Console.WriteLine("send: " + cmd.ToString("X16"));

            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                AddToCanTrace("Couldn't send message");
            }
            // wait for response, should be 30 00 00 00 00 00 00 00
            data = 0;
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            data = response.getData();
            int numberOfFrames = (int)len / 7; // remnants?
            if (((int)len % 7) > 0) numberOfFrames++;
            byte iFrameNumber = 0x21;
            int txpnt = 0;
            if (data == 0x0000000000000030)
            {
                for (int i = 0; i < numberOfFrames; i++)
                {
                    cmd = 0x0000000000000000; // 0x34 = upload data to ECU
                    msg.setData(cmd);
                    msg.setCanData(iFrameNumber, 0);
                    msg.setCanData(memdata[txpnt++], 1);
                    msg.setCanData(memdata[txpnt++], 2);
                    msg.setCanData(memdata[txpnt++], 3);
                    msg.setCanData(memdata[txpnt++], 4);
                    msg.setCanData(memdata[txpnt++], 5);
                    msg.setCanData(memdata[txpnt++], 6);
                    msg.setCanData(memdata[txpnt++], 7);
                    iFrameNumber++;
                    if (!canUsbDevice.sendMessage(msg))
                    {
                        AddToCanTrace("Couldn't send message");
                    }
                    Thread.Sleep(1);
                    // send the data with 7 bytes at a time
                }
                m_canListener.setupWaitMessage(0x7E8);
                response = new CANMessage();
                response = m_canListener.waitMessage(1000);
                data = response.getData();
                Console.WriteLine("received: " + data.ToString("X8"));
            }
            _stallKeepAlive = false;
            return true;
        }

        private void SendKeepAlive()
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 2);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0000000000003E01; // always 2 bytes
            msg.setData(cmd);
            msg.elmExpectedResponses = 1;
            //Console.WriteLine("KA sent");
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            //Console.WriteLine("received KA: " + response.getCanData(1).ToString("X2"));
        }

        private void AddToCanTrace(string line)
        {
            //Console.WriteLine(line);
            if (m_EnableCanLog)
            {
                DateTime dtnow = DateTime.Now;
                lock (this)
                {
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\dataTrace.txt", true))
                        {
                            sw.WriteLine(dtnow.ToString("dd/MM/yyyy HH:mm:ss.fff") + " - " + line);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        private Stopwatch sw = new Stopwatch();

        [Obsolete("getFlashContent is deprecated, use getFlashWithBootloader.")]
        public byte[] getFlashContent()
        {
            _stallKeepAlive = true;
            bool success = false;
            int retryCount = 0;
            int startAddress = 0x000000;
            int blockSize = 0x40;
            int bufpnt = 0;
            byte[] buf = new byte[0x100000];
            int blockCount = 0;
            SendKeepAlive();
            sw.Reset();
            sw.Start();

            //for (int i = 0; i < buf.Length / blockSize; i++)
            while (startAddress < buf.Length)
            {
                if (!canUsbDevice.isOpen())
                {
                    _stallKeepAlive = false;
                    return buf;
                }
                byte[] readbuf = readMemory(startAddress, blockSize, out success);
                if (success)
                {
                    if (readbuf.Length == blockSize)
                    {
                        for (int j = 0; j < blockSize; j++)
                        {
                            buf[bufpnt++] = readbuf[j];
                        }
                    }
                    //string infoStr = "Address: " + startAddress.ToString("X8"); //+ " ";
                    CastProgressReadEvent((float)(bufpnt * 100) / (float)buf.Length);
                    startAddress += blockSize;
                    retryCount = 0;
                }
                else
                {
                    CastInfoEvent("Frame dropped, retrying " + startAddress.ToString("X8") + " " + retryCount.ToString(), ActivityType.DownloadingFlash);
                    retryCount++;
                    // read all available message from the bus now

                    for (int i = 0; i < 10; i++)
                    {
                        CANMessage response = new CANMessage();
                        ulong data = 0;
                        response = new CANMessage();
                        response = m_canListener.waitMessage(10);
                        data = response.getData();
                    }



                    if (retryCount == maxRetries)
                    {
                        CastInfoEvent("Failed to download flash content", ActivityType.ConvertingFile);
                        _stallKeepAlive = false;
                        return buf;
                    }
                }
                blockCount++;
                if (sw.ElapsedMilliseconds > 3000) // once every 3 seconds
                //if ((blockCount % 10) == 0)
                {
                    sw.Stop();
                    sw.Reset();
                    SendKeepAlive();
                    sw.Start();
                }

            }
            sw.Stop();
            _stallKeepAlive = false;
            return buf;
        }

        public byte[] getSRAMSnapshot()
        {
            bool success = false;
            int retryCount = 0;

            _stallKeepAlive = true;
            int startAddress = 0x100000;
            int blockSize = 0x40;
            int bufpnt = 0;
            byte[] buf = new byte[0x7000];
            success = false;
            //for (int i = 0; i < buf.Length/blockSize; i++)
            while (bufpnt < buf.Length - 1)
            {
                if (!canUsbDevice.isOpen())
                {
                    _stallKeepAlive = false;
                    return buf;
                }

                byte[] readbuf = readMemory(startAddress, blockSize, out success);
                if (success)
                {

                    if (readbuf.Length == blockSize)
                    {
                        for (int j = 0; j < blockSize; j++)
                        {
                            buf[bufpnt++] = readbuf[j];
                        }
                    }
                    CastProgressReadEvent((float)(bufpnt * 85) / (float)buf.Length);
                    retryCount = 0;
                    startAddress += blockSize;
                }
                else
                {
                    CastInfoEvent("Frame dropped, retrying", ActivityType.DownloadingFlash);
                    retryCount++;
                    if (retryCount == maxRetries)
                    {
                        CastInfoEvent("Failed to download SRAM content", ActivityType.ConvertingFile);
                        _stallKeepAlive = false;
                        return buf;
                    }
                }
                SendKeepAlive();
            }
            _stallKeepAlive = false;
            return buf;
        }

        private byte getCanData(ulong m_data, uint a_index)
        {
            return (byte)(m_data >> (int)(a_index * 8));
        }
        
        private byte[] sendReadDataByLocalIdentifier(int address, int length, out bool success)
        {
            // we send: 0040000000002106
            // .. send: 06 21 80 00 00 00 00 00

            success = false;
            byte[] retData = new byte[length];
            if (!canUsbDevice.isOpen()) return retData;

            CANMessage msg = new CANMessage(0x7E0, 0, 7);//<GS-18052011> ELM327 support requires the length byte
            //Console.WriteLine("Reading " + address.ToString("X8") + " len: " + length.ToString("X2"));
            ulong cmd = 0x0000000000002106; // always 2 bytes
            ulong addressHigh = (uint)address & 0x0000000000FF0000;
            addressHigh /= 0x10000;
            ulong addressMiddle = (uint)address & 0x000000000000FF00;
            addressMiddle /= 0x100;
            ulong addressLow = (uint)address & 0x00000000000000FF;
            ulong len = (ulong)length;


            cmd |= (addressLow * 0x1000000000000);
            cmd |= (addressMiddle * 0x10000000000);
            cmd |= (addressHigh * 0x100000000);
            cmd |= (len * 0x10000); // << 2 * 8
            //Console.WriteLine("send: " + cmd.ToString("X16"));
            /*cmd |= (ulong)(byte)(address & 0x000000FF) << 4 * 8;
            cmd |= (ulong)(byte)((address & 0x0000FF00) >> 8) << 3 * 8;
            cmd |= (ulong)(byte)((address & 0x00FF0000) >> 2 * 8) << 2 * 8;
            cmd |= (ulong)(byte)((address & 0xFF000000) >> 3 * 8) << 8;*/
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            msg.elmExpectedResponses = 19; //in 19 messages there are 0x82 = 130 bytes of data, bootloader requests 0x80 =128 each time
            if (!canUsbDevice.sendMessage(msg))
            {
                AddToCanTrace("Couldn't send message");

            }
            // wait for max two messages to get rid of the alive ack message
            CANMessage response = new CANMessage();
            ulong data = 0;
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            data = response.getData();

            if (getCanData(data, 0) == 0x7E)
            {
                AddToCanTrace("Got 0x7E message as response to 0x21, ReadDataByLocalIdentifier command");
                success = false;
                return retData;
            }
            else if (response.getData() == 0x00000000)
            {
                AddToCanTrace("Get blank response message to 0x21, ReadDataByLocalIdentifier");
                success = false;
                return retData;
            }
            else if (getCanData(data, 0) == 0x03 && getCanData(data, 1) == 0x7F && getCanData(data, 2) == 0x23)
            {
                // reason was 0x31
                AddToCanTrace("No security access granted");
                RequestSecurityAccess(0);
                success = false;
                return retData;
            }
            else if (getCanData(data, 2) != 0x61 && getCanData(data, 1) != 0x61)
            {
                if (data == 0x0000000000007E01)
                {
                    // was a response to a KA.
                }
                AddToCanTrace("Incorrect response to 0x23, sendReadDataByLocalIdentifier.  Byte 2 was " + getCanData(data, 2).ToString("X2"));
                success = false;
                return retData;
            }
            //TODO: Check whether we need more than 2 bytes of data and wait for that many records after sending an ACK
            int rx_cnt = 0;
            byte frameIndex = 0x21;
            if (length > 4)
            {
                retData[rx_cnt++] = getCanData(data, 4);
                retData[rx_cnt++] = getCanData(data, 5);
                retData[rx_cnt++] = getCanData(data, 6);
                retData[rx_cnt++] = getCanData(data, 7);
                // in that case, we need more records from the ECU
                // Thread.Sleep(1);
                SendAckMessageT8(); // send ack to request more bytes
                //Thread.Sleep(1);
                // now we wait for the correct number of records to be received
                int m_nrFrameToReceive = ((length - 4) / 7);
                if ((len - 4) % 7 > 0) m_nrFrameToReceive++;
                //AddToCanTrace("Number of frames: " + m_nrFrameToReceive.ToString());
                while (m_nrFrameToReceive > 0)
                {
                    // response = new CANMessage();
                    //response.setData(0);
                    //response.setID(0);
                    // m_canListener.setupWaitMessage(0x7E8);
                    response = m_canListener.waitMessage(1000);
                    data = response.getData();
                    //AddToCanTrace("frame " + frameIndex.ToString("X2") + ": " + data.ToString("X16"));
                    if (frameIndex != getCanData(data, 0))
                    {
                        // sequence broken
                        AddToCanTrace("Received invalid sequenced frame " + frameIndex.ToString("X2") + ": " + data.ToString("X16"));
                        m_canListener.dumpQueue();
                        success = false;
                        return retData;
                    }
                    else if (data == 0x0000000000000000)
                    {
                        AddToCanTrace("Received blank message while waiting for data");
                        success = false;
                        return retData;
                    }
                    frameIndex++;
                    if (frameIndex > 0x2F) frameIndex = 0x20;
                    // additional check for sequencing of frames
                    m_nrFrameToReceive--;
                    //AddToCanTrace("frames left: " + m_nrFrameToReceive.ToString());
                    // add the bytes to the receive buffer
                    //string checkLine = string.Empty;
                    for (uint fi = 1; fi < 8; fi++)
                    {
                        //checkLine += getCanData(data, fi).ToString("X2");
                        if (rx_cnt < retData.Length) // prevent overrun
                        {
                            retData[rx_cnt++] = getCanData(data, fi);
                        }
                    }
                    //AddToCanTrace("frame(2): " + checkLine);
                    //Thread.Sleep(1);

                }

                canUsbDevice.RequestDeviceReady();

            }
            else
            {
                if (retData.Length > rx_cnt) retData[rx_cnt++] = getCanData(data, 4);
                if (retData.Length > rx_cnt) retData[rx_cnt++] = getCanData(data, 5);
                if (retData.Length > rx_cnt) retData[rx_cnt++] = getCanData(data, 6);
                if (retData.Length > rx_cnt) retData[rx_cnt++] = getCanData(data, 7);
                //AddToCanTrace("received data: " + retData[0].ToString("X2"));
            }
            /*string line = address.ToString("X8") + " ";
            foreach (byte b in retData)
            {
                line += b.ToString("X2") + " ";
            }
            AddToCanTrace(line);*/
            success = true;

            return retData;
        }

        //KWP2000 can read more than 6 bytes at a time.. but for now we are happy with this
        private byte[] sendReadCommand(int address, int length, out bool success)
        {

            success = false;
            byte[] retData = new byte[length];
            if (!canUsbDevice.isOpen()) return retData;

            CANMessage msg = new CANMessage(0x7E0, 0, 7);//<GS-18052011> ELM327 support requires the length byte
            //Console.WriteLine("Reading " + address.ToString("X8") + " len: " + length.ToString("X2"));
            ulong cmd = 0x0000000000002306; // always 2 bytes
            ulong addressHigh = (uint)address & 0x0000000000FF0000;
            addressHigh /= 0x10000;
            ulong addressMiddle = (uint)address & 0x000000000000FF00;
            addressMiddle /= 0x100;
            ulong addressLow = (uint)address & 0x00000000000000FF;
            ulong len = (ulong)length;


            cmd |= (addressLow * 0x100000000);
            cmd |= (addressMiddle * 0x1000000);
            cmd |= (addressHigh * 0x10000);
            cmd |= (len * 0x1000000000000);
            //Console.WriteLine("send: " + cmd.ToString("X16"));
            /*cmd |= (ulong)(byte)(address & 0x000000FF) << 4 * 8;
            cmd |= (ulong)(byte)((address & 0x0000FF00) >> 8) << 3 * 8;
            cmd |= (ulong)(byte)((address & 0x00FF0000) >> 2 * 8) << 2 * 8;
            cmd |= (ulong)(byte)((address & 0xFF000000) >> 3 * 8) << 8;*/
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                AddToCanTrace("Couldn't send message");

            }
            // wait for max two messages to get rid of the alive ack message
            CANMessage response = new CANMessage();
            ulong data = 0;
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            data = response.getData();

            if (getCanData(data, 0) == 0x7E)
            {
                AddToCanTrace("Got 0x7E message as response to 0x23, readMemoryByAddress command");
                success = false;
                return retData;
            }
            else if (response.getData() == 0x00000000)
            {
                AddToCanTrace("Get blank response message to 0x23, readMemoryByAddress");
                success = false;
                return retData;
            }
            else if (getCanData(data, 0) == 0x03 && getCanData(data, 1) == 0x7F && getCanData(data, 2) == 0x23)
            {
                // reason was 0x31
                AddToCanTrace("No security access granted");
                RequestSecurityAccess(0);
                success = false;
                return retData;
            }
            /*else if (getCanData(data, 0) != 0x10)
            {
                AddToCanTrace("Incorrect response message to 0x23, readMemoryByAddress. Byte 0 was " + getCanData(data, 0).ToString("X2"));
                success = false;
                return retData;
            }
            else if (getCanData(data, 1) != len + 4)
            {
                AddToCanTrace("Incorrect length data message to 0x23, readMemoryByAddress.  Byte 1 was " + getCanData(data, 1).ToString("X2"));
                success = false;
                return retData;
            }*/
            else if (getCanData(data, 2) != 0x63 && getCanData(data, 1) != 0x63)
            {
                if (data == 0x0000000000007E01)
                {
                    // was a response to a KA.
                }
                AddToCanTrace("Incorrect response to 0x23, readMemoryByAddress.  Byte 2 was " + getCanData(data, 2).ToString("X2"));
                success = false;
                return retData;
            }
            //TODO: Check whether we need more than 2 bytes of data and wait for that many records after sending an ACK
            int rx_cnt = 0;
            byte frameIndex = 0x21;
            if (length > 3)
            {
                retData[rx_cnt++] = getCanData(data, 6);
                retData[rx_cnt++] = getCanData(data, 7);
                // in that case, we need more records from the ECU
                // Thread.Sleep(1);
                SendAckMessageT8(); // send ack to request more bytes
                //Thread.Sleep(1);
                // now we wait for the correct number of records to be received
                int m_nrFrameToReceive = ((length - 2) / 7);
                if ((len - 2) % 7 > 0) m_nrFrameToReceive++;
                //AddToCanTrace("Number of frames: " + m_nrFrameToReceive.ToString());
                while (m_nrFrameToReceive > 0)
                {
                    // response = new CANMessage();
                    //response.setData(0);
                    //response.setID(0);
                    // m_canListener.setupWaitMessage(0x7E8);
                    response = m_canListener.waitMessage(1000);
                    data = response.getData();
                    //AddToCanTrace("frame " + frameIndex.ToString("X2") + ": " + data.ToString("X16"));
                    if (frameIndex != getCanData(data, 0))
                    {
                        // sequence broken
                        AddToCanTrace("Received invalid sequenced frame " + frameIndex.ToString("X2") + ": " + data.ToString("X16"));
                        m_canListener.dumpQueue();
                        success = false;
                        return retData;
                    }
                    else if (data == 0x0000000000000000)
                    {
                        AddToCanTrace("Received blank message while waiting for data");
                        success = false;
                        return retData;
                    }
                    frameIndex++;
                    // additional check for sequencing of frames
                    m_nrFrameToReceive--;
                    //AddToCanTrace("frames left: " + m_nrFrameToReceive.ToString());
                    // add the bytes to the receive buffer
                    //string checkLine = string.Empty;
                    for (uint fi = 1; fi < 8; fi++)
                    {
                        //checkLine += getCanData(data, fi).ToString("X2");
                        if (rx_cnt < retData.Length) // prevent overrun
                        {
                            retData[rx_cnt++] = getCanData(data, fi);
                        }
                    }
                    //AddToCanTrace("frame(2): " + checkLine);
                    //Thread.Sleep(1);

                }

            }
            else
            {
                if (retData.Length > rx_cnt) retData[rx_cnt++] = getCanData(data, 5);
                if (retData.Length > rx_cnt) retData[rx_cnt++] = getCanData(data, 6);
                if (retData.Length > rx_cnt) retData[rx_cnt++] = getCanData(data, 7);
                //AddToCanTrace("received data: " + retData[0].ToString("X2"));
            }
            /*string line = address.ToString("X8") + " ";
            foreach (byte b in retData)
            {
                line += b.ToString("X2") + " ";
            }
            AddToCanTrace(line);*/
            success = true;

            return retData;
        }

        public float readBatteryVoltageOBDII()
        {
            // send message to read DTCs pid 0x18
            float retval = 0;
            ulong cmd = 0x0000000000420102; // only stored DTCs
            //SendMessage(data);  // software version
            CANMessage msg = new CANMessage(0x7DF, 0, 8);
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            // wait for a 0x58 or a 0x7F message
            CANMessage response = new CANMessage();
            ulong data = 0;
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            data = response.getData();
            retval = Convert.ToInt32(getCanData(data, 3)) * 256;
            retval += Convert.ToInt32(getCanData(data, 4));
            retval /= 1000;
            return retval;
        }

        // How to read DTC codes
        //A7 A6    First DTC character
        //-- --    -------------------
        // 0  0    P - Powertrain
        // 0  1    C - Chassis
        // 1  0    B - Body
        // 1  1    U - Network

        //A5 A4    Second DTC character
        //-- --    --------------------
        // 0  0    0
        // 0  1    1
        // 1  0    2
        // 1  1    3

        //A3 A2 A1 A0    Third/Fourth/Fifth DTC characters
        //-- -- -- --    -------------------
        // 0  0  0  0    0
        // 0  0  0  1    1
        // 0  0  1  0    2
        // 0  0  1  1    3
        // 0  1  0  0    4
        // 0  1  0  1    5
        // 0  1  1  0    6
        // 0  1  1  1    7
        // 1  0  0  0    8
        // 1  0  0  1    9
        // 1  0  1  0    A
        // 1  0  1  1    B
        // 1  1  0  0    C
        // 1  1  0  1    D
        // 1  1  1  0    E
        // 1  1  1  1    F

        // Example
        // E1 03 ->
        // 1110 0001 0000 0011
        // 11=U
        //   10=2
        //      0001=1
        //           0000=0
        //                0011=3
        //----------------------
        // U2103
        private static string GetDtcDescription(CANMessage responseDTC)
        {
            int firstDtcNum = (0xC0 & Convert.ToInt32(responseDTC.getCanData(1))) >> 6;
            char firstDtcChar = '-';
            if (firstDtcNum == 0)
            {
                firstDtcChar = 'P';
            }
            else if (firstDtcNum == 1)
            {
                firstDtcChar = 'C';
            }
            else if (firstDtcNum == 2)
            {
                firstDtcChar = 'B';
            }
            else if (firstDtcNum == 3)
            {
                firstDtcChar = 'U';
            }
            int secondDtcNum = (0x30 & Convert.ToInt32(responseDTC.getCanData(1))) >> 4;

            int thirdDtcNum = (0x0F & Convert.ToInt32(responseDTC.getCanData(1)));

            int forthDtcNum = (0xF0 & Convert.ToInt32(responseDTC.getCanData(2))) >> 4;

            int fifthDtcNum = (0x0F & Convert.ToInt32(responseDTC.getCanData(2)));

            // It seems Trionic8 return 00
            //byte failureTypeByte = responseDTC.getCanData(3);

            byte statusByte = responseDTC.getCanData(4);
            String statusDescription = string.Empty;
            if (0x80 == (0x80 & statusByte)) statusDescription += "warningIndicatorRequestedState ";
            if (0x40 == (0x40 & statusByte)) statusDescription += "currentDTCSincePowerUp ";
            if (0x20 == (0x20 & statusByte)) statusDescription += "testNotPassedSinceCurrentPowerUp ";
            if (0x10 == (0x10 & statusByte)) statusDescription += "historyDTC ";
            if (0x08 == (0x08 & statusByte)) statusDescription += "testFailedSinceDTCCleared ";
            if (0x04 == (0x04 & statusByte)) statusDescription += "testNotPassedSinceDTCCleared ";
            if (0x02 == (0x02 & statusByte)) statusDescription += "currentDTC ";
            if (0x01 == (0x01 & statusByte)) statusDescription += "DTCSupportedByCalibration ";

            return "DTC: " + firstDtcChar + secondDtcNum.ToString("d") + thirdDtcNum.ToString("X") + forthDtcNum.ToString("X") + fifthDtcNum.ToString("X") + " StatusByte: " + statusByte.ToString("X2") + " StatusDescription: " + statusDescription;
        }

        public string[] readDTCCodes()
        {
            // test code
            //ulong c = 0x0000006F00070181;//81 01 07 00 6F 00 00 00
            //ulong c = 0x000000FD00220181; //81 01 22 00 FD 00 00 00
            //CANMessage test = new CANMessage();
            //test.setData(c);
            //AddToCanTrace(GetDtcDescription(test));

            // send message to read DTC
            StartSession10();

            List<string> list = new List<string>();

            // ReadDiagnosticInformation $A9 Service
            //  readStatusOfDTCByStatusMask $81 Request
            //      DTCStatusMask $12= 0001 0010
            //        0 Bit 7 warningIndicatorRequestedState
            //        0 Bit 6 currentDTCSincePowerUp
            //        0 Bit 5 testNotPassedSinceCurrentPowerUp
            //        1 Bit 4 historyDTC
            //        0 Bit 3 testFailedSinceDTCCleared
            //        0 Bit 2 testNotPassedSinceDTCCleared
            //        1 Bit 1 currentDTC
            //        0 Bit 0 DTCSupportedByCalibration
            ulong cmd = 0x000000001281A903; // 7E0 03 A9 81 12 00 00 00 00

            CANMessage msg = new CANMessage(0x7E0, 0, 4);//<GS-18052011> ELM327 support requires the length byte
            msg.setData(cmd);
            msg.elmExpectedResponses = 15;
            m_canListener.setupWaitMessage(0x7E8);
            canUsbDevice.SetupCANFilter("7E8", "DFF"); // Mask will allow 7E8 and 5E8
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }

            CANMessage response = new CANMessage();
            ulong data = 0;
            // Wait for response 
            // 7E8 03 7F A9 78 00 00 00 00
            response = m_canListener.waitMessage(1000);
            data = response.getData();

            if (response.getCanData(1) == 0x7F && response.getCanData(2) == 0xA9 && response.getCanData(3) == 0x78) // RequestCorrectlyReceived-ResponsePending ($78, RC_RCR-RP)
            {
                // Now wait for all DTCs
                m_canListener.setupWaitMessage(0x5E8);

                bool more_errors = true;
                while (more_errors)
                {
                    CANMessage responseDTC = new CANMessage();
                    responseDTC = m_canListener.waitMessage(1000);

                    // Read until response: EndOfDTCReport
                    if (responseDTC.getCanData(1)==0 && responseDTC.getCanData(2)==0 && responseDTC.getCanData(3)==0)
                    {
                        more_errors = false;
                        list.Add("No more errors!");
                    }
                    else
                    {
                        string dtcDescription = GetDtcDescription(responseDTC);
                        AddToCanTrace(dtcDescription);
                        list.Add(dtcDescription);
                    }

                }
            }
            else if (response.getCanData(1) == 0x7F && response.getCanData(2) == 0xA9)
            {
                string info = TranslateErrorCode(response.getCanData(3));
                CastInfoEvent("Error: " + info, ActivityType.ConvertingFile);
            }

            Send0120();

            return list.ToArray();
        }

        // MattiasC, this one is probably not working, need a car to test
        // look at readDTCCodes() how it was improved.
        public string[] readDTCCodesCIM()
        {
            // test code
            //ulong c = 0x0000006F00070181;//81 01 07 00 6F 00 00 00
            //ulong c = 0x000000FD00220181; //81 01 22 00 FD 00 00 00
            //CANMessage test = new CANMessage();
            //test.setData(c);
            //AddToCanTrace(GetDtcDescription(test));

            // send message to read DTC
            StartSession10();

            List<string> list = new List<string>();

            ulong cmd = 0x000000001281A903; // 245 03 A9 81 12 00 00 00 00

            CANMessage msg = new CANMessage(0x245, 0, 8);//<GS-18052011> ELM327 support requires the length byte
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x545);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }

            CANMessage response = new CANMessage();
            ulong data = 0;
            // Wait for response 
            // 545 03 7F A9 78 00 00 00 00
            response = m_canListener.waitMessage(1000);
            data = response.getData();

            if (response.getCanData(1) == 0x7F && response.getCanData(2) == 0xA9 && response.getCanData(3) == 0x78)
            {
                // Now wait for all DTCs
                m_canListener.setupWaitMessage(0x545);

                bool more_errors = true;
                while (more_errors)
                {
                    CANMessage responseDTC = new CANMessage();
                    responseDTC = m_canListener.waitMessage(1000);

                    // Read until response:   No more errors, status == 0xFF
                    int dtcStatus = Convert.ToInt32(responseDTC.getCanData(4));
                    if (dtcStatus == 0xFF)
                    {
                        more_errors = false;
                        list.Add("0xFF No more errors!");
                    }
                    else if (dtcStatus == 0x97)
                    {
                        more_errors = false;
                        list.Add("0x17 No more errors!");
                    }
                    else
                    {
                        string dtcDescription = GetDtcDescription(responseDTC);
                        list.Add(dtcDescription);
                    }
                }
            }

            Send0120();

            return list.ToArray();
        }

        private void CastProgressWriteEvent(float percentage)
        {
            if (onWriteProgress != null)
            {
                onWriteProgress(this, new WriteProgressEventArgs(percentage));
            }
        }

        private void CastProgressReadEvent(float percentage)
        {
            if (onReadProgress != null)
            {
                onReadProgress(this, new ReadProgressEventArgs(percentage));
            }
        }

        private void CastInfoEvent(string info, ActivityType type)
        {
            Console.WriteLine(info);
            if (onCanInfo != null)
            {
                onCanInfo(this, new CanInfoEventArgs(info, type));
            }
        }

        public bool TestCIMAccess()
        {
            return RequestSecurityAccessCIM(0);
        }

        private void SendDeviceControlMessageWithCode(byte command, string secretcode /*ulong code*/)
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 7);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x000000000000AE07;
            ulong lcommand = command;
            cmd |= (lcommand * 0x10000);
            ulong code = 0;
            //0x4D4E415100000000
            code |= Convert.ToByte(secretcode[3]);
            code = code << 8;
            code |= Convert.ToByte(secretcode[2]);
            code = code << 8;
            code |= Convert.ToByte(secretcode[1]);
            code = code << 8;
            code |= Convert.ToByte(secretcode[0]);
            code = code << 4 * 8;

            cmd |= code;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage ECMresponse = new CANMessage();
            ECMresponse = m_canListener.waitMessage(1000);
        }

        private bool readDataByPacketID(byte command, uint responseID)
        {
            //SendCommandNoResponse(0x7E0, 0x000000006201AA03);
            CANMessage msg = new CANMessage(0x7E0, 0, 3);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x000000000001AA03;
            ulong lcommand = command;
            cmd |= (lcommand * 0x1000000);
            msg.setData(cmd);
            m_canListener.setupWaitMessage(responseID);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage ECMresponse = new CANMessage();
            ECMresponse = m_canListener.waitMessage(1000);
            return true;
        }

        private int GetProgrammingStateNormal()
        {
            Console.WriteLine("Get programming state");
            CANMessage msg = new CANMessage(0x7E0, 0, 2);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x000000000000A201; // 0x02 0x10 0x02
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            ulong data = response.getData();
            Console.WriteLine("Get programming state response: " + data.ToString("X16"));
            //\__ 00 00 03 11 02 e2 01 00 00 00 00 00 Magic reply, T8 replies with 0311 and programming state 01(recovery state?)
            if (getCanData(data, 1) != 0xE2 || getCanData(data, 0) != 0x02)
            {
                return 0;
            }
            return Convert.ToInt32(getCanData(data, 2));
        }

        public bool ProgramVINProcedure(string vinNumber, string secretcode)
        {
            bool retval = false;
            CastInfoEvent("Start program VIN process", ActivityType.ConvertingFile);
            _stallKeepAlive = true;
            //BroadcastKeepAlive();
            //Thread.Sleep(1000);
            //GetDiagnosticDataIdentifier();
            //Thread.Sleep(1000);
            string currentVIN = GetVehicleVIN();

            //TODO: Add logging of VIN in hexcodes here.

            if (currentVIN.Trim() == "") currentVIN = " is empty";
            CastInfoEvent("Current VIN " + currentVIN, ActivityType.ConvertingFile);
            BroadcastKeepAlive();
            Thread.Sleep(500);
            GetProgrammingStateNormal();
            //BroadcastRequest(0xA2);
            Thread.Sleep(500);
            CastInfoEvent("Request diag ID: " + GetDiagnosticDataIdentifier(), ActivityType.ConvertingFile);
            Thread.Sleep(500);
            BroadcastKeepAlive();
            Thread.Sleep(500);
            BroadcastKeepAlive();
            Thread.Sleep(500);
            BroadcastKeepAlive();
            Thread.Sleep(500);
            BroadcastKeepAlive();
            Thread.Sleep(500);
            _securityLevel = AccessLevel.AccessLevel01;

            if (RequestSecurityAccess(0))
            {
                BroadcastKeepAlive();
                Thread.Sleep(500);
                BroadcastKeepAlive();
                Thread.Sleep(500);
                BroadcastKeepAlive();
                Thread.Sleep(500);
                BroadcastKeepAlive();
                Thread.Sleep(500);

                Send0120(); // cancel diag session
                GetDiagnosticDataIdentifier();

                CastInfoEvent("Request 0xAA", ActivityType.ConvertingFile);
                readDataByPacketID(0x62, 0x5E8);
                //SendCommandNoResponse(0x7E0, 0x000000006201AA03);
                for (int tel = 0; tel < 10; tel++)
                {
                    CastInfoEvent("Waiting... " + tel.ToString() + "/10", ActivityType.ConvertingFile);
                    BroadcastKeepAlive();
                    Thread.Sleep(3000);
                }
                //CastInfoEvent("Request 0xA0", ActivityType.ConvertingFile);
                //RequestECUInfo(0xA0);
                CastInfoEvent("Request 0xAA", ActivityType.ConvertingFile);
                //SendCommandNoResponse(0x7E0, 0x000000000201AA03);
                readDataByPacketID(0x02, 0x5E8);
                BroadcastKeepAlive();
                Thread.Sleep(500);
                RequestECUInfo(0x90); // read VIN again
                Thread.Sleep(500);
                SendDeviceControlMessageWithCode(0x60, /*0x4D4E415100000000*/ secretcode);

                CastInfoEvent("Waiting...", ActivityType.ConvertingFile);
                Thread.Sleep(1000);
                BroadcastKeepAlive();
                CastInfoEvent("Waiting...", ActivityType.ConvertingFile);
                Thread.Sleep(1000);
                BroadcastKeepAlive();
                CastInfoEvent("Waiting...", ActivityType.ConvertingFile);
                Thread.Sleep(1000);
                BroadcastKeepAlive();
                CastInfoEvent("Waiting...", ActivityType.ConvertingFile);
                Thread.Sleep(1000);

                SendDeviceControlMessageWithCode(0x6e, /*0x4D4E415100000000*/ secretcode);

                //CastInfoEvent("Clearing VIN...", ActivityType.ConvertingFile);
                //ProgramVIN("                 ");
                CastInfoEvent("Programming VIN...", ActivityType.ConvertingFile);
                retval = ProgramVIN(vinNumber);
            }
            else
            {
                retval = false;
            }
            _stallKeepAlive = false;
            return retval;
        }

        /// <summary>
        /// Marries the ECM to a car
        /// </summary>
        /// <returns></returns>
        public bool MarryECM()
        {
            CastInfoEvent("Start marry process", ActivityType.ConvertingFile);
            BroadcastKeepAlive();
            Thread.Sleep(1000);
            BroadcastRequestDiagnoseID();
            Thread.Sleep(1000);
            string currentVIN = GetVehicleVIN();

            //TODO: Add logging of VIN in hexcodes here.

            if (currentVIN.Trim() == "") currentVIN = " is empty";
            CastInfoEvent("Current VIN " + currentVIN, ActivityType.ConvertingFile);
            BroadcastKeepAlive();
            BroadcastRequest(0xA2);
            Thread.Sleep(500);
            BroadcastRequestDiagnoseID();
            BroadcastKeepAlive();
            Thread.Sleep(1000);
            SendCommandNoResponse(0x7E0, 0x000000006201AA03);
            BroadcastKeepAlive();
            Thread.Sleep(1000);
            RequestECUInfo(0xA0);
            SendCommandNoResponse(0x7E0, 0x000000000201AA03);
            BroadcastKeepAlive();
            Thread.Sleep(1000);
            CastInfoEvent("Getting security access to CIM", ActivityType.ConvertingFile);

            if (RequestSecurityAccessCIM(0))
            {
                CastInfoEvent("Security access to CIM OK", ActivityType.ConvertingFile);
                BroadcastKeepAlive();
                string VINFromCIM = RequestCIMInfo(0x90);
                BroadcastKeepAlive();
                CastInfoEvent("Current VIN in CIM: " + VINFromCIM, ActivityType.ConvertingFile);
                if (ProgramVIN(VINFromCIM))
                {
                    CastInfoEvent("Programmed VIN into ECU", ActivityType.ConvertingFile);
                    BroadcastKeepAlive();
                    VINFromCIM = RequestCIMInfo(0x90);
                    if (SendSecretCodetoCIM())
                    {
                        CastInfoEvent("Sending marry command", ActivityType.ConvertingFile);
                        BroadcastKeepAlive();
                        VINFromCIM = RequestCIMInfo(0x90);
                        if (MarryCIMAndECU())
                        {
                            CastInfoEvent("Married ECU to car, finalizing procedure...", ActivityType.ConvertingFile);
                            BroadcastKeepAlive();
                            Thread.Sleep(1000);
                            CastInfoEvent("Waiting... (1/10)", ActivityType.ConvertingFile);
                            BroadcastKeepAlive();
                            Thread.Sleep(1000);
                            CastInfoEvent("Waiting... (2/10)", ActivityType.ConvertingFile);
                            BroadcastKeepAlive();
                            Thread.Sleep(1000);
                            CastInfoEvent("Waiting... (3/10)", ActivityType.ConvertingFile);
                            BroadcastKeepAlive();
                            Thread.Sleep(1000);
                            CastInfoEvent("Waiting... (4/10)", ActivityType.ConvertingFile);
                            BroadcastKeepAlive();
                            Thread.Sleep(1000);
                            CastInfoEvent("Waiting... (5/10)", ActivityType.ConvertingFile);
                            BroadcastKeepAlive();
                            Thread.Sleep(1000);
                            CastInfoEvent("Waiting... (6/10)", ActivityType.ConvertingFile);
                            BroadcastKeepAlive();
                            Thread.Sleep(1000);
                            CastInfoEvent("Waiting... (7/10)", ActivityType.ConvertingFile);
                            BroadcastKeepAlive();
                            Thread.Sleep(1000);
                            CastInfoEvent("Waiting... (8/10)", ActivityType.ConvertingFile);
                            BroadcastKeepAlive();
                            Thread.Sleep(1000);
                            CastInfoEvent("Waiting... (9/10)", ActivityType.ConvertingFile);
                            BroadcastKeepAlive();
                            Thread.Sleep(1000);
                            CastInfoEvent("Waiting... (10/10)", ActivityType.ConvertingFile);
                            BroadcastKeepAlive();
                            Thread.Sleep(1000);
                            CastInfoEvent("Getting security access to ECU", ActivityType.ConvertingFile);
                            _securityLevel = AccessLevel.AccessLevel01;
                            if (RequestSecurityAccess(0))
                            {
                                CastInfoEvent("Security access to ECU OK", ActivityType.ConvertingFile);
                                BroadcastRequestDiagnoseID();
                                BroadcastKeepAlive();
                                string vehicleVIN = GetVehicleVIN();
                                CastInfoEvent("Current VIN: " + vehicleVIN, ActivityType.ConvertingFile);
                                BroadcastKeepAlive();
                                CastInfoEvent("Sending F1 F2 F3...", ActivityType.ConvertingFile);
                                Thread.Sleep(1000);
                                DynamicallyDefineLocalIdentifier(0xF1, 0x40);
                                DynamicallyDefineLocalIdentifier(0xF2, 0x41);
                                DynamicallyDefineLocalIdentifier(0xF3, 0x42);
                                BroadcastKeepAlive();
                                CastInfoEvent("Sending 0xAA commands with F1 F2 F3...", ActivityType.ConvertingFile);
                                SendCommandNoResponse(0x7E0, 0x00000000F101AA03);
                                SendCommandNoResponse(0x7E0, 0x00000000F201AA03);
                                SendCommandNoResponse(0x7E0, 0x00000000F301AA03);

                                // set original oil quality indicator
                                if (_oilQualityRead <= 0 || _oilQualityRead > 100) _oilQualityRead = 50; // set to 50% by default
                                SetOilQuality(_oilQualityRead);

                                Thread.Sleep(200);
                                CastInfoEvent("Ending session (1/5)", ActivityType.ConvertingFile);
                                SendDeviceControlMessage(0x16);
                                Thread.Sleep(1000);
                                BroadcastKeepAlive();
                                CastInfoEvent("Ending session (2/5)", ActivityType.ConvertingFile);
                                Broadcast0401();
                                Thread.Sleep(1000);
                                CastInfoEvent("Ending session (3/5)", ActivityType.ConvertingFile);
                                BroadcastKeepAlive();
                                Thread.Sleep(1000);
                                CastInfoEvent("Ending session (4/5)", ActivityType.ConvertingFile);
                                BroadcastKeepAlive();
                                Broadcast0401();
                                BroadcastKeepAlive();
                                Thread.Sleep(1000);
                                CastInfoEvent("Ending session (5/5)", ActivityType.ConvertingFile);
                                return true;
                            }


                        }
                        else
                        {
                            CastInfoEvent("Failed to marry ECU to car", ActivityType.ConvertingFile);
                        }

                    }
                }
                else
                {
                    CastInfoEvent("Failed to program VIN into ECU: " + VINFromCIM, ActivityType.ConvertingFile);
                }

            }



            return false;
        }

        private bool Broadcast0401()
        {
            CANMessage msg = new CANMessage(0x11, 0, 8);
            ulong cmd = 0x0000000000000401;
            msg.setData(cmd);
            // ECU should respond with 0000000000004401
            // CIM responds with 0000000078047F03 and 0000000000004401

            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage ECMresponse = new CANMessage();
            ECMresponse = m_canListener.waitMessage(1000);
            ulong rxdata = ECMresponse.getData();
            m_canListener.setupWaitMessage(0x645);
            int waitMsgCount = 0;
            while (waitMsgCount < 10)
            {

                CANMessage CIMresponse = new CANMessage();
                CIMresponse = m_canListener.waitMessage(1000);
                rxdata = CIMresponse.getData();
                if (getCanData(rxdata, 1) == 0x44)
                {
                    return true;
                }

                else if (getCanData(rxdata, 1) == 0x7F && getCanData(rxdata, 2) == 0x04 && getCanData(rxdata, 3) == 0x78)
                {
                    CastInfoEvent("Waiting for process to finish in CIM", ActivityType.ConvertingFile);
                }
                waitMsgCount++;
            }
            return false;
        }

        private bool MarryCIMAndECU()
        {
            //0000000000633B02
            CANMessage msg = new CANMessage(0x245, 0, 8);
            ulong cmd = 0x0000000000633B02;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x645);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            int waitMsgCount = 0;
            while (waitMsgCount < 10)
            {

                CANMessage ECMresponse = new CANMessage();
                ECMresponse = m_canListener.waitMessage(1000);
                ulong rxdata = ECMresponse.getData();
                // response might be 00000000783B7F03 for some time
                // final result should be 0000000000637B02
                if (getCanData(rxdata, 1) == 0x7B && getCanData(rxdata, 2) == 0x63)
                {
                    return true;
                }

                else if (getCanData(rxdata, 1) == 0x7F && getCanData(rxdata, 2) == 0x3B && getCanData(rxdata, 3) == 0x78)
                {
                    CastInfoEvent("Waiting for marry process to complete between CIM and car", ActivityType.ConvertingFile);
                }
                waitMsgCount++;
            }
            return false;
        }

        /// <summary>
        /// Divorces the ECM from the Car
        /// </summary>
        /// <returns></returns>
        public bool DivorceECM()
        {
            CastInfoEvent("Start divorce process", ActivityType.ConvertingFile);
            BroadcastKeepAlive();
            Thread.Sleep(1000);
            BroadcastRequestDiagnoseID();
            Thread.Sleep(1000);
            string currentVIN = GetVehicleVIN();
            CastInfoEvent("Current VIN " + currentVIN, ActivityType.ConvertingFile);
            Thread.Sleep(1000);
            BroadcastKeepAlive();
            // now, request security access to the CIM
            Send0120(); // start a session
            SendCommandNoResponse(0x7E0, 0x000000006201AA03);
            Thread.Sleep(1000);
            BroadcastKeepAlive();
            SendCommandNoResponse(0x7E0, 0x000000000201AA03);
            if (SendSecretCode1())
            {
                Thread.Sleep(1000);
                BroadcastKeepAlive();
                if (SendSecretCode2())
                {
                    Thread.Sleep(1000);
                    BroadcastKeepAlive();
                    // now write spaces into the VIN in trionic
                    CastInfoEvent("Clearing VIN", ActivityType.ConvertingFile);
                    if (ProgramVIN("                 "))
                    {
                        CastInfoEvent("VIN cleared", ActivityType.ConvertingFile);
                        DynamicallyDefineLocalIdentifier(0xF1, 0x40);
                        DynamicallyDefineLocalIdentifier(0xF2, 0x41);
                        DynamicallyDefineLocalIdentifier(0xF3, 0x42);
                        BroadcastKeepAlive();
                        SendCommandNoResponse(0x7E0, 0x00000000F101AA03);
                        SendCommandNoResponse(0x7E0, 0x00000000F201AA03);
                        SendCommandNoResponse(0x7E0, 0x00000000F301AA03);
                        Thread.Sleep(200);
                        RequestECUInfo(0x29);
                        _oilQualityRead = GetOilQualityPercentage();
                        CastInfoEvent("Oil quality indicator: " + _oilQualityRead.ToString("F2") + " %", ActivityType.ConvertingFile);
                        RequestECUInfo(0x2A);
                        Thread.Sleep(1000);
                        SendDeviceControlMessage(0x16);
                        BroadcastKeepAlive();
                        return true;
                    }
                }
            }
            return false;
        }

        private void SendDeviceControlMessage(byte command)
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 3);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x000000000000AE02;
            ulong lcommand = command;
            cmd |= (lcommand * 0x10000);
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage ECMresponse = new CANMessage();
            ECMresponse = m_canListener.waitMessage(1000);
        }

        private void DynamicallyDefineLocalIdentifier(byte id, byte type)
        {
            //0000004006F12C04
            //0000004106F22C04
            //0000004206F32C04
            CANMessage msg = new CANMessage(0x7E0, 0, 5);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0000000006002C04;
            ulong lid = id;
            ulong ltype = type;
            cmd |= (ltype * 0x100000000);
            cmd |= (lid * 0x10000);
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage ECMresponse = new CANMessage();
            ECMresponse = m_canListener.waitMessage(1000);
            //ulong rxdata = ECMresponse.getData();
        }
        private ulong AddByteToCommand(ulong cmd, byte b2add, int position)
        {
            ulong retval = cmd;
            ulong lbyte = b2add;
            switch (position)
            {
                case 0:
                    retval |= lbyte;
                    break;
                case 1:
                    retval |= lbyte * 0x100;
                    break;
                case 2:
                    retval |= lbyte * 0x10000;
                    break;
                case 3:
                    retval |= lbyte * 0x1000000;
                    break;
                case 4:
                    retval |= lbyte * 0x100000000;
                    break;
                case 5:
                    retval |= lbyte * 0x10000000000;
                    break;
                case 6:
                    retval |= lbyte * 0x1000000000000;
                    break;
                case 7:
                    retval |= lbyte * 0x100000000000000;
                    break;
            }
            return retval;
        }

        public bool ProgramVIN(string VINNumber)
        {
            CANMessage msg = new CANMessage(0x7E0, 0, 8);
            ulong cmd = 0x00000000903B1310;
            if (VINNumber.Length > 17) VINNumber = VINNumber.Substring(0, 17);// lose more than 17 digits
            if (VINNumber.Length < 17) VINNumber = VINNumber.PadRight(17, '0');
            if (VINNumber.Length != 17) return false;

            cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[0]), 4);
            cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[1]), 5);
            cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[2]), 6);
            cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[3]), 7);

            msg.setData(cmd);

            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            
            ulong rxdata = m_canListener.waitMessage(1000).getData();
            if (rxdata == 0x0000000000000030)
            {
                //2020202020202021
                //0020202020202022
                cmd = 0x0000000000000021;
                cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[4]), 1);
                cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[5]), 2);
                cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[6]), 3);
                cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[7]), 4);
                cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[8]), 5);
                cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[9]), 6);
                cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[10]), 7);

                msg.setData(cmd);
                m_canListener.setupWaitMessage(0x7E8);
                canUsbDevice.sendMessage(msg);
                cmd = 0x0000000000000022;
                cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[11]), 1);
                cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[12]), 2);
                cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[13]), 3);
                cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[14]), 4);
                cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[15]), 5);
                cmd = AddByteToCommand(cmd, Convert.ToByte(VINNumber[16]), 6);
                //msg.setLength(7); // only 7 bytes for the last message
                msg.setData(cmd);
                m_canListener.setupWaitMessage(0x7E8);
                canUsbDevice.sendMessage(msg);
                // wait for ack
                //0000000000907B02
                
                rxdata = m_canListener.waitMessage(1000).getData();
                if (getCanData(rxdata, 1) == 0x7B && getCanData(rxdata, 2) == 0x90)
                {
                    return true;
                }

            }
            return false;
        }

        private bool SendSecretCode2()
        {
            //44585349006EAE07
            CANMessage msg = new CANMessage(0x7E0, 0, 8);
            ulong cmd = 0x44585349006EAE07;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage ECMresponse = new CANMessage();
            ECMresponse = m_canListener.waitMessage(1000);
            ulong rxdata = ECMresponse.getData();
            if (getCanData(rxdata, 1) == 0xEE && getCanData(rxdata, 2) == 0x6E)
            {
                return true;
            }
            return false;

        }

        private bool SendSecretCodetoCIM()
        {
            //0044585349603B06
            CANMessage msg = new CANMessage(0x245, 0, 8);
            ulong cmd = 0x0044585349603B06;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x645);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage ECMresponse = new CANMessage();
            ECMresponse = m_canListener.waitMessage(1000);
            ulong rxdata = ECMresponse.getData();
            if (getCanData(rxdata, 1) == 0x7B && getCanData(rxdata, 2) == 0x60)
            {
                return true;
            }
            return false;

        }

        private bool SendSecretCode1()
        {
            //445853490060AE07
            CANMessage msg = new CANMessage(0x7E0, 0, 8);
            ulong cmd = 0x445853490060AE07;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage ECMresponse = new CANMessage();
            ECMresponse = m_canListener.waitMessage(1000);
            ulong rxdata = ECMresponse.getData();
            if (getCanData(rxdata, 1) == 0xEE && getCanData(rxdata, 2) == 0x60)
            {
                return true;
            }
            return false;

        }

        private void SendCommandNoResponse(uint destID, ulong data)
        {
            CANMessage msg = new CANMessage(destID, 0, 8);
            ulong cmd = data;
            msg.setData(cmd);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
        }

        private void BroadcastRequest(byte id)
        {
            ulong lid = id;
            CANMessage msg = new CANMessage(0x11, 0, 8);
            ulong cmd = 0x0000000000000001;
            cmd |= (lid * 0x100);
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage ECMresponse = new CANMessage();
            ECMresponse = m_canListener.waitMessage(1000);
            m_canListener.setupWaitMessage(0x645);
            CANMessage CIMresponse = new CANMessage();
            CIMresponse = m_canListener.waitMessage(1000);
        }

        private void BroadcastRequestDiagnoseID()
        {
            //0101 000000009A1A02FE	
            CANMessage msg = new CANMessage(0x11, 0, 8);
            ulong cmd = 0x00000000009A1A02;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage ECMresponse = new CANMessage();
            ECMresponse = m_canListener.waitMessage(1000);
            m_canListener.setupWaitMessage(0x645);
            CANMessage CIMresponse = new CANMessage();
            CIMresponse = m_canListener.waitMessage(1000);
            // wait for response of CIM and ECU
        }

        private void BroadcastKeepAlive()
        {
            //0101 00000000003E01FE
            CANMessage msg = new CANMessage(0x11, 0, 2);
            ulong cmd = 0x0000000000003E01;
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x311);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage ECMresponse = new CANMessage();
            ECMresponse = m_canListener.waitMessage(1000);
            //07E8 00000084019A5A04
            //0645 00000008039A5A04
        }

        /// <summary>
        /// Sets the speed limit in a T8 ECU
        /// </summary>
        /// <param name="speedlimit">speed limit in km/h</param>
        /// <returns></returns>
        public bool SetSpeedLimiter(int speedlimit)
        {
            bool retval = false;
            // writeDataByIdentifier
            //0000008C0A023B04
            speedlimit *= 10;
            CANMessage msg = new CANMessage(0x7E0, 0, 5);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0000000000023B04; //0000008C0A023B04 example  0A8C = 2700
            byte b1 = Convert.ToByte(speedlimit / 256);
            byte b2 = Convert.ToByte(speedlimit - (int)b1 * 256);
            cmd = AddByteToCommand(cmd, b1, 3);
            cmd = AddByteToCommand(cmd, b2, 4);
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage ECMresponse = new CANMessage();
            ECMresponse = m_canListener.waitMessage(1000);
            ulong rxdata = ECMresponse.getData();
            // response should be 0000000000027B02
            if (getCanData(rxdata, 1) == 0x7B && getCanData(rxdata, 2) == 0x02)
            {
                retval = true;
            }
            // Negative Response 0x7F Service <nrsi> <service> <returncode>
            // Bug: this is never handled because its sent with id=0x7E8
            else if (getCanData(rxdata, 1) == 0x7F && getCanData(rxdata, 2) == 0x3B)
            {
                string info = TranslateErrorCode(getCanData(rxdata, 3));
                CastInfoEvent("Error: " + info, ActivityType.ConvertingFile);
            }
            return retval;
        }

        /// <summary>
        /// Sets the Oil quality indication (used for service interval calculation)
        /// </summary>
        /// <param name="percentage"></param>
        /// <returns></returns>
        public bool SetOilQuality(float percentage)
        {
            bool retval = false;
            // writeDataByIdentifier
            //000D340000253B06
            percentage *= 256;
            int iper = Convert.ToInt32(percentage);
            CANMessage msg = new CANMessage(0x7E0, 0, 7);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0000000000253B06; //000D340000253B06 example  0000340D = 52,05078125 percent
            byte b1 = Convert.ToByte(iper / 256);
            byte b2 = Convert.ToByte(iper - (int)b1 * 256);
            cmd = AddByteToCommand(cmd, b1, 5);
            cmd = AddByteToCommand(cmd, b2, 6);
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage ECMresponse = new CANMessage();
            ECMresponse = m_canListener.waitMessage(1000);
            ulong rxdata = ECMresponse.getData();
            // response should be 0000000000027B02
            if (getCanData(rxdata, 1) == 0x7B && getCanData(rxdata, 2) == 0x25)
            {
                retval = true;
            }
            return retval;
        }

        /// <summary>
        /// Sets the RPM limit in a T8 ECU
        /// </summary>
        /// <param name="rpmlimit"></param>
        /// <returns></returns>
        public bool SetRPMLimiter(int rpmlimit)
        {
            bool retval = false;
            //0000000618293B04
            CANMessage msg = new CANMessage(0x7E0, 0, 5);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x0000000000293B04; //0000000618293B04 example  1806 = 6150
            byte b1 = Convert.ToByte(rpmlimit / 256);
            byte b2 = Convert.ToByte(rpmlimit - (int)b1 * 256);
            cmd = AddByteToCommand(cmd, b1, 3);
            cmd = AddByteToCommand(cmd, b2, 4);
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage ECMresponse = new CANMessage();
            ECMresponse = m_canListener.waitMessage(1000);
            ulong rxdata = ECMresponse.getData();
            // response should be 0000000000027B02
            if (getCanData(rxdata, 1) == 0x7B && getCanData(rxdata, 2) == 0x29)
            {
                retval = true;
            }
            return retval;
        }

        public void setECUparameterVIN(string vin)
        {
            // 62 DPID + 01 sendOneResponse + $AA ReadDataByPacketIdentifier
            CANMessage msg62 = new CANMessage(0x7E0, 0, 4); //<GS-18052011> ELM327 support requires the length byte
            msg62.setData(0x000000006201AA03);
            m_canListener.setupWaitMessage(0x5E8);
            CastInfoEvent("Wait for response 5E8 62 00 00", ActivityType.ConvertingFile);
            if (!canUsbDevice.sendMessage(msg62))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response62 = new CANMessage();
            response62 = m_canListener.waitMessage(1000);
            Console.WriteLine("---" + response62.getData().ToString("X16"));
            //05E8	62	00	00	02	A7	01	7F	01
            if (response62.getCanData(0) == 0x62)
            {
                if (response62.getCanData(1) == 0x00)
                {
                    if (response62.getCanData(2) == 0x00)
                    {
                        CastInfoEvent("Got response 5E8 62 00 00", ActivityType.ConvertingFile);
                    }
                }
            }

            if (GetManufacturersEnableCounter() == 0x00)
                CastInfoEvent("GetManufacturersEnableCounter == 0x00", ActivityType.ConvertingFile);

            CastInfoEvent("ECM EOL Parameter Settings-part1", ActivityType.ConvertingFile);
            // 02 DPID + 01 sendOneResponse + $AA ReadDataByPacketIdentifier
            CANMessage msg = new CANMessage(0x7E0, 0, 4); //<GS-18052011> ELM327 support requires the length byte
            msg.setData(0x000000000201AA03);
            m_canListener.setupWaitMessage(0x5E8);
            CastInfoEvent("Wait for response 5E8 02 02", ActivityType.ConvertingFile);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage response = new CANMessage();
            response = m_canListener.waitMessage(1000);
            Console.WriteLine("---" + response.getData().ToString("X16"));
            //05E8	02	02	A0	42	80	A0	00	00
            if (response.getCanData(0) == 0x02)
            {
                if (response.getCanData(1) == 0x02)
                {
                    CastInfoEvent("Got response 5E8 02 02", ActivityType.ConvertingFile);
                }
            }

            if (ProgramVIN(vin))
                CastInfoEvent("ProgramVIN true", ActivityType.ConvertingFile);

            Thread.Sleep(200);

            //RequestSecurityAccess(2000);
            //CastInfoEvent("End programming?", ActivityType.ConvertingFile);
            //SendCommandNoResponse(0x7E0, 0x0000000000012702); // 01 SPSrequestSeed + $27 SecurityAccess

            //CastInfoEvent("Unidentified, security access again?", ActivityType.ConvertingFile);
            //SendCommandNoResponse(0x7E0, 0x000000EFA4022704); // EFA4 securitykey + 02 SPSsendKey + $27 SecurityAccess

            //SendCommandNoResponse(0x7E0, 0x0000000000901A02);
            string newVIN = GetVehicleVIN();
            CastInfoEvent("New VIN: " + newVIN, ActivityType.ConvertingFile);

        }

        public bool setECUparameterE85(float percentage)
        {
            bool retval = false;
            percentage *= 256;
            int iper = Convert.ToInt32(percentage);
            CANMessage msg = new CANMessage(0x7E0, 0, 5);//<GS-18052011> ELM327 support requires the length byte
            ulong cmd = 0x000000000018AE04; // <ControlByte 5-1> <CPID Number 0x18> <Device Control 0xAE service>
            byte b1 = Convert.ToByte(iper / 256);
            cmd = AddByteToCommand(cmd, b1, 4);
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x7E8);
            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }
            CANMessage ECMresponse = new CANMessage();
            ECMresponse = m_canListener.waitMessage(1000);
            ulong rxdata = ECMresponse.getData();
            // response should be 000000000018EE02
            if (getCanData(rxdata, 1) == 0xEE && getCanData(rxdata, 2) == 0x18) // <EE positive response service id> <cpid>
            {
                retval = true;
            }
            // Negative Response 0x7F Service <nrsi> <service> <returncode>
            // Bug: this is never handled because negative response its sent with id=0x7E8
            else if (getCanData(rxdata, 1) == 0x7F && getCanData(rxdata, 2) == 0xAE)
            {
                string info = TranslateErrorCode(getCanData(rxdata, 3));
                CastInfoEvent("Error: " + info, ActivityType.ConvertingFile);
            }
            return retval;
        }

        public class CanInfoEventArgs : System.EventArgs
        {
            private ActivityType _type;

            public ActivityType Type
            {
                get { return _type; }
                set { _type = value; }
            }

            private string _info;

            public string Info
            {
                get { return _info; }
                set { _info = value; }
            }

            public CanInfoEventArgs(string info, ActivityType type)
            {
                this._info = info;
                this._type = type;
            }
        }

        public class WriteProgressEventArgs : System.EventArgs
        {
            private float _percentage;

            private int _bytestowrite;

            public int Bytestowrite
            {
                get { return _bytestowrite; }
                set { _bytestowrite = value; }
            }

            private int _byteswritten;

            public int Byteswritten
            {
                get { return _byteswritten; }
                set { _byteswritten = value; }
            }

            public float Percentage
            {
                get { return _percentage; }
                set { _percentage = value; }
            }

            public WriteProgressEventArgs(float percentage)
            {
                this._percentage = percentage;
            }

            public WriteProgressEventArgs(float percentage, int bytestowrite, int byteswritten)
            {
                this._bytestowrite = bytestowrite;
                this._byteswritten = byteswritten;
                this._percentage = percentage;
            }
        }

        public class ReadProgressEventArgs : System.EventArgs
        {
            private float _percentage;

            public float Percentage
            {
                get { return _percentage; }
                set { _percentage = value; }
            }

            public ReadProgressEventArgs(float percentage)
            {
                this._percentage = percentage;
            }
        }

        public void GetVehicleVINfromT7()
        {
            string vin;
            string immo;
            string engineType;
            string swVersion;
            float e85level;

            if (m_EnableCanLog)
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

        public void getFlashWithT7Flasher(string a_fileName)
        {
            if (m_EnableCanLog)
            {
                KWPHandler.startLogging();
            }
            if (CheckStatusT7Flasher())
            {
                CastInfoEvent("Starting download of flash", ActivityType.ConvertingFile);
                tmrReadProcessChecker.Enabled = true;
                flash.readFlash(a_fileName);
            }
        }

        public void UpdateFlashWithT7Flasher(string a_fileName)
        {
            if (m_EnableCanLog)
            {
                KWPHandler.startLogging();
            }
            if (!tmrReadProcessChecker.Enabled)
            {
                // check reading status periodically
                AddToCanTrace("Starting flash procedure, checking flashing process status");
                if (CheckStatusT7Flasher())
                {
                    tmrWriteProcessChecker.Enabled = true;
                    CastInfoEvent("Flashing: " + a_fileName, ActivityType.ConvertingFile);
                    AddToCanTrace("Calling flash.writeFlash with filename: " + a_fileName);
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
                    CastInfoEvent("Finished download of flash", ActivityType.FinishedDownloadingFlash);
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
                        AddToCanTrace("tmrWriteProcessChecker_Tick: Completed flashing procedure");
                        break;
                    case T7Flasher.FlashStatus.DoinNuthin:
                        AddToCanTrace("tmrWriteProcessChecker_Tick: DoinNuthin");
                        break;
                    case T7Flasher.FlashStatus.EraseError:
                        AddToCanTrace("tmrWriteProcessChecker_Tick: EraseError");
                        break;
                    case T7Flasher.FlashStatus.Eraseing:
                        AddToCanTrace("tmrWriteProcessChecker_Tick: Eraseing");
                        break;
                    case T7Flasher.FlashStatus.NoSequrityAccess:
                        AddToCanTrace("tmrWriteProcessChecker_Tick: NoSequrityAccess");
                        break;
                    case T7Flasher.FlashStatus.NoSuchFile:
                        AddToCanTrace("tmrWriteProcessChecker_Tick: NoSuchFile");
                        break;
                    case T7Flasher.FlashStatus.ReadError:
                        AddToCanTrace("tmrWriteProcessChecker_Tick: ReadError");
                        break;
                    case T7Flasher.FlashStatus.Reading:
                        break;
                    case T7Flasher.FlashStatus.WriteError:
                        AddToCanTrace("tmrWriteProcessChecker_Tick: WriteError");
                        break;
                    case T7Flasher.FlashStatus.Writing:
                        break;
                    default:
                        AddToCanTrace("tmrWriteProcessChecker_Tick: " + stat);
                        break;
                }

                if (stat == T7Flasher.FlashStatus.Completed)
                {
                    flash.stopFlasher();
                    tmrWriteProcessChecker.Enabled = false;
                    CastInfoEvent("Finished flash session", ActivityType.FinishedFlashing);
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
                    CastInfoEvent("A write error occured, please retry to flash without cutting power to the ECU", ActivityType.FinishedFlashing);
                }
            }
        }

        public string GetE85AdaptionStatusFromT7()
        {
            string status;
            KWPHandler.getInstance().getE85AdaptionStatus(out status);
            return status;
        }

        public bool ForceE85AdaptionT7()
        {
            return KWPHandler.getInstance().forceE85Adaption() == KWPResult.OK;
        }

        public bool SetE85LevelT7(int level)
        {
            return KWPHandler.getInstance().setE85Level(level) == KWPResult.OK;
        }

        public float GetE85LevelT7()
        {
            float level;
            KWPHandler.getInstance().getE85Level(out level);
            return level;
        }

        public bool ResetT7()
        {
            return KWPHandler.getInstance().ResetECU();
        }

        /// <summary>
        /// Sets the ELM filters to show all messages
        /// </summary>
        /// <returns></returns>
        private void SetELMFilters()
        {
            canUsbDevice.SetupCANFilter("7E8", "000");
        }


    }
}
