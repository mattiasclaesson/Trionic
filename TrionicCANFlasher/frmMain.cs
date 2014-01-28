using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.IO.Ports;
using Microsoft.Win32;
using TrionicCANLib;

namespace TrionicCANFlasher
{
    public delegate void DelegateUpdateStatus(TrionicCANLib.TrionicCan.CanInfoEventArgs e);
    public delegate void DelegateProgressStatus(float percentage);

    public partial class frmMain : Form
    {
        readonly TrionicCANLib.TrionicCan trionicCan = new TrionicCANLib.TrionicCan();
        DateTime dtstart;
        public DelegateUpdateStatus m_DelegateUpdateStatus;
        public DelegateProgressStatus m_DelegateProgressStatus;

        public frmMain()
        {

            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            InitializeComponent();
            m_DelegateUpdateStatus = updateStatusInBox;
            m_DelegateProgressStatus = updateProgress;

            EnableUserInput(true);
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
            listBox1.Items.Add(uiItem);
            while (listBox1.Items.Count > 100) listBox1.Items.RemoveAt(0);
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
            if (cbEnableLogging.Checked)
            {
                LogHelper.Log(item);
            }
            Application.DoEvents();
        }

        private void btnFlashEcu_Click(object sender, EventArgs e)
        {
            GetUIOptions();
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Bin files|*.bin", Multiselect = false })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {                    
                    if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
                    {
                        SetT7AdapterType();

                        AddLogItem("Opening connection");
                        EnableUserInput(false);
                        if (trionicCan.openT7Device())
                        {
                            Thread.Sleep(1000);
                            AddLogItem("Update flash content");
                            Application.DoEvents();
                            dtstart = DateTime.Now;
                            trionicCan.UpdateFlashWithT7Flasher(ofd.FileName);
                        }
                        else
                        {
                            AddLogItem("Unable to connect to Trionic 7 ECU");
                        }
                    }
                    else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
                    {
                        trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                        SetT8AdapterType();

                        EnableUserInput(false);
                        AddLogItem("Opening connection");
                        if (trionicCan.openDevice(false))
                        {
                            Thread.Sleep(1000);
                            dtstart = DateTime.Now;
                            AddLogItem("Update flash content");
                            Application.DoEvents();
                            if (trionicCan.UpdateFlash(ofd.FileName))
                            {
                                AddLogItem("Flash sequence done");
                            }
                            else
                            {
                                AddLogItem("Failed to update flash");
                            }
                            TimeSpan ts = DateTime.Now - dtstart;
                            AddLogItem("Total duration: " + ts.Minutes + " minutes " + ts.Seconds + " seconds");
                        }
                        else
                        {
                            AddLogItem("Unable to connect to Trionic 8 ECU");
                        }
                        trionicCan.Cleanup();
                        EnableUserInput(true);
                        AddLogItem("Connection terminated");
                    }
                }
            }
        }

        private void EnableUserInput(bool enable)
        {
            btnFlashECU.Enabled = enable;
            btnReadECU.Enabled = enable;
            btnGetECUInfo.Enabled = enable;
            btnReadSRAM.Enabled = enable;
            btnRecoverECU.Enabled = enable;
            btnSecurityAccess.Enabled = enable;
            btnReadDTC.Enabled = enable;
            btnSetECUVIN.Enabled = enable;
            btnSetE85.Enabled = enable;
            btnSetSpeed.Enabled = enable;
            btnResetECU.Enabled = enable;
            cbxAdapterType.Enabled = enable;
            cbxComPort.Enabled = enable;
            cbxEcuType.Enabled = enable;
            label1.Enabled = enable;
            tbParameter.Enabled = enable;
            cbEnableLogging.Enabled = enable;
            cbOnlyPBus.Enabled = enable;
            cbDisableConnectionCheck.Enabled = enable;

            if (IsElmAdapterSelected ||
                cbxAdapterType.SelectedIndex == (int)CANBusAdapter.JUST4TRIONIC)
            {
                cbxComPort.Enabled = true;
            }
            else
            {
                cbxComPort.Enabled = false;
            }

            // Always disable
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                btnReadSRAM.Enabled = false;
                btnRecoverECU.Enabled = false;
                btnSecurityAccess.Enabled = false;
                btnReadDTC.Enabled = false;
                btnSetECUVIN.Enabled = false;
                btnSetSpeed.Enabled = false;

                if (cbxAdapterType.SelectedIndex == (int)CANBusAdapter.COMBI)
                {
                    btnGetECUInfo.Enabled = false;
                    btnSetE85.Enabled = false;
                }

                if (IsElmAdapterSelected)
                {
                    btnFlashECU.Enabled = false;
                    btnReadECU.Enabled = false;
                    btnGetECUInfo.Enabled = false;
                    btnSetE85.Enabled = false;
                }
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {
                btnResetECU.Enabled = false;

                if (IsElmAdapterSelected)
                {
                    //btnRecoverECU.Enabled = false;
                    btnReadDTC.Enabled = false;
                    //btnSetECUVIN.Enabled = false;
                }
            }
        }

        private bool IsElmAdapterSelected
        {
            get
            {
                return cbxAdapterType.SelectedIndex == (int)CANBusAdapter.ELM327 ||
                    cbxAdapterType.SelectedIndex == (int)CANBusAdapter.OBDLinkSX;
            }
        }

        private void btnReadECU_Click(object sender, EventArgs e)
        {
            GetUIOptions();
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
                                SetT7AdapterType();

                                AddLogItem("Opening connection");
                                EnableUserInput(false);

                                if (trionicCan.openT7Device())
                                {
                                    // check reading status periodically

                                    Thread.Sleep(1000);
                                    AddLogItem("Aquiring flash content");
                                    Application.DoEvents();
                                    dtstart = DateTime.Now;
                                    trionicCan.getFlashWithT7Flasher(sfd.FileName);
                                }
                                else
                                {
                                    AddLogItem("Unable to connect to Trionic 7 ECU");
                                }
                            }
                            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
                            {
                                trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                                SetT8AdapterType();

                                EnableUserInput(false);
                                AddLogItem("Opening connection");
                                if (trionicCan.openDevice(false))
                                {
                                    Thread.Sleep(1000);
                                    dtstart = DateTime.Now;
                                    AddLogItem("Aquiring flash content");
                                    Application.DoEvents();
                                    //byte[] snapshot = trionicCan.getFlashContent();
                                    byte[] snapshot = trionicCan.getFlashWithBootloader();
                                    try
                                    {
                                        File.WriteAllBytes(sfd.FileName, snapshot);
                                        AddLogItem("Download done");
                                    }
                                    catch (Exception E)
                                    {
                                        AddLogItem("Could not write file... " + E.Message);
                                    }
                                    TimeSpan ts = DateTime.Now - dtstart;
                                    AddLogItem("Total duration: " + ts.Minutes + " minutes " + ts.Seconds + " seconds");
                                }
                                else
                                {
                                    AddLogItem("Unable to connect to Trionic 8 ECU");
                                }
                                trionicCan.Cleanup();
                                EnableUserInput(true);
                                AddLogItem("Connection terminated");
                            }
                        }
                    }
                }
            }
        }

        private void btnGetEcuInfo_Click(object sender, EventArgs e)
        {
            GetUIOptions();
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                SetT7AdapterType();

                AddLogItem("Opening connection");
                EnableUserInput(false);

                if (trionicCan.openT7Device())
                {
                    Thread.Sleep(1000);
                    AddLogItem("Aquiring ECU info");
                    Application.DoEvents();
                    trionicCan.GetVehicleVINfromT7();
                }
                else
                {
                    AddLogItem("Unable to connect to Trionic 7 ECU");
                }
                trionicCan.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {
                trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                SetT8AdapterType();

                EnableUserInput(false);
                AddLogItem("Opening connection");
                if (trionicCan.openDevice(false))
                {
                    AddLogItem("VINNumber       : " + trionicCan.GetVehicleVIN());           //0x90
                    AddLogItem("Calibration set : " + trionicCan.GetCalibrationSet());       //0x74
                    AddLogItem("Codefile version: " + trionicCan.GetCodefileVersion());      //0x73
                    AddLogItem("ECU description : " + trionicCan.GetECUDescription());       //0x72
                    AddLogItem("ECU hardware    : " + trionicCan.GetECUHardware());          //0x71
                    AddLogItem("ECU sw number   : " + trionicCan.GetECUSWVersionNumber());   //0x95
                    AddLogItem("Programming date: " + trionicCan.GetProgrammingDate());      //0x99
                    AddLogItem("Build date      : " + trionicCan.GetBuildDate());            //0x0A
                    AddLogItem("Serial number   : " + trionicCan.GetSerialNumber());         //0xB4       
                    AddLogItem("Software version: " + trionicCan.GetSoftwareVersion());      //0x08
                    AddLogItem("0F identifier   : " + trionicCan.RequestECUInfo(0x0F, ""));
                    AddLogItem("SW identifier 1 : " + trionicCan.RequestECUInfo(0xC1, ""));
                    AddLogItem("SW identifier 2 : " + trionicCan.RequestECUInfo(0xC2, ""));
                    AddLogItem("SW identifier 3 : " + trionicCan.RequestECUInfo(0xC3, ""));
                    AddLogItem("SW identifier 4 : " + trionicCan.RequestECUInfo(0xC4, ""));
                    AddLogItem("SW identifier 5 : " + trionicCan.RequestECUInfo(0xC5, ""));
                    AddLogItem("SW identifier 6 : " + trionicCan.RequestECUInfo(0xC6, ""));
                    AddLogItem("Hardware type   : " + trionicCan.RequestECUInfo(0x97, ""));
                    AddLogItem("75 identifier   : " + trionicCan.RequestECUInfo(0x75, ""));
                    AddLogItem("Engine type     : " + trionicCan.RequestECUInfo(0x0C, ""));
                    AddLogItem("Supplier ID     : " + trionicCan.RequestECUInfo(0x92, ""));
                    AddLogItem("Speed limiter   : " + trionicCan.GetTopSpeed() + " km/h");
                    AddLogItem("Oil quality     : " + trionicCan.GetOilQualityPercentage().ToString("F2") + " %");
                    AddLogItem("SAAB partnumber : " + trionicCan.GetSaabPartnumber());
                    AddLogItem("Diagnostic ID   : " + trionicCan.GetDiagnosticDataIdentifier());
                    AddLogItem("End model partnr: " + trionicCan.GetInt64FromID(0xCB));
                    AddLogItem("Basemodel partnr: " + trionicCan.GetInt64FromID(0xCC));
                }

                trionicCan.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
        }

        private void btnReadSRAM_Click(object sender, EventArgs e)
        {
            GetUIOptions();
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {
                using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "SRAM snapshots|*.RAM" })
                {
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                        SetT8AdapterType();

                        EnableUserInput(false);
                        AddLogItem("Opening connection");
                        if (trionicCan.openDevice(false))
                        {
                            Thread.Sleep(1000);
                            dtstart = DateTime.Now;
                            AddLogItem("Aquiring snapshot");
                            Application.DoEvents();
                            byte[] snapshot = trionicCan.getSRAMSnapshot();
                            byte[] snapshot7000 = trionicCan.getSRAMSnapshotWithBootloader();
                            byte[] total = new byte[0x008000];
                            snapshot.CopyTo(total, 0);
                            snapshot7000.CopyTo(total, 0x7000);
                            try
                            {
                                File.WriteAllBytes(sfd.FileName, total);
                                AddLogItem("Snapshot done");
                            }
                            catch (Exception E)
                            {
                                AddLogItem("Could not write file... " + E.Message);
                            }
                            TimeSpan ts = DateTime.Now - dtstart;
                            AddLogItem("Total duration: " + ts.Minutes + " minutes " + ts.Seconds + " seconds");
                        }
                        else
                        {
                            AddLogItem("Unable to connect to Trionic 8 ECU");
                        }
                        trionicCan.Cleanup();
                        EnableUserInput(true);
                        AddLogItem("Connection terminated");
                    }
                }
            }
        }

        private void btnRecoverECU_Click(object sender, EventArgs e)
        {
            GetUIOptions();
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {
                using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Binary files|*.bin", Multiselect = false })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {                        
                        trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                        SetT8AdapterType();

                        EnableUserInput(false);
                        AddLogItem("Opening connection");
                        if (trionicCan.openDevice(false))
                        {
                            Thread.Sleep(1000);
                            dtstart = DateTime.Now;
                            AddLogItem("Recovering ECU");
                            Application.DoEvents();
                            if (trionicCan.RecoverECU(ofd.FileName))
                            {
                                AddLogItem("Recovery done");
                            }
                            else
                            {
                                AddLogItem("Failed to recover ECU");
                            }
                            TimeSpan ts = DateTime.Now - dtstart;
                            AddLogItem("Total duration: " + ts.Minutes + " minutes " + ts.Seconds + " seconds");
                        }
                        else
                        {
                            AddLogItem("Unable to connect to Trionic 8 ECU");
                        }
                        trionicCan.Cleanup();
                        EnableUserInput(true);
                        AddLogItem("Connection terminated");
                    }
                }
            }
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
            catch (Exception) { }
            SaveRegistrySetting("ECU", cbxEcuType.SelectedItem.ToString());
            SaveRegistrySetting("EnableLogging", cbEnableLogging.Checked);
            SaveRegistrySetting("OnlyPBus", cbOnlyPBus.Checked);
            SaveRegistrySetting("DisableCanCheck", cbDisableConnectionCheck.Checked);
            SaveRegistrySetting("ComSpeed", cbxComSpeed.SelectedItem.ToString());
            trionicCan.Cleanup();
            Environment.Exit(0);
        }

        private void SetT8AdapterType()
        {
            if (IsElmAdapterSelected ||
                cbxAdapterType.SelectedIndex == (int)CANBusAdapter.JUST4TRIONIC)
            {
                trionicCan.ForcedComport = cbxComPort.SelectedItem.ToString();
                //set selected com speed
                switch (cbxComSpeed.SelectedIndex)
                {
                    case (int)ComSpeed.S4Mbit:
                        trionicCan.ForcedBaudrate = 2000000;
                        break;
                    case (int)ComSpeed.S1Mbit:
                        trionicCan.ForcedBaudrate = 1000000;
                        break;
                    case (int)ComSpeed.S230400:
                        trionicCan.ForcedBaudrate = 230400;
                        break;
                    case (int)ComSpeed.S115200:
                        trionicCan.ForcedBaudrate = 115200;
                        break;
                    case (int)ComSpeed.S57600:
                        trionicCan.ForcedBaudrate = 57600;
                        break;
                    case (int)ComSpeed.S38400:
                        trionicCan.ForcedBaudrate = 38400;
                        break;
                    default:
                        trionicCan.ForcedBaudrate = 0; //default , no speed will be changed
                        break;
                }
                if (cbxAdapterType.SelectedIndex == (int)CANBusAdapter.OBDLinkSX)
                    trionicCan.BaseBaudrate = 115200;
                else
                    trionicCan.BaseBaudrate = 38400;

            }
            if (IsElmAdapterSelected)
                trionicCan.setCANDevice(CANBusAdapter.ELM327);
            else
                trionicCan.setCANDevice((CANBusAdapter)cbxAdapterType.SelectedIndex);
        }

        private void SetT7AdapterType()
        {
            if (cbxAdapterType.SelectedIndex == (int)CANBusAdapter.ELM327 ||
                cbxAdapterType.SelectedIndex == (int)CANBusAdapter.JUST4TRIONIC)
            {
                trionicCan.ForcedComport = cbxComPort.SelectedItem.ToString();
            }
            trionicCan.setT7CANDevice((CANBusAdapter)cbxAdapterType.SelectedIndex);
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
            Application.DoEvents();

            trionicCan.onReadProgress += trionicCan_onReadProgress;
            trionicCan.onWriteProgress += trionicCan_onWriteProgress;
            trionicCan.onCanInfo += trionicCan_onCanInfo;

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
            catch (Exception)
            {
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
                        }
                        catch (Exception)
                        {
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

        void trionicCan_onWriteProgress(object sender, TrionicCANLib.TrionicCan.WriteProgressEventArgs e)
        {
            UpdateProgressStatus(e.Percentage);
        }

        void trionicCan_onCanInfo(object sender, TrionicCANLib.TrionicCan.CanInfoEventArgs e)
        {
            UpdateFlashStatus(e);
        }

        void trionicCan_onReadProgress(object sender, TrionicCANLib.TrionicCan.ReadProgressEventArgs e)
        {
            UpdateProgressStatus(e.Percentage);
        }

        private void cbxAdapterType_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableUserInput(true);
        }

        private void btnReadDTC_Click(object sender, EventArgs e)
        {
            GetUIOptions();
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {               
                trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                SetT8AdapterType();

                EnableUserInput(false);
                AddLogItem("Opening connection");
                if (trionicCan.openDevice(false))
                {
                    string[] codes = trionicCan.readDTCCodes();
                    foreach (string a in codes)
                    {
                        AddLogItem(a);
                    }
                }

                trionicCan.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {

        }

        private void btnSetECUVIN_Click(object sender, EventArgs e)
        {
            GetUIOptions();
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {               
                trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                SetT8AdapterType();

                EnableUserInput(false);
                AddLogItem("Opening connection");
                if (trionicCan.openDevice(true))
                {
                    string vin = tbParameter.Text;
                    if (vin.Length == 17)
                    {
                        AddLogItem("setECUparameterVIN:");
                        trionicCan.setECUparameterVIN(vin);
                    }
                    else
                    {
                        AddLogItem("Error expected VIN length 17");
                    }
                }

                trionicCan.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
        }

        private void btnSecurityAccess_Click(object sender, EventArgs e)
        {
            GetUIOptions();
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {                
                trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                SetT8AdapterType();

                EnableUserInput(false);
                AddLogItem("Opening connection");
                trionicCan.openDevice(true);
                EnableUserInput(true);
            }
        }

        private void btnSetE85_Click(object sender, EventArgs e)
        {
            GetUIOptions();
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                SetT7AdapterType();

                EnableUserInput(false);
                AddLogItem("Opening connection");
                if (trionicCan.openT7Device())
                {
                    int e85;
                    if (int.TryParse(tbParameter.Text, out e85))
                    {
                        AddLogItem("SetE85:" + trionicCan.SetE85LevelT7(e85));
                    }
                }

                trionicCan.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            else if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {
                trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                SetT8AdapterType();

                EnableUserInput(false);
                AddLogItem("Opening connection");
                if (trionicCan.openDevice(true))
                {
                    float e85;
                    if (float.TryParse(tbParameter.Text, out e85))
                    {
                        AddLogItem("SetE85:" + trionicCan.setECUparameterE85(e85));
                    }
                }

                trionicCan.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
        }

        private void GetUIOptions()
        {
            trionicCan.EnableCanLog = cbEnableLogging.Checked;
            trionicCan.OnlyPBus = cbOnlyPBus.Checked;
            trionicCan.DisableCanConnectionCheck = cbDisableConnectionCheck.Checked;
        }

        private void btnSetSpeed_Click(object sender, EventArgs e)
        {
            GetUIOptions();
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC8)
            {               
                trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                SetT8AdapterType();

                EnableUserInput(false);
                AddLogItem("Opening connection");
                if (trionicCan.openDevice(true))
                {
                    int speed;
                    if (int.TryParse(tbParameter.Text, out speed))
                    {
                        AddLogItem("SetSpeed:" + trionicCan.SetSpeedLimiter(speed));
                    }
                }

                trionicCan.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
        }

        private void btnResetECU_Click(object sender, EventArgs e)
        {
            GetUIOptions();
            if (cbxEcuType.SelectedIndex == (int)ECU.TRIONIC7)
            {
                
                SetT7AdapterType();

                AddLogItem("Opening connection");
                EnableUserInput(false);

                if (trionicCan.openT7Device())
                {
                    Thread.Sleep(1000);
                    AddLogItem("Reset T7");
                    Application.DoEvents();
                    trionicCan.ResetT7();
                }
                else
                {
                    AddLogItem("Unable to connect to Trionic 7 ECU");
                }
                trionicCan.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
        }

        private void updateStatusInBox(TrionicCANLib.TrionicCan.CanInfoEventArgs e)
        {
            AddLogItem(e.Info);
            if (e.Type == ActivityType.FinishedFlashing || e.Type == ActivityType.FinishedDownloadingFlash)
            {
                TimeSpan ts = DateTime.Now - dtstart;
                AddLogItem("Total duration: " + ts.Minutes + " minutes " + ts.Seconds + " seconds");
                trionicCan.Cleanup();
                EnableUserInput(true);
            }
        }

        private void UpdateFlashStatus(TrionicCANLib.TrionicCan.CanInfoEventArgs e)
        {
            try
            {
                Invoke(m_DelegateUpdateStatus, e);
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
        }

        private void updateProgress(float percentage)
        {
            if (progressBar1.Value != (int)percentage)
            {
                progressBar1.Value = (int)percentage;
            }
            string text = percentage.ToString("F0") + "%";
            if (label1.Text != text)
            {
                label1.Text = text;
                Application.DoEvents();
            }
        }

        private void UpdateProgressStatus(float percentage)
        {
            try
            {
                Invoke(m_DelegateProgressStatus, percentage);
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
        }

        private void cbxEcuType_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableUserInput(true);
        }

    }
}
