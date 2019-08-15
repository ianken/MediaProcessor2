using System;
using System.Diagnostics;

namespace LibMediaProcessor
{
    /// <summary>
    /// FFBase manages all of the common FFEMPG support logic and parameters.
    /// For example, MPEG transport and program files need specific arguments to parse properly when scanning or encoding.
    /// Maps generalized settings (like encode speed) to FFMPEG specific setting strings.
    ///  </summary>
    public partial class FFBase
    {
        //Used by base and derived classes...
        protected readonly Utilities utils;

        //MPEG files - these require special args to FFMPEG to ensure proper timecode behavior
        protected readonly string[] MpegFiles = { "mpg", "mpeg", "ts", "m2ts" };

        //Aggressive probesize ensures we detect some problematic media like S302 audio...
        //Default FFEMPG initial args. 
        protected readonly string FFRootCmdDefault = " -hide_banner -probesize 50000000 -y ";
        //Special flags needed when reading MPEG program and transport streams.
        protected readonly string FFRootCmdMpg = " -hide_banner -probesize 50000000 -fflags +genpts -y ";

        //Filter chains for scaling operations...
        //These allow for scaling video to a target resolution while maintaining aspect ratio and adding letter or pillar boxing as needed.
        //This is used to match media. IE: scale a behind the scenes video shot at 4:3 SD to a main feature shot in 16:9 HD. 
        protected readonly string ScaleVideoToTarget =
            "scale=iw*sar:ih[a];[a]scale=-4:ih[b];[b]scale=iw*min({0}/iw\\,{1}/ih):ih*min({0}/iw\\,{1}/ih)[c];[c]pad={0}:{1}:({0}-iw*min({0}/iw\\,{1}/ih))/2:({1}-ih*min({0}/iw\\,{1}/ih))/2[d];[d]setsar=1:1";
        protected readonly string ScaleVideoToTargetSquare =
            "scale=iw*min({0}/iw\\,{1}/ih):ih*min({0}/iw\\,{1}/ih)[c];[c]pad={0}:{1}:({0}-iw*min({0}/iw\\,{1}/ih))/2:({1}-ih*min({0}/iw\\,{1}/ih))/2[d];[d]setsar=1:1";

        //De-interlacing filters

        //Used when source is cleanly telecined. Decimate to film FPS 
        protected static readonly string DeintFilter_PureTelecine = "fieldmatch=order=auto:combmatch=full:combpel=80:cthresh=8,decimate";
        //For mixed sources when video frame rate should be preserved. IE: old Anime that's a mess.
        protected static readonly string DeintFilter_VideoBias = "fieldmatch=order=auto:combmatch=full:combpel=90:cthresh=9,yadif=deint=interlaced";
        //For mixed sources where decimation is OK.
        //IE: media where opening and closing credits are combed video but feature is cleanly telecined.
        protected static readonly string DeintFilter_FilmBias = "fieldmatch=order=auto:combmatch=full:combpel=80:cthresh=8,decimate,yadif=deint=interlaced";
        //Used when source has no film telecine pattern - SD
        protected static readonly string DeintFilter_PureVideoSD = "yadif=1:0,mcdeint=0:0:10,framestep=2";
        //Used when source has no film telecine pattern - HD
        protected static readonly string DeintFilter_PureVideoHD = "yadif";
        
        //Callbacks for console output parsing
        private readonly Utilities.ProgressParser DefaultStdOutParser;
        private readonly Utilities.ProgressParser DefaultStdErrParser;

        //These are used by the callbacks
        private decimal duration;   //Duration, allows callback to report progress.
        private string stdErrScan;  //Accumulates all console output from StdError.
        private string stdOutScan;  //Accumulates all console output from StdOut.

        public FFBase()
        {
            this.utils = new Utilities();
            this.DefaultStdOutParser = this.FFStdOutParser;
            this.DefaultStdErrParser = this.FFStdErrParser;
        }

        /// <summary>
        /// Executes FFEMPG with the given cmd line. 
        /// <param name="cmd"> FFEMPG commands to execute.</param>
        /// <param name="dur"> Duration of the media being processed.</param>
        /// Returns console output
        /// </summary>
        public ConsoleOutput  ExecuteFFMpeg_LogProgress(string cmd, decimal dur)
        {
            this.duration = dur;
            //reset to avoid bad data on multiple runs...
            this.stdErrScan = "";
            this.stdOutScan = "";

            this.utils.ExecuteCommand_Parser(this.utils.mediaTools.FfEncPath, cmd, this.DefaultStdOutParser, this.DefaultStdErrParser);
            return new ConsoleOutput(this.stdOutScan,this.stdErrScan);
        }

        /// <summary>
        /// Parser to run report progress while running FFMPEG
        /// </summary>
        private void FFStdOutParser(object sendingProcess, DataReceivedEventArgs data)
        {
            if (!String.IsNullOrEmpty(data.Data))
            {
                this.stdOutScan += $"{data.Data}\r\n";
            }
        }

        /// <summary>
        /// Parser to run report progress while running FFMPEG
        /// </summary>
        private void FFStdErrParser(object sendingProcess, DataReceivedEventArgs data)
        {
            TimeSpan ts = TimeSpan.FromSeconds((int)this.duration);

            if (!String.IsNullOrEmpty(data.Data))
            {
             
                if (data.Data.Contains("fps") && data.Data.Contains("time"))
                {
                    string[] msgs = data.Data.Split(' ');
                    foreach (string s in msgs)
                    {
                        if (s.Contains("time"))

                            this.utils.LogProxy(
                                $"Progress: {s.Split('=')[1]} of {ts.Hours:D2}H:{ts.Minutes:D2}M:{ts.Seconds:D2}S",
                                Utilities.LogLevel.Info, false);
                    }
                }
                else
                {
                    this.stdErrScan += $"{data.Data}\r\n";
                }
            }
        }
    }
}
