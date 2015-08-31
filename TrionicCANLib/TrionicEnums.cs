using System;
using System.Collections.Generic;
using System.Linq;

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
        LAWICEL,
        COMBI,
        ELM327,
        JUST4TRIONIC,
        KVASER,
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
