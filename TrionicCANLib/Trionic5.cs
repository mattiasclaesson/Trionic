//Bootloaders allemaal aanpassen en als resource in de DLL stoppen. DLL dan obfuscaten!
//Bootloaders all adapt and as a resource in the DLL stop. DLL then obfuscaten!

// 
// (c) 2009,2010 by General Failure and Dilemma
// portions (c) 2010 by Sophie Dexter
//
//

/* Immeasurably enhanced by Sophie Dexter 17/09/2010
 * I have changed (nearly) everything :o)
 * 
 * Used google translate to translate your TODO comments lol
 * Now only use MyBooty whenever a bootloader is needed
 * Added understanding of S9 messages to UploadBootLoader so that MyBooty can be used!!!
 * UploadBootLoader no longer accepts the optional boot address
 * Added ProgramFlashBin so that BIN files can be used without the need to convert to S19 files
 * Changed UpgradeECU to use ProgramFlashBin instead of converting BIN files to S19 files.
 * Changed sendReadCommand to accept a Uint32 instead of Uint16 so that DumpECU can use it.
 * Changed to DumpECU to use C7 type commands for dumping the FLASH (needs MyBooty to uploaded
 * to th ECU to be able to dump FLASH addresses)
 * Changed GetChipTypes to return offset address and flash chip type so that GetECUType _could_
 * use it instead of dumping the footer. GetChipTypes returns 6 bytes similar to sendC3Command,
 * the offset address (0x40000/0x6000) is in the same position as sendC3Command puts the last
 * used address (0x7FFFF)
 * 
 * Deleted some functions that are no longer needed !!!
 * 
 * Some things yet to do (not essential)
 * Change GetECUType to use GetChipTypes instead of reading the footer - I haven't attempted
 * this because I'm not entirely sure what the flashfile parameter is used for. However, I'm sure
 * that GetECUType will be a lot simpler if GetChipTypes is used.
 * readRAM could be changed to work with Uint32 addresses so that DumpECU can call it instead
 * of more or less duplicating the same code as readRAM, but I haven't changed it because readRAM
 * is a public function and I don't know if changing the variable types would mess up other things
 * that didn't expect anything to change!
 * 
 */


/********************************************************************************

WARNING: Use at your own risk, sadly this software comes with no guarantees.
This software is provided 'free' and in good faith, but the author does not
accept liability for any damage arising from its use.

********************************************************************************/



using System;
using System.Collections.Generic;
using System.Text;
using TrionicCANLib;
using TrionicCANLib.API;
using TrionicCANLib.CAN;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using TrionicCANLib.Checksum;





using System.Linq;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections;
using NLog;


namespace TrionicCANLib.API
{
    public class Trionic5 : ITrionic
    {
        private Logger logger = LogManager.GetCurrentClassLogger();
        private System.Timers.Timer tmr = new System.Timers.Timer(2000);
        private CANListener m_canListener;

        // Made global as an attempt to make things consistent.
        private const int ChecksumTimeoutmS = 10000;
        private const int GeneralTimeoutmS  =  1000;
        private const int EraseTimeoutmS    = 60000;

        private string CR = "\r";
        private string NL = "\n";
        private string TIMEOUT = "timeout";
        private const string MyBooty = @"S00E000004598B4E0A93E8A3FE93738F
S12350004EB8500C4EB8507A4EED0004207C00FFFA0410FC007F0810000367FA33FC040543
S123502000FFFA4833FC040500FFFA5033FC040500FFFA5433FC303000FFFA5233FC50309C
S123504000FFFA560279FFBF00FFFC140079001000FFFC1402798FDF00FFFC16007900505A
S123506000FFFC162A7C000400002C7C00FFFA263C3C55553E3CAAAA4E754EB851A8207C5C
S123508000005754227C0000575C101012C012FC000922FC0808080832BC08085B890C00DB
S12350A000C7670000CE0C00007F630000540C0000A5670000380C0000C0670000800C009F
S12350C000C2670000940C0000C36700009A0C0000C8670000B80C0000C9670000B84EB852
S12350E052100C3800C2575C66904E752018E188801821C0576611D0576A42116000FFE0CD
S1235100247C0000576B700642811218D5C11438576A14D85201B202670A51C8FFF642115E
S12351206000FFBC4EB854A812C06700FFB222840279FFBF00FFFC146000FFA44EB856BE59
S12351404EB8526012C06700FF9622820279FFBF00FFFC146000FF880279FFBF00FFFC14AA
S123516042116000FF7A421922BC0007FFFF6000FF6E2018E18880102440421912D212E22C
S123518012E212E212E212A26000FF544EB856346000FF4C4EB856BE421922CD12F8576464
S12351A012B857656000FF38207C00005754247C00F007FF267C00F0080040C1007C0700D3
S12351C014BC00024A1314BC00120813000067FA14BC001308130006660846C13C863C8740
S12351E060D814BC001316BC00087014741B1480520010D3B00263F614BC001208130000D2
S123520067FA14BC00130813000666D646C14E75227C0000575C247C00F007FF267C00F0A6
S1235220080040C1007C070014BC00024A1314BC0006421314BC00070813000067FA7009B8
S12352407410148052001699B00263F614BC000816BC008814BC000616BC008046C14E7502
S123526010385764123857650C00008966160C0100B8670001040C0100B4670000F470044F
S1235280600002240C000001661E0C010025670000E80C0100A7670000D80C0100206700EB
S12352A0018C7005600002000C000031660E0C0100B4670000BC7006600001EC0C00001F03
S12352C066120C0100D5674E0C01005D67507007600001D40C0000BF660E0C0100B5670086
S12352E0014C7008600001C00C000020660E0C010020670001387009600001AC0C0000378E
S1235300660E0C0100A467000124700A60000198700360000192243C000400006006243CD5
S1235320000200003B8701700000AAAA3B4655543BBC808001700000AAAA3B8701700000C7
S1235340AAAA3B4655543BBC101001700000AAAA323CABE04E7151C9FFFC50C03C863C878D
S1235360B03528FF6600013E538266F060000132243C000400006006243C0002000020026C
S1235380163C0040183C00C03ABCFFFF3ABCFFFF42554A3528FF67247A191B8328FF4235E4
S12353A028FF72154E7151C9FFFC1B8428FF720C4E7151C9FFFC4A3528FF660A3C863C87B4
S12353C0538266CC6004534566D042554A554A0566067002600000D0240050C0163C002057
S12353E0183C00A02A3C03E803E81B8328FF1B8328FF323C55F04E7151C9FFFC1B8428FFA2
S1235400720C4E7151C9FFFC3C863C87B03528FF66084845538266E46004534566CC4255C6
S12354204A554A456700007E6000007670011B8709300000AAAA1B86092055541BBC008015
S123544009300000AAAA1B8709300000AAAA1B86092055541BBC001009300000AAAA3C86E3
S12354603C871A35080008050007662A0805000567EC1A35080008050007661A1B8709303F
S12354800000AAAA1B86092055541BBC00F009300000AAAA600E53806700FF946000000250
S12354A04280600270014E75267C000057662853BBCC6200017C4282142B00041238576543
S12354C00C010020670000EE0C0100D567220C01005D671C0C0100B5670000DA0C010020BE
S12354E0670000D20C0100A4670000CA60000078200C0280000000FF99C0247C000057ECCC
S1235500223C0000010035B418FE18FE558166F6D5C0220215B3180418FF538166F695C0A8
S12355203B8701700000AAAA3B4655543BBCA0A001700000AAAA323C010039B218FE18FE34
S1235540558166F6323C55F04E7151C9FFFC323C0100363218FEB67418FE660000D4558151
S123556066F0600000C8163C0040183C00C0103328040C0000FF67247A19198328FF19800F
S123558028FF72154E7151C9FFFC198428FF720C4E7151C9FFFCB03428FF6606538266CEEF
S12355A06004530566D442554A554A05670000826000007A123C00A0200CD082C0BC0000C1
S12355C0000108400000163328040C0300FF67581B8709300000AAAA1B86092055541B81FE
S12355E009300000AAAA198328FFC63C0080183428FF1A04C83C0080B803672C0805000560
S123560067EC183428FFC83C0080B803671A1B8709300000AAAA1B86092055541BBC00F097
S123562009300000AAAA6008538266964240600270014E75247C0007FFFB4280428142829E
S123564042834284183C00FD12126700006C0C0100FF67000064102AFFFF95C1558AB0047B
S123566066E610321800040000300C40000A65025F00E98A8400530166E8C5430C0400FE81
S12356806704520460C2B682632E0C830007FFFF642652832442428042813C863C87121ACB
S12356A0D081121AD081B5C366F0B0B90007FFFC660642192280600412BC00014E75247CE0
S12356C0000057640079004000FFFC14323C55F04E7151C9FFFC50D550D51ABC009014D523
S12356E014AD000250D550D50C22008967640C120001675E0C12003167580279FFBF00FFED
S1235700FC14323CABE04E7151C9FFFC1B8701700000AAAA1B4655541BBC0090017000005F
S1235720AAAA323C55F04E7151C9FFFC14D514AD00021B8701700000AAAA1B4655541BBC96
S117574000F001700000AAAA323C55F04E7151C9FFFC4E7552
S10A5764000000000000003A
S10457EB00B9
S9035000AC";

        override public void setCANDevice(CANBusAdapter adapterType)
        {
            if (adapterType == CANBusAdapter.LAWICEL)
            {
                canUsbDevice = new CANUSBDevice();
            }
            else if (adapterType == CANBusAdapter.ELM327)
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

            canUsbDevice.UseOnlyPBus = true;
            canUsbDevice.TrionicECU = ECU.TRIONIC5;
            canUsbDevice.onReceivedAdditionalInformation += new ICANDevice.ReceivedAdditionalInformation(canUsbDevice_onReceivedAdditionalInformation);
            canUsbDevice.onReceivedAdditionalInformationFrame += new ICANDevice.ReceivedAdditionalInformationFrame(canUsbDevice_onReceivedAdditionalInformationFrame);
            if (m_canListener == null)
            {
                m_canListener = new CANListener();
            }
            canUsbDevice.addListener(m_canListener);
            canUsbDevice.AcceptOnlyMessageIds = new List<uint> { 0x00c, 0, 0, 0, 0 };
        }

        override public void SetSelectedAdapter(string adapter)
        {
            canUsbDevice.SetSelectedAdapter(adapter);
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
            CastInfoEvent("Open called in Trionic 5", ActivityType.ConvertingFile);
            MM_BeginPeriod(1);
            OpenResult openResult = OpenResult.OpenError;
            try
            {
                openResult = canUsbDevice.open();
            }
            catch (Exception e)
            {
                CastInfoEvent("Exception opening device " + e.ToString(), ActivityType.ConvertingFile);
            }

            if (openResult != OpenResult.OK)
            {
                CastInfoEvent("Open failed in Trionic 5", ActivityType.ConvertingFile);
                canUsbDevice.close();
                MM_EndPeriod(1);
                return false;
            }
            return true;
        }

