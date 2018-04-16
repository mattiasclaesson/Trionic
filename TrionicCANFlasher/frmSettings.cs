using System;
using System.ComponentModel;
using System.Windows.Forms;
using TrionicCANLib.API;
using Microsoft.Win32;
using NLog;

namespace TrionicCANFlasher
{
    public partial class frmSettings : Form
    {
        private Logger logger = LogManager.GetCurrentClassLogger();
        private m_adaptertype _m_adaptertype = new m_adaptertype();
        private m_interframe _m_interframe = new m_interframe();
        private m_adapter _m_adapter = new m_adapter();
        private m_selecu _m_selecu = new m_selecu();
        private m_baud _m_baud = new m_baud();

        // Default settings
        private bool m_fullscreen = false;
        private bool m_collapsed  = false;
        private int  m_width  = -1;
        private int  m_height = -1;

        private bool m_enablelog = true;  // "Enable logging"
        private bool m_onlypbus  = true;  // "Only P-Bus connection"
        private bool m_onbflash  = true;  // "Use flasher on device" (CombiAdapter)
        private bool m_uselegion = true;  // "Use Legion bootloader"
        private bool m_poweruser = false; // "I am a power user"
        private bool m_unlocksys = false; // "Unlock system partitions"
        private bool m_unlckboot = false; // "Unlock boot partition"
        private bool m_autocsum  = false; // "Auto update checksum"

        // Hidden features
        private bool cbEnableSUFeatures = false; // This mode does not have a checkbox
        private int  m_hiddenclicks     = 5;     // Click this many times + 1 to enable su features
        private bool m_enablesufeatures = false; // enable / disable su features
        private bool m_verifychecksum   = true;  // Check checksum of file before flashing
        private bool m_uselastpointer   = true;  // Legion. Use the "last address of bin" feature or just regular partition md5
        private bool m_faster           = false; // Legion. Speed up certain tasks

        // Used to lock out SettingsLogic while populating items
        private bool m_lockout = true;

        public bool VerifyChecksum
        {
            get { return m_verifychecksum;  }
        }

        public bool Faster
        {
            get { return m_faster;  }
        }

        public bool UseLastMarker
        {
            get { return m_uselastpointer; }
        }

        public class m_interframe
        {
            private string m_name = "1200 (Default)";
            private int m_index = 9;
            private static uint[] m_dels = 
            {
                300, 400, 500, 600, 700,
                800, 900,1000,1100,1200, // (Default)
               1300,1400,1500,1600,1700,
               1800,1900,2000
            };

            public int Index
            {
                get { return m_index; }
                set { m_index = value; }
            }

            public string Name
            {
                get { return m_name; }
                set { m_name = value; }
            }

            public uint Value
            {
                get {

                    if (m_index >= 0 && m_index < 18)
                    {
                        return m_dels[m_index];
                    }
                    else
                    {
                        return 1200;
                    }
                }
            }
        }

        public class m_selecu
        {
            private string m_name = null;
            private int m_index = -1;

            public int Index
            {
                get { return m_index; }
                set { m_index = value; }
            }
            public string Name
            {
                get { return m_name; }
                set { m_name = value; }
            }
        }

        public class m_adaptertype
        {
            private string m_name = null;
            private int m_index = -1;

            public int Index
            {
                get { return m_index;  }
                set { m_index = value; }
            }
            public string Name
            {
                get { return m_name;  }
                set { m_name = value; }
            }
        }

        public class m_adapter
        {
            private string m_name = null;
            private int m_index = -1;

            public int Index
            {
                get { return m_index;  }
                set { m_index = value; }
            }
            public string Name
            {
                get { return m_name;  }
                set { m_name = value; }
            }
        }

        public class m_baud
        {
            private string m_name = null;
            private int m_index = -1;

            public int Index
            {
                get { return m_index;  }
                set { m_index = value; }
            }
            public string Name
            {
                get { return m_name;  }
                set { m_name = value; }
            }
        }

