using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace TrionicCANLib.API
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
        FinishedDownloadingFooter,
        CalculatingChecksum,
        QueryingECUTypeInfo
    }
    
    public enum CANBusAdapter : int
    {
        [Description("Lawicel CANUSB")]
        LAWICEL,
        [Description("CombiAdapter")]
        COMBI,
        [Description("OBDLink SX")]
        ELM327,
        [Description("Just4Trionic")]
        JUST4TRIONIC,
        [Description("Kvaser HS")]
        KVASER,
        [Description("J2534")]
        J2534
    };

    public enum ECU : int
    {
        [Description("Trionic 5")]
        TRIONIC5,
        [Description("Trionic 7")]
        TRIONIC7,
        [Description("Trionic 8; Main")]
        TRIONIC8,
        [Description("Trionic 8; MCP (Experimental)")]
        TRIONIC8_MCP,

        [Description("Bosch ME9.6")]
        MOTRONIC96,

        [Description("Z22SE; Main (Experimental)")]
        Z22SEMain_LEG,
        [Description("Z22SE; MCP (Experimental)")]
        Z22SEMCP_LEG,


        // Sorry for moving these but the enumration gets borked when items are "disabled"

        // [Description("Bosch ME9.6.1")]
        MOTRONIC961,

        // [Description("EDC16C39 car XX")]
        EDC16C39,

        // [Description("EDC17C19")]
        EDC17C19
    };

    public enum SleepTime : int
    {
        Default = 2,
        ELM327 = 0
    };

    public enum ComSpeed : int
    {
        S115200,
        S230400,
        S1Mbit,
        S2Mbit
    };

    public enum Latency : int
    {
        Low,
        Default
    };
}