        public string getSymbolTable()
        {
            string symbolTable = "";

            symbolTable = sendCommand("S", 1);
            symbolTable = sendCommand(CR, "END" + CR + NL);
            return symbolTable;
        }


        private ECUType DetermineECU()
        {
            byte[] footer = getECUFooter();
            byte[] chiptypes = GetChipTypes();

            string romoffset = getIdentifierFromFooter(footer, ECUIdentifier.ROMoffset);

            string flashzize = "256 kB";
            switch (chiptypes[0])
            {
                case 0xB8:      // Intel/CSI/OnSemi 28F512
                case 0x25:      // AMD 28F512
                    flashzize = "128 kB";
                    break;
                case 0x5D:      // Atmel 29C512
                    flashzize = "128 kB";
                    break;
                case 0xB4:      // Intel/CSI/OnSemi 28F010
                case 0xA7:      // AMD 28F010
                    break;
                case 0x20:      // AMD/ST 29F010
                case 0xA4:      // AMIC 29F010
                    break;
                case 0xD5:      // Atmel 29C010
                    break;
                case 0xB5:      // SST 39F010
                    break;
                default:
                    flashzize = "Unknown";
                    break;
            }

            switch (flashzize)
            {
                case "128 kB":
                    switch (romoffset)
                    {
                        case "060000":
                            CastInfoEvent("This is a Trionic 5.2 ECU with 128 kB of FLASH", ActivityType.ConvertingFile);
                            return ECUType.T52ECU;
                        default:
                            CastInfoEvent("!!! ERROR !!! This is a Trionic 5.2 ECU running an unknown firmware", ActivityType.ConvertingFile);
                            return ECUType.Unknown;
                    }
                case "256 kB":
                    switch (romoffset)
                    {
                        case "040000":
                            CastInfoEvent("This is a Trionic 5.5 ECU with 256 kB of FLASH", ActivityType.ConvertingFile);
                            return ECUType.T55ECU;
                        case "060000":
                            CastInfoEvent("This is a Trionic 5.5 ECU with a T5.2 BIN", ActivityType.ConvertingFile);
                            return ECUType.T55AST52;
                        default:
                            CastInfoEvent("!!! ERROR !!! This is a Trionic 5.5 ECU running an unknown firmware", ActivityType.ConvertingFile);
                            return ECUType.Unknown;
                    }
                default:
                    break;
            }

            return 0;
        }

