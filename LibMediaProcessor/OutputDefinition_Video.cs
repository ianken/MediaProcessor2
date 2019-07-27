using System.Collections.Generic;
using System.Security;

namespace LibMediaProcessor
{
    public class VideoOutputDefinition : OutputDefinition
    {
        //Since all output is square pixels, only width needs to be defined.
        //Height of the output stream will be computed based on source aspect ratio.
        public int Width { get; set; }
        public int VBVBufferSize { get; set; }
        public int PeakBitrate { get; set; }
        public int NumPasses { get; set; }
        public bool AllowSceneDetection { get; set; }
    }
}