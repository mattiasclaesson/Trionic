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
        [Description("Kvaser")]
        KVASER,
        [Description("J2534")]
        J2534
    };

    public enum ECU : int
    {
        TRIONIC5,
        TRIONIC7,
        TRIONIC8,
        MOTRONIC96,
        TRIONIC8_MCP,
        Z22SEMain_LEG,
        Z22SEMCP_LEG
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