        // Todo: A lot...
        public void WriteFlash(string a_fileName)
        {
            UploadBootLoader();
            // if (UploadBootLoader())
            {
                Thread.Sleep(200);
                FileInfo fi = new FileInfo(a_fileName);
                bool OkToUpgrade = true;

                ECUType ECU_type = DetermineECU();

                switch (ECU_type)
                {
                    case ECUType.T52ECU:
                        if (fi.Length != 0x20000)
                        {
                            CastInfoEvent("Not a Trionic 5.2 BIN File!", ActivityType.ConvertingFile);
                            OkToUpgrade = false;
                        }
                        break;
                    case ECUType.T55ECU:
                        switch (fi.Length)
                        {
                            case 0x20000:
                                DialogResult result = MessageBox.Show("Do you want to upload a T5.2 BIN file to your T5.5 ECU",
                                    "ECU Conversion Question",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question,
                                    MessageBoxDefaultButton.Button2);
                                OkToUpgrade = (result == DialogResult.Yes) ? true : false;
                                break;
                            case 0x40000:
                                break;
                            default:
                                CastInfoEvent("Not a Trionic 5.5 BIN File!", ActivityType.ConvertingFile);
                                OkToUpgrade = false;
                                break;
                        }
                        break;
                    case ECUType.T55AST52:
                        switch (fi.Length)
                        {
                            case 0x20000:
                                break;
                            case 0x40000:
                                DialogResult result = MessageBox.Show("Do you want to upload a T5.5 BIN file to your ECU that has been used as a T5.2?",
                                    "ECU Conversion Question",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question,
                                    MessageBoxDefaultButton.Button2);
                                OkToUpgrade = (result == DialogResult.Yes) ? true : false;
                                break;
                            default:
                                CastInfoEvent("Not a Trionic BIN File!", ActivityType.ConvertingFile);
                                OkToUpgrade = false;
                                break;
                        }
                        break;
                }

                if (OkToUpgrade)
                {
                    CastInfoEvent("Starting FLASH update session...", ActivityType.ConvertingFile);
                    UpgradeResult result = UpgradeECU(a_fileName, ECU_type);

                    switch (result)
                    {
                        case UpgradeResult.Success:
                            CastInfoEvent("!!! SUCCESS !!!", ActivityType.ConvertingFile);
                            GetECUInfo(false);
                            CastInfoEvent("Your ECU is ready to use your new BIN file :-)", ActivityType.ConvertingFile);
                            break;
                        case UpgradeResult.InvalidFile:
                            CastInfoEvent("!!! ERROR !!! Invalid file for the selected ECU type :-o", ActivityType.ConvertingFile);
                            break;
                        case UpgradeResult.InvalidECUType:
                            CastInfoEvent("!!! ERROR !!! Invalid ECU type selected :-o", ActivityType.ConvertingFile);
                            break;
                        case UpgradeResult.ProgrammingFailed:
                            CastInfoEvent("!!! FAILURE !!! Could not program the FLASH in your ECU :-(", ActivityType.ConvertingFile);
                            break;
                        case UpgradeResult.EraseFailed:
                            CastInfoEvent("!!! FAILURE !!! Could not erase the FLASH in your ECU :-(", ActivityType.ConvertingFile);
                            break;
                        case UpgradeResult.ChecksumFailed:
                            CastInfoEvent("!!! FAILURE !!! Checksums don't match after FLASHing :-(", ActivityType.ConvertingFile);
                            break;
                        default:
                            CastInfoEvent("!!! ERROR!!! There was a problem I haven't catered for ???", ActivityType.ConvertingFile);
                            break;
                    }
                    switch (result)
                    {
                        case UpgradeResult.ProgrammingFailed:
                        case UpgradeResult.EraseFailed:
                        case UpgradeResult.ChecksumFailed:
                            CastInfoEvent("There are many reasons why you saw this error message,", ActivityType.ConvertingFile);
                            CastInfoEvent("e.g. a problem with your power supply voltage or you may", ActivityType.ConvertingFile);
                            CastInfoEvent("be unlucky enough to have 'Bad FLASH chips' :-(", ActivityType.ConvertingFile);
                            CastInfoEvent("You should retry FLASHing your BIN file but if it fails", ActivityType.ConvertingFile);
                            CastInfoEvent("again your only option is to try to recover your ECU", ActivityType.ConvertingFile);
                            CastInfoEvent("using a BDM interface !!!", ActivityType.ConvertingFile);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    ExitBootloader();
                }
            }
            // device.DisableLogging();
            // manageControls(programMode.connected);
        }

        public string getSWVersion(bool DoFindSync)
        {
            string r_swVersion = "";
            if (DoFindSync)
            {
                //findSynch();
            }

            //            sendCommand(CR, 1);
            // send capital S for T5.2... lowercase s is not supported
            r_swVersion = sendCommand("s", 1);
            Console.WriteLine(r_swVersion);
            //Console.WriteLine("s sent");
            r_swVersion = sendCommand(CR, CR + NL, 20);
            //Console.WriteLine("CR sent");
            //            return trimString(r_swVersion);
            Console.WriteLine("sw version s: " + r_swVersion);
            r_swVersion = trimString(r_swVersion);
            r_swVersion = r_swVersion.Replace(">", "");
            if (r_swVersion.Length > 12)
            {
                r_swVersion = r_swVersion.Substring(r_swVersion.Length - 12, 12);
            }

            return r_swVersion;
        }

        public string getSWVersionT52(bool DoFindSync)
        {
            string r_swVersion = "";
            if (DoFindSync)
            {
                // findSynch();
            }
            // send capital S for T5.2... lowercase s is not supported
            r_swVersion = sendCommand("S", 1);
            r_swVersion = sendCommand(CR, CR + NL);
            r_swVersion = trimString(r_swVersion);
            Console.WriteLine("sw version S: " + r_swVersion);
            if (r_swVersion.Length > 12)
            {
                r_swVersion = r_swVersion.Substring(r_swVersion.Length - 12, 12);
            }
            return r_swVersion;
        }

        public void GetECUInfo(bool UpldLDR)
        {
            if (UpldLDR)
            {
                if (!UploadBootLoader())
                    return;
                Thread.Sleep(500);
            }

            // statusActivity.Text = "getting Footer";
            byte[] footer = getECUFooter();
            byte[] chiptypes = GetChipTypes();

            CastInfoEvent("", ActivityType.ConvertingFile);
            // statusActivity.Text = "IDLE";
            string swversion = getIdentifierFromFooter(footer, ECUIdentifier.Dataname);
            string romoffset = getIdentifierFromFooter(footer, ECUIdentifier.ROMoffset);
            // string checksum = ReturnChecksum();



            //getChecksum
            // statusSWVersion.Text = "SW Version: " + swversion;
            // statusChecksum.Text = "Checksum: " + checksum;
            // identify ECU
            string flashzize = "256 kB";
            switch (chiptypes[0])
            {
                case 0xB8:      // Intel/CSI/OnSemi 28F512
                case 0x25:      // AMD 28F512
                    // statusFLASHType.Text = "Type: 28F512";
                    flashzize = "128 kB";
                    break;
                case 0x5D:      // Atmel 29C512
                    // statusFLASHType.Text = "Type: 29C512";
                    flashzize = "128 kB";
                    break;
                case 0xB4:      // Intel/CSI/OnSemi 28F010
                case 0xA7:      // AMD 28F010
                    // statusFLASHType.Text = "Type: 28F010";
                    break;
                case 0x20:      // AMD/ST 29F010
                case 0xA4:      // AMIC 29F010
                    // statusFLASHType.Text = "Type: 29F010";
                    break;
                case 0xD5:      // Atmel 29C010
                    // statusFLASHType.Text = "Type: 29C010";
                    break;
                case 0xB5:      // SST 39F010
                    // statusFLASHType.Text = "Type: 39F010";
                    break;
                default:
                    // statusFLASHType.Text = "Type: 0x" + chiptypes[0].ToString("X2") + " - Unknown";
                    flashzize = "Unknown";
                    break;
            }
            // statusFLASHSize.Text = "Size: " + flashzize;
            switch (chiptypes[1])
            {
                case 0x89:      // Intel
                    // statusFLASHMake.Text = "Make: Intel";
                    break;
                case 0x01:      // AMD
                    // statusFLASHMake.Text = "Make: AMD/Spansion";
                    break;
                case 0x31:      // CSI/OnSemi
                    // statusFLASHMake.Text = "Make: CSI";
                    break;
                case 0x1F:      // Atmel
                    // statusFLASHMake.Text = "Make: Atmel";
                    break;
                case 0xBF:      // SST/Microchip
                    // statusFLASHMake.Text = "Make: SST/Microchip";
                    break;
                case 0x20:      // ST
                    // statusFLASHMake.Text = "Make: ST Microelectronics";
                    break;
                case 0x37:      // AMIC
                    // statusFLASHMake.Text = "Make: AMIC";
                    break;
                default:
                    // statusFLASHMake.Text = "Make: 0x" + chiptypes[1].ToString("X2") + " - Unknown";
                    break;
            }
            switch (flashzize)
            {
                case "128 kB":
                    switch (romoffset)
                    {
                        case "060000":
                            // ECU_type = ECUType.T52ECU;
                            // statusECU.Text = "ECU Type: Trionic 5.2";
                            CastInfoEvent("This is a Trionic 5.2 ECU with 128 kB of FLASH", ActivityType.ConvertingFile);
                            break;
                        default:
                            // ECU_type = ECUType.Unknown;
                            // statusECU.Text = "ECU Type: Unknown";
                            CastInfoEvent("!!! ERROR !!! This type of ECU is unknown", ActivityType.ConvertingFile);
                            break;
                    }
                    break;
                case "256 kB":
                    switch (romoffset)
                    {
                        case "040000":
                            // ECU_type = ECUType.T55ECU;
                            // statusECU.Text = "ECU Type: Trionic 5.5";
                            CastInfoEvent("This is a Trionic 5.5 ECU with 256 kB of FLASH", ActivityType.ConvertingFile);
                            break;
                        case "060000":
                            // ECU_type = ECUType.T55AST52;
                            // statusECU.Text = "ECU Type: T5.5 as T5.2";
                            CastInfoEvent("This is a Trionic 5.5 ECU with a T5.2 BIN", ActivityType.ConvertingFile);
                            break;
                        default:
                            // ECU_type = ECUType.Unknown;
                            // statusECU.Text = "ECU Type: Unknown";
                            CastInfoEvent("!!! ERROR !!! This type of ECU is unknown", ActivityType.ConvertingFile);
                            break;
                    }
                    break;
                default:
                    // ECU_type = ECUType.Unknown;
                    // statusECU.Text = "ECU Type: Unknown";
                    CastInfoEvent("!!! ERROR !!! This type of ECU is unknown", ActivityType.ConvertingFile);
                    break;
            }

            CastInfoEvent("Part Number: " + getIdentifierFromFooter(footer, ECUIdentifier.Partnumber), ActivityType.ConvertingFile);
            CastInfoEvent("Software ID: " + getIdentifierFromFooter(footer, ECUIdentifier.SoftwareID), ActivityType.ConvertingFile);
            CastInfoEvent("SW Version: " + swversion, ActivityType.ConvertingFile);
            CastInfoEvent("Engine Type: " + getIdentifierFromFooter(footer, ECUIdentifier.EngineType), ActivityType.ConvertingFile);
            CastInfoEvent("IMMO Code: " + getIdentifierFromFooter(footer, ECUIdentifier.ImmoCode), ActivityType.ConvertingFile);
            CastInfoEvent("Other Info: " + getIdentifierFromFooter(footer, ECUIdentifier.Unknown), ActivityType.ConvertingFile);
            CastInfoEvent("ROM Start: 0x" + romoffset, ActivityType.ConvertingFile);
            CastInfoEvent("Code End: 0x" + getIdentifierFromFooter(footer, ECUIdentifier.CodeEnd), ActivityType.ConvertingFile);
            CastInfoEvent("ROM End: 0x" + getIdentifierFromFooter(footer, ECUIdentifier.ROMend), ActivityType.ConvertingFile);
            VerifyChecksum();
            // CastInfoEvent("Checksum: " + checksum, ActivityType.ConvertingFile);

            // Exit bootloader
            ExitBootloader();
        }

        public byte[] getECUFooter()
        {
            byte[] footer = new byte[0x80];
            Console.WriteLine("Start ECU dump");
            CastInfoEvent("Getting data from ECU", ActivityType.StartDownloadingFooter);

            byte[] buffer2 = new byte[6];
            UInt32 address = (0x7FF80 + 5);
            for (int i = 0; i < (0x80 / 6); i++)
            {
                buffer2 = this.sendReadCommand(address);
                address += 6;
                for (int j = 0; j < 6; j++)
                    footer[(i * 6) + j] = buffer2[j];
                CastProgressWriteEvent((int)((i * 100) / (0x80 / 6)));
                CastBytesTransmitted((int)(address - (0x7FF80 + 5)));
            }
            // Mop up the last few bytes if not an exact multiple of 6
            // It has to be done this way because the T5 ECU resets if
            // you try to read past the end of the FLASH addresses
            buffer2 = this.sendReadCommand(0x7FFFF);
            for (int j = 2; j < 6; j++)
                footer[0x80 - 6 + j] = buffer2[j];
            CastProgressWriteEvent(100);
            CastBytesTransmitted(0x80);

            CastInfoEvent("Finished getting data from ECU", ActivityType.FinishedDownloadingFooter);
            return footer;
        }

        public string getIdentifierFromFooter(byte[] footer, ECUIdentifier identifier)
        {
            string result = string.Empty;

            int offset = footer.Length - 0x05;  //  avoid the stored checksum
            while (offset > 0)
            {
                int length = footer[offset--];
                ECUIdentifier search = (ECUIdentifier)footer[offset--];
                if (search == identifier)
                {
                    for (int i = 0; i < length; i++)
                    {
                        result += (char)footer[offset--];
                    }
                    return result;
                }
                offset -= length;
            }

            CastInfoEvent("Error getting Identifier Version", ActivityType.QueryingECUTypeInfo);
            return result;
        }


        public byte[] readRAM(ushort address, uint length)
        {
            lock (this)
            {
                byte[] buffer = new byte[length];
                byte[] buffer2 = new byte[6];

                // maybe we need to clear the receive buffer of the can interface first

                //m_canDevice.clearReceiveBuffer();

                address = (ushort)(address + 5);
                uint num = length / 6;
                if ((length % 6) > 0)
                {
                    num++;
                }
                for (int i = 0; i < num; i++)
                {
                    buffer2 = this.sendReadCommand(address);
                    address = (ushort)(address + 6);
                    for (int j = 0; j < 6; j++)
                    {
                        if (((i * 6) + j) < length)
                        {
                            buffer[(i * 6) + j] = buffer2[j];
                        }
                    }
                    if (length == 0x8000)
                    {
                        CastProgressReadEvent((i * 600) / 0x8000);
                    }
                    //if (buffer[0] == 0 && buffer[1] == 0) break;
                }
                if (length == 0x8000)
                {
                    CastProgressReadEvent(100);
                }
                Thread.Sleep(1); // <GS-13122010> was 1


                return buffer;
            }
        }



        public byte[] readRAM_old(UInt16 address, uint length)
        {
            length += 5;                        //The gods wants us to read 5 extra bytes.
            byte[] data = new byte[length];
            byte[] tmpData = new byte[6];
            byte[] retData = new byte[length - 5];
            uint nrOfReads = length / 6;
            if ((length % 6) > 1)
                nrOfReads++;
            for (int i = 0; i < nrOfReads; i++)
            {
                tmpData = sendReadCommand(address);
                address += 6;
                for (int j = 0; j < 6; j++)
                {
                    if ((i * 6 + j) == length)
                    {
                        //Time to sacrifice 5 bytes to the gods.
                        for (int k = 0; k < length - 5; k++)
                            retData[k] = data[k + 5];
                        return retData;
                    }
                    data[i * 6 + j] = tmpData[j];
                }
            }

            //Time to sacrifice 5 bytes to the gods.
            for (int i = 0; i < length - 5; i++)
                retData[i] = data[i + 5];
            return retData;
        }

        /// <summary>
        /// Write a byte array to an address.
        /// </summary>
        /// <param name="address">Address. Must be greater than 0x1000</param>
        /// <param name="data">Data to be written</param>
        /// <returns></returns>
        public bool writeRam(UInt16 address, byte[] data)
        {
            /* if (address < 0x1000)
                 throw new Exception("Invalid address");*/
            lock (this)
            {
                bool Silent = false;
                if (data.Length <= 16) Silent = true;
                int progress = 0;
                UInt16 sendAddress = address;
                byte[] _currentData = readRAM((ushort)address, (uint)data.Length);
                Thread.Sleep(10);
                for (int i = 0; i < data.Length; i++)
                {
                    if (_currentData[i] != data[i])
                    {
                        sendWriteCommand(sendAddress++, data[i]);
                    }
                    else
                    {
                        sendAddress++; // increase anyway
                    }
                    progress = (i * 100) / data.Length;
                    if (!Silent)
                    {
                        CastProgressWriteEvent(progress);
                    }
                    //                sendWriteCommandGuido(sendAddress++, data[i]);
                }
                if (!Silent)
                {
                    CastProgressWriteEvent(100);
                }
                return true;
            }
        }

        public bool writeRamForced(UInt16 address, byte[] data)
        {
            lock (this)
            {
                UInt16 sendAddress = address;
                for (int i = 0; i < data.Length; i++)
                {
                    sendWriteCommand(sendAddress++, data[i]);
                    Thread.Sleep(0);
                }
                return true;
            }
        }


        private void CastBytesTransmitted(int bytestransmitted)
        {
            /*if (onBytesTransmitted != null)
            {
                onBytesTransmitted(this, new WriteProgressEventArgs(bytestransmitted));
            }*/
        }

        private bool sendWriteCommand(UInt16 address, byte data)
        {
            string addr     = Convert.ToString(address, 16);
            string sendData = Convert.ToString(data, 16);

            addr     = addr.ToUpper();
            sendData = sendData.ToUpper();

            while (addr.Length < 4)
                addr = "0" + addr;
            if (sendData.Length < 2)
                sendData = "0" + sendData;

            //Console.WriteLine("Send write command on address: " + addr + " with data: " + sendData);

            sendCommandByteForRead("W"[0]);
            waitNoAck();
            sendCommandByteForRead(addr.Substring(0, 1)[0]);
            waitNoAck();
            sendCommandByteForRead(addr.Substring(1, 1)[0]);
            waitNoAck();
            sendCommandByteForRead(addr.Substring(2, 1)[0]);
            waitNoAck();
            sendCommandByteForRead(addr.Substring(3, 1)[0]);
            waitNoAck();
            sendCommandByteForRead(sendData.Substring(0, 1)[0]);
            waitNoAck();
            sendCommandByteForRead(sendData.Substring(1, 1)[0]);
            waitNoAck();
            sendCommandByteForRead(CR[0]);
            waitNoAck();

            return true;
        }

        public byte[] sendBootLoaderEraseCommand()
        {
            CANMessage msg      = new CANMessage(0x005, 0, 8);
            CANMessage response = new CANMessage();
            byte[] retData      = new byte[8];
            ulong data, cmd     = 0xC0;

            // This is why ELM is NOT fun to work with...
            if (canUsbDevice is CANELM327Device)
            {
                uint ret = (uint)EraseTimeoutmS / 100;
                do
                {
                    msg.setData(cmd);
                    m_canListener.setupWaitMessage(0x00c);
                    if (!canUsbDevice.sendMessage(msg))
                        logger.Debug("Couldn't send message");

                    response = m_canListener.waitMessage(100);
                    cmd = 0;
                } while (response.getID() == 0 && --ret > 0);
            }
            else
            {
                msg.setData(cmd);
                m_canListener.setupWaitMessage(0x00c);

                if (!canUsbDevice.sendMessage(msg))
                    logger.Debug("Couldn't send message");

                response = m_canListener.waitMessage(EraseTimeoutmS);
            }

            data = response.getData();

            for (int i = 0; i < 8; i++)
                retData[7 - i] = (byte)(data >> i * 8);
            return retData;
        }


        // sending A5 address command for uploading bootloader and flash file
        public byte[] sendBootloaderAddressCommand(Int32 address, byte len)
        {
            CANMessage msg      = new CANMessage(0x005, 0, 8);
            CANMessage response = new CANMessage();
            byte[] retData      = new byte[8];
            ulong data, cmd     = 0xA5;

            cmd |= (ulong)(byte) (address & 0x0000FF) << 4 * 8;
            cmd |= (ulong)(byte)((address & 0x00FF00) >> 8) << 3 * 8;
            cmd |= (ulong)(byte)((address & 0xFF0000) >> 2 * 8) << 2 * 8;
            cmd |= (ulong)((byte)(len)) << 5 * 8;

            msg.setData(cmd);

            m_canListener.setupWaitMessage(0x00c);
            if (!canUsbDevice.sendMessage(msg))
            {
                logger.Debug("Couldn't send message");
                return retData;
            }

            response = m_canListener.waitMessage(GeneralTimeoutmS);
            data     = response.getData();

            for (int i = 0; i < 8; i++)
                retData[7 - i] = (byte)(data >> i * 8);

            return retData;
        }


        public byte[] sendBootVectorAddressSRAM(Int32 address)
        {
            CANMessage msg      = new CANMessage(0x005, 0, 8);
            CANMessage response = new CANMessage();
            byte[] retData      = new byte[8];
            ulong  cmd          = 0xC1;

            cmd |= (ulong)(byte) (address & 0x0000FF) << 4 * 8;
            cmd |= (ulong)(byte)((address & 0x00FF00) >> 8) << 3 * 8;
            cmd |= (ulong)(byte)((address & 0xFF0000) >> 2 * 8) << 2 * 8;

            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x00c);

            if (!canUsbDevice.sendMessage(msg))
            {
                logger.Debug("Couldn't send message");
                return retData;
            }
            
            response   = m_canListener.waitMessage(GeneralTimeoutmS);
            ulong data = response.getData();

            for (int i = 0; i < 8; i++)
                retData[7 - i] = (byte)(data >> i * 8);

            return retData;
        }