        public m_selecu SelectedECU
        {
            get { return _m_selecu;  }
            set { _m_selecu = value; }
        }

        public m_adaptertype AdapterType
        {
            get { return _m_adaptertype;  }
            set { _m_adaptertype = value; }
        }

        public m_adapter Adapter
        {
            get { return _m_adapter;  }
            set { _m_adapter = value; }
        }

        public m_baud Baudrate
        {
            get { return _m_baud;  }
            set { _m_baud = value; }
        }

        public m_interframe InterframeDelay
        {
            get { return _m_interframe;  }
            set { _m_interframe = value; }
        }


        public int MainWidth
        {
            get { return m_width;  }
            set { m_width = value; }
        }

        public int MainHeight
        {
            get { return m_height;  }
            set { m_height = value; }
        }

        public bool Fullscreen
        {
            get { return m_fullscreen;  }
            set { m_fullscreen = value; }
        }

        public bool Collapsed
        {
            get { return m_collapsed; }
            set { m_collapsed = value; }
        }

        public bool CombiFlasher
        {
            get { return m_onbflash;  }
            set { m_onbflash = value; }
        }

        public bool OnlyPBus
        {
            get { return m_onlypbus;  }
            set { m_onlypbus = value; }
        }

        public bool EnableLogging
        {
            get { return m_enablelog;  }
            set { m_enablelog = value; }
        }

        public bool UseLegion
        {
            get { return m_uselegion;  }
            set { m_uselegion = value; }
        }

        public bool PowerUser
        {
            get { return m_poweruser;  }
            set { m_poweruser = value; }
        }

        public bool UnlockSys
        {
            get { return m_unlocksys;  }
            set { m_unlocksys = value; }
        }

        public bool UnlockBoot
        {
            get { return m_unlckboot;  }
            set { m_unlckboot = value; }
        }

        public bool AutoChecksum
        {
            get { return m_autocsum;  }
            set { m_autocsum = value; }
        }

        private void LoadItems()
        {
            // Lock out logics while populating items
            m_lockout = true;

            try
            {
                if (AdapterType.Name != null)
                {
                    cbxAdapterType.SelectedItem = AdapterType.Name;
                }

                if (Adapter.Name != null)
                {
                    cbxAdapterItem.SelectedItem = Adapter.Name;
                }

                if (Baudrate.Name != null)
                {
                    cbxComSpeed.SelectedItem = Baudrate.Name;
                }

                if (InterframeDelay.Name != null)
                {
                    cbxInterFrame.SelectedItem = InterframeDelay.Name;
                }
            }

            catch (Exception ex)
            {
                logger.Debug(ex.Message);
            }

            cbOnlyPBus.Checked = m_onlypbus;
            cbEnableLogging.Checked = m_enablelog;
            cbUseLegion.Checked = m_uselegion;
            cbOnboardFlasher.Checked = m_onbflash;

            cbPowerUser.Checked = m_poweruser;
            cbUnlockSys.Checked = m_unlocksys;
            cbUnlockBoot.Checked = m_unlckboot;
            cbAutoChecksum.Checked = m_autocsum;

            // This is not a real checkbox.
            cbEnableSUFeatures = m_enablesufeatures;

            cbUseLastPointer.Checked = m_uselastpointer;
            cbCheckChecksum.Checked = m_verifychecksum;
            cbFasterDamnit.Checked = m_faster;

            m_lockout = false;
        }

