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
        FinishedDownloadingFooter
    }
    
    public enum CANBusAdapter : int
    {
        [Description("Lawicel CANUSB")]
        LAWICEL,
        [Description("CombiAdapter")]
        COMBI,
        [Description("ELM327 v1.3 or higher")]
        ELM327,
        [Description("Just4Trionic")]
        JUST4TRIONIC,
        [Description("Kvaser")]
        KVASER,
        [Description("OBDLink MX WiFi")]
        MXWIFI
    };

    public enum ECU : int
    {
        TRIONIC7,
        TRIONIC8,
        MOTRONIC96
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
}