        // sending data from bootloader or flash file 
        public byte[] sendBootloaderDataCommand(byte[] data, byte len)
        {
            CANMessage msg      = new CANMessage(0x005, 0, 8);
            CANMessage response = new CANMessage();
            byte[] retData      = new byte[8];
            ulong  uldata, cmd  = 0;
            int    cnt          = 0;

            foreach (byte b in data)
                cmd |= (ulong)((byte)b) << (cnt++) * 8;

            // This is why ELM is NOT fun to work with...
            if (canUsbDevice is CANELM327Device)
            {
                uint ret = (uint)GeneralTimeoutmS / 100;
                do
                {
                    msg.setData(cmd);
                    m_canListener.setupWaitMessage(0x00c);

                    if (!canUsbDevice.sendMessage(msg))
                        logger.Debug("Couldn't send message");

                    response = m_canListener.waitMessage(100);

                } while (response.getID() == 0 && --ret > 0);
            }
            else
            {
                msg.setData(cmd);
                m_canListener.setupWaitMessage(0x00c);

                if (!canUsbDevice.sendMessage(msg))
                    logger.Debug("Couldn't send message");

                response = m_canListener.waitMessage(GeneralTimeoutmS);
            }

            uldata = response.getData();

            for (int i = 0; i < 8; i++)
                retData[7 - i] = (byte)(uldata >> i * 8);

            return retData;
        }

        // reading data from T5 ECU
        // address should only be a 16 bit address to read SRAM before the bootloader has been loaded
        // address can be a 32 bit address to read FLASH BIN only after the bootloader has been loaded
        private byte[] sendReadCommand(UInt32 address)
        {
            CANMessage msg      = new CANMessage(0x005, 0, 8);
            CANMessage response = new CANMessage();
            byte[] retData      = new byte[6];
            ulong  data, cmd    = 0xC7;

            //          Console.WriteLine("Send read command");
            cmd |= (ulong)(byte) (address & 0x000000FF) << 4 * 8;
            cmd |= (ulong)(byte)((address & 0x0000FF00) >> 8) << 3 * 8;
            cmd |= (ulong)(byte)((address & 0x00FF0000) >> 2 * 8) << 2 * 8;
            cmd |= (ulong)(byte)((address & 0xFF000000) >> 3 * 8) << 8;

            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x00c);

            if (!canUsbDevice.sendMessage(msg))
            {
                Console.WriteLine("Couldn't send message");
            }

            //sw.Stop();
            //Console.WriteLine("Send took " + sw.ElapsedMilliseconds.ToString() + " ms");
            
            //sw.Reset();
            //sw.Start();

            response = m_canListener.waitMessage(GeneralTimeoutmS);
            data = response.getData();

            for (int i = 2; i < 8; i++)
                retData[7 - i] = (byte)(data >> i * 8);

            return retData;
        }

        private string sendCommand(string a_command)
        {
            string retString = "";
            try
            {
                sendCommandByte(a_command[0]);
                retString = waitForResponse();
            }
            catch (Exception E)
            {
                Console.WriteLine("Error receiving response to a command in sendCommand: " + E.Message);
            }
            return retString;
        }

        private void sendNoAck(string a_command)
        {
            sendCommandByte(a_command[0]);
        }

        private string sendCommand(string a_command, uint a_nrOfBytes)
        {
            string retString = "";
            string str = "";

            try
            {
                sendCommandByte(a_command[0]);
                for (int i = 0; i < a_nrOfBytes; i++)
                {
                    str = waitForResponse();
                    if (str.Equals(TIMEOUT))
                    {
                        return retString;
                    }
                    retString += str;
                }
            }
            catch (Exception E)
            {
                Console.WriteLine("Error receiving response on command in sendCommand 2: " + E.Message);
            }
            return retString;
        }


        private string sendCommand(string a_command, string a_endChar)
        {
            string retString = "";
            try
            {
                sendCommandByte(a_command[0]);
                string recChar = "";
                //                int cnt = 0;
                do
                {
                    recChar = waitForResponse();
                    if (recChar.Equals(TIMEOUT))
                    {
                        return retString;
                    }
                    retString += recChar;
                    if (!a_endChar.EndsWith(CR + NL))      //Not variable length.                
                    {
                        if (retString.Length > a_endChar.Length)
                        {
                            return retString;
                        }
                    }
                }
                while (!retString.EndsWith(a_endChar) /*&& cnt++ < 10*/);
            }
            catch (Exception E)
            {
                Console.WriteLine("Error receiving response on command 3: " + E.Message);
            }
            return retString;
        }

        private string sendCommand(string a_command, string a_endChar, int max_chars_to_wait)
        {
            string retString = "";
            try
            {
                sendCommandByte(a_command[0]);
                string recChar = "";
                int cnt = 0;
                do
                {
                    recChar = waitForResponse(20);
                    if (recChar.Equals(TIMEOUT))
                    {
                        return retString;
                    }
                    retString += recChar;
                    if (!a_endChar.EndsWith(CR + NL))      //Not variable length.                
                    {
                        if (retString.Length > a_endChar.Length)
                        {
                            return retString;
                        }
                    }
                }
                while (!retString.EndsWith(a_endChar) && cnt++ < max_chars_to_wait);
            }
            catch (Exception E)
            {
                Console.WriteLine("Error receiving response on command 3: " + E.Message);
            }
            return retString;
        }

        private void sendCommandByte(char a_commandByte)
        {
            CANMessage msg   = new CANMessage(0x005, 0, 8);
            int max_sends    = 5000; // revert back to 5000!
            uint nrOfResends =    0;
            ulong cmd        =    0;

            cmd |= (ulong)a_commandByte;
            cmd <<= 8;
            cmd |= (ulong)0xC4;
            cmd |= 0xFFFFFFFFFFFF0000;

            msg.setData(cmd);
            
            while (!canUsbDevice.sendMessage(msg))
            {
                if (nrOfResends++ > max_sends)
                {
                    logger.Debug("Couldn't send message");
                    return;
                }
                //throw new Exception("Couldn't send message"); ;
            }
            if (nrOfResends < max_sends)
            {
                //Console.WriteLine("Sending succeeded");
            }
        }

        private void sendCommandByteForRead(char a_commandByte)
        {
            CANMessage msg = new CANMessage(0x006, 0, 2);
            ulong cmd = 0;

            //Console.WriteLine(">" + a_commandByte );
            //Console.WriteLine("Send command byte for read");

            cmd |= (ulong)a_commandByte;
            cmd <<= 8;
            cmd |= (ulong)0xC4;
            msg.setData(cmd);

            if (!canUsbDevice.sendMessage(msg))
            {
                logger.Debug("Couldn't send message");
                return;
            }
        }

        private string waitForResponse()
        {
            CANMessage response = new CANMessage();
            bool   timeout      = false;
            string returnString = "";

            m_canListener.setupWaitMessage(0x00c);
            response = m_canListener.waitMessage(GeneralTimeoutmS);
            
            if (timeout)
            {
                return TIMEOUT;
            }
            /* if ((byte)response.getData() == 0xC4)
             {
                 response = m_canListener.waitForMessage(0x00C, 1000);
                 DumpCanMsg(response, false);
                 Console.WriteLine("received C4 command");
                 //throw new Exception("Error receiving data (1)");
             }

             else */
            if ((byte)response.getData() != 0xC6)
            {
                //DumpCanMsg(response, false);
                //Console.WriteLine("Error rx data (1)");
                //throw new Exception("Error receiving data (1)");
                return TIMEOUT;
            }
            if (response.getLength() < 8)
            {
                returnString = TIMEOUT;
                return returnString;
            }

            returnString += (char)((response.getData() >> 16) & 0xFF);
            sendAck();

            return returnString;
        }

        private string waitForResponse(int mstimeout)
        {
            CANMessage response = new CANMessage();
            bool   timeout      = false;
            string returnString = "";
            
            m_canListener.setupWaitMessage(0x00c);
            response = m_canListener.waitMessage(mstimeout);

            if (timeout)
            {
                return TIMEOUT;
            }
            /* if ((byte)response.getData() == 0xC4)
             {
                 response = m_canListener.waitForMessage(0x00C, 1000);
                 DumpCanMsg(response, false);
                 Console.WriteLine("received C4 command");
                 //throw new Exception("Error receiving data (1)");
             }

             else */
            if ((byte)response.getData() != 0xC6)
            {
                //DumpCanMsg(response, false);
                //Console.WriteLine("Error rx data (1)");
                //throw new Exception("Error receiving data (1)");
                return TIMEOUT;
            }
            if (response.getLength() < 8)
            {
                returnString = TIMEOUT;
                return returnString;
            }

            returnString += (char)((response.getData() >> 16) & 0xFF);
            sendAck();

            return returnString;
        }

