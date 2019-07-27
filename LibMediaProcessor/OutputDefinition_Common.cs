using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LibMediaProcessor
{
    public class OutputDefinition
    {
        public enum StreamRole
        {
            Undefined = 0,
            Streaming,
            Download,
            Both
        }

        public int TargetBitrate { get; set; }
        public string OutputFileName { get; set; }
        public List<string> EncoderOptionOverride { get; set; }
        public EncodeJob.Encoders Encoder { get; set; }
        public StreamRole Role { get; set; }
        public string StreamName { get; set; }

        public OutputDefinition()
        {
            this.EncoderOptionOverride = new List<string>();
            //This allows for per-stream encoder selection.
            //Used for audio jobs where per-output codec options are required (IE: AAC and EAC3 from one input)
            //Not supported currently in the video encode path.
            this.Encoder = EncodeJob.Encoders.None;
        }
    }
}