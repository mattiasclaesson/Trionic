using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.IO.Ports;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;
using TrionicCANLib;
using TrionicCANLib.API;
using TrionicCANLib.Firmware;
using TrionicCANLib.Checksum;
using System.Drawing;
using NLog;
using CommonSuite;

namespace TrionicCANFlasher
{
    public delegate void DelegateUpdateStatus(ITrionic.CanInfoEventArgs e);
    public delegate void DelegateProgressStatus(int percentage);

    public partial class frmMain : Form
    {
        readonly Trionic8 trionic8 = new Trionic8();
        readonly Trionic7 trionic7 = new Trionic7();
        readonly Trionic5 trionic5 = new Trionic5();
        DateTime dtstart;
        public DelegateUpdateStatus m_DelegateUpdateStatus;
        public DelegateProgressStatus m_DelegateProgressStatus;
        public ChecksumDelegate.ChecksumUpdate m_ShouldUpdateChecksum;
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        msiupdater m_msiUpdater;
        BackgroundWorker bgworkerLogCanData;

        public frmMain()
        {
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            InitializeComponent();
            m_DelegateUpdateStatus = updateStatusInBox;
            m_DelegateProgressStatus = updateProgress;
            m_ShouldUpdateChecksum = updateChecksum;
            SetupListboxWrapping();
            EnableUserInput(true);
        }

        private void SetupListboxWrapping()
        {
            listBoxLog.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            listBoxLog.MeasureItem += lst_MeasureItem;
            listBoxLog.DrawItem += lst_DrawItem;
        }

        private void lst_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = (int)e.Graphics.MeasureString(listBoxLog.Items[e.Index].ToString(), listBoxLog.Font, listBoxLog.Width).Height;
        }