        private void StoreItems()
        {
            try
            {
                if (cbxAdapterType.SelectedIndex >= 0)
                {
                    AdapterType.Index = cbxAdapterType.SelectedIndex;
                    AdapterType.Name = cbxAdapterType.SelectedItem.ToString();
                }

                if (cbxAdapterItem.SelectedIndex >= 0)
                {
                    Adapter.Index = cbxAdapterItem.SelectedIndex;
                    Adapter.Name = cbxAdapterItem.SelectedItem.ToString();
                }

                if (cbxComSpeed.SelectedIndex >= 0)
                {
                    Baudrate.Index = cbxComSpeed.SelectedIndex;
                    Baudrate.Name = cbxComSpeed.SelectedItem.ToString();
                }

                if (cbxInterFrame.SelectedIndex >= 0)
                {
                    InterframeDelay.Index = cbxInterFrame.SelectedIndex;
                    InterframeDelay.Name = cbxInterFrame.SelectedItem.ToString();
                }
            }

            catch (Exception ex)
            {
                logger.Debug(ex.Message);
            }

            m_onlypbus = cbOnlyPBus.Checked;
            m_enablelog = cbEnableLogging.Checked;
            m_uselegion = cbUseLegion.Checked;
            m_onbflash = cbOnboardFlasher.Checked;

            m_poweruser = cbPowerUser.Checked;
            m_unlocksys = cbUnlockSys.Checked;
            m_unlckboot = cbUnlockBoot.Checked;
            m_autocsum = cbAutoChecksum.Checked;

            // This is not a real checkbox.
            m_enablesufeatures = cbEnableSUFeatures;

            m_uselastpointer = cbUseLastPointer.Checked;
            m_verifychecksum = cbCheckChecksum.Checked;
            m_faster = cbFasterDamnit.Checked;
        }