        private void DumpCanMsg(CANMessage canMsg, bool IsTransmit)
        {
            DateTime dt = DateTime.Now;
            try
            {
                using (StreamWriter sw = new StreamWriter(@"c:\" + dt.Year.ToString("D4") + dt.Month.ToString("D2") + dt.Day.ToString("D2") + "-CanTrace.log", true))
                {
                    if (IsTransmit)
                    {
                        // get the byte transmitted
                        int transmitvalue = (int)(canMsg.getData() & 0x000000000000FF00);
                        transmitvalue /= 256;

                        sw.WriteLine(dt.ToString("dd/MM/yyyy HH:mm:ss") + " TX: id=" + canMsg.getID().ToString("X2") + " len= " + canMsg.getLength().ToString("X8") + " data=" + canMsg.getData().ToString("X16") + " " + canMsg.getFlags().ToString("X2") + "\t ts: " + canMsg.getTimeStamp().ToString("X16") + " flags: " + canMsg.getFlags().ToString("X2"));
                    }
                    else
                    {
                        // get the byte received
                        int receivevalue = (int)(canMsg.getData() & 0x0000000000FF0000);
                        receivevalue /= (256 * 256);
                        sw.WriteLine(dt.ToString("dd/MM/yyyy HH:mm:ss") + " RX: id=" + canMsg.getID().ToString("X2") + " len= " + canMsg.getLength().ToString("X8") + " data=" + canMsg.getData().ToString("X16") + " " + canMsg.getFlags().ToString("X2") + "\t ts: " + canMsg.getTimeStamp().ToString("X16") + " flags: " + canMsg.getFlags().ToString("X2"));
                    }
                }
            }
            catch (Exception E)
            {
                Console.WriteLine("Failed to write to logfile: " + E.Message);
            }
        }


        private string waitNoAck()
        {
            CANMessage response = new CANMessage();
            bool   timeout      = false;
            string returnString = "";

            m_canListener.setupWaitMessage(0x00c);
            response = m_canListener.waitMessage(GeneralTimeoutmS * 2);

            if (timeout)
            {
                return TIMEOUT;
            }
            if ((byte)response.getData() != 0xC6)
            {
                byte b = (byte)response.getData();
                Console.WriteLine("Error rx data (2): " + b.ToString("X2"));
                //throw new Exception("Error receiving data (2)");

            }
            if (response.getLength() < 8)
            {
                returnString = "";
                return returnString;
            }

            returnString += (char)((response.getData() >> 16) & 0xFF);

            return returnString;
        }


        private void sendAck()
        {
            CANMessage ack = new CANMessage(0x006, 0, 2);

            //if (m_canDevice == null)
            //    throw new Exception("CAN device not set");
            
            ack.setData(0x00000000000000C6);

            if (!canUsbDevice.sendMessage(ack))
            {
                //   throw new Exception("Couldn't send message");
                Console.WriteLine("Couldn't send message");
            }
        }



        string trimString(string a_string)
        {
            a_string = a_string.TrimStart('>');
            a_string = a_string.TrimEnd('\n');
            a_string = a_string.TrimEnd('\r');

            return a_string;
        }


        /// <summary>
        /// Cleans up connections and resources
        /// </summary>
        override public void Cleanup()
        {
            try
            {
                m_ECU = ECU.TRIONIC5;
                tmr.Stop();
                MM_EndPeriod(1);
                logger.Debug("Cleanup called in Trionic5");
                //m_canDevice.removeListener(m_canListener);
                if (m_canListener != null)
                {
                    m_canListener.FlushQueue();
                }
                if (canUsbDevice != null)
                {
                    if (canUsbDevice is LPCCANDevice)
                    {
                        LPCCANDevice lpc = (LPCCANDevice)canUsbDevice;
                        lpc.disconnect();
                    }
                    canUsbDevice.close();
                    canUsbDevice = null;
                }
            }
            catch (Exception e)
            {
                logger.Debug(e.Message);
            }

            LogManager.Flush();
        }
        /// <summary>
        /// Sends a C2 command to the ECU or bootloader (exit bootloader command)
        /// </summary>
        /// <returns></returns>
        public byte[] sendC2Command()
        {
            CANMessage msg      = new CANMessage(0x005, 0, 8);
            CANMessage response = new CANMessage();
            byte[] retData      = new byte[6];
            ulong  data, cmd    = 0xC2;

            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x00c);

            if (!canUsbDevice.sendMessage(msg))
            {
                logger.Debug("Couldn't send message");
            }

            response = m_canListener.waitMessage(GeneralTimeoutmS);
            data     = response.getData();

            for (int i = 2; i < 8; i++)
                retData[7 - i] = (byte)(data >> i * 8);

            return retData;
        }

        /// <summary>
        /// Gets the checksum (C8 command) from the bootloader (non native command)
        /// </summary>
        /// <returns></returns>
        public byte[] getChecksum()
        {
            CANMessage msg      = new CANMessage(0x005, 0, 8);
            CANMessage response = new CANMessage();
            byte[] retData      = new byte[8];
            ulong  data, cmd    = 0xC8;

            // This is why ELM is NOT fun to work with...
            if (canUsbDevice is CANELM327Device)
            {
                uint ret = ChecksumTimeoutmS / 100;
                do
                {
                    msg.setData(cmd);
                    m_canListener.setupWaitMessage(0x00c);

                    if (!canUsbDevice.sendMessage(msg))
                        logger.Debug("Couldn't send message");

                    response = m_canListener.waitMessage(100);
                    cmd = 0;
                } while (response.getID() == 0 && --ret > 0);
            }
            else
            {
                msg.setData(cmd);
                m_canListener.setupWaitMessage(0x00c);

                if (!canUsbDevice.sendMessage(msg))
                    logger.Debug("Couldn't send message");

                response = m_canListener.waitMessage(ChecksumTimeoutmS);
            }

            data = response.getData();

            for (int i = 0; i < 8; i++)
                retData[7 - i] = (byte)(data >> i * 8);

            return retData;
        }

        /// <summary>
        /// Sends a C3 command to the ECU or bootloader
        /// </summary>
        /// <returns></returns>
        public byte[] sendC3Command()
        {
            CANMessage msg      = new CANMessage(0x005, 0, 8);
            CANMessage response = new CANMessage();
            byte[] retData      = new byte[6];
            ulong  data, cmd    = 0xC3;

            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x00c);

            if (!canUsbDevice.sendMessage(msg))
            {
                logger.Debug("Couldn't send message");
            }

            response = m_canListener.waitMessage(GeneralTimeoutmS);
            data     = response.getData();

            for (int i = 2; i < 8; i++)
                retData[7 - i] = (byte)(data >> i * 8);

            return retData;
            // unit responded B8 89 FF FF 07 00
        }

        /// <summary>
        /// Sends a free form command to the ECU or bootloader
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public byte[] sendFreeCommand(ulong cmd)
        {
            CANMessage msg      = new CANMessage(0x005, 0, 8);
            CANMessage response = new CANMessage();
            byte[] retData      = new byte[8];
            
            msg.setData(cmd);
            m_canListener.setupWaitMessage(0x00c);

            if (!canUsbDevice.sendMessage(msg))
            {
                logger.Debug("Couldn't send message");
            }

            // checksum command (C8) may take longer than 1 second, so set it to 10
            response   = m_canListener.waitMessage(ChecksumTimeoutmS); 
            ulong data = response.getData();

            for (int i = 0; i < 8; i++)
                retData[7 - i] = (byte)(data >> i * 8);

            return retData;
        }


        /// <summary>
        /// Sends a C9 command to the bootloader to get offset address and
        /// FLASH chip types
        /// </summary>
        /// <returns></returns>
        public byte[] GetChipTypes()
        {
            byte[] retData = new byte[6];

            if (canUsbDevice.isOpen())
            {
                CANMessage msg      = new CANMessage(0x005, 0, 8);
                CANMessage response = new CANMessage();
                ulong data, cmd     = 0xC9;

                msg.setData(cmd);
                m_canListener.setupWaitMessage(0x00c);

                if (!canUsbDevice.sendMessage(msg))
                {
                    logger.Debug("Couldn't send message");
                    return retData;
                }
                
                /*if (!canUsbDevice.sendMessage(msg))
                    throw new Exception("Couldn't send message");
                response = new CANMessage();
                response = m_canListener.waitForMessage(0x00C, 1000);*/

                response = m_canListener.waitMessage(GeneralTimeoutmS);
                data = response.getData();

                for (int i = 2; i < 8; i++)
                    retData[7 - i] = (byte)(data >> i * 8);

                return retData;
                // unit responded 20 01 00 00 04 00
                //                || || |      | - 0x40000 = T5.5
                //                || || - 0x01 = AMD
                //                || - 0x20 = 29F010 
            }
            else
            {
                CastInfoEvent("Device is not open!", ActivityType.ConvertingFile);
                return retData;
            }
        }