        private void lst_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            e.DrawFocusRectangle();
            if(e.Index>=0)
                e.Graphics.DrawString(listBoxLog.Items[e.Index].ToString(), e.Font, new SolidBrush(e.ForeColor), e.Bounds);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Trace(e.ToString());
        }

        void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            logger.Trace(e.ToString());
        }

        private void AddLogItem(string item)
        {
            var uiItem = DateTime.Now.ToString("HH:mm:ss.fff") + " - " + item;
            listBoxLog.Items.Add(uiItem);
            while (listBoxLog.Items.Count > 100) listBoxLog.Items.RemoveAt(0);
            listBoxLog.SelectedIndex = listBoxLog.Items.Count - 1;
            logger.Trace(item);
            Application.DoEvents();
        }

        private void btnFlashEcu_Click(object sender, EventArgs e)
        {
            DialogResult result = DialogResult.Cancel;
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8 || cbxEcuType.SelectedIndex == (int)ECU.MOTRONIC96 ||
                cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8_MCP ||
                cbxEcuType.SelectedIndex == (int)ECU.Z22SEMain_LEG || cbxEcuType.SelectedIndex == (int)ECU.Z22SEMCP_LEG)
                result = MessageBox.Show("Attach a charger. Now turn key to ON to wakeup ECU.",
                "Critical Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                result = MessageBox.Show("Attach a charger. Turn key to ON wait a few seconds, turn to LOCK. Wait 15 to 20 seconds then initiate the flash operation in car.",
                "Critical Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
            }
            // Trionic 5 is complex. Skip dialog until a sutitable one has been written
            if (result == DialogResult.Cancel && cbxEcuType.SelectedIndex != (int)ECU.TRIONIC5)
            {
                return;
            }

            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Bin files|*.bin", Multiselect = false })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (checkFileSize(ofd.FileName))
                    {
                        if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC5)
                        {
                            ChecksumResult checksumResult = ChecksumT5.VerifyChecksum(ofd.FileName, cbAutoChecksum.Checked, m_ShouldUpdateChecksum);
                            if (checksumResult != ChecksumResult.Ok)
                            {
                                AddLogItem("Checksum check failed: " + checksumResult);
                                return;
                            }

                            SetGenericOptions(trionic5);
                            AddLogItem("Opening connection");
                            EnableUserInput(false);
                            if (trionic5.openDevice())
                            {
                                Thread.Sleep(1000);
                                AddLogItem("Update FLASH content");
                                Application.DoEvents();
                                dtstart = DateTime.Now;
                                trionic5.WriteFlash(ofd.FileName);
                                trionic5.Cleanup();
                                EnableUserInput(true);
                            }
                            else
                            {
                                AddLogItem("Unable to connect to Trionic 5 ECU");
                                trionic5.Cleanup();
                                EnableUserInput(true);
                                AddLogItem("Connection terminated");
                            }
                        }
                        else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
                        {
                            ChecksumResult checksumResult = ChecksumT7.VerifyChecksum(ofd.FileName, cbAutoChecksum.Checked, ChecksumT7.DO_NOT_AUTOFIXFOOTER, m_ShouldUpdateChecksum); // TODO: mattias, add AutoFixFooter to settings?
                            if (checksumResult != ChecksumResult.Ok)
                            {
                                AddLogItem("Checksum check failed: " + checksumResult);
                                return;
                            }

                            SetGenericOptions(trionic7);
                            trionic7.UseFlasherOnDevice = cbOnlyPBus.Checked ? cbUseFlasherOnDevice.Checked : false;

                            AddLogItem("Opening connection");
                            EnableUserInput(false);
                            if (trionic7.openDevice())
                            {
                                Thread.Sleep(1000);
                                AddLogItem("Update FLASH content");
                                Application.DoEvents();
                                dtstart = DateTime.Now;
                                trionic7.WriteFlash(ofd.FileName);
                            }
                            else
                            {
                                AddLogItem("Unable to connect to Trionic 7 ECU");
                                trionic7.Cleanup();
                                EnableUserInput(true);
                                AddLogItem("Connection terminated");
                            }
                        }
                        else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
                        {
                            ChecksumResult checksumResult = ChecksumT8.VerifyChecksum(ofd.FileName, cbAutoChecksum.Checked, m_ShouldUpdateChecksum);
                            if (checksumResult != ChecksumResult.Ok)
                            {
                                AddLogItem("Checksum check failed: " + checksumResult);
                                return;
                            }

                            SetGenericOptions(trionic8);

                            EnableUserInput(false);
                            AddLogItem("Opening connection");
                            trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                            trionic8.FormatBootPartition = cbFormatBootPartition.Checked;
                            trionic8.FormatSystemPartitions = cbFormatSystemPartitions.Checked;
                            if (trionic8.openDevice(false))
                            {
                                Thread.Sleep(1000);
                                dtstart = DateTime.Now;
                                AddLogItem("Update FLASH content");
                                Application.DoEvents();
                                BackgroundWorker bgWorker;
                                bgWorker = new BackgroundWorker();
                                if (cbUseLegionBootloader.Checked)
                                {
                                    bgWorker.DoWork += new DoWorkEventHandler(trionic8.WriteFlashLegT8);
                                }
                                else
                                {
                                    bgWorker.DoWork += new DoWorkEventHandler(trionic8.WriteFlash);
                                }
                                bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                                bgWorker.RunWorkerAsync(ofd.FileName);
                            }
                            else
                            {
                                AddLogItem("Unable to connect to Trionic 8 ECU");
                                trionic8.Cleanup();
                                EnableUserInput(true);
                                AddLogItem("Connection terminated");
                            }
                        }
                        else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8_MCP)
                        {
                            SetGenericOptions(trionic8);

                            EnableUserInput(false);
                            AddLogItem("Opening connection");
                            trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                            
                            trionic8.FormatSystemPartitions = true; // This is undefined in mcp.
                            trionic8.FormatBootPartition    = cbFormatBootPartition.Checked;

                            if (trionic8.openDevice(false))
                            {
                                Thread.Sleep(1000);
                                dtstart = DateTime.Now;
                                AddLogItem("Update FLASH content");
                                Application.DoEvents();
                                BackgroundWorker bgWorker;
                                bgWorker = new BackgroundWorker();
                                bgWorker.DoWork += new DoWorkEventHandler(trionic8.WriteFlashLegMCP);
                                bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                                bgWorker.RunWorkerAsync(ofd.FileName);
                            }
                            else
                            {
                                AddLogItem("Unable to connect to Trionic 8 ECU");
                                trionic8.Cleanup();
                                EnableUserInput(true);
                                AddLogItem("Connection terminated");
                            }
                        }
                        else if (cbxEcuType.SelectedIndex == (int)ECU.Z22SEMain_LEG)
                        {
                            SetGenericOptions(trionic8);

                            EnableUserInput(false);
                            AddLogItem("Opening connection");
                            trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                            
                            // Do _NOT_ make these an option. It works in a completely different way than trionic 8..
                            trionic8.FormatSystemPartitions = true;
                            trionic8.FormatBootPartition    = true;

                            if (trionic8.openDevice(false))
                            {
                                Thread.Sleep(1000);
                                dtstart = DateTime.Now;
                                AddLogItem("Update FLASH content");
                                Application.DoEvents();
                                BackgroundWorker bgWorker;
                                bgWorker = new BackgroundWorker();
                                bgWorker.DoWork += new DoWorkEventHandler(trionic8.WriteFlashLegZ22SE_Main);
                                bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                                bgWorker.RunWorkerAsync(ofd.FileName);
                            }
                            else
                            {
                                AddLogItem("Unable to connect to Z22SE ECU");
                                trionic8.Cleanup();
                                EnableUserInput(true);
                                AddLogItem("Connection terminated");
                            }
                        }
                        else if (cbxEcuType.SelectedIndex == (int)ECU.Z22SEMCP_LEG)
                        {
                            SetGenericOptions(trionic8);

                            EnableUserInput(false);
                            AddLogItem("Opening connection");
                            trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                            
                            trionic8.FormatSystemPartitions = true; // This is undefined in mcp.
                            trionic8.FormatBootPartition    = cbFormatBootPartition.Checked;

                            if (trionic8.openDevice(false))
                            {
                                Thread.Sleep(1000);
                                dtstart = DateTime.Now;
                                AddLogItem("Update FLASH content");
                                Application.DoEvents();
                                BackgroundWorker bgWorker;
                                bgWorker = new BackgroundWorker();
                                bgWorker.DoWork += new DoWorkEventHandler(trionic8.WriteFlashLegZ22SE_MCP);
                                bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                                bgWorker.RunWorkerAsync(ofd.FileName);
                            }
                            else
                            {
                                AddLogItem("Unable to connect to Z22SE ECU");
                                trionic8.Cleanup();
                                EnableUserInput(true);
                                AddLogItem("Connection terminated");
                            }
                        }
                        else if (cbxEcuType.SelectedIndex == (int)ECU.MOTRONIC96)
                        {
                            SetGenericOptions(trionic8);

                            EnableUserInput(false);
                            AddLogItem("Opening connection");
                            trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                            if (trionic8.openDevice(false))
                            {
                                string ecuCalibrationset = trionic8.GetCalibrationSet();
                                ecuCalibrationset = SubString8(ecuCalibrationset);
                                if (ecuCalibrationset == "")
                                {
                                    AddLogItem("ECU connection issue, check logs");
                                }
                                else
                                {
                                    string ecuMainOS = trionic8.RequestECUInfo(0xC1, "");
                                    ecuMainOS = SubString8(ecuMainOS);
                                    string ecuEngineCalib = trionic8.RequestECUInfo(0xC2, "");
                                    ecuEngineCalib = SubString8(ecuEngineCalib);
                                    string ecuSystemCalib = trionic8.RequestECUInfo(0xC3, "");
                                    ecuSystemCalib = SubString8(ecuSystemCalib);
                                    string ecuSpeedoCalib = trionic8.RequestECUInfo(0xC4, "");
                                    ecuSpeedoCalib = SubString8(ecuSpeedoCalib);
                                    string ecuSlaveOS = trionic8.RequestECUInfo(0xC5, "");
                                    ecuSlaveOS = SubString8(ecuSlaveOS);

                                    bool flash = true;
                                    int flashStart = (int)FileME96.EngineCalibrationAddress;
                                    int flashEnd = (int)FileME96.EngineCalibrationAddressEnd;

                                    string fileMainOS = FileME96.getMainOSVersion(ofd.FileName);
                                    AddLogItem("Main OS version in file: " + fileMainOS);
                                    if (fileMainOS != string.Empty)
                                    {
                                        AddLogItem("Main OS version in ECU: " + ecuMainOS);
                                        if (ecuMainOS != fileMainOS)
                                        {
                                            AddLogItem("Main OS version differs between file and ECU");
                                            if (cbFormatSystemPartitions.Checked)
                                            {
                                                AddLogItem("User has selected option format system partitions");
                                                FileInfo fi = new FileInfo(ofd.FileName);
                                                flashStart = (int)FileME96.MainOSAddress;
                                                flashEnd = (int)fi.Length;
                                            }
                                            else
                                            {
                                                AddLogItem("Aborted flash, format system partitions is unchecked");
                                                flash = false;
                                            }
                                        }
                                        else
                                        {
                                            flash = FlashEngineCalibration(ofd.FileName, ecuEngineCalib);
                                        }
                                    }
                                    else
                                    {
                                        // Or just force the user to read the complete ecu instead.

                                        // Check that the basefile version is matched with beginning of calibrationset
                                        string basefileInfo = FileME96.getFileInfo(ofd.FileName);
                                        if (!basefileInfo.Contains(ecuCalibrationset.Substring(0, 4)))
                                        {
                                            AddLogItem("Basefile and file to write is not compatible " + basefileInfo + " and " + ecuCalibrationset);
                                            flash = false;
                                        }
                                        else
                                        {
                                            flash = FlashEngineCalibration(ofd.FileName, ecuEngineCalib);
                                        }
                                    }

                                    if (flash)
                                    {
                                        AddLogItem("Flash addresses start:" + flashStart.ToString("X") + " and end: " + flashEnd.ToString("X"));
                                        Thread.Sleep(1000);
                                        dtstart = DateTime.Now;
                                        AddLogItem("Update FLASH content");
                                        Application.DoEvents();
                                        FlashReadArguments args = new FlashReadArguments() { FileName = ofd.FileName, start = flashStart, end = flashEnd };
                                        BackgroundWorker bgWorker;
                                        bgWorker = new BackgroundWorker();
                                        bgWorker.DoWork += new DoWorkEventHandler(trionic8.WriteFlashME96);
                                        bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                                        bgWorker.RunWorkerAsync(args);
                                    }
                                    else
                                    {
                                        AddLogItem("Flash operation aborted");
                                        trionic8.Cleanup();
                                        EnableUserInput(true);
                                        AddLogItem("Connection terminated");
                                    }
                                }
                            }
                            else
                            {
                                AddLogItem("Unable to connect to ME9.6 ECU");
                                trionic8.Cleanup();
                                EnableUserInput(true);
                                AddLogItem("Connection terminated");
                            }
                        }
                    }
                }
            }
            LogManager.Flush();
        }

        private bool FlashEngineCalibration(string fileName, string ecuEngineCalib)
        {
            bool flash = true;

            string fileEngineCalib = FileME96.getEngineCalibrationVersion(fileName);
            AddLogItem("Engine Calibration version in file: " + fileEngineCalib);
            if (fileEngineCalib != string.Empty)
            {
                AddLogItem("Engine Calibration version in ECU: " + ecuEngineCalib);
                if (ecuEngineCalib != fileEngineCalib)
                {
                    AddLogItem("Aborted flash, Engine Calibration version differs between file and ECU");
                    flash = false;
                }
                else
                {
                    // Read the ecu here and compare with file. 
                    // So we know if there is any point in writing?

                    DialogResult ask = MessageBox.Show("Do you want to overwrite calibration?",
                        "Calibration write", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                    if (ask == DialogResult.No)
                    {
                        flash = false;
                    }
                }
            }
            return flash;
        }

        private static string SubString8(string value)
        {
            return value.Length < 8 ? value : value.Substring(0, 8);
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                AddLogItem("Stopped");
            }
            else if (e.Result != null && (bool)e.Result)
            {
                AddLogItem("Operation done");
            }
            else
            {
                AddLogItem("Operation failed");
            }
         
            TimeSpan ts = DateTime.Now - dtstart;
            AddLogItem("Total duration: " + ts.Minutes + " minutes " + ts.Seconds + " seconds");
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC5)
            {
                trionic5.Cleanup();
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                trionic7.Cleanup();
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8      ||
                     cbxEcuType.SelectedIndex == (int)ECU.MOTRONIC96    ||
                     cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8_MCP  ||
                     cbxEcuType.SelectedIndex == (int)ECU.Z22SEMain_LEG ||
                     cbxEcuType.SelectedIndex == (int)ECU.Z22SEMCP_LEG)
            {
                trionic8.Cleanup();
            }
            EnableUserInput(true);
            AddLogItem("Connection terminated");
        }

        bool checkFileSize(string fileName)
        {
            FileInfo fi = new FileInfo(fileName);
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC5)
            {
                if (fi.Length != FileT5.LengthT52 && fi.Length != FileT5.LengthT55)
                {
                    AddLogItem("Not a trionic 5 file");
                    return false;
                }
            }
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                if (fi.Length != FileT7.Length)
                {
                    AddLogItem("Not a trionic 7 file");
                    return false;
                }
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8 || cbxEcuType.SelectedIndex == (int)ECU.Z22SEMain_LEG)
            {
                if (fi.Length != FileT8.Length)
                {
                    AddLogItem("Not a trionic 8 file");
                    return false;
                }
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.MOTRONIC96)
            {
                if (fi.Length != FileME96.Length && fi.Length != FileME96.LengthComplete)
                {
                    AddLogItem("Not a Motronic ME9.6 file");
                    return false;
                }
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8_MCP || cbxEcuType.SelectedIndex == (int)ECU.Z22SEMCP_LEG)
            {
                if (fi.Length != FileT8mcp.Length)
                {
                    AddLogItem("Not a trionic 8 mcp file");
                    return false;
                }
            }
            return true;
        }

        private void EnableUserInput(bool enable)
        {
            btnFlashECU.Enabled = enable;
            btnReadECU.Enabled = enable;
            btnGetECUInfo.Enabled = enable;
            btnReadSRAM.Enabled = enable;
            btnRecoverECU.Enabled = enable;
            btnReadDTC.Enabled = enable;
            cbxAdapterType.Enabled = enable;
            cbxEcuType.Enabled = enable;
            cbEnableLogging.Enabled = enable;
            cbOnlyPBus.Enabled = enable;
            cbUseLegionBootloader.Enabled = enable;
            cbFormatBootPartition.Enabled = enable;
            cbFormatSystemPartitions.Enabled = enable;
            btnEditParameters.Enabled = enable;
            btnReadECUcalibration.Enabled = enable;
            btnRestoreT8.Enabled = enable;
            btnLogData.Enabled = enable;
            cbAutoChecksum.Enabled = enable;

            if (cbxAdapterType.SelectedIndex == (int)CANBusAdapter.ELM327 ||
                cbxAdapterType.SelectedIndex == (int)CANBusAdapter.KVASER ||
                cbxAdapterType.SelectedIndex == (int)CANBusAdapter.LAWICEL ||
                cbxAdapterType.SelectedIndex == (int)CANBusAdapter.J2534)
            {
                cbAdapter.Enabled = enable;
            }
            else
            {
                cbAdapter.Enabled = false;
            }

            if (cbxAdapterType.SelectedIndex == (int)CANBusAdapter.ELM327)
            {
                cbxComSpeed.Enabled = enable;
            }
            else
            {
                cbxComSpeed.Enabled = false;
            }

            if (cbxAdapterType.SelectedIndex == (int)CANBusAdapter.COMBI &&
                cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                cbUseFlasherOnDevice.Enabled = enable;
            }
            else
            {
                cbUseFlasherOnDevice.Enabled = false;
            }
            // Always disable
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC5)
            {
                // Disable Legion features
                cbUseLegionBootloader.Enabled = 
                cbFormatSystemPartitions.Enabled =
                cbFormatBootPartition.Enabled = false;

                cbOnlyPBus.Enabled            = false;
                btnReadECUcalibration.Enabled = false;
                btnReadDTC.Enabled            = false;
                btnEditParameters.Enabled     = false;
                btnRecoverECU.Enabled         = false;
                btnRestoreT8.Enabled          = false;
            }
            // Always disable
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                btnRecoverECU.Enabled            = false;
                btnReadECUcalibration.Enabled    = false;
                btnRestoreT8.Enabled             = false;
                cbUseLegionBootloader.Enabled    = false;
                cbFormatBootPartition.Enabled    = false;
                cbFormatSystemPartitions.Enabled = false;
            }

            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {
                // Always disable
                btnReadECUcalibration.Enabled = false;

                // These are handled differently than the rest; Only enable if "enable" and their corresponding dependency is set
                cbFormatSystemPartitions.Enabled = enable ? cbUseLegionBootloader.Checked : false;
                cbFormatBootPartition.Enabled    = enable ? cbFormatSystemPartitions.Checked : false;
            }

            // Always disable
            if (cbxEcuType.SelectedIndex == (int)ECU.MOTRONIC96)
            {
                btnReadSRAM.Enabled = false;
                btnRestoreT8.Enabled = false;
                btnRecoverECU.Enabled = false;
                cbUseLegionBootloader.Enabled = false;
                cbFormatBootPartition.Enabled = false;
                cbAutoChecksum.Enabled = false;
            }
            
            // Always disable
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8_MCP || cbxEcuType.SelectedIndex == (int)ECU.Z22SEMain_LEG || cbxEcuType.SelectedIndex == (int)ECU.Z22SEMCP_LEG)
            {
                btnReadDTC.Enabled = false;
                btnReadECUcalibration.Enabled = false;
                // Bootloader handles recovery, if at all possible, on MCP.
                btnRecoverECU.Enabled = false;

                if (cbxEcuType.SelectedIndex == (int)ECU.Z22SEMain_LEG)
                {
                    cbFormatBootPartition.Enabled = false;
                    cbFormatSystemPartitions.Enabled = false;
                }

                if (cbxEcuType.SelectedIndex == (int)ECU.Z22SEMain_LEG || cbxEcuType.SelectedIndex == (int)ECU.Z22SEMCP_LEG)
                {
                    cbUseLegionBootloader.Enabled = false;
                }

                btnRestoreT8.Enabled = false;
                btnReadSRAM.Enabled = false;

                btnGetECUInfo.Enabled = false;
                btnEditParameters.Enabled = false;
                cbAutoChecksum.Enabled = false;
            }
        }

        private void btnReadECU_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "Bin files|*.bin" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    if (sfd.FileName != string.Empty)
                    {
                        if (Path.GetFileName(sfd.FileName) != string.Empty)
                        {
                            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC5)
                            {
                                SetGenericOptions(trionic5);

                                AddLogItem("Opening connection");
                                EnableUserInput(false);

                                if (trionic5.openDevice())
                                {
                                    Thread.Sleep(1000);
                                    dtstart = DateTime.Now;
                                    AddLogItem("Acquiring FLASH content");
                                    Application.DoEvents();
                                    BackgroundWorker bgWorker;
                                    bgWorker = new BackgroundWorker();

                                    bgWorker.DoWork += new DoWorkEventHandler(trionic5.DumpECU);

                                    bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                                    bgWorker.RunWorkerAsync(sfd.FileName);
                                }
                                else
                                {
                                    AddLogItem("Unable to connect to Trionic 5 ECU");
                                    trionic5.Cleanup();
                                    AddLogItem("Connection closed");
                                    EnableUserInput(true);
                                }
                            }
                            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
                            {
                                SetGenericOptions(trionic7);
                                trionic7.UseFlasherOnDevice = cbOnlyPBus.Checked ? cbUseFlasherOnDevice.Checked : false;

                                AddLogItem("Opening connection");
                                EnableUserInput(false);

                                if (trionic7.openDevice())
                                {
                                    // check reading status periodically
                                    Thread.Sleep(1000);
                                    AddLogItem("Acquiring FLASH content");
                                    Application.DoEvents();
                                    dtstart = DateTime.Now;
                                    trionic7.ReadFlash(sfd.FileName);
                                }
                                else
                                {
                                    AddLogItem("Unable to connect to Trionic 7 ECU");
                                    trionic7.Cleanup();
                                    AddLogItem("Connection closed");
                                    EnableUserInput(true);
                                }
                            }
                            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
                            {
                                SetGenericOptions(trionic8);

                                EnableUserInput(false);
                                AddLogItem("Opening connection");
                                trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                                if (trionic8.openDevice(false))
                                {
                                    Thread.Sleep(1000);
                                    dtstart = DateTime.Now;
                                    AddLogItem("Acquiring FLASH content");
                                    Application.DoEvents();
                                    BackgroundWorker bgWorker;
                                    bgWorker = new BackgroundWorker();                                   
                                    if (cbUseLegionBootloader.Checked)
                                    {
                                        bgWorker.DoWork += new DoWorkEventHandler(trionic8.ReadFlashLegT8);
                                    }
                                    else
                                    {
                                        bgWorker.DoWork += new DoWorkEventHandler(trionic8.ReadFlash);
                                    }
                                    bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                                    bgWorker.RunWorkerAsync(sfd.FileName);
                                }
                                else
                                {
                                    AddLogItem("Unable to connect to Trionic 8 ECU");
                                    trionic8.Cleanup();
                                    EnableUserInput(true);
                                    AddLogItem("Connection terminated");
                                }
                            }
                            else if (cbxEcuType.SelectedIndex == (int)ECU.MOTRONIC96)
                            {
                                SetGenericOptions(trionic8);

                                EnableUserInput(false);
                                AddLogItem("Opening connection");
                                trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                                if (trionic8.openDevice(false))
                                {
                                    Thread.Sleep(1000);
                                    dtstart = DateTime.Now;
                                    AddLogItem("Acquiring FLASH content");
                                    Application.DoEvents();
                                    FlashReadArguments args = new FlashReadArguments() { FileName = sfd.FileName, start = (int)FileME96.MainOSAddress, end = (int)FileME96.LengthComplete };
                                    BackgroundWorker bgWorker;
                                    bgWorker = new BackgroundWorker();
                                    bgWorker.DoWork += new DoWorkEventHandler(trionic8.ReadFlashME96);
                                    bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                                    bgWorker.RunWorkerAsync(args);
                                }
                                else
                                {
                                    AddLogItem("Unable to connect to ME9.6 ECU");
                                    trionic8.Cleanup();
                                    EnableUserInput(true);
                                    AddLogItem("Connection terminated");
                                }
                            }

                            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8_MCP)
                            {
                                SetGenericOptions(trionic8);

                                EnableUserInput(false);
                                AddLogItem("Opening connection");
                                trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                                if (trionic8.openDevice(false))
                                {
                                    Thread.Sleep(1000);
                                    dtstart = DateTime.Now;
                                    AddLogItem("Acquiring FLASH content");
                                    Application.DoEvents();
                                    BackgroundWorker bgWorker;
                                    bgWorker = new BackgroundWorker();
                                    bgWorker.DoWork += new DoWorkEventHandler(trionic8.ReadFlashLegMCP);
                                    bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                                    bgWorker.RunWorkerAsync(sfd.FileName);
                                }
                                else
                                {
                                    AddLogItem("Unable to connect to Trionic 8 ECU");
                                    trionic8.Cleanup();
                                    EnableUserInput(true);
                                    AddLogItem("Connection terminated");
                                }
                            }
                            else if (cbxEcuType.SelectedIndex == (int)ECU.Z22SEMain_LEG)
                            {
                                SetGenericOptions(trionic8);

                                EnableUserInput(false);
                                AddLogItem("Opening connection");
                                trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                                if (trionic8.openDevice(false))
                                {
                                    Thread.Sleep(1000);
                                    dtstart = DateTime.Now;
                                    AddLogItem("Acquiring FLASH content");
                                    Application.DoEvents();
                                    BackgroundWorker bgWorker;
                                    bgWorker = new BackgroundWorker();
                                    bgWorker.DoWork += new DoWorkEventHandler(trionic8.ReadFlashLegZ22SE_Main);
                                    bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                                    bgWorker.RunWorkerAsync(sfd.FileName);
                                }
                                else
                                {
                                    AddLogItem("Unable to connect to Z22SE ECU");
                                    trionic8.Cleanup();
                                    EnableUserInput(true);
                                    AddLogItem("Connection terminated");
                                }
                            }
                            else if (cbxEcuType.SelectedIndex == (int)ECU.Z22SEMCP_LEG)
                            {
                                SetGenericOptions(trionic8);

                                EnableUserInput(false);
                                AddLogItem("Opening connection");
                                trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                                if (trionic8.openDevice(false))
                                {
                                    Thread.Sleep(1000);
                                    dtstart = DateTime.Now;
                                    AddLogItem("Acquiring FLASH content");
                                    Application.DoEvents();
                                    BackgroundWorker bgWorker;
                                    bgWorker = new BackgroundWorker();
                                    bgWorker.DoWork += new DoWorkEventHandler(trionic8.ReadFlashLegZ22SE_MCP);
                                    bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                                    bgWorker.RunWorkerAsync(sfd.FileName);
                                }
                                else
                                {
                                    AddLogItem("Unable to connect to Z22SE ECU");
                                    trionic8.Cleanup();
                                    EnableUserInput(true);
                                    AddLogItem("Connection terminated");
                                }
                            }



                        }
                    }
                }
            }
            LogManager.Flush();
        }

        private void btnGetEcuInfo_Click(object sender, EventArgs e)
        {
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC5)
            {
                SetGenericOptions(trionic5);

                AddLogItem("Opening connection");
                EnableUserInput(false);

                if (trionic5.openDevice())
                {
                    Thread.Sleep(1000);
                    AddLogItem("Aquiring ECU info");
                    Application.DoEvents();
                    trionic5.GetECUInfo(true);
                }
                else
                {
                    AddLogItem("Unable to connect to Trionic 5 ECU");
                }
                trionic5.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                SetGenericOptions(trionic7);
                trionic7.UseFlasherOnDevice = false;

                AddLogItem("Opening connection");
                EnableUserInput(false);

                if (trionic7.openDevice())
                {
                    Thread.Sleep(1000);
                    AddLogItem("Aquiring ECU info");
                    Application.DoEvents();
                    trionic7.GetECUInfo();
                }
                else
                {
                    AddLogItem("Unable to connect to Trionic 7 ECU");
                }
                trionic7.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {
                SetGenericOptions(trionic8);

                EnableUserInput(false);
                AddLogItem("Opening connection");
                trionic8.SecurityLevel = AccessLevel.AccessLevelFD;
                if (trionic8.openDevice(false))
                {
                    // ELM devices cannot detect send failures until in the readMessage thread
                    // Added a connection check here to avoid confused users when all fields show blank!
                    string ecuhardware = trionic8.GetECUHardware();
                    if (ecuhardware == "")
                    {
                        AddLogItem("ECU connection issue, check logs");
                    }
                    else
                    {
                        AddLogItem("VINNumber                 : " + trionic8.GetVehicleVIN());            //0x90
                        AddLogItem("Calibration set           : " + trionic8.GetCalibrationSet());        //0x74
                        AddLogItem("Codefile version          : " + trionic8.GetCodefileVersion());       //0x73
                        AddLogItem("ECU description           : " + trionic8.GetECUDescription());        //0x72
                        AddLogItem("ECU hardware              : " + ecuhardware);                         //0x71
                        AddLogItem("ECU sw number             : " + trionic8.GetECUSWVersionNumber());    //0x95
                        AddLogItem("Programming date          : " + trionic8.GetProgrammingDate());       //0x99
                        AddLogItem("Build date                : " + trionic8.GetBuildDate());             //0x0A
                        AddLogItem("Serial number             : " + trionic8.GetSerialNumber());          //0xB4       
                        AddLogItem("Software version          : " + trionic8.GetSoftwareVersion());       //0x08
                        AddLogItem("0F identifier             : " + trionic8.RequestECUInfo(0x0F, ""));
                        AddLogItem("SW identifier 1           : " + trionic8.RequestECUInfo(0xC1, ""));
                        AddLogItem("SW identifier 2           : " + trionic8.RequestECUInfo(0xC2, ""));
                        AddLogItem("SW identifier 3           : " + trionic8.RequestECUInfo(0xC3, ""));
                        AddLogItem("SW identifier 4           : " + trionic8.RequestECUInfo(0xC4, ""));
                        AddLogItem("SW identifier 5           : " + trionic8.RequestECUInfo(0xC5, ""));
                        AddLogItem("SW identifier 6           : " + trionic8.RequestECUInfo(0xC6, ""));
                        AddLogItem("Hardware type             : " + trionic8.RequestECUInfo(0x97, ""));
                        AddLogItem("75 identifier             : " + trionic8.RequestECUInfo(0x75, ""));
                        AddLogItem("Engine type               : " + trionic8.RequestECUInfo(0x0C, ""));
                        AddLogItem("Supplier ID               : " + trionic8.RequestECUInfo(0x92, ""));
                        AddLogItem("Speed limiter             : " + trionic8.GetTopSpeed() + " km/h");
                        AddLogItem("Oil quality               : " + trionic8.GetOilQuality().ToString("F2") + " %");
                        AddLogItem("SAAB partnumber           : " + trionic8.GetSaabPartnumber());
                        AddLogItem("Diagnostic ID             : " + trionic8.GetDiagnosticDataIdentifier());
                        AddLogItem("End model partnr          : " + trionic8.GetInt64FromID(0xCB));
                        AddLogItem("Basemodel partnr          : " + trionic8.GetInt64FromID(0xCC));
                        AddLogItem("ManufacturersEnableCounter: " + trionic8.GetManufacturersEnableCounter());
                        bool convertible, sai, highoutput, biopower, clutchStart;
                        TankType tankType;
                        DiagnosticType diagnosticType;
                        string rawPI01;
                        trionic8.GetPI01(out convertible, out sai, out highoutput, out biopower, out diagnosticType, out clutchStart, out tankType, out rawPI01);

                        logger.Debug("PI 0x01         : Cab:" + convertible + " SAI:" + sai + " HighOutput:" + highoutput + " Biopower:" + biopower + " DiagnosticType:" + diagnosticType + " ClutchStart:" + clutchStart + " TankType:" + tankType + " rawValues: " + rawPI01);
                        logger.Debug("PI 0x03         : " + trionic8.GetPI03());
                        logger.Debug("PI 0x04         : " + trionic8.GetPI04());
                        logger.Debug("PI 0x07         : " + trionic8.GetPI07());
                        logger.Debug("PI 0x2E         : " + trionic8.GetPI2E());
                        logger.Debug("PI 0xB9         : " + trionic8.GetPIB9());
                        logger.Debug("PI 0x24         : " + trionic8.GetPI24());
                        logger.Debug("PI 0xA0         : " + trionic8.GetPIA0());
                        logger.Debug("PI 0x96         : " + trionic8.GetPI96());
                        
                        // On a non biopower bin this request seem to poison the session, do it last always!
                        AddLogItem("E85                       : " + trionic8.GetE85Percentage().ToString("F2") + " %");
                    }
                }

                trionic8.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.MOTRONIC96)
            {
                SetGenericOptions(trionic8);

                EnableUserInput(false);
                AddLogItem("Opening connection");
                if (trionic8.openDevice(false)) // change to test securityaccess
                {
                    // ELM devices cannot detect send failures until in the readMessage thread
                    // Added a connection check here to avoid confused users when all fields show blank!
                    string calibrationset = trionic8.GetCalibrationSet();
                    if (calibrationset == "")
                    {
                        AddLogItem("ECU connection issue, check logs");
                    }
                    else
                    {
                        string ecuMainOS = trionic8.RequestECUInfo(0xC1, "");
                        ecuMainOS = SubString8(ecuMainOS);
                        string ecuEngineCalib = trionic8.RequestECUInfo(0xC2, "");
                        ecuEngineCalib = SubString8(ecuEngineCalib);
                        string ecuSystemCalib = trionic8.RequestECUInfo(0xC3, "");
                        ecuSystemCalib = SubString8(ecuSystemCalib);
                        string ecuSpeedoCalib = trionic8.RequestECUInfo(0xC4, "");
                        ecuSpeedoCalib = SubString8(ecuSpeedoCalib);
                        string ecuSlaveOS = trionic8.RequestECUInfo(0xC5, "");
                        ecuSlaveOS = SubString8(ecuSlaveOS);

                        AddLogItem("VINNumber       : " + trionic8.GetVehicleVIN());           //0x90
                        AddLogItem("Calibration set : " + trionic8.GetCalibrationSet());       //0x74
                        AddLogItem("Codefile version: " + trionic8.GetCodefileVersion());      //0x73
                        AddLogItem("Serial number   : " + trionic8.GetSerialNumber());         //0xB4
                        AddLogItem("Main OS         : " + ecuMainOS);
                        AddLogItem("Engine Calib    : " + ecuEngineCalib);
                        AddLogItem("System Calib    : " + ecuSystemCalib);
                        AddLogItem("Speedo Calib    : " + ecuSpeedoCalib);
                        AddLogItem("Slave OS        : " + ecuSlaveOS);
                        AddLogItem("Hardware type   : " + trionic8.RequestECUInfo(0x97, ""));
                        AddLogItem("Supplier ID     : " + trionic8.RequestECUInfo(0x92, ""));
                        AddLogItem("Speed limiter   : " + trionic8.GetTopSpeed() + " km/h");
                        AddLogItem("Diagnostic ID   : " + trionic8.GetDiagnosticDataIdentifier());
                        AddLogItem("End model partnr: " + trionic8.GetInt64FromID(0xCB));
                        AddLogItem("Basemodel partnr: " + trionic8.GetInt64FromID(0xCC));
                        AddLogItem("Unknown         : " + trionic8.RequestECUInfo(0x98, ""));
                    }
                }

                trionic8.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            LogManager.Flush();
        }

        private void btnReadSRAM_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "SRAM snapshots|*.RAM" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC5)
                    {
                        SetGenericOptions(trionic5);

                        AddLogItem("Opening connection");
                        EnableUserInput(false);

                        if (trionic5.openDevice())
                        {
                            Thread.Sleep(1000);
                            AddLogItem("Aquiring snapshot");
                            Application.DoEvents();
                            dtstart = DateTime.Now;
                            trionic5.GetSRAMSnapshot(sfd.FileName);
                        }
                        else
                        {
                            AddLogItem("Unable to connect to Trionic 5 ECU");
                        }
                        trionic5.Cleanup();
                        EnableUserInput(true);
                        AddLogItem("Connection terminated");
                    }
                    else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
                    {    
                        SetGenericOptions(trionic7);
                        trionic7.UseFlasherOnDevice = false;

                        AddLogItem("Opening connection");
                        EnableUserInput(false);

                        if (trionic7.openDevice())
                        {
                            Thread.Sleep(1000);
                            AddLogItem("Aquiring snapshot");
                            Application.DoEvents();
                            dtstart = DateTime.Now;
                            trionic7.GetSRAMSnapshot(sfd.FileName);
                        }
                        else
                        {
                            AddLogItem("Unable to connect to Trionic 7 ECU");
                        }
                        trionic7.Cleanup();
                        EnableUserInput(true);
                        AddLogItem("Connection terminated");
                    }
                    else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
                    {
                        SetGenericOptions(trionic8);

                        EnableUserInput(false);
                        AddLogItem("Opening connection");
                        trionic8.SecurityLevel = AccessLevel.AccessLevelFD;
                        if (trionic8.openDevice(true))
                        {
                            Thread.Sleep(1000);
                            dtstart = DateTime.Now;
                            AddLogItem("Aquiring snapshot");
                            Application.DoEvents();
                            BackgroundWorker bgWorker;
                            bgWorker = new BackgroundWorker();
                            bgWorker.DoWork += new DoWorkEventHandler(trionic8.GetSRAMSnapshot);
                            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                            bgWorker.RunWorkerAsync(sfd.FileName);
                        }
                        else
                        {
                            AddLogItem("Unable to connect to Trionic 8 ECU");
                        }
                    }
                }
            }
            LogManager.Flush();
        }

        private void btnRecoverECU_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Binary files|*.bin", Multiselect = false })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (checkFileSize(ofd.FileName))
                    {
                        if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
                        {
                            SetGenericOptions(trionic8);
                            trionic8.SetCANFilterIds(Trionic8.FilterIdRecovery);

                            EnableUserInput(false);
                            AddLogItem("Opening connection");
                            trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                            trionic8.FormatBootPartition = cbFormatBootPartition.Checked;
                            trionic8.FormatSystemPartitions = cbFormatSystemPartitions.Checked;
                            if (trionic8.openDevice(false))
                            {
                                Thread.Sleep(1000);
                                dtstart = DateTime.Now;
                                AddLogItem("Recovering ECU");
                                Application.DoEvents();
                                BackgroundWorker bgWorker;
                                bgWorker = new BackgroundWorker();
                                if (cbUseLegionBootloader.Checked)
                                {
                                    bgWorker.DoWork += new DoWorkEventHandler(trionic8.RecoverECU_Leg);
                                }
                                else
                                {
                                    bgWorker.DoWork += new DoWorkEventHandler(trionic8.RecoverECU_Def);
                                }
                                bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                                bgWorker.RunWorkerAsync(ofd.FileName);
                            }
                            else
                            {
                                AddLogItem("Unable to connect to Trionic 8 ECU");
                                trionic8.Cleanup();
                                EnableUserInput(true);
                                AddLogItem("Connection terminated");
                            }
                        }
                    }
                }
            }
            LogManager.Flush();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveRegistrySetting("AdapterType", cbxAdapterType.SelectedItem != null ? cbxAdapterType.SelectedItem.ToString() : String.Empty);
            SaveRegistrySetting("Adapter", cbAdapter.SelectedItem != null ?  cbAdapter.SelectedItem.ToString() :  String.Empty);
            SaveRegistrySetting("ECU", cbxEcuType.SelectedItem != null ? cbxEcuType.SelectedItem.ToString() : String.Empty);
            SaveRegistrySetting("EnableLogging", cbEnableLogging.Checked);
            SaveRegistrySetting("OnlyPBus", cbOnlyPBus.Checked);
            SaveRegistrySetting("ComSpeed", cbxComSpeed.SelectedItem != null ? cbxComSpeed.SelectedItem.ToString() : String.Empty);
            SaveRegistrySetting("UseLegionBootloader", cbUseLegionBootloader.Checked);
            SaveRegistrySetting("FormatSystemPartitions", cbFormatSystemPartitions.Checked);
            SaveRegistrySetting("FormatBootPartition", cbFormatBootPartition.Checked);
            SaveRegistrySetting("AutoChecksum", cbAutoChecksum.Checked);
            trionic8.Cleanup();
            trionic7.Cleanup();
            trionic5.Cleanup();
            System.Windows.Forms.Application.Exit();
        }

        private void SetGenericOptions(ITrionic trionic)
        {
            trionic.OnlyPBus = cbOnlyPBus.Checked;

            switch(cbxEcuType.SelectedIndex)
            {
                case (int)ECU.TRIONIC5:
                    trionic.ECU = ECU.TRIONIC5;
                    break;
                case (int)ECU.TRIONIC7:
                    trionic.ECU = ECU.TRIONIC7;
                    break;
                case (int)ECU.TRIONIC8:
                    trionic.ECU = ECU.TRIONIC8;
                    break;
                case (int)ECU.TRIONIC8_MCP:
                    trionic.ECU = ECU.TRIONIC8_MCP;
                    break;
                case (int)ECU.Z22SEMain_LEG:
                    trionic.ECU = ECU.Z22SEMain_LEG;
                    break;
                case (int)ECU.Z22SEMCP_LEG:
                    trionic.ECU = ECU.Z22SEMCP_LEG;
                    break;
                case (int)ECU.MOTRONIC96:
                    trionic.ECU = ECU.MOTRONIC96;
                    break;
                default:
                    break;
            }

            switch (cbxAdapterType.SelectedIndex)
            {
                case (int)CANBusAdapter.JUST4TRIONIC:
                    trionic.ForcedBaudrate = 115200;
                    break;
                case (int)CANBusAdapter.ELM327:
                    //set selected com speed
                    switch (cbxComSpeed.SelectedIndex)
                    {
                        case (int)ComSpeed.S2Mbit:
                            trionic.ForcedBaudrate = 2000000;
                            break;
                        case (int)ComSpeed.S1Mbit:
                            trionic.ForcedBaudrate = 1000000;
                            break;
                        case (int)ComSpeed.S230400:
                            trionic.ForcedBaudrate = 230400;
                            break;
                        case (int)ComSpeed.S115200:
                            trionic.ForcedBaudrate = 115200;
                            break;
                        default:
                            trionic.ForcedBaudrate = 0; //default , no speed will be changed
                            break;
                    }
                    break;
                default:
                    break;
            }

            trionic.setCANDevice((CANBusAdapter)cbxAdapterType.SelectedIndex);
            if (cbAdapter.SelectedItem != null)
            {
                trionic.SetSelectedAdapter(cbAdapter.SelectedItem.ToString());
            }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            Text = "TrionicCANFlasher v" + System.Windows.Forms.Application.ProductVersion;
            logger.Trace(Text);
            logger.Trace(".dot net CLR " + System.Environment.Version);

            // get additional info from registry if available
            LoadRegistrySettings();
            CheckRegistryFTDI();

            trionic5.onReadProgress += trionicCan_onReadProgress;
            trionic5.onWriteProgress += trionicCan_onWriteProgress;
            trionic5.onCanInfo += trionicCan_onCanInfo;

            trionic7.onReadProgress += trionicCan_onReadProgress;
            trionic7.onWriteProgress += trionicCan_onWriteProgress;
            trionic7.onCanInfo += trionicCan_onCanInfo;

            trionic8.onReadProgress += trionicCan_onReadProgress;
            trionic8.onWriteProgress += trionicCan_onWriteProgress;
            trionic8.onCanInfo += trionicCan_onCanInfo;

            EnableUserInput(true);
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            try
            {
                m_msiUpdater = new msiupdater(new Version(System.Windows.Forms.Application.ProductVersion));
                m_msiUpdater.Apppath = System.Windows.Forms.Application.UserAppDataPath;
                m_msiUpdater.onDataPump += new msiupdater.DataPump(m_msiUpdater_onDataPump);
                m_msiUpdater.onUpdateProgressChanged += new msiupdater.UpdateProgressChanged(m_msiUpdater_onUpdateProgressChanged);
                m_msiUpdater.CheckForUpdates("http://develop.trionictuning.com/TrionicCANFlasher/", "canflasher", "TrionicCANFlash.msi");
            }
            catch (Exception E)
            {
                AddLogItem(E.Message);
            }
        }

        void m_msiUpdater_onUpdateProgressChanged(msiupdater.MSIUpdateProgressEventArgs e)
        {

        }

        void m_msiUpdater_onDataPump(msiupdater.MSIUpdaterEventArgs e)
        {
            if (e.UpdateAvailable)
            {
                frmUpdateAvailable frmUpdate = new frmUpdateAvailable();
                frmUpdate.SetVersionNumber(e.Version.ToString());
                if (m_msiUpdater != null)
                {
                    m_msiUpdater.Blockauto_updates = false;
                }
                if (frmUpdate.ShowDialog() == DialogResult.OK)
                {
                    if (m_msiUpdater != null)
                    {
                        m_msiUpdater.ExecuteUpdate(e.Version);
                        System.Windows.Forms.Application.Exit();
                    }
                }
                else
                {
                    // user choose "NO", don't bug him again!
                    if (m_msiUpdater != null)
                    {
                        m_msiUpdater.Blockauto_updates = false;
                    }
                }
            }
        }

        private void GetAdapterInformation()
        {
            if (cbxAdapterType.SelectedIndex != -1)
            {
                logger.Debug("ITrionic.GetAdapterNames selectedIndex=" + cbxAdapterType.SelectedIndex);
                string[] adapters = ITrionic.GetAdapterNames((CANBusAdapter)cbxAdapterType.SelectedIndex);
                cbAdapter.Items.Clear();
                foreach (string adapter in adapters)
                {
                    cbAdapter.Items.Add(adapter);
                    logger.Debug("Adaptername=" + adapter);
                }
                
                try
                {
                    if (adapters.Length > 0)
                        cbAdapter.SelectedIndex = 0;
                }
                catch (Exception e)
                {
                    AddLogItem(e.Message);
                }
            }
        }

        private void LoadRegistrySettings()
        {
            RegistryKey SoftwareKey = Registry.CurrentUser.CreateSubKey("Software");
            RegistryKey ManufacturerKey = SoftwareKey.CreateSubKey("MattiasC");
            using (RegistryKey Settings = ManufacturerKey.CreateSubKey("TrionicCANFlasher"))
            {
                if (Settings != null)
                {
                    string[] vals = Settings.GetValueNames();
                    foreach (string a in vals)
                    {
                        try
                        {
                            if (a == "Adapter")
                            {
                                cbAdapter.SelectedItem = Settings.GetValue(a).ToString();
                            }
                            else if (a == "AdapterType")
                            {
                                cbxAdapterType.SelectedItem = Settings.GetValue(a).ToString();
                            }
                            else if (a == "ECU")
                            {
                                cbxEcuType.SelectedItem = Settings.GetValue(a).ToString();
                            }
                            else if (a == "EnableLogging")
                            {
                                cbEnableLogging.Checked = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "OnlyPBus")
                            {
                                cbOnlyPBus.Checked = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "ComSpeed")
                            {
                                cbxComSpeed.SelectedItem = Settings.GetValue(a).ToString();
                            }
                            else if (a == "UseLegionBootlooder")
                            {
                                cbUseLegionBootloader.Checked = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "FormatSystemPartitions")
                            {
                                cbFormatSystemPartitions.Checked = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "FormatBootPartition")
                            {
                                cbFormatBootPartition.Checked = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "AutoChecksum")
                            {
                                cbAutoChecksum.Checked = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            
                        }
                        catch (Exception e)
                        {
                            AddLogItem(e.Message);
                        }
                    }
                }
            }
        }

        private void CheckRegistryFTDI()
        {
            if (cbxAdapterType.SelectedIndex == (int)CANBusAdapter.ELM327)
            {
                using (RegistryKey FTDIBUSKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum\\FTDIBUS"))
                {
                    if (FTDIBUSKey != null)
                    {
                        string[] vals = FTDIBUSKey.GetSubKeyNames();
                        foreach (string name in vals)
                        {
                            if (name.StartsWith("VID_0403+PID_6001"))
                            {
                                using (RegistryKey NameKey = FTDIBUSKey.OpenSubKey(name + "\\0000\\Device Parameters"))
                                {
                                    if (NameKey != null)
                                    {
                                        String PortName = NameKey.GetValue("PortName").ToString();
                                        if (cbAdapter.SelectedItem != null && cbAdapter.SelectedItem.Equals(PortName))
                                        {
                                            String Latency = NameKey.GetValue("LatencyTimer").ToString();
                                            AddLogItem(String.Format("ELM327 FTDI setting for {0} LatencyTimer {1}ms.", PortName, Latency));
                                            if (!Latency.Equals("2") && !Latency.Equals("1")) 
                                            {
                                                MessageBox.Show("Warning LatencyTimer should be set to 2 ms", "ELM327 FTDI setting", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void SaveRegistrySetting(string key, string value)
        {
            RegistryKey SoftwareKey = Registry.CurrentUser.CreateSubKey("Software");
            RegistryKey ManufacturerKey = SoftwareKey.CreateSubKey("MattiasC");
            using (RegistryKey saveSettings = ManufacturerKey.CreateSubKey("TrionicCANFlasher"))
            {
                saveSettings.SetValue(key, value);
            }
        }

        private static void SaveRegistrySetting(string key, bool value)
        {
            RegistryKey SoftwareKey = Registry.CurrentUser.CreateSubKey("Software");
            RegistryKey ManufacturerKey = SoftwareKey.CreateSubKey("MattiasC");
            using (RegistryKey saveSettings = ManufacturerKey.CreateSubKey("TrionicCANFlasher"))
            {
                saveSettings.SetValue(key, value);
            }
        }

        void trionicCan_onWriteProgress(object sender, ITrionic.WriteProgressEventArgs e)
        {
            UpdateProgressStatus(e.Percentage);
        }

        void trionicCan_onCanInfo(object sender, ITrionic.CanInfoEventArgs e)
        {
            UpdateFlashStatus(e);
        }

        void trionicCan_onReadProgress(object sender, ITrionic.ReadProgressEventArgs e)
        {
            UpdateProgressStatus(e.Percentage);
        }

        private void cbxAdapterType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbxAdapterType.SelectedIndex == (int)CANBusAdapter.JUST4TRIONIC)
            {
                cbxComSpeed.SelectedIndex = (int)ComSpeed.S115200;
            }

            EnableUserInput(true);
            GetAdapterInformation();
        }

        private void btnReadDTC_Click(object sender, EventArgs e)
        {
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                SetGenericOptions(trionic7);
                trionic7.UseFlasherOnDevice = false;

                EnableUserInput(false);
                AddLogItem("Opening connection");
                if (trionic7.openDevice())
                {
                    string[] codes = trionic7.ReadDTC();
                    foreach (string a in codes)
                    {
                        AddLogItem(a);
                    }
                }

                trionic7.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {
                SetGenericOptions(trionic8);

                EnableUserInput(false);
                AddLogItem("Opening connection");
                trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                if (trionic8.openDevice(false))
                {
                    string[] codes = trionic8.ReadDTC();
                    foreach (string a in codes)
                    {
                        AddLogItem(a);
                    }
                }

                trionic8.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.MOTRONIC96)
            {
                SetGenericOptions(trionic8);

                EnableUserInput(false);
                AddLogItem("Opening connection");
                trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                if (trionic8.openDevice(false))
                {
                    string[] codes = trionic8.ReadDTC();
                    foreach (string a in codes)
                    {
                        AddLogItem(a);
                    }
                }

                trionic8.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            LogManager.Flush();
        }

        private void updateStatusInBox(ITrionic.CanInfoEventArgs e)
        {
            AddLogItem(e.Info);
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                if (e.Type == ActivityType.FinishedFlashing || e.Type == ActivityType.FinishedDownloadingFlash)
                {
                    TimeSpan ts = DateTime.Now - dtstart;
                    AddLogItem("Total duration: " + ts.Minutes + " minutes " + ts.Seconds + " seconds");
                    trionic7.Cleanup();
                    AddLogItem("Connection closed");
                    EnableUserInput(true);
                }
            }
        }

        private void UpdateFlashStatus(ITrionic.CanInfoEventArgs e)
        {
            try
            {
                Invoke(m_DelegateUpdateStatus, e);
            }
            catch (Exception ex)
            {
                AddLogItem(ex.Message);
            }
        }

        private void updateProgress(int percentage)
        {
            if (progressBar1.Value != percentage)
            {
                progressBar1.Value = percentage;
            }
            if (cbEnableLogging.Checked)
            {
                logger.Trace("progress: " + percentage.ToString("F0") + "%");
            }
        }

        private void UpdateProgressStatus(int percentage)
        {
            try
            {
                Invoke(m_DelegateProgressStatus, percentage);
            }
            catch (Exception e)
            {
                logger.Trace(e.Message);
            }
        }

        private void cbxEcuType_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableUserInput(true);
        }

        private void documentation_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("TrionicCanFlasher.pdf");
        }

        private void btnEditParameters_Click(object sender, EventArgs e)
        {
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                SetGenericOptions(trionic7);
                trionic7.UseFlasherOnDevice = false;

                EnableUserInput(false);
                AddLogItem("Opening connection");
                if (trionic7.openDevice())
                {
                    EditParameters pi = new EditParameters();
                    pi.setECU(ECU.TRIONIC7);
                    float e85 = trionic7.GetE85Percentage();
                    pi.E85 = e85;

                    if (pi.ShowDialog() == DialogResult.OK)
                    {
                        if (!pi.E85.Equals(e85))
                        {
                            if(trionic7.SetE85Percentage((int)pi.E85))
                            {
                                AddLogItem("Set fields successful, E85Percentage:" + pi.E85);
                            }
                            else
                            {
                                AddLogItem("Set fields failed, E85Percentage:" + pi.E85);
                            }
                        }
                    }
                }

                trionic7.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {
                SetGenericOptions(trionic8);

                EnableUserInput(false);
                AddLogItem("Opening connection");
                trionic8.SecurityLevel = AccessLevel.AccessLevelFD;
                if (trionic8.openDevice(true))
                {
                    EditParameters pi = new EditParameters();
                    pi.setECU(ECU.TRIONIC8);

                    float oil = trionic8.GetOilQuality();
                    pi.Oil = oil;

                    string vin = trionic8.GetVehicleVIN();
                    pi.VIN = vin;

                    bool convertible, sai, highoutput, biopower, clutchStart;
                    TankType tankType;
                    DiagnosticType diagnosticType;
                    string rawPI01;
                    trionic8.GetPI01(out convertible, out sai, out highoutput, out biopower, out diagnosticType, out clutchStart, out tankType, out rawPI01);
                    pi.Convertible = convertible;
                    pi.SAI = sai;
                    pi.Highoutput = highoutput;
                    pi.Biopower = biopower;
                    pi.DiagnosticType = diagnosticType;
                    pi.TankType = tankType;
                    pi.ClutchStart = clutchStart;
                    AddLogItem("Read fields");
                    AddLogItem("Convertible:" + pi.Convertible + " SAI:" + pi.SAI + " HighOutput:" + pi.Highoutput + " Biopower:" + pi.Biopower + " DiagnosticType:" + pi.DiagnosticType + " ClutchStart:" + pi.ClutchStart + " TankType:" + pi.TankType);

                    int topspeed = trionic8.GetTopSpeed();
                    pi.TopSpeed = topspeed;

                    // On a non biopower this call seem to poison the session, do it last!
                    float e85 = trionic8.GetE85Percentage();
                    pi.E85 = e85;

                    if (pi.ShowDialog() == DialogResult.OK)
                    {
                        if (!pi.Convertible.Equals(convertible) || !pi.SAI.Equals(sai) || !pi.Highoutput.Equals(highoutput) || !pi.Biopower.Equals(biopower) || !pi.ClutchStart.Equals(clutchStart) || !pi.DiagnosticType.Equals(diagnosticType) || !pi.TankType.Equals(tankType))
                        {
                            AddLogItem("Detected changed values from user:" + pi.Convertible + " SAI:" + pi.SAI + " HighOutput:" + pi.Highoutput + " Biopower:" + pi.Biopower + " DiagnosticType:" + pi.DiagnosticType + " ClutchStart:" + pi.ClutchStart + " TankType:" + pi.TankType);
                            
                            // Do a second read to make sure the first one was ok
                            bool convertible2, sai2, highoutput2, biopower2, clutchStart2;
                            TankType tankType2;
                            DiagnosticType diagnosticType2;
                            trionic8.GetPI01(out convertible2, out sai2, out highoutput2, out biopower2, out diagnosticType2, out clutchStart2, out tankType2, out rawPI01);
                            if (convertible2.Equals(convertible) && sai2.Equals(sai) && highoutput2.Equals(highoutput) && biopower2.Equals(biopower) && clutchStart2.Equals(clutchStart) && diagnosticType2.Equals(diagnosticType) && tankType2.Equals(tankType))
                            {
                                if (trionic8.SetPI01(pi.Convertible, pi.SAI, pi.Highoutput, pi.Biopower, pi.DiagnosticType, pi.ClutchStart, pi.TankType))
                                {
                                    AddLogItem("Set fields successful");
                                }
                                else
                                {
                                    AddLogItem("Set fields failed");
                                }
                            }
                            else
                            {
                                AddLogItem("Set fields failed, verification read does not match");
                            }
                        }

                        if (!pi.VIN.Equals(vin))
                        {
                            if(trionic8.SetVIN(pi.VIN))
                            {
                                AddLogItem("Set fields successful, VIN:" + pi.VIN);
                            }
                            else
                            {
                                AddLogItem("Set fields failed, VIN:" + pi.VIN);
                            }
                        }

                        if (!pi.TopSpeed.Equals(topspeed))
                        {
                            if(trionic8.SetTopSpeed(pi.TopSpeed))
                            {
                                AddLogItem("Set fields successful, TopSpeed:" + pi.TopSpeed);
                            }
                            else
                            {
                                AddLogItem("Set fields failed, TopSpeed:" + pi.TopSpeed);
                            }
                        }

                        if (!pi.E85.ToString("F2").Equals(e85.ToString("F2")))
                        {
                            if(trionic8.SetE85Percentage(pi.E85))
                            {
                                AddLogItem("Set fields successful, E85Percentage:" + pi.E85);
                            }
                            else
                            {
                                AddLogItem("Set fields failed, E85Percentage:" + pi.E85);
                            }
                        }

                        if (!pi.Oil.ToString("F2").Equals(oil.ToString("F2")))
                        {
                            if(trionic8.SetOilQuality(pi.Oil))
                            {
                                AddLogItem("Set fields successful, OilQuality:" + pi.Oil);
                            }
                            else
                            {
                                AddLogItem("Set fields failed, OilQuality:" + pi.Oil);
                            }
                        }
                    }
                }
                trionic8.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.MOTRONIC96)
            {
                SetGenericOptions(trionic8);

                EnableUserInput(false);
                AddLogItem("Opening connection");
                trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                if (trionic8.openDevice(true))
                {
                    EditParameters pi = new EditParameters();
                    pi.setECU(ECU.MOTRONIC96);

                    int topspeed = trionic8.GetTopSpeed();
                    pi.TopSpeed = topspeed;

                    string vin = trionic8.GetVehicleVIN();
                    pi.VIN = vin;

                    if (pi.ShowDialog() == DialogResult.OK)
                    {
                        if (!pi.TopSpeed.Equals(topspeed))
                        {
                            if(trionic8.SetTopSpeed(pi.TopSpeed))
                            {
                                AddLogItem("Set fields successful, TopSpeed:" + pi.TopSpeed);
                            }
                            else
                            {
                                AddLogItem("Set fields failed, TopSpeed:" + pi.TopSpeed);
                            }
                        }

                        if (!pi.VIN.Equals(vin))
                        {
                            if (trionic8.ProgramVIN(pi.VIN))
                            {
                                AddLogItem("Set fields successful, VIN:" + pi.VIN);
                            }
                            else
                            {
                                AddLogItem("Set fields failed, VIN:" + pi.VIN);
                            }
                        }
                    }
                }

                trionic8.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            LogManager.Flush();
        }

        private void btnReadECUcalibration_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "Bin files|*.bin" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    if (sfd.FileName != string.Empty)
                    {
                        if (Path.GetFileName(sfd.FileName) != string.Empty)
                        {
                            if (cbxEcuType.SelectedIndex == (int)ECU.MOTRONIC96)
                            {
                                SetGenericOptions(trionic8);

                                EnableUserInput(false);
                                AddLogItem("Opening connection");
                                trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                                if (trionic8.openDevice(false))
                                {
                                    Thread.Sleep(1000);
                                    dtstart = DateTime.Now;
                                    AddLogItem("Acquiring FLASH content");
                                    Application.DoEvents();
                                    var args = new FlashReadArguments() { FileName = sfd.FileName, start = (int)FileME96.EngineCalibrationAddress, end = (int)FileME96.EngineCalibrationAddressEnd };
                                    BackgroundWorker bgWorker;
                                    bgWorker = new BackgroundWorker();
                                    bgWorker.DoWork += new DoWorkEventHandler(trionic8.ReadFlashME96);
                                    bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                                    bgWorker.RunWorkerAsync(args);
                                }
                                else
                                {
                                    AddLogItem("Unable to connect to ME9.6 ECU");
                                    trionic8.Cleanup();
                                    EnableUserInput(true);
                                    AddLogItem("Connection terminated");
                                }
                            }
                        }
                    }
                }
            }
            LogManager.Flush();
        }

        private void cbEnableLogging_CheckedChanged(object sender, EventArgs e)
        {
            UpdateLogManager();
        }

        private void UpdateLogManager()
        {
            if (cbEnableLogging.Checked)
            {
                LogManager.EnableLogging();
            }
            else
            {
                LogManager.DisableLogging();
            }
        }

        private void linkLabelLogging_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/MattiasC/TrionicCANFlasher";
            Process.Start(path);
        }

        private void btnRestoreT8_Click(object sender, EventArgs e)
        {
            DialogResult result = DialogResult.Cancel;
            result = MessageBox.Show("Power on ECU",
                "Information", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);

            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Bin files|*.bin", Multiselect = false })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (checkFileSize(ofd.FileName))
                    {
                        if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
                        {
                            ChecksumResult checksum = ChecksumT8.VerifyChecksum(ofd.FileName, cbAutoChecksum.Checked, m_ShouldUpdateChecksum);
                            if (checksum != ChecksumResult.Ok)
                            {
                                AddLogItem("Checksum check failed: " + checksum);
                                return;
                            }

                            SetGenericOptions(trionic8);

                            EnableUserInput(false);
                            AddLogItem("Opening connection");
                            trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                            if (trionic8.openDevice(false))
                            {
                                Thread.Sleep(1000);
                                dtstart = DateTime.Now;
                                AddLogItem("Update FLASH content");
                                Application.DoEvents();
                                BackgroundWorker bgWorker;
                                bgWorker = new BackgroundWorker();
                                bgWorker.DoWork += new DoWorkEventHandler(trionic8.RestoreT8);
                                bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                                bgWorker.RunWorkerAsync(ofd.FileName);

                            }
                            else
                            {
                                AddLogItem("Unable to connect to Trionic 8 ECU");
                                trionic8.Cleanup();
                                EnableUserInput(true);
                                AddLogItem("Connection terminated");
                            }
                        }
                    }
                }
            }
            LogManager.Flush();
        }

        private void btnLogData_Click(object sender, EventArgs e)
        {
            if (btnLogData.Text != "Stop")
            {
                // Force logging on
                LogManager.EnableLogging();
                dtstart = DateTime.Now;
                if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC5)
                {
                    SetGenericOptions(trionic5);

                    EnableUserInput(false);
                    btnLogData.Enabled = true;
                    AddLogItem("Opening connection");
                    if (trionic5.openDevice())
                    {
                        StartBGWorkerLog(trionic5);
                        btnLogData.Text = "Stop";
                    }
                    else
                    {
                        // Reset logging to setting
                        UpdateLogManager();
                        btnLogData.Text = "Log Data";
                        EnableUserInput(true);
                    }
                }
                else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
                {
                    SetGenericOptions(trionic7);
                    trionic7.UseFlasherOnDevice = false;

                    EnableUserInput(false);
                    btnLogData.Enabled = true;
                    AddLogItem("Opening connection");
                    if (trionic7.openDevice())
                    {
                        StartBGWorkerLog(trionic7);
                        btnLogData.Text = "Stop";
                    }
                    else
                    {
                        // Reset logging to setting
                        UpdateLogManager();
                        btnLogData.Text = "Log Data";
                        EnableUserInput(true);
                    }
                }
                else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8 ||
                    cbxEcuType.SelectedIndex == (int)ECU.MOTRONIC96    ||
                    cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8_MCP  ||
                    cbxEcuType.SelectedIndex == (int)ECU.Z22SEMain_LEG ||
                    cbxEcuType.SelectedIndex == (int)ECU.Z22SEMCP_LEG)
                {
                    SetGenericOptions(trionic8);

                    EnableUserInput(false);
                    btnLogData.Enabled = true;
                    AddLogItem("Opening connection");
                    trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                    if (trionic8.openDevice(false))
                    {
                        StartBGWorkerLog(trionic8);
                        btnLogData.Text = "Stop";
                    }
                    else
                    {
                        // Reset logging to setting
                        UpdateLogManager();
                        btnLogData.Text = "Log Data";
                        EnableUserInput(true);
                    }
                }
            }
            else
            {
                bgworkerLogCanData.CancelAsync();
                // Reset logging to setting
                UpdateLogManager();
                btnLogData.Text = "Log Data";
                EnableUserInput(true);
            }
        }

        private void StartBGWorkerLog(ITrionic trionic)
        {
            AddLogItem("Logging in progress");
            bgworkerLogCanData = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bgworkerLogCanData.DoWork += trionic.LogCANData;
            bgworkerLogCanData.RunWorkerCompleted += bgWorker_RunWorkerCompleted;
            bgworkerLogCanData.RunWorkerAsync();
        }

        private void cbUseLegionBootloader_CheckedChanged(object sender, EventArgs e)
        {
            cbFormatSystemPartitions.Enabled = cbUseLegionBootloader.Checked;
            cbFormatBootPartition.Enabled = (cbFormatSystemPartitions.Checked && cbFormatSystemPartitions.Enabled);
        }

        private void cbFormatSystemPartitions_CheckedChanged(object sender, EventArgs e)
        {
            cbFormatSystemPartitions.Enabled = cbUseLegionBootloader.Checked;
            cbFormatBootPartition.Enabled = cbFormatSystemPartitions.Checked;
        }

        private bool updateChecksum(string layer, string filechecksum, string realchecksum)
        {
            AddLogItem(layer);
            AddLogItem("File Checksum: " + filechecksum);
            AddLogItem("Real Checksum: " + realchecksum);

            using (frmChecksum frm = new frmChecksum() { Layer = layer, FileChecksum = filechecksum, RealChecksum = realchecksum })
            {
                return frm.ShowDialog() == DialogResult.OK ? true : false;
            }
        }
    }
}
