using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.IO.Ports;
using Microsoft.Win32;
using TrionicCANLib;

namespace T8CANFlasher
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
            InitializeComponent();
            m_DelegateUpdateStatus = updateStatusInBox;
            m_DelegateProgressStatus = updateProgress;

            EnableUserInput(true);
        }

        private void AddLogItem(string item)
        {
            item = DateTime.Now.ToString("HH:mm:ss.fff") + " - " + item;
            listBox1.Items.Add(item);
            while (listBox1.Items.Count > 100) listBox1.Items.RemoveAt(0);
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
            if (checkBox1.Checked)
            {

                lock (this)
                {
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(Application.StartupPath + "\\log.txt", true))
                        {
                            sw.WriteLine(item);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            Application.DoEvents();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex == (int)ECU.TRIONIC7)
            {
                using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Bin files|*.bin", Title = "Select binary file to flash...", Multiselect = false })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        SetT7AdapterType();
                        trionicCan.EnableCanLog = checkBox1.Checked;
                        trionicCan.OnlyPBus = checkBox2.Checked;
                        trionicCan.DisableCanConnectionCheck = checkBox3.Checked;
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
                }
            }
            else if (comboBox3.SelectedIndex == (int)ECU.TRIONIC8)
            {
                using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Binary files|*.bin", Multiselect = false })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        SetT8AdapterType();
                        trionicCan.EnableCanLog = checkBox1.Checked;
                        trionicCan.OnlyPBus = checkBox2.Checked;
                        trionicCan.DisableCanConnectionCheck = checkBox3.Checked;
                        trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                        AddLogItem("Opening connection");
                        EnableUserInput(false);
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
            button1.Enabled = enable;
            button4.Enabled = enable;
            button5.Enabled = enable;
            button6.Enabled = enable;
            button7.Enabled = enable;
            button8.Enabled = enable;
            button9.Enabled = enable;
            button11.Enabled = enable;
            button13.Enabled = enable;
            button14.Enabled = enable;
            button15.Enabled = enable;
            comboBox1.Enabled = enable;
            comboBox2.Enabled = enable;
            comboBox3.Enabled = enable;
            label1.Enabled = enable;
            textBox2.Enabled = enable;
            checkBox1.Enabled = enable;
            checkBox2.Enabled = enable;
            checkBox3.Enabled = enable;

            if (comboBox1.SelectedIndex == (int)CANBusAdapter.ELM327 ||
                comboBox1.SelectedIndex == (int)CANBusAdapter.JUST4TRIONIC)
            {
                comboBox2.Enabled = true;
            }
            else
            {
                comboBox2.Enabled = false;
            }

            // Always disable
            if (comboBox3.SelectedIndex == (int)ECU.TRIONIC7)
            {
                button6.Enabled = false;
                button7.Enabled = false;
                button8.Enabled = false;
                button9.Enabled = false;
                button11.Enabled = false;
                button14.Enabled = false;
                button15.Enabled = false;

                if (comboBox1.SelectedIndex == (int)CANBusAdapter.COMBI)
                {
                    button5.Enabled = false;
                    button13.Enabled = false;
                }

                if (comboBox1.SelectedIndex == (int)CANBusAdapter.ELM327)
                {
                    button1.Enabled = false;
                    button4.Enabled = false;
                    button5.Enabled = false;
                    button13.Enabled = false;
                }
            }
            else if (comboBox3.SelectedIndex == (int)ECU.TRIONIC8)
            {
                if (comboBox1.SelectedIndex == (int)CANBusAdapter.ELM327)
                {
                    button7.Enabled = false;
                    button9.Enabled = false;
                    button11.Enabled = false;
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex == (int)ECU.TRIONIC7)
            {
                SetT7AdapterType();
                trionicCan.EnableCanLog = checkBox1.Checked;
                trionicCan.OnlyPBus = checkBox2.Checked;
                trionicCan.DisableCanConnectionCheck = checkBox3.Checked;
                AddLogItem("Opening connection");
                EnableUserInput(false);

                if (trionicCan.openT7Device())
                {
                    using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "Bin files|*.bin" })
                    {
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            // check reading status periodically
                            if (sfd.FileName != string.Empty)
                            {
                                if (Path.GetFileName(sfd.FileName) != string.Empty)
                                {
                                    Thread.Sleep(1000);
                                    AddLogItem("Aquiring flash content");
                                    Application.DoEvents();
                                    dtstart = DateTime.Now;
                                    trionicCan.getFlashWithT7Flasher(sfd.FileName);
                                }
                            }
                        }
                    }
                }
                else
                {
                    AddLogItem("Unable to connect to Trionic 7 ECU");
                }
            }
            else if (comboBox3.SelectedIndex == (int)ECU.TRIONIC8)
            {
                using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "Binary files|*.bin" })
                {
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        SetT8AdapterType();
                        trionicCan.EnableCanLog = checkBox1.Checked;
                        trionicCan.OnlyPBus = checkBox2.Checked;
                        trionicCan.DisableCanConnectionCheck = checkBox3.Checked;
                        trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                        AddLogItem("Opening connection");
                        EnableUserInput(false);
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

        private void button5_Click(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex == (int)ECU.TRIONIC7)
            {
                SetT7AdapterType();
                trionicCan.EnableCanLog = checkBox1.Checked;
                trionicCan.OnlyPBus = checkBox2.Checked;
                trionicCan.DisableCanConnectionCheck = checkBox3.Checked;
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
            else if (comboBox3.SelectedIndex == (int)ECU.TRIONIC8)
            {
                SetT8AdapterType();
                trionicCan.EnableCanLog = checkBox1.Checked;
                trionicCan.OnlyPBus = checkBox2.Checked;
                trionicCan.DisableCanConnectionCheck = checkBox3.Checked;

                EnableUserInput(false);
                AddLogItem("Opening connection");
                trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
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

        private void button6_Click(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex == (int)ECU.TRIONIC8)
            {
                using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "SRAM snapshots|*.RAM" })
                {
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        SetT8AdapterType();
                        trionicCan.EnableCanLog = checkBox1.Checked;
                        trionicCan.OnlyPBus = checkBox2.Checked;
                        trionicCan.DisableCanConnectionCheck = checkBox3.Checked;
                        trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                        AddLogItem("Opening connection");
                        EnableUserInput(false);
                        if (trionicCan.openDevice(false))
                        {
                            Thread.Sleep(1000);
                            dtstart = DateTime.Now;
                            AddLogItem("Aquiring snapshot");
                            Application.DoEvents();
                            byte[] snapshot = trionicCan.getSRAMSnapshotWithBootloader();
                            //byte[] snapshot = trionicCan.getSRAMSnapshot();
                            try
                            {
                                File.WriteAllBytes(sfd.FileName, snapshot);
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

        private void button7_Click(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex == (int)ECU.TRIONIC8)
            {
                using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Binary files|*.bin", Multiselect = false })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        SetT8AdapterType();
                        trionicCan.EnableCanLog = checkBox1.Checked;
                        trionicCan.OnlyPBus = checkBox2.Checked;
                        trionicCan.DisableCanConnectionCheck = checkBox3.Checked;
                        trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                        AddLogItem("Opening connection");
                        EnableUserInput(false);
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

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveRegistrySetting("AdapterType", comboBox1.SelectedItem.ToString());
            try
            {
                SaveRegistrySetting("Comport", comboBox2.SelectedItem.ToString());
            } catch (Exception) {}
            SaveRegistrySetting("ECU", comboBox3.SelectedItem.ToString());
            SaveRegistrySetting("EnableLogging", checkBox1.Checked);
            SaveRegistrySetting("OnlyPBus", checkBox2.Checked);
            SaveRegistrySetting("DisableCanCheck", checkBox3.Checked);
            trionicCan.Cleanup();
            Environment.Exit(0);
        }

        private void SetT8AdapterType()
        {
            if (comboBox1.SelectedIndex == (int)CANBusAdapter.ELM327 ||
                comboBox1.SelectedIndex == (int)CANBusAdapter.JUST4TRIONIC)
            {
                trionicCan.ForcedComport = comboBox2.SelectedItem.ToString();
            }
            trionicCan.setCANDevice((CANBusAdapter)comboBox1.SelectedIndex);
        }

        private void SetT7AdapterType()
        {
            if (comboBox1.SelectedIndex == (int)CANBusAdapter.ELM327 ||
                comboBox1.SelectedIndex == (int)CANBusAdapter.JUST4TRIONIC)
            {
                trionicCan.ForcedComport = comboBox2.SelectedItem.ToString();
            }
            trionicCan.setT7CANDevice((CANBusAdapter)comboBox1.SelectedIndex);
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            Application.DoEvents();

            SerialPortInformation();

            comboBox1.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;

            // get additional info from registry if available
            LoadRegistrySettings();
            Application.DoEvents();

            trionicCan.onReadProgress += trionicCan_onReadProgress;
            trionicCan.onWriteProgress += trionicCan_onWriteProgress;
            trionicCan.onCanInfo += trionicCan_onCanInfo;

            EnableUserInput(true);
        }

        private void SerialPortInformation()
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
                comboBox2.Items.Add(FilterString(port));
            try
            {
                if (ports.Length > 0)
                    comboBox2.SelectedIndex = 0;
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
            RegistryKey TempKey = Registry.CurrentUser.CreateSubKey("Software");

            using (RegistryKey Settings = TempKey.CreateSubKey("TrionicCANFlasher"))
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
                                comboBox2.SelectedItem = Settings.GetValue(a).ToString();
                            }
                            else if (a == "AdapterType")
                            {
                                comboBox1.SelectedItem = Settings.GetValue(a).ToString();
                            }
                            else if (a == "ECU")
                            {
                                comboBox3.SelectedItem = Settings.GetValue(a).ToString();
                            }
                            else if (a == "EnableLogging")
                            {
                                checkBox1.Checked = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "OnlyPBus")
                            {
                                checkBox2.Checked = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "DisableCanCheck")
                            {
                                checkBox3.Checked = Convert.ToBoolean(Settings.GetValue(a).ToString());
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
            RegistryKey TempKey = Registry.CurrentUser.CreateSubKey("Software");
            using (RegistryKey saveSettings = TempKey.CreateSubKey("TrionicCANFlasher"))
            {
                saveSettings.SetValue(key, value);
            }
        }

        private void SaveRegistrySetting(string key, bool value)
        {
            RegistryKey TempKey = Registry.CurrentUser.CreateSubKey("Software");
            using (RegistryKey saveSettings = TempKey.CreateSubKey("TrionicCANFlasher"))
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

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableUserInput(true);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex == (int)ECU.TRIONIC8)
            {
                SetT8AdapterType();
                trionicCan.EnableCanLog = checkBox1.Checked;

                EnableUserInput(false);
                AddLogItem("Opening connection");
                trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
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

        private void button11_Click(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex == (int)ECU.TRIONIC8)
            {
                SetT8AdapterType();
                trionicCan.EnableCanLog = checkBox1.Checked;

                EnableUserInput(false);
                AddLogItem("Opening connection");
                trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                if (trionicCan.openDevice(true))
                {
                    string vin = textBox2.Text;
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

        private void button8_Click(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex == (int)ECU.TRIONIC8)
            {
                SetT8AdapterType();
                trionicCan.EnableCanLog = checkBox1.Checked;

                EnableUserInput(false);
                AddLogItem("Opening connection");
                trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                trionicCan.openDevice(true);
                EnableUserInput(true);
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex == (int)ECU.TRIONIC7)
            {
                SetT7AdapterType();
                trionicCan.EnableCanLog = checkBox1.Checked;
                trionicCan.OnlyPBus = checkBox2.Checked;
                trionicCan.DisableCanConnectionCheck = checkBox3.Checked;

                EnableUserInput(false);
                AddLogItem("Opening connection");
                if (trionicCan.openT7Device())
                {
                    int e85;
                    if (int.TryParse(textBox2.Text, out e85))
                    {
                        AddLogItem("SetE85:" + trionicCan.SetE85LevelT7(e85));
                    }
                }

                trionicCan.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
            else if (comboBox3.SelectedIndex == (int)ECU.TRIONIC8)
            {
                SetT8AdapterType();
                trionicCan.EnableCanLog = checkBox1.Checked;

                EnableUserInput(false);
                AddLogItem("Opening connection");
                trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                if (trionicCan.openDevice(true))
                {
                    float e85;
                    if (float.TryParse(textBox2.Text, out e85))
                    {
                        AddLogItem("SetE85:" + trionicCan.setECUparameterE85(e85));
                    }
                }

                trionicCan.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex == (int)ECU.TRIONIC8)
            {
                SetT8AdapterType();
                trionicCan.EnableCanLog = checkBox1.Checked;

                EnableUserInput(false);
                AddLogItem("Opening connection");
                trionicCan.SecurityLevel = TrionicCANLib.AccessLevel.AccessLevel01;
                if (trionicCan.openDevice(true))
                {
                    int speed;
                    if (int.TryParse(textBox2.Text, out speed))
                    {
                        AddLogItem("SetSpeed:" + trionicCan.SetSpeedLimiter(speed));
                    }
                }

                trionicCan.Cleanup();
                AddLogItem("Connection closed");
                EnableUserInput(true);
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            int input;
            if (int.TryParse(textBox2.Text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out input))
            {
                byte[] seed = new byte[2];
                seed[0] = Convert.ToByte(input / 256);
                seed[1] = Convert.ToByte(input - (int)seed[0] * 256);

                SeedToKey s2k = new SeedToKey();
                byte[] key = s2k.calculateKey(seed, AccessLevel.AccessLevel01);
                AddLogItem("Key (" + key[0].ToString("X2") + key[1].ToString("X2") + ") calculated from seed (" + seed[0].ToString("X2") + seed[1].ToString("X2") + ")");
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
            string text = percentage.ToString("F2") + " % done";
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

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableUserInput(true);
        }
    }
}