        public void LoadRegistrySettings()
        {
            // Fetch adapter types from TrionicCANLib.API
            cbxAdapterType.Items.Clear();

            foreach (var AdapterType in Enum.GetValues(typeof(CANBusAdapter)))
            {
                try
                {
                    cbxAdapterType.Items.Add(((DescriptionAttribute)AdapterType.GetType().GetField(AdapterType.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false)[0]).Description.ToString());
                }

                catch (Exception ex)
                {
                    logger.Debug(ex.Message);
                }
            }

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
                            if (a == "AdapterType")
                            {
                                AdapterType.Name = Settings.GetValue(a).ToString();
                            }
                            else if (a == "Adapter")
                            {
                                Adapter.Name = Settings.GetValue(a).ToString();
                            }
                            else if (a == "ComSpeed")
                            {
                                Baudrate.Name = Settings.GetValue(a).ToString();
                            }
                            else if (a == "ECU")
                            {
                                SelectedECU.Name = Settings.GetValue(a).ToString();
                            }

                            else if (a == "EnableLogging")
                            {
                                m_enablelog = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "OnboardFlasher")
                            {
                                m_onbflash = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "OnlyPBus")
                            {
                                m_onlypbus = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "UseLegionBootloader")
                            {
                                m_uselegion = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }

                            else if (a == "PowerUser")
                            {
                                m_poweruser = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "FormatSystemPartitions")
                            {
                                m_unlocksys = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "FormatBootPartition")
                            {
                                m_unlckboot = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "AutoChecksum")
                            {
                                m_autocsum = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }

                            else if (a == "SuperUser")
                            {
                                m_enablesufeatures = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "UseLastAddressPointer")
                            {
                                m_uselastpointer = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }

                            else if (a == "ViewWidth")
                            {
                                m_width = Convert.ToInt32(Settings.GetValue(a).ToString());
                            }
                            else if (a == "ViewHeight")
                            {
                                m_height = Convert.ToInt32(Settings.GetValue(a).ToString());
                            }
                            else if (a == "ViewFullscreen")
                            {
                                m_fullscreen = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                            else if (a == "ViewCollapsed")
                            {
                                m_collapsed = Convert.ToBoolean(Settings.GetValue(a).ToString());
                            }
                        }

                        catch (Exception ex)
                        {
                            logger.Debug(ex.Message);
                        }
                    }
                }
            }

            try
            {
                if (AdapterType.Name != null)
                {
                    cbxAdapterType.SelectedItem = AdapterType.Name;
                    AdapterType.Index = cbxAdapterType.SelectedIndex;
                }

                if (Adapter.Name != null)
                {
                    cbxAdapterItem.SelectedItem = Adapter.Name;
                    Adapter.Index = cbxAdapterItem.SelectedIndex;
                }

                if (Baudrate.Name != null)
                {
                    cbxComSpeed.SelectedItem = Baudrate.Name;
                    Baudrate.Index = cbxComSpeed.SelectedIndex;
                }
            }

            catch (Exception ex)
            {
                logger.Debug(ex.Message);
            }

            /////////////////////////////////////////////
            // Recover from strange settings in registry

            // Make sure settings are returned to safe values in case power user is not enabled
            if (!m_poweruser)
            {
                m_unlocksys = false;
                m_unlckboot = false;
                m_autocsum = false;

                m_enablesufeatures = false;
            }

            // We have to plan this section.. 
            if (!m_enablesufeatures)
            {
                m_verifychecksum = true;
                m_uselastpointer = true;
                m_faster = false;
                InterframeDelay.Index = 9;
            }

            // Maybe we should have different unlock sys for ME9 and T8?
            if (!m_unlocksys)
            {
                m_unlckboot = false;
            }

            // This should never happen but here it is. Just in case
            if (m_fullscreen && m_collapsed)
            {
                m_collapsed = false;
                m_fullscreen = false;
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

        public void SaveRegistrySettings()
        {
            SaveRegistrySetting("AdapterType", AdapterType.Name != null ? AdapterType.Name : String.Empty);
            SaveRegistrySetting("Adapter", Adapter.Name != null ? Adapter.Name : String.Empty);
            SaveRegistrySetting("ComSpeed", Baudrate.Name != null ? Baudrate.Name : String.Empty);
            SaveRegistrySetting("ECU", SelectedECU.Name != null ? SelectedECU.Name : String.Empty);

            SaveRegistrySetting("EnableLogging", m_enablelog);
            SaveRegistrySetting("OnboardFlasher", m_onbflash);
            SaveRegistrySetting("OnlyPBus", m_onlypbus);
            SaveRegistrySetting("UseLegionBootloader", m_uselegion);

            SaveRegistrySetting("PowerUser", m_poweruser);
            SaveRegistrySetting("FormatSystemPartitions", m_unlocksys);
            SaveRegistrySetting("FormatBootPartition", m_unlckboot);
            SaveRegistrySetting("AutoChecksum", m_autocsum);

            SaveRegistrySetting("SuperUser", m_enablesufeatures);
            SaveRegistrySetting("UseLastAddressPointer", m_uselastpointer);

            SaveRegistrySetting("ViewWidth", m_width.ToString());
            SaveRegistrySetting("ViewHeight", m_height.ToString());
            SaveRegistrySetting("ViewFullscreen", m_fullscreen);
            SaveRegistrySetting("ViewCollapsed", m_collapsed);
        }

        private void GetAdapterInformation()
        {
            if (cbxAdapterType.SelectedIndex >= 0)
            {
                logger.Debug("ITrionic.GetAdapterNames selectedIndex=" + cbxAdapterType.SelectedIndex);
                string[] adapters = ITrionic.GetAdapterNames((CANBusAdapter)cbxAdapterType.SelectedIndex);
                cbxAdapterItem.Items.Clear();
                foreach (string adapter in adapters)
                {
                    cbxAdapterItem.Items.Add(adapter);
                    logger.Debug("Adaptername=" + adapter);
                }

                try
                {
                    if (adapters.Length > 0)
                        cbxAdapterItem.SelectedIndex = 0;
                }
                catch (Exception ex)
                {
                    logger.Debug(ex.Message);
                }
            }
        }

        /// <summary>
        /// This method determines what should be enabled / shown depending on what the user has selected
        /// </summary>
        private void SettingsLogic()
        {
            // Check if we're being populated or if the user did something
            if (!m_lockout)
            {

                int typeindex = cbxAdapterType.SelectedIndex;
                int ecuindex = SelectedECU.Index;

                cbOnboardFlasher.Enabled = false;
                cbEnableLogging.Enabled = true;
                cbUseLegion.Enabled = false;
                cbOnlyPBus.Enabled = true;
                cbPowerUser.Enabled = true;
                cbUnlockSys.Enabled = false;
                cbUnlockBoot.Enabled = false;
                cbAutoChecksum.Enabled = false;

                if (cbEnableSUFeatures && cbPowerUser.Checked)
                {
                    cbPowerUser.Text = "I am a super user";
                }
                else
                {
                    cbPowerUser.Text = "I am a power user";
                }

                cbxAdapterItem.Enabled = false;

                if (typeindex == (int)CANBusAdapter.LAWICEL ||
                    typeindex == (int)CANBusAdapter.KVASER ||
                    typeindex == (int)CANBusAdapter.J2534)
                {
                    cbxAdapterItem.Enabled = true;
                }

                if (typeindex == (int)CANBusAdapter.ELM327)
                {
                    cbxAdapterItem.Enabled = true;
                    cbOnboardFlasher.Visible = false;

                    ComBaudLabel.Visible = true;
                    cbxComSpeed.Enabled = true;
                    cbxComSpeed.Visible = true;
                }
                else
                {
                    ComBaudLabel.Visible = false;
                    cbxComSpeed.Enabled = false;
                    cbxComSpeed.Visible = false;

                    cbOnboardFlasher.Visible = true;
                }

                if (typeindex >= 0)
                {
                    if (ecuindex == (int)ECU.TRIONIC5)
                    {
                        cbOnlyPBus.Enabled = false;
                        cbAutoChecksum.Enabled = true;
                    }

                    else if (ecuindex == (int)ECU.TRIONIC7)
                    {
                        cbOnboardFlasher.Enabled = typeindex == (int)CANBusAdapter.COMBI ? cbOnlyPBus.Checked : false;
                        cbAutoChecksum.Enabled = true;
                    }

                    else if (ecuindex == (int)ECU.TRIONIC8)
                    {
                        cbUseLegion.Enabled = true;
                        cbUnlockSys.Enabled = cbUseLegion.Checked;
                        cbUnlockBoot.Enabled = (cbUnlockSys.Checked && cbUnlockSys.Enabled);

                        if (!cbUnlockSys.Checked)
                        {
                            cbUnlockBoot.Checked = false;
                        }
                        cbAutoChecksum.Enabled = true;
                    }

                    else if (ecuindex == (int)ECU.TRIONIC8_MCP)
                    {
                        cbUseLegion.Checked = true;
                        cbUnlockBoot.Enabled = true;
                    }

                    else if (ecuindex == (int)ECU.MOTRONIC96)
                    {
                        cbUnlockSys.Enabled = true;
                    }

                    // Other ECUs do not have power user features
                    else
                    {
                        cbPowerUser.Enabled = cbEnableSUFeatures;
                    }

                    if (!cbPowerUser.Enabled)
                    {
                        cbUnlockSys.Enabled = false;
                        cbUnlockBoot.Enabled = false;
                        cbAutoChecksum.Enabled = false;
                    }

                    if (!cbPowerUser.Checked)
                    {
                        cbUnlockSys.Enabled = false;
                        cbUnlockBoot.Enabled = false;
                        cbAutoChecksum.Enabled = false;
                        cbUnlockSys.Checked = false;
                        cbUnlockBoot.Checked = false;
                        cbAutoChecksum.Checked = false;

                        // Can not be super user without being a power user first
                        cbEnableSUFeatures = false;

                        // Restore safe settings
                        cbUnlockBoot.Checked = false;
                        cbUnlockSys.Checked = false;
                        cbAutoChecksum.Checked = false;
                    }

                    if (cbEnableSUFeatures)
                    {
                        bool precheck = ((ecuindex == (int)ECU.TRIONIC8 && cbUseLegion.Checked && cbUseLegion.Enabled) ||
                            ecuindex == (int)ECU.TRIONIC8_MCP || ecuindex == (int)ECU.Z22SEMain_LEG || ecuindex == (int)ECU.Z22SEMCP_LEG);

                        InterframeLabel.Enabled = precheck;
                        cbxInterFrame.Enabled = precheck;
                        cbUseLastPointer.Enabled = (ecuindex == (int)ECU.TRIONIC8 && cbUseLegion.Checked && cbUseLegion.Enabled);
                        cbFasterDamnit.Enabled = precheck;
                        cbCheckChecksum.Enabled = (ecuindex == (int)ECU.TRIONIC8 || ecuindex == (int)ECU.TRIONIC7 || ecuindex == (int)ECU.TRIONIC5);
                    }
                    else
                    {
                        InterframeLabel.Enabled = false;
                        cbUseLastPointer.Enabled = false;
                        cbFasterDamnit.Enabled = false;
                        cbxInterFrame.Enabled = false;
                        cbCheckChecksum.Enabled = false;

                        // Restore safe settings
                        cbFasterDamnit.Checked = false;
                        cbUseLastPointer.Checked = true;
                        cbCheckChecksum.Checked = true;
                        cbxInterFrame.SelectedIndex = 9;
                    }
                }

                // No adapter selected, blank everything!
                else
                {
                    cbPowerUser.Enabled = false;
                    cbEnableLogging.Enabled = false;
                    cbAutoChecksum.Enabled = false;
                    cbOnlyPBus.Enabled = false;
                }
            }
        }

        /// <summary>
        /// Dialog has been shown. Now populate it with current settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DialogShown(object sender, EventArgs e)
        {
            // Reset click counter
            if (m_hiddenclicks > 0)
            {
                m_hiddenclicks = 5;
            }

            // Restore the regular label
            label2.Text = "Advanced features";

            LoadItems();
            SettingsLogic();
        }

        private void cbxAdapterType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbxAdapterType.SelectedIndex == (int)CANBusAdapter.JUST4TRIONIC)
            {
                cbxComSpeed.SelectedIndex = (int)ComSpeed.S115200;
            }

            // Prevent checkboxes from popping in and out as the adapter name is loaded
            SettingsLogic();

            GetAdapterInformation();

            // Now perform a real check
            SettingsLogic();
        }

        private void cbOnlyPBus_Checkchanged(object sender, EventArgs e)
        {
            SettingsLogic();
        }

        private void cbUseLegion_CheckedChanged(object sender, EventArgs e)
        {
            SettingsLogic();
        }

        private void cbUnlockSys_CheckedChanged(object sender, EventArgs e)
        {
            SettingsLogic();
        }

        private void cbPowerUser_CheckedChanged(object sender, EventArgs e)
        {
            // Ignore changes made by "LoadItems"
            if (!m_lockout)
            {
                if (cbEnableSUFeatures && !cbPowerUser.Checked)
                {
                    label2.Text = "Advanced features";
                    cbPowerUser.Checked = true;
                    cbEnableSUFeatures = false;
                    m_hiddenclicks = 5;
                }

                SettingsLogic();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            StoreItems();
            this.Close();
        }

        private void bntDiscard_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        
        /// <summary>
        /// Enable super user options by clicking "Advanced features" 6 times
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void label2_Click(object sender, EventArgs e)
        {
            if (!cbEnableSUFeatures && cbPowerUser.Checked)
            {
                if (m_hiddenclicks > 0)
                {
                    m_hiddenclicks--;
                }
                else
                {
                    label2.Text = "You are in deep water now..";
                    cbEnableSUFeatures = true;
                    m_hiddenclicks = 5;
                    SettingsLogic();  
                }
            }

            // Feature is already enabled
            else if (cbPowerUser.Checked)
            {
                if (m_hiddenclicks > 0)
                {
                    m_hiddenclicks--;
                }
                else
                {
                    m_hiddenclicks = 5;
                    label2.Text = "Already a super user";
                }
            }
        }

        public frmSettings()
        {
            InitializeComponent();
        }
    }
}
