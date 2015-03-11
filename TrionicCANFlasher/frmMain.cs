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
using System.Drawing;

namespace TrionicCANFlasher
{
    public delegate void DelegateUpdateStatus(ITrionic.CanInfoEventArgs e);
    public delegate void DelegateProgressStatus(int percentage);

    public partial class frmMain : Form
    {
        readonly Trionic8 trionic8 = new Trionic8();
        readonly Trionic7 trionic7 = new Trionic7();
        DateTime dtstart;
        public DelegateUpdateStatus m_DelegateUpdateStatus;
        public DelegateProgressStatus m_DelegateProgressStatus;

        public frmMain()
        {
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            InitializeComponent();
            m_DelegateUpdateStatus = updateStatusInBox;
            m_DelegateProgressStatus = updateProgress;
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
            System.Diagnostics.Debug.WriteLine(e.ToString());
        }

        void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.ToString());
            //throw new NotImplementedException();
        }

        private void AddLogItem(string item)
        {
            var uiItem = DateTime.Now.ToString("HH:mm:ss.fff") + " - " + item;
            listBoxLog.Items.Add(uiItem);
            while (listBoxLog.Items.Count > 100) listBoxLog.Items.RemoveAt(0);
            listBoxLog.SelectedIndex = listBoxLog.Items.Count - 1;
            if (cbEnableLogging.Checked)
            {
                LogHelper.Log(item);
            }
            Application.DoEvents();
        }

        private void btnFlashEcu_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Bin files|*.bin", Multiselect = false })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (checkFileSize(ofd.FileName))
                    {
                        if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
                        {
                            SetGenericOptions(trionic7);
                            trionic7.ELM327Kline = cbELM327Kline.Checked;
                            trionic7.UseFlasherOnDevice = true;

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
                                DoWorkEventArgs args = new DoWorkEventArgs(ofd.FileName);
                                trionic8.WriteFlash(this, args);
                                while (args.Result == null)
                                {
                                    System.Windows.Forms.Application.DoEvents();
                                    Thread.Sleep(10);
                                }
                                if ((bool)args.Result == true)
                                {
                                    AddLogItem("Operation done");
                                }
                                else
                                {
                                    AddLogItem("Operation failed");
                                }
                                TimeSpan ts = DateTime.Now - dtstart;
                                AddLogItem("Total duration: " + ts.Minutes + " minutes " + ts.Seconds + " seconds");
                                trionic8.Cleanup();
                                EnableUserInput(true);
                                AddLogItem("Connection terminated");

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
            LogHelper.Flush();
        }

        bool checkFileSize(string fileName)
        {
            FileInfo fi = new FileInfo(fileName);
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                if (fi.Length != 0x80000)
                {
                    AddLogItem("Not a trionic 7 file");
                    return false;
                }
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {
                if (fi.Length != 0x100000)
                {
                    AddLogItem("Not a trionic 8 file");
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
            btnSetECUVIN.Enabled = enable;
            btnSetE85.Enabled = enable;
            btnSetSpeed.Enabled = enable;
            cbxAdapterType.Enabled = enable;
            cbxEcuType.Enabled = enable;
            label1.Enabled = enable;
            tbParameter.Enabled = enable;
            cbEnableLogging.Enabled = enable;
            cbOnlyPBus.Enabled = enable;
            cbDisableConnectionCheck.Enabled = enable;
            btnEditParameters.Enabled = enable;

            if (cbxAdapterType.SelectedIndex == (int)CANBusAdapter.ELM327 ||
                cbxAdapterType.SelectedIndex == (int)CANBusAdapter.JUST4TRIONIC)
            {
                cbxComPort.Enabled = enable;
                cbxComSpeed.Enabled = enable;
            }
            else
            {
                cbxComPort.Enabled = false;
                cbxComSpeed.Enabled = false;
            }

            if (cbxAdapterType.SelectedIndex == (int)CANBusAdapter.ELM327 &&
                cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                cbELM327Kline.Enabled = enable;
            }
            else
            {
                cbELM327Kline.Enabled = false;
            }

            // Always disable
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                btnRecoverECU.Enabled = false;
                btnSetECUVIN.Enabled = false;
                btnSetSpeed.Enabled = false;
                btnEditParameters.Enabled = false;
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
                            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
                            {
                                SetGenericOptions(trionic7);
                                trionic7.ELM327Kline = cbELM327Kline.Checked;
                                trionic7.UseFlasherOnDevice = true;

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
                                }
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
                                    Thread.Sleep(1000);
                                    dtstart = DateTime.Now;
                                    AddLogItem("Acquiring FLASH content");
                                    Application.DoEvents();
                                    DoWorkEventArgs args = new DoWorkEventArgs(sfd.FileName);
                                    trionic8.ReadFlash(this, args);
                                    if ((bool)args.Result == true)
                                    {
                                        AddLogItem("Operation done");
                                    }
                                    else
                                    {
                                        AddLogItem("Operation failed");
                                    }
                                    TimeSpan ts = DateTime.Now - dtstart;
                                    AddLogItem("Total duration: " + ts.Minutes + " minutes " + ts.Seconds + " seconds");
                                    trionic8.Cleanup();
                                    EnableUserInput(true);
                                    AddLogItem("Connection terminated");
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
            }
            LogHelper.Flush();
        }

        private void btnGetEcuInfo_Click(object sender, EventArgs e)
        {
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                SetGenericOptions(trionic7);
                trionic7.ELM327Kline = cbELM327Kline.Checked;
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
                    AddLogItem("VINNumber       : " + trionic8.GetVehicleVIN());            //0x90
                    AddLogItem("Calibration set : " + trionic8.GetCalibrationSet());        //0x74
                    AddLogItem("Codefile version: " + trionic8.GetCodefileVersion());       //0x73
                    AddLogItem("ECU description : " + trionic8.GetECUDescription());        //0x72
                    AddLogItem("ECU hardware    : " + trionic8.GetECUHardware());           //0x71
                    AddLogItem("ECU sw number   : " + trionic8.GetECUSWVersionNumber());    //0x95
                    AddLogItem("Programming date: " + trionic8.GetProgrammingDate());       //0x99
                    AddLogItem("Build date      : " + trionic8.GetBuildDate());             //0x0A
                    AddLogItem("Serial number   : " + trionic8.GetSerialNumber());          //0xB4       
                    AddLogItem("Software version: " + trionic8.GetSoftwareVersion());       //0x08
                    AddLogItem("0F identifier   : " + trionic8.RequestECUInfo(0x0F, ""));
                    AddLogItem("SW identifier 1 : " + trionic8.RequestECUInfo(0xC1, ""));
                    AddLogItem("SW identifier 2 : " + trionic8.RequestECUInfo(0xC2, ""));
                    AddLogItem("SW identifier 3 : " + trionic8.RequestECUInfo(0xC3, ""));
                    AddLogItem("SW identifier 4 : " + trionic8.RequestECUInfo(0xC4, ""));
                    AddLogItem("SW identifier 5 : " + trionic8.RequestECUInfo(0xC5, ""));
                    AddLogItem("SW identifier 6 : " + trionic8.RequestECUInfo(0xC6, ""));
                    AddLogItem("Hardware type   : " + trionic8.RequestECUInfo(0x97, ""));
                    AddLogItem("75 identifier   : " + trionic8.RequestECUInfo(0x75, ""));
                    AddLogItem("Engine type     : " + trionic8.RequestECUInfo(0x0C, ""));
                    AddLogItem("Supplier ID     : " + trionic8.RequestECUInfo(0x92, ""));
                    AddLogItem("Speed limiter   : " + trionic8.GetTopSpeed() + " km/h");
                    AddLogItem("Rpm limiter     : " + trionic8.GetRPMLimiter() + " rpm");   //0x29
                    AddLogItem("Oil quality     : " + trionic8.GetOilQuality().ToString("F2") + " %");
                    AddLogItem("SAAB partnumber : " + trionic8.GetSaabPartnumber());
                    AddLogItem("Diagnostic ID   : " + trionic8.GetDiagnosticDataIdentifier());
                    AddLogItem("End model partnr: " + trionic8.GetInt64FromID(0xCB));
                    AddLogItem("Basemodel partnr: " + trionic8.GetInt64FromID(0xCC));
                    bool convertible, sai, highoutput;
                    trionic8.GetPI01(out convertible, out sai, out highoutput);
                    AddLogItem("PI 0x01         : Cab:" + convertible + " SAI:" + sai + " HighOutput:" + highoutput);
                    AddLogItem("PI 0x03         : " + trionic8.GetPI03());
                    AddLogItem("PI 0x04         : " + trionic8.GetPI04());
                    AddLogItem("PI 0x07         : " + trionic8.GetPI07());
                    AddLogItem("PI 0x2E         : " + trionic8.GetPI2E());
                    AddLogItem("PI 0xB9         : " + trionic8.GetPIB9());
                    AddLogItem("PI 0x24         : " + trionic8.GetPI24());
                    AddLogItem("PI 0xA0         : " + trionic8.GetPIA0());
                    AddLogItem("PI 0x96         : " + trionic8.GetPI96());
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
                    AddLogItem("VINNumber       : " + trionic8.GetVehicleVIN());           //0x90
                    AddLogItem("Calibration set : " + trionic8.GetCalibrationSet());       //0x74
                    AddLogItem("Codefile version: " + trionic8.GetCodefileVersion());      //0x73
                    AddLogItem("ECU description : " + trionic8.GetECUDescription());       //0x72
                    AddLogItem("ECU hardware    : " + trionic8.GetECUHardware());          //0x71
                    AddLogItem("ECU sw number   : " + trionic8.GetECUSWVersionNumber());   //0x95
                    AddLogItem("Programming date: " + trionic8.GetProgrammingDate());      //0x99
                    AddLogItem("Build date      : " + trionic8.GetBuildDate());            //0x0A
                    AddLogItem("Serial number   : " + trionic8.GetSerialNumber());         //0xB4       
                    AddLogItem("Software version: " + trionic8.GetSoftwareVersion());      //0x08
                    AddLogItem("0F identifier   : " + trionic8.RequestECUInfo(0x0F, ""));
                    AddLogItem("SW identifier 1 : " + trionic8.RequestECUInfo(0xC1, ""));
                    AddLogItem("SW identifier 2 : " + trionic8.RequestECUInfo(0xC2, ""));
                    AddLogItem("SW identifier 3 : " + trionic8.RequestECUInfo(0xC3, ""));
                    AddLogItem("SW identifier 4 : " + trionic8.RequestECUInfo(0xC4, ""));
                    AddLogItem("SW identifier 5 : " + trionic8.RequestECUInfo(0xC5, ""));
                    AddLogItem("SW identifier 6 : " + trionic8.RequestECUInfo(0xC6, ""));
                    AddLogItem("Hardware type   : " + trionic8.RequestECUInfo(0x97, ""));
                    AddLogItem("75 identifier   : " + trionic8.RequestECUInfo(0x75, ""));
                    AddLogItem("Engine type     : " + trionic8.RequestECUInfo(0x0C, ""));
                    AddLogItem("Supplier ID     : " + trionic8.RequestECUInfo(0x92, ""));
                    AddLogItem("Speed limiter   : " + trionic8.GetTopSpeed() + " km/h");
                    AddLogItem("Oil quality     : " + trionic8.GetOilQuality().ToString("F2") + " %");
                    AddLogItem("SAAB partnumber : " + trionic8.GetSaabPartnumber());
                    AddLogItem("Diagnostic ID   : " + trionic8.GetDiagnosticDataIdentifier());
                    AddLogItem("End model partnr: " + trionic8.GetInt64FromID(0xCB));
                    AddLogItem("Basemodel partnr: " + trionic8.GetInt64FromID(0xCC));
                    AddLogItem("Unknown         : " + trionic8.RequestECUInfo(0x98, ""));
                }

                trionic8.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            LogHelper.Flush();
        }

        private void btnReadSRAM_Click(object sender, EventArgs e)
        {
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "SRAM snapshots|*.RAM" })
                {
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        SetGenericOptions(trionic7);
                        trionic7.ELM327Kline = cbELM327Kline.Checked;
                        trionic7.UseFlasherOnDevice = false;

                        AddLogItem("Opening connection");
                        EnableUserInput(false);

                        if (trionic7.openDevice())
                        {
                            // check reading status periodically

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
                }
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {
                using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "SRAM snapshots|*.RAM" })
                {
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        SetGenericOptions(trionic8);

                        EnableUserInput(false);
                        AddLogItem("Opening connection");
                        trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                        if (trionic8.openDevice(false))
                        {
                            Thread.Sleep(1000);
                            dtstart = DateTime.Now;
                            AddLogItem("Aquiring snapshot");
                            Application.DoEvents();
                            byte[] snapshot = trionic8.getSRAMSnapshot();
                            byte[] snapshot7000 = trionic8.ReadSRAMSnapshot();
                            byte[] total = new byte[0x008000];
                            snapshot.CopyTo(total, 0);
                            snapshot7000.CopyTo(total, 0x7000);
                            try
                            {
                                File.WriteAllBytes(sfd.FileName, total);
                                AddLogItem("Snapshot done");
                            }
                            catch (Exception ex)
                            {
                                AddLogItem("Could not write file... " + ex.Message);
                            }
                            TimeSpan ts = DateTime.Now - dtstart;
                            AddLogItem("Total duration: " + ts.Minutes + " minutes " + ts.Seconds + " seconds");
                        }
                        else
                        {
                            AddLogItem("Unable to connect to Trionic 8 ECU");
                        }
                        trionic8.Cleanup();
                        EnableUserInput(true);
                        AddLogItem("Connection terminated");
                    }
                }
            }
            LogHelper.Flush();
        }

        private void btnRecoverECU_Click(object sender, EventArgs e)
        {
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {
                using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Binary files|*.bin", Multiselect = false })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        SetGenericOptions(trionic8);

                        EnableUserInput(false);
                        AddLogItem("Opening connection");
                        trionic8.SecurityLevel = AccessLevel.AccessLevel01;
                        if (trionic8.openDevice(false))
                        {
                            Thread.Sleep(1000);
                            dtstart = DateTime.Now;
                            AddLogItem("Recovering ECU");
                            Application.DoEvents();
                            DoWorkEventArgs args = new DoWorkEventArgs(ofd.FileName);
                            trionic8.RecoverECU(this, args);
                            while (args.Result == null)
                            {
                                System.Windows.Forms.Application.DoEvents();
                                Thread.Sleep(10);
                            }
                            if ((bool)args.Result == true)
                            {
                                AddLogItem("Operation done");
                            }
                            else
                            {
                                AddLogItem("Operation failed");
                            }
                            TimeSpan ts = DateTime.Now - dtstart;
                            AddLogItem("Total duration: " + ts.Minutes + " minutes " + ts.Seconds + " seconds");
                            trionic8.Cleanup();
                            EnableUserInput(true);
                            AddLogItem("Connection terminated");
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
            LogHelper.Flush();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveRegistrySetting("AdapterType", cbxAdapterType.SelectedItem.ToString());
            try
            {
                SaveRegistrySetting("Comport", cbxComPort.SelectedItem.ToString());
            }
            catch (Exception ex)
            {
                AddLogItem(ex.Message);
            }
            SaveRegistrySetting("ECU", cbxEcuType.SelectedItem.ToString());
            SaveRegistrySetting("EnableLogging", cbEnableLogging.Checked);
            SaveRegistrySetting("OnlyPBus", cbOnlyPBus.Checked);
            SaveRegistrySetting("DisableCanCheck", cbDisableConnectionCheck.Checked);
            SaveRegistrySetting("ComSpeed", cbxComSpeed.SelectedItem.ToString());
            SaveRegistrySetting("ELM327Kline", cbELM327Kline.Checked);
            trionic8.Cleanup();
            Environment.Exit(0);
        }

        private void SetGenericOptions(ITrionic trionic)
        {
            trionic.EnableLog = cbEnableLogging.Checked;
            trionic.OnlyPBus = cbOnlyPBus.Checked;
            trionic.DisableCanConnectionCheck = cbDisableConnectionCheck.Checked;
            
            if (cbxAdapterType.SelectedIndex == (int)CANBusAdapter.ELM327 ||
                cbxAdapterType.SelectedIndex == (int)CANBusAdapter.JUST4TRIONIC)
            {
                trionic.ForcedComport = cbxComPort.SelectedItem.ToString();
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
            }

            trionic.setCANDevice((CANBusAdapter)cbxAdapterType.SelectedIndex);
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            Text = "TrionicCANFlasher v" + System.Windows.Forms.Application.ProductVersion;
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            Application.DoEvents();

            SerialPortInformation();

            cbxAdapterType.SelectedIndex = 0;
            cbxEcuType.SelectedIndex = 0;
            cbxComSpeed.SelectedIndex = 0;

            // get additional info from registry if available
            LoadRegistrySettings();
            CheckRegistryFTDI();
            Application.DoEvents();

            trionic7.onReadProgress += trionicCan_onReadProgress;
            trionic7.onWriteProgress += trionicCan_onWriteProgress;
            trionic7.onCanInfo += trionicCan_onCanInfo;

            trionic8.onReadProgress += trionicCan_onReadProgress;
            trionic8.onWriteProgress += trionicCan_onWriteProgress;
            trionic8.onCanInfo += trionicCan_onCanInfo;

            EnableUserInput(true);

            if (System.Diagnostics.Debugger.IsAttached)
            {
                cbEnableLogging.Checked = true;
            }
        }

        private void SerialPortInformation()
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
                cbxComPort.Items.Add(FilterString(port));
            try
            {
                if (ports.Length > 0)
                    cbxComPort.SelectedIndex = 0;
            }
            catch (Exception e)
            {
                AddLogItem(e.Message);
            }
        }

        private static string FilterString(string port)
        {
            string retval = string.Empty;
            foreach (char c in port)
            {
                if (c >= 0x30 && c <= 'Z') retval += c;
            }
            return retval.Trim();
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
                            if (a == "Comport")
                            {
                                cbxComPort.SelectedItem = Settings.GetValue(a).ToString();
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
                            else if (a == "DisableCanCheck")
                            {
                                cbDisableConnectionCheck.Checked = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "ComSpeed")
                            {
                                cbxComSpeed.SelectedItem = Settings.GetValue(a).ToString();
                            }
                            else if (a == "ELM327Kline")
                            {
                                cbELM327Kline.Checked = Convert.ToBoolean(Settings.GetValue(a).ToString());
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
                                        if (cbxComPort.SelectedItem != null && cbxComPort.SelectedItem.Equals(PortName))
                                        {
                                            String Latency = NameKey.GetValue("LatencyTimer").ToString();
                                            AddLogItem(String.Format("ELM327 FTDI setting for {0} LatencyTimer {1}ms.", PortName, Latency));
                                            if (!Latency.Equals("2"))
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
            EnableUserInput(true);
        }

        private void btnReadDTC_Click(object sender, EventArgs e)
        {
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                SetGenericOptions(trionic7);
                trionic7.ELM327Kline = cbELM327Kline.Checked;
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
            LogHelper.Flush();
        }

        private void btnSetECUVIN_Click(object sender, EventArgs e)
        {
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {
                SetGenericOptions(trionic8);

                EnableUserInput(false);
                AddLogItem("Opening connection");
                trionic8.SecurityLevel = AccessLevel.AccessLevelFD;
                if (trionic8.openDevice(true))
                {
                    string vin = tbParameter.Text;
                    if (vin.Length == 17)
                    {
                        AddLogItem("setECUparameterVIN:");
                        trionic8.SetVIN(vin);
                    }
                    else
                    {
                        AddLogItem("Error expected VIN length 17");
                    }
                }

                trionic8.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            LogHelper.Flush();
        }

        private void btnSetE85_Click(object sender, EventArgs e)
        {
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                SetGenericOptions(trionic7);
                trionic7.ELM327Kline = cbELM327Kline.Checked;
                trionic7.UseFlasherOnDevice = false;

                EnableUserInput(false);
                AddLogItem("Opening connection");
                if (trionic7.openDevice())
                {
                    int e85;
                    if (int.TryParse(tbParameter.Text, out e85))
                    {
                        if (trionic7.SetE85Percentage(e85))
                        {
                            AddLogItem("Set E85% successfull");
                        }
                        else
                        {
                            AddLogItem("Set E85% failed");
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
                    float e85;
                    if (float.TryParse(tbParameter.Text, out e85))
                    {
                        if (trionic8.SetE85Percentage(e85))
                        {
                            AddLogItem("Set E85% successfull");
                        }
                        else
                        {
                            AddLogItem("Set E85% failed");
                        }
                    }
                }

                trionic8.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            LogHelper.Flush();
        }

        private void btnSetSpeed_Click(object sender, EventArgs e)
        {
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {
                SetGenericOptions(trionic8);

                EnableUserInput(false);
                AddLogItem("Opening connection");
                trionic8.SecurityLevel = AccessLevel.AccessLevelFD;
                if (trionic8.openDevice(true))
                {
                    int speed;
                    if (int.TryParse(tbParameter.Text, out speed))
                    {
                        if (trionic8.SetTopSpeed(speed))
                        {
                            AddLogItem("Set SpeedLimiter successfull");
                        }
                        else
                        {
                            AddLogItem("Set SpeedLimiter failed");
                        }
                    }
                }

                trionic8.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            LogHelper.Flush();
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
            string text = percentage.ToString("F0") + "%";
            if (cbEnableLogging.Checked)
            {
                LogHelper.Log("progress: " + text);
            }
            if (label1.Text != text)
            {
                label1.Text = text;
                Application.DoEvents();
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
                AddLogItem(e.Message);
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
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {
                SetGenericOptions(trionic8);

                EnableUserInput(false);
                AddLogItem("Opening connection");
                trionic8.SecurityLevel = AccessLevel.AccessLevelFD;
                if (trionic8.openDevice(true))
                {
                    PiSelection pi = new PiSelection();
                    bool convertible, sai, highoutput;
                    trionic8.GetPI01(out convertible, out sai, out highoutput);
                    pi.Convertible = convertible;
                    pi.SAI = sai;
                    pi.Highoutput = highoutput;

                    int rpm = trionic8.GetRPMLimiter();
                    pi.RPMLimit = rpm;

                    string vin = trionic8.GetVehicleVIN();
                    pi.VIN = vin;

                    int topspeed = trionic8.GetTopSpeed();
                    pi.TopSpeed = topspeed;

                    float e85 = trionic8.GetE85Percentage();
                    pi.E85 = e85;

                    if (pi.ShowDialog() == DialogResult.OK)
                    {
                        if (!pi.Convertible.Equals(convertible) || !pi.SAI.Equals(sai) || !pi.Highoutput.Equals(highoutput))
                        {
                            if (trionic8.SetPI01(pi.Convertible, pi.SAI, pi.Highoutput))
                            {
                                AddLogItem("Set fields successfull");
                                AddLogItem("Convertible:" + pi.Convertible + " SAI:" + pi.SAI + " HighOutput:" + pi.Highoutput);
                            }
                            else
                            {
                                AddLogItem("Set fields failed");
                            }
                        }

                        if (!pi.RPMLimit.Equals(rpm))
                        {
                            trionic8.SetRPMLimiter(pi.RPMLimit);
                        }

                        if (!pi.VIN.Equals(vin))
                        {
                            trionic8.SetVIN(pi.VIN);
                        }

                        if (!pi.TopSpeed.Equals(topspeed))
                        {
                            trionic8.SetTopSpeed(pi.TopSpeed);
                        }

                        if (!pi.E85.Equals(e85))
                        {
                            trionic8.SetE85Percentage(pi.E85);
                        }
                    }
                }
                trionic8.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            LogHelper.Flush();
        }
    }
}
