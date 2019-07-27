using System;
using System.Linq;

namespace LibMediaProcessor
{
    public partial class FFMediaScanner : FFBase
    {
        private string RunScan(MediaProperties ma, FilterChain fc, int startSeconds = 0, int scanDuration = 0)
        {
            var videoStream = ma.Streams.FirstOrDefault(m => m.StreamType == StreamType.Video);

            if (scanDuration == 0)
            {
                scanDuration = Convert.ToInt32(ma.MediaDuration);
            }

            var scanCmd =
                $" -ss {startSeconds} -y -i {ma.MediaFile}  -pix_fmt yuv420p -t {scanDuration} {fc.GetVideoFilters()} -an -f null NUL";

            return this.ExecuteFFMpeg_LogProgress(scanCmd, scanDuration).StdErr;
        }
    }
}
