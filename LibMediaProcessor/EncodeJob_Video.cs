using System.Collections.Generic;

namespace LibMediaProcessor
{
   
    public class VideoEncodeJob : EncodeJob
    {
    
        public enum OutputColorSpec
        {
            Unknown = 0,
            REC709 = 1,
            REC601,
            HDR10,
            DolbyVision
        }

        public enum EncodeSpeed
        {
            Slow = 1,
            Fast,
            Faster
        }

        public enum PixelFormat
        {
            YUV420p = 1,
            YUV420p10,
            YUV422p10,
        }
        public enum DeinterlaceOverride
        {
            None = 0,
            Mixed_FilmBias,
            Mixed_VideoBias,
            PureTelecine,
            PureVideoSD,
            PureVideoHD
        }

        //Allow automatic cropping?
        public bool AutoCrop { get; set; }
        //GOP structure is fixed across all streams
        public int GopLengthSeconds { get; set; } = 4;
        //Rate control lookahead
        public int LookAheadFrames { get; set; } = 48;
        //If match width and height are set then the scale filter will scale
        //to the specified dimensions before any further processing.
        //This is to facilitate matching one piece of media to another
        //IE: scale a dub-card to match the feature content.
        public int MatchWidth { get; set; } = 0;
        public int MatchHeight { get; set; } = 0;
        //Match rate: this will force the output encode to a specific frame rate.
        //It can introduce judder. For dub-card and extras matching.
        public decimal MatchRate { get; set; } = 0;
        //If set, this job requires subtitles be burned into all output video.
        public string BurnSubs { get; set; } = string.Empty;

        public OutputColorSpec ColorSpec { get; set; }
        public EncodeSpeed Preset { get; set; } = EncodeSpeed.Fast;
        public PixelFormat PixelFmt { get; set; } = PixelFormat.YUV420p;
        public DeinterlaceOverride DeintOverride { get; set; } = DeinterlaceOverride.None;
        //Job HDR metadata:
        public MasteringDisplayPrimaries HdrMasteringDisplayPrimaries { get; set; }
        public MasteringDisplayLuminance HdrMasteringDisplayLuminance { get; set; }
        public Cea8613HdrData HdrCea8613HdrData { get; set; }


        //Output streams to generate
        public List<VideoOutputDefinition> OutputMedia = new List<VideoOutputDefinition>();

        public VideoEncodeJob(string language, string outputFolder) :
            base(language, outputFolder)
        {
        }

        /// <summary>
        /// Adds input media to job
        /// </summary>
        /// <param name="m">MediaAttributes object describing a single input</param>
        public void AddInputMedia(MediaProperties m)
        {
            //Only one source per video encode job is permitted...
            if (this.InputMedia.Count == 1)
            {
                this.utils.LogProxy($"Video encoding jobs may have only one input...",
                    Utilities.LogLevel.AppError);
            }

            //If present, fetch HDR data from source and add it to job
            //But only if it is not already set.
            //IE: trust delivered side-car metadata over embedded metadata
            
            if (m.HasHDR && this.HdrMasteringDisplayPrimaries == null )
            {
                this.HdrMasteringDisplayPrimaries = m.Streams[m.FirstVideoIndex].MasteringDisplayPrimaries;
                this.HdrMasteringDisplayLuminance = m.Streams[m.FirstVideoIndex].MasteringDisplayLuminance;
                this.HdrCea8613HdrData = m.Streams[m.FirstVideoIndex].Cea8613HdrData;
            }

            this.InputMedia.Add(m);
        }

        /// <summary>
        /// Adds output definition to job.
        /// </summary>
        /// <param name="v">VideoOutputDefinition describing one video stream</param>
        public void AddOutputMedia(VideoOutputDefinition v)
        {
            //Any pre--add work goes here...
            this.OutputMedia.Add(v);
        }

        /// <summary>
        /// Runs job against specified encoder
        /// </summary>
        public void Runjob()
        {
            //minimal job validation...
            this.Validate();
            
            //This is kinda hacky. 
            //ToDo: seems like a good candidate for interfaces...
            switch (this.Encoder)
            {
                case Encoders.FFVideoEncoder_x264:
                case Encoders.FFVideoEncoder_x265:
                    var encoder = new FFVideoEncoder();
                    encoder.Execute(this);
                    break;
                default:
                    this.utils.LogProxy($"Specified encoder ID: \"{this.Encoder}\" not implemented.",
                        Utilities.LogLevel.AppError);
                    break;
            }
        }

        /// <summary>
        /// Simple job validation
        /// </summary>
        public void Validate()
        {
            //Note: in the event that additional encoders are added, this check will need to be revised.
            if (this.InputMedia[0].HasHDR && this.Encoder != Encoders.FFVideoEncoder_x265)
            {
                this.utils.LogProxy(
                    $"Video source indicates HDR but selected encoder does not support it...",
                    Utilities.LogLevel.AppError);

            }

            //Input media may only have one video stream...
            if (this.InputMedia[0].VideoStreamCount != 1)
            {
                this.utils.LogProxy(
                    $"Video encoding job input may have only one video stream, not {this.InputMedia[0].VideoStreamCount}",
                    Utilities.LogLevel.AppError);
            }

            //Per-stream encoder override is intended for audio encode jobs...
            foreach (VideoOutputDefinition v in this.OutputMedia)
            {
                if (v.Encoder != Encoders.None)
                {
                    this.utils.LogProxy(
                        $"Video encoding job does not support per-stream encoder overrides...",
                        Utilities.LogLevel.AppError);
                }
            }
        }
    }
}