        /// <summary>
        /// Uploads and starts the given bootloader file to the ECU connected via CANbus.
        /// </summary>
        /// <param name="bootloaderfile"></param>
        /// <returns></returns>
        public bool UploadBootLoader()
        {
            Console.WriteLine("Uploading bootloader");

            // Prevent ECU from causing trouble after power on-reset
            m_canListener.FlushQueue();

            // if can connection is available, upload the bootloader
            // now upload all the records in the S19 bootloader file
            using (StringReader sr = new StringReader(MyBooty))
            {
                
                string extraInfo     = string.Empty;
                string line          = string.Empty;
                int bytestransmitted = 0;
                int bytesread        = 0;

                while ((line = sr.ReadLine()) != null)
                {
                    bytesread += line.Length + 2;
                    if (line.StartsWith("S0"))
                    {
                        //if (!BinaryReflection.IsAppDebugMode(Application.ExecutablePath))
                        {
                            byte[] res = sendBootloaderAddressCommand(0, 0);
                            if ((res.Length != 8) || (res[7] != 0xA5) || (res[6] != 0x00))
                            {
                                extraInfo = "0L" + res.Length.ToString("X2") + " ";
                                foreach (byte b in res)
                                {
                                    extraInfo += b.ToString("X2") + " ";
                                }
                                CastInfoEvent("Could not start uploading bootloader..." + extraInfo, ActivityType.StartUploadingBootloader);
                                return false;
                            }
                        }
                        //AddToLog("Starting upload of bootloader");
                        CastInfoEvent("Starting upload of bootloader...", ActivityType.StartUploadingBootloader);
                    }
                    else if (line.StartsWith("S1")) // also support S1 lines
                    {
                        //S1137090267C000060DC4E93B9F90007FFFF63B65D
                        int len     = Convert.ToInt32(line.Substring(2, 2), 16);
                        byte[] data = new byte[len - 3]; // substract address (2 bytes) and checksum (1 byte)
                        int address = Convert.ToInt32(line.Substring(4, 4), 16);
                        int framecount, bytessent;

                        //Console.WriteLine("Sending " + len.ToString() + " bytes");
                        for (int t = 0; t < len - 3; t++)
                        {
                            int bytevalue = Convert.ToInt32(line.Substring((t * 2) + 8, 2), 16);
                            data.SetValue((byte)bytevalue, t);
                        }

                        framecount = 1 + (len - 3) / 7;
                        bytessent  = 0;
                        byte[] res = sendBootloaderAddressCommand((ushort)address, (byte)(len - 3));

                        if ((res.Length != 8) || (res[7] != 0xA5) || (res[6] != 0x00))
                        {
                            extraInfo = "1L" + res.Length.ToString("X2") + " ";
                            foreach (byte b in res)
                            {
                                extraInfo += b.ToString("X2") + " ";
                            }
                            CastInfoEvent("Could not start uploading bootloader..." + extraInfo, ActivityType.StartUploadingBootloader);
                            return false;
                        }
                        for (int frame = 0; frame < framecount; frame++)
                        {
                            //   t5can.sendBootloaderAddressCommand(address, len);
                            byte[] dataframe = new byte[8];
                            dataframe.SetValue((byte)(frame * 7), 0);
                            for (int bcnt = 0; bcnt < 7; bcnt++)
                            {
                                byte currbyte = 0;
                                if (bytessent < data.Length) currbyte = (byte)data.GetValue(bytessent);
                                bytessent++;
                                dataframe.SetValue(currbyte, bcnt + 1);
                            }
                            res = sendBootloaderDataCommand(dataframe, 8);
                            if ((res.Length != 8) || (res[7] != (byte)(frame * 7)) || (res[6] != 0x00))
                            {
                                extraInfo = "1L" + res.Length.ToString("X2") + " ";
                                foreach (byte b in res)
                                {
                                    extraInfo += b.ToString("X2") + " ";
                                }
                                CastInfoEvent("Could not upload bootloader..." + extraInfo, ActivityType.StartUploadingBootloader);
                                return false;
                            }
                        }
                        int prgs = (bytesread * 100) / (int)MyBooty.Length;
                        CastProgressWriteEvent(prgs);
                        bytestransmitted += (len - 3);
                        CastBytesTransmitted(bytestransmitted);
                    }
                    else if (line.StartsWith("S2"))
                    {
                        // parse the line
                        // S213005E6233FC303000FFFA5233FC503000FFFAAA
                        int len = Convert.ToInt32(line.Substring(2, 2), 16);
                        byte[] data = new byte[len - 4]; // substract address (3 bytes) and checksum (1 byte)
                        int address = Convert.ToInt32(line.Substring(4, 6), 16);
                        for (int t = 0; t < len - 4; t++)
                        {
                            int bytevalue = Convert.ToInt32(line.Substring((t * 2) + 10, 2), 16);
                            data.SetValue((byte)bytevalue, t);
                        }
                        int framecount = 1 + (len - 4) / 7;
                        int bytessent = 0;
                        byte[] res = sendBootloaderAddressCommand((ushort)address, (byte)(len - 4));
                        if ((res.Length != 8) || (res[7] != 0xA5) || (res[6] != 0x00))
                        {
                            extraInfo = "2L" + res.Length.ToString("X2") + " ";
                            foreach (byte b in res)
                            {
                                extraInfo += b.ToString("X2") + " ";
                            }
                            foreach (byte b in res)
                            {
                                extraInfo += b.ToString("X2") + " ";
                            }
                            CastInfoEvent("Could not start uploading bootloader..." + extraInfo, ActivityType.StartUploadingBootloader);
                            return false;
                        }
                        for (int frame = 0; frame < framecount; frame++)
                        {
                            //   t5can.sendBootloaderAddressCommand(address, len);
                            byte[] dataframe = new byte[8];
                            dataframe.SetValue((byte)(frame * 7), 0);
                            for (int bcnt = 0; bcnt < 7; bcnt++)
                            {
                                byte currbyte = 0;
                                if (bytessent < data.Length) currbyte = (byte)data.GetValue(bytessent);
                                bytessent++;
                                dataframe.SetValue(currbyte, bcnt + 1);
                            }
                            res = sendBootloaderDataCommand(dataframe, 8);
                            if ((res.Length != 8) || (res[7] != (byte)(frame * 7)) || (res[6] != 0x00))
                            {
                                extraInfo = "2L" + res.Length.ToString("X2") + " ";
                                foreach (byte b in res)
                                {
                                    extraInfo += b.ToString("X2") + " ";
                                }
                                CastInfoEvent("Could not upload bootloader..." + extraInfo, ActivityType.StartUploadingBootloader);
                                return false;
                            }
                        }
                        int prgs = (bytesread * 100) / (int)MyBooty.Length;
                        // Get rid of another warning
                        if (prgs > 99)
                            prgs = 100;
                        CastProgressWriteEvent(prgs);
                        bytestransmitted += (len - 4);
                        CastBytesTransmitted(bytestransmitted);
                    }
                    else if (line.StartsWith("S9"))
                    {
                        // terminate session with boot vector address to run SRAM from
                        // S9035000AC
                        int address = Convert.ToInt32(line.Substring(4, 4), 16);
                        // C1 00 00 50 00 00 00 06 
                        //if (!BinaryReflection.IsAppDebugMode(Application.ExecutablePath))
                        {
                            byte[] res = sendBootVectorAddressSRAM(address);
                            if ((res.Length != 8) /*|| (res[7] != 0xC1)*/ || (res[6] != 0x00))
                            {
                                extraInfo = "9L" + res.Length.ToString("X2") + " ";
                                foreach (byte b in res)
                                {
                                    extraInfo += b.ToString("X2") + " ";
                                }
                                CastInfoEvent("Could not execute bootloader..." + extraInfo, ActivityType.FinishedUploadingBootloader);
                                return false;
                            }
                        }
                        //AddToLog("Jumping to SRAM to start executing bootloader");
                        CastInfoEvent("Executing bootloader...", ActivityType.FinishedUploadingBootloader);

                    }
                    else if (line.StartsWith("S8"))
                    {
                        // terminate session with boot vector address to run SRAM from
                        // S8040060C4D7
                        int address = Convert.ToInt32(line.Substring(4, 6), 16);
                        // C1 00 00 60 C4 00 00 06 
                        //if (!BinaryReflection.IsAppDebugMode(Application.ExecutablePath))
                        {
                            byte[] res = sendBootVectorAddressSRAM(address);
                            if ((res.Length != 8) /*|| (res[7] != 0xC1)*/ || (res[6] != 0x00))
                            {
                                extraInfo = "8L" + res.Length.ToString("X2") + " ";
                                foreach (byte b in res)
                                {
                                    extraInfo += b.ToString("X2") + " ";
                                }
                                CastInfoEvent("Could not execute bootloader..." + extraInfo, ActivityType.FinishedUploadingBootloader);
                                return false;
                            }
                        }
                        //AddToLog("Jumping to SRAM to start executing bootloader");
                        CastInfoEvent("Executing bootloader...", ActivityType.FinishedUploadingBootloader);

                    }
                    else if (line.StartsWith("S7"))
                    {
                        // terminate session with boot vector address to run SRAM from
                        // S8040060C4D7
                        // S705205C00007E
                        int address = Convert.ToInt32(line.Substring(4, 8), 16);
                        // C1 00 00 60 C4 00 00 06 
                        //if (!BinaryReflection.IsAppDebugMode(Application.ExecutablePath))
                        {
                            byte[] res = sendBootVectorAddressSRAM(address);
                            if ((res.Length != 8) /*|| (res[7] != 0xC1)*/ || (res[6] != 0x00))
                            {
                                extraInfo = "7L" + res.Length.ToString("X2") + " ";
                                foreach (byte b in res)
                                {
                                    extraInfo += b.ToString("X2") + " ";
                                }
                                CastInfoEvent("Could not execute bootloader..." + extraInfo, ActivityType.FinishedUploadingBootloader);
                                return false;
                            }
                        }
                        //AddToLog("Jumping to SRAM to start executing bootloader");
                        CastInfoEvent("Executing bootloader...", ActivityType.FinishedUploadingBootloader);
                    }
                    else
                    {
                        CastInfoEvent("Unknown S record seen " + line, ActivityType.UploadingBootloader);
                        return false;
                    }
                }
            }
            //progressBar1.Value = 0; // reset
            return true;
        }

        /// <summary>
        /// Erases the flash in the ECU by sending the delete command to the bootloader running in the ECUs SRAM
        /// </summary>
        /// <returns></returns>
        public bool EraseFlash()
        {
            CastInfoEvent("Erasing FLASH...", ActivityType.StartErasingFlash);

            byte[] res       = sendBootLoaderEraseCommand();
            string extraInfo = string.Empty;

            if ((res[7] == 0xC0) && (res[6] == 0x00))
            {
                CastInfoEvent("FLASH erased... ", ActivityType.FinishedErasingFlash);
                return true;
            }
            else
            {
                foreach (byte b in res)
                {
                    extraInfo += b.ToString("X2") + " ";
                }
                CastInfoEvent("Could Not Erase FLASH !!! " + extraInfo, ActivityType.FinishedErasingFlash);
                return false;
            }
        }

        /// <summary>
        /// Programs the flash in the ECU by sending the S19 file in pieces to the bootloader running in the ECUs SRAM
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool ProgramFlash(string filename, ECUType type)
        {
            // if can connection is available, upload the bootloader
            // now upload all the records in the S19 bootloader file
            FileInfo fi = new FileInfo(filename);

            using (StreamReader sr = new StreamReader(filename))
            {
                string line          = string.Empty;
                byte[] dataframe     = new byte[8];
                int bytestransmitted = 0;
                int prgs, bytesread  = 0;
                int len, framecount;
                int bytessent;
                Int32 address;
                byte currbyte;

                while ((line = sr.ReadLine()) != null)
                {
                    bytesread += line.Length + 2;
                    if (line.StartsWith("S0"))
                    {
                        //if (!BinaryReflection.IsAppDebugMode(Application.ExecutablePath))
                        {
                            // maybe not necessary t5can.sendBootloaderAddressCommand(0, 0);
                        }
                        // AddToLog("Starting upload of new flash");
                        CastInfoEvent("Start upload of new program...", ActivityType.StartFlashing);
                    }
                    else if (line.StartsWith("S2"))
                    {
                        len         = Convert.ToInt32(line.Substring(2, 2), 16);
                        byte[] data = new byte[len - 4]; // substract address (3 bytes) and checksum (1 byte)
                        address     = Convert.ToInt32(line.Substring(4, 6), 16);

                        // add flash start offset address
                        address += ((type == ECUType.T52ECU)) ? 0x60000 : 0x40000;

                        for (int t = 0; t < len - 4; t++)
                        {
                            int bytevalue = Convert.ToInt32(line.Substring((t * 2) + 10, 2), 16);
                            data.SetValue((byte)bytevalue, t);
                        }

                        framecount = 1 + (len - 4) / 7;
                        bytessent = 0;
                        sendBootloaderAddressCommand(address, (byte)(len - 4));

                        /** option 1 **/
                        for (int frame = 0; frame < framecount; frame++)
                        {
                            dataframe.SetValue((byte)(frame * 7), 0);
                            for (int bcnt = 0; bcnt < 7; bcnt++)
                            {
                                currbyte = 0;
                                if (bytessent < data.Length) currbyte = (byte)data.GetValue(bytessent);
                                bytessent++;
                                dataframe.SetValue(currbyte, bcnt + 1);
                            }
                            sendBootloaderDataCommand(dataframe, 8);
                        }
                        /** end option 1 **/

                        /** option 2 **/
                        /*for (int frame = 0; frame < framecount; frame++)
                        {
                            int dtlen = data.Length - bytessent;
                            if (dtlen > 7) dtlen = 7;
                            t5can.sendBootloaderAddressCommand(address, (byte)dtlen);
                            byte[] dataframe = new byte[8];
                            int bscnt = 0;
                            for (int bcnt = 0; bcnt < 7; bcnt++)
                            {
                                byte currbyte = 0;
                                if (bytessent < data.Length)
                                {
                                    currbyte = (byte)data.GetValue(bytessent);
                                    bscnt++;
                                }
                                bytessent++;
                                dataframe.SetValue(currbyte, bcnt + 1);
                            }
                            dataframe.SetValue((byte)(bscnt), 0);
                            t5can.sendBootloaderDataCommand(dataframe, 8);
                            address += 7; // last one don't care because it will be recalculated
                            //Thread.Sleep(10);
                        }
                        */
                        /** end option 2 **/
                        bytestransmitted += (len - 4);
                        prgs = (bytesread * 100) / (int)fi.Length;
                        CastProgressWriteEvent(prgs);
                        CastBytesTransmitted(bytestransmitted);

                    }
                    else if (line.StartsWith("S8"))
                    {
                        // terminate session with boot vector address to run SRAM from
                        // S8040060C4D7
                        //int address = Convert.ToInt32(line.Substring(4, 6), 16);
                        // C1 00 00 60 C4 00 00 06 
                        //t5can.sendBootVectorAddressSRAM(address);
                        //Thread.Sleep(500);
                        //DumpChecksum();
                        //Thread.Sleep(100);
                        CastInfoEvent("Flash programming finished", ActivityType.FinishedFlashing);
                    }
                    else
                    {
                        //AddToLog("Unknown S record seen: " + line);

                    }
                }
            }
            //progressBar1.Value = 0; // reset
            //sendC2Command(); // reset ECU
            return true;
        }


        /// <summary>
        /// Programs the flash in the ECU by sending a BIN file in pieces to the bootloader running in the ECUs SRAM
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool ProgramFlashBin(string filename, ECUType type)
        {
            // if can connection is available, upload the bootloader
            // now upload all the records in the S19 bootloader file

            // if all else fails, revert back to S19 files, so convert the bin to S19 and program that
            // with ProgramFlash function!

            FileInfo fi = new FileInfo(filename);

            using (FileStream fs = File.OpenRead(filename))
            {
                CastInfoEvent("Start upload of new program...", ActivityType.StartFlashing);

                int start     = (type == ECUType.T52ECU) ? 0x60000 : 0x40000;
                int bytesread = 0;

                while ((start + bytesread) < 0x80000)
                {
                    // read a section of 0x80 bytes from the BIN file and keep it in a buffer to send to the T5 ECU
                    byte[] bytes = new byte[0x80];
                    // something has gone wrong if we cannot read another 0x80 bytes!!
                    if (fs.Read(bytes, 0,/*bytesread,*/ 0x80) != 0x80) //<GS-22092010> parameter error!
                    {
                        string BytesSoFar = bytesread.ToString("X6");
                        CastInfoEvent("Reading the BIN File Failed after: 0x" + BytesSoFar + " Bytes !!!", ActivityType.UploadingFlash);
                        return false;
                    }
                    // send a bootloader address message
                    byte[] result = sendBootloaderAddressCommand((start + bytesread), 0x80);
                    //DEBUG ONLY
                    //string extraInfo = string.Empty;
                    //foreach (byte b in result)
                    //{
                    //    extraInfo += b.ToString("X2") + " ";
                    //}
                    //CastInfoEvent("send address A5 command: " + extraInfo, ActivityType.UploadingFlash);
                    //DEBUG ONLY
                    bytesread += 0x80;
                    byte[] dataframe = new byte[8];

                    // Construct and send the bootloader frames
                    // NOTE the last frame sent may have less than 7 real data bytes but 7 bytes are always sent. In this case the unnecessary bytes
                    // are repeated from the previous frame. This is OK because the T5 ECU knows how many bytes to expect (because the count of bytes
                    // in the S-Record is sent with the upload address) and ignores any extra bytes in the last frame.
                    for (int i = 0; i < 0x80; i++)
                    {
                        // set the index number
                        if (i % 7 == 0)
                            dataframe.SetValue((byte)(i), 0);
                        // put bytes them in the dataframe!
                        dataframe.SetValue(bytes[i], (i % 7) + 1);
                        // send a bootloader frame whenever 7 bytes or a block of 0x80 bytes have been read from the BIN file
                        if ((i % 7 == 6) || (i == 0x80 - 1))
                        {
                            byte[] result2 = sendBootloaderDataCommand(dataframe, 8);
                            //DEBUG ONLY
                            //string extraInfo = string.Empty;
                            //foreach (byte b in result2)
                            //{
                            //    extraInfo += b.ToString("X2") + " ";
                            //}
                            //CastInfoEvent("send data command: " + extraInfo, ActivityType.UploadingFlash);
                            //DEBUG ONLY
                            if ((byte)result2.GetValue(6) != 0x00)
                            {
                                string BytesSoFar = bytesread.ToString("X6");
                                CastInfoEvent("FLASHing Failed after: 0x" + BytesSoFar + " Bytes !!!", ActivityType.UploadingFlash);
                                return false;
                            }
                        }
                        // show progress information
                    }
                    int prgs = (type == ECUType.T52ECU) ? ((bytesread * 100) / 0x20000) : ((bytesread * 100) / 0x40000);
                    CastProgressWriteEvent(prgs);
                    CastBytesTransmitted(bytesread);
                    // 
                    // Rewind to start of file if FLASHing a T5.2 BIN file to A T5.5 ECU
                    // to FLASH 2 copies of the BIN File to T5.5's larger FLASH chips
                    //
                    if ((type != ECUType.T52ECU) && (bytesread == 0x20000) && (fi.Length == 0x20000))
                    {
                        fs.Seek(0, SeekOrigin.Begin);
                    }
                }
                CastInfoEvent("Flash programming finished", ActivityType.FinishedFlashing);
            }
            return true;
        }

        /// <summary>
        /// Dumps the SRAM content to a binary file
        /// </summary>
        /// <param name="filename"></param>
        public void DumpSRAMContent(string filename)
        {
            //read 256 bytes per cycle in a loop until 0x8000 is read
            //this will enable a progress bar when downloading SRAM
            CastInfoEvent("Downloading adaption data...", ActivityType.DownloadingSRAM);

            byte[] sram = readRAM(0, 0x8000);
            DumpAdaptionData(filename, sram);

            CastInfoEvent("Adaption data saved...", ActivityType.DownloadingSRAM);
        }

        /// <summary>
        /// Dumps the given adaption data to a binary file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="buffer"></param>
        private void DumpAdaptionData(string filename, byte[] buffer)
        {
            FileStream fs = new FileStream(filename, FileMode.Create);
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write(buffer);
                bw.Flush();
            }
            fs.Close();
            fs.Dispose();
        }

        private string readpartnumber(string m_currentfile)
        {
            int length = 0;
            string value = string.Empty;
            int offset = ReadMarkerAddress(m_currentfile, 0x01, out length, out value);
            string retval = string.Empty;
            FileStream fsi1 = File.OpenRead(m_currentfile);
            BinaryReader br1 = new BinaryReader(fsi1);
            fsi1.Position = offset - length;
            string temp = string.Empty;
            for (int i = 0; i < length; i++)
            {
                temp += (char)br1.ReadByte();
            }
            for (int i2 = 0; i2 < length; i2++)
            {
                retval += temp[(length - 1) - i2];
            }
            fsi1.Flush();
            br1.Close();
            fsi1.Close();
            fsi1.Dispose();
            return retval;
        }

        /// <summary>
        /// Gets the flash offset from the footer file downloaded from the ECU (through bootloader code)
        /// </summary>
        /// <param name="m_currentfile"></param>
        /// <returns></returns>
        private string readflashoffset(string m_currentfile)
        {
            int length = 0;
            string value = string.Empty;
            int offset = ReadMarkerAddress(m_currentfile, 0xFD, out length, out value);
            string retval = string.Empty;
            FileStream fsi1 = File.OpenRead(m_currentfile);
            BinaryReader br1 = new BinaryReader(fsi1);
            fsi1.Position = offset - length;
            string temp = string.Empty;
            for (int i = 0; i < length; i++)
            {
                temp += (char)br1.ReadByte();
            }
            for (int i2 = 0; i2 < length; i2++)
            {
                retval += temp[(length - 1) - i2];
            }
            fsi1.Flush();
            br1.Close();
            fsi1.Close();
            fsi1.Dispose();
            return retval;
        }

        /// <summary>
        /// Reads a marker address from a trionic 5 footer file (downloaded by this program)
        /// </summary>
        /// <param name="m_currentfile"></param>
        /// <param name="value"></param>
        /// <param name="length"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        private int ReadMarkerAddress(string m_currentfile, int value, out int length, out string val)
        {
            val        = string.Empty;
            length     = 0;
            int retval = 0;

            if (m_currentfile != string.Empty)
            {
                if (File.Exists(m_currentfile))
                {
                    // read the file footer
                    //3ff00 - 0x3ffff
                    FileStream fs = new FileStream(m_currentfile, FileMode.Open, FileAccess.Read);
                    BinaryReader br = new BinaryReader(fs);
                    //                    int fileoffset = m_currentfile_size - 0x100;
                    //                    fs.Seek(/*0x3FF00*/fileoffset, SeekOrigin.Begin);
                    byte[] inb = br.ReadBytes(0xFF);
                    //int offset = 0;
                    for (int t = 0; t < 0xFF; t++)
                    {
                        if (((byte)inb.GetValue(t) == (byte)value) && ((byte)inb.GetValue(t + 1) < 0x30))
                        {
                            // marker gevonden
                            // lees 6 terug
                            retval = /*fileoffset +*/ t;
                            length = (byte)inb.GetValue(t + 1);
                            break;
                        }
                    }
                    fs.Seek((retval - length), SeekOrigin.Begin);
                    byte[] info = br.ReadBytes(length);
                    for (int bc = info.Length - 1; bc >= 0; bc--)
                    {
                        val += Convert.ToChar(info.GetValue(bc));
                    }
                    fs.Flush();
                    fs.Close();
                    fs.Dispose();

                }
            }

            return retval;
        }

        //TODO: nog een functie maken die de checksum van de geflashte ECU controleert
        // met command via canbus of via download flash & verify
        //TODO: still a function that the checksum of the monitors flashed ECU
        // Command to flash via CAN bus or via download & verify

        /// <summary>
        /// Dumps the flash contents of a ECU to a binary file. Be sure to pass the correct ECUType
        /// </summary>
        /// <param name="flashfile"></param>
        /// <param name="type"></param>
        public void DumpECU(object sender, DoWorkEventArgs workEvent)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            string filename = (string)workEvent.Argument;

            CastInfoEvent("Start ECU dump", ActivityType.StartDownloadingFlash);
            workEvent.Result = false;

            // Modified to only continue if the bootloader could be uploaded
            if (UploadBootLoader())
            {
                CastInfoEvent("Bootloader uploaded", ActivityType.StartDownloadingFlash);
                CastInfoEvent("Determining ECU type", ActivityType.StartDownloadingFlash);
                ECUType type = DetermineECU();

                // No point in fetching two copies on t55ast52 (And t52 only has 128K of flash)
                UInt32 start = 0x40000;
                if (type == ECUType.T52ECU || type == ECUType.T55AST52)
                    start = 0x60000;

                UInt32 length = 0x80000 - start;
                byte[] buffer = new byte[length];

                CastInfoEvent("Downloading flash from ECU", ActivityType.StartDownloadingFlash);
                //TODO: hier het aantal keren dat gelezen moet worden nog aanpassen aan het ECUType
                //TODO: specify the times that must be read even adjust the ECUType
                byte[] buffer2 = new byte[6];
                UInt32 address = (start + 5);
                for (int i = 0; i < (length / 6); i++)
                {
                    buffer2 = this.sendReadCommand(address);
                    address += 6;
                    for (int j = 0; j < 6; j++)
                        buffer[(i * 6) + j] = buffer2[j];
                    CastProgressWriteEvent((int)((i * 600) / length));
                    CastBytesTransmitted((int)(address - (start + 5)));
                }
                // Mop up the last few bytes if not an exact multiple of 6
                // It has to be done this way because the T5 ECU resets if
                // you try to read past the end of the FLASH addresses
                if ((length % 6) > 0)
                {
                    buffer2 = this.sendReadCommand(start + length - 1);
                    for (int j = (int)(6 - (length % 6)); j < 6; j++)
                        buffer[length - 6 + j] = buffer2[j];
                }

                // Todo: Check if dump is correct!
                ExitBootloader();
                CastInfoEvent("Finished downloading flash from ECU", ActivityType.FinishedDownloadingFlash);
                CastProgressWriteEvent(100);
                CastBytesTransmitted((int)length);

                File.WriteAllBytes(filename, buffer);
                Md5Tools.WriteMd5HashFromByteBuffer(filename, buffer);
                workEvent.Result = true;
            }
            else
            {
                CastInfoEvent("Could not upload bootloader", ActivityType.FinishedDownloadingFlash);
            }
        }

        /// <summary>
        /// Determines the ECU type based on the footer that is downloaded from the ECU. A bootloader is uploaded to do so.
        /// </summary>
        /// <param name="flashfile"></param>
        /// <returns></returns>
        public ECUType GetECUType(string flashfile)
        {
            byte[] retval = new byte[0x100];
            Console.WriteLine("Start ECU dump");
            string path = Path.GetDirectoryName(flashfile);
            Console.WriteLine("Path = " + path);
            Console.WriteLine("Uploading bootloader");
            UploadBootLoader();
            Console.WriteLine("Bootloader uploaded");
            CastInfoEvent("Getting data from ECU", ActivityType.StartDownloadingFooter);

            byte[] buffer2 = new byte[6];
            UInt32 address = (0x7FF00 + 5);
            for (int i = 0; i < (0x100 / 6); i++)
            {
                buffer2 = this.sendReadCommand(address);
                address += 6;
                for (int j = 0; j < 6; j++)
                    retval[(i * 6) + j] = buffer2[j];
                CastProgressWriteEvent((int)((i * 100) / (0x100 / 6)));
                CastBytesTransmitted((int)(address - (0x7FF00 + 5)));
            }
            // Mop up the last few bytes if not an exact multiple of 6
            // It has to be done this way because the T5 ECU resets if
            // you try to read past the end of the FLASH addresses
            buffer2 = this.sendReadCommand(0x7FFFF);
            for (int j = 2; j < 6; j++)
                retval[0x100 - 6 + j] = buffer2[j];
            CastProgressWriteEvent(100);
            CastBytesTransmitted(0x100);

            CastInfoEvent("Finished getting data from ECU", ActivityType.FinishedDownloadingFooter);
            //DownloadFlashContent();
            //TODO hier nog het ECU type bepalen adhv de footer informatie die we al opgehaald hebben
            //TODO still the ECU type determination means of the most footer information we have already retrieved
            FileStream fs = new FileStream(flashfile, FileMode.Create);
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write(retval);
            }
            fs.Close();
            fs.Dispose();
            ECUType rettype = ECUType.Unknown;
            try
            {
                //Console.WriteLine("ECUType information: " + readflashoffset(flashfile));
                int flashoffset = Convert.ToInt32(readflashoffset(flashfile), 16);
                if (flashoffset == 0x60000) rettype = ECUType.T52ECU;
                if (flashoffset == 0x40000) rettype = ECUType.T55ECU20MHZ;
            }
            catch (Exception E)
            {
                Console.WriteLine("Failed to determine boxtype: " + E.Message);
            }

            return rettype;
        }

        /// <summary>
        /// Dumps the checksum (from C8 command, only available when bootloader is running on certain ECUs) to the console
        /// </summary>
        private void DumpChecksum()
        {
            byte[] checksumbytes = getChecksum();
            foreach (byte b in checksumbytes)
            {
                Console.WriteLine(b.ToString("X2"));

            }
        }


        /// <summary>
        /// Upgrades the ECU with the given file. Be sure to pass the right ECUType 
        /// </summary>
        /// <param name="flashfile"></param>
        /// <param name="ecutype"></param>
        private UpgradeResult UpgradeECU(string flashfile, ECUType ecutype)
        {
            UpgradeResult result = UpgradeResult.Success;
            string path = Path.GetDirectoryName(flashfile);
            //ecutype = DetermineConnectedECUType(); //<GS-22032011> Does not matter anymore what ECUtype is in the box?
            // hier nog controleren of de lengte van de bin wel past bij het ECUType

            if (!EraseFlash())
            {
                return UpgradeResult.EraseFailed;
            }
            if (!ProgramFlashBin(flashfile, ecutype))
            {
                return UpgradeResult.ProgrammingFailed;
            }
            if (!VerifyChecksum())
            {
                result = UpgradeResult.ChecksumFailed;
            }
            CastProgressWriteEvent(100);

            return result;
        }


        /*
        public ChecksumResult VerifyECU(ECUType ecutype, string path)
        {
            ChecksumResult result = ChecksumResult.Ok;
            UploadBootLoader();
            //                DumpChecksum(); 
            if (!VerifyChecksum())
            {
                result = ChecksumResult.Invalid;
            }
            ExitBootloader();
            return result;

        }*/

        public bool VerifyChecksum()
        {
            byte[] res = getChecksum();
            if (res.Length == 8)
            {
                //if ((byte)checksumbytes.GetValue(6) != 0x00) retval = false;
                if (res[6] == 0x00)
                {
                    string Checksum = string.Empty;
                    for (int i = 5; i > 1; i--)
                    {
                        Checksum += res[i].ToString("X2");
                    }
                    CastInfoEvent("FLASH Checksum OK: " + Checksum, ActivityType.CalculatingChecksum);
                    return true;
                }
                else
                {
                    string extraInfo = string.Empty;
                    foreach (byte b in res)
                    {
                        extraInfo += b.ToString("X2") + " ";
                    }
                    CastInfoEvent("Checksum FAIL !!! " + extraInfo, ActivityType.CalculatingChecksum);
                    return false;
                }
            }
            CastInfoEvent("Could NOT Determine Checksum !!!", ActivityType.CalculatingChecksum);
            return false;
        }

        public string ReturnChecksum()
        {
            string Checksum = string.Empty;
            byte[] res = getChecksum();
            if ((res.Length == 8) && (res[7] == 0xC8) && (res[6] == 0x00))
            {
                for (int i = 5; i > 1; i--)
                {
                    Checksum += res[i].ToString("X2");
                }
            }
            else
            {
                Checksum = "ERROR";
            }
            return Checksum;
        }

        public void ExitBootloader()
        {
            sendC2Command(); // reset ECU
            CastInfoEvent("ECU is reset", ActivityType.FinishedFlashing);
        }

        /// <summary>
        /// Gets the checksum from the ECU, dumps it to the console and tries to get the ECU out of the bootloader routine
        /// </summary>
        /// <param name="type"></param>
        /// <param name="path"></param>
        public void GetChecksumWithBootloader(ECUType type, string path)
        {
            UploadBootLoader();
            DumpChecksum();
            sendC2Command();
        }
    }



    public enum UpgradeResult : int
    {
        Success,
        InvalidFile,
        InvalidECUType,
        EraseFailed,
        ProgrammingFailed,
        ChecksumFailed
    }

    public enum ECUType : int
    {
        T52ECU,
        T55ECU16MHZAMDIntel,
        T55ECU16MHZCatalyst,
        T55ECU20MHZ,
        Autodetect,
        Unknown,
        T55ECU,
        T55AST52,
    }

    public enum ECUIdentifier : int
    {
        Partnumber = 0x01,
        SoftwareID = 0x02,
        Dataname   = 0x03, // SW Version
        EngineType = 0x04,
        ImmoCode   = 0x05,
        Unknown    = 0x06,
        ROMend     = 0xFC, // Always 07FFFF
        ROMoffset  = 0xFD, // T5.5 = 040000, T5.2 = 020000
        CodeEnd    = 0xFE
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
        private int _percentage;

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

        public int Percentage
        {
            get { return _percentage; }
            set { _percentage = value; }
        }

        public WriteProgressEventArgs(int percentage)
        {
            this._percentage = percentage;
        }

        public WriteProgressEventArgs(int percentage, int bytestowrite, int byteswritten)
        {
            this._bytestowrite = bytestowrite;
            this._byteswritten = byteswritten;
            this._percentage = percentage;
        }
    }

    public class ReadProgressEventArgs : System.EventArgs
    {
        private int _percentage;

        public int Percentage
        {
            get { return _percentage; }
            set { _percentage = value; }
        }

        public ReadProgressEventArgs(int percentage)
        {
            this._percentage = percentage;
        }
    }

    /// <summary>
    /// Generate, uploads and start the given bootloader file to the ECU connected via CANbus.
    /// </summary>
    /// <param name="bootloaderfile"></param>
    /// <returns></returns>

}
