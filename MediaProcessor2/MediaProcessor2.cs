using System;
using System.Threading.Tasks;
using LibMediaProcessor;

namespace MediaProcessor2
{

    /// <summary>
    /// The work-flow will not use this app, it's purpose to is enable experimenting
    /// with various scenarios outside of the test framework.
    /// </summary>
    
    class MediaProcessor2
    {
        static void Main(string[] args)
        {
            //For now, this is used for debugging various scenarios and testing the console spew
            //MediaAttributes ma = new MediaAttributes(@"D:\TestMedia\NormalizerTest\SUB_BURN\TestSubs.mp4");
            //MediaAttributes ma = new MediaAttributes(@"D:\TestMedia\NormalizerTest\AudioCases\8xMono_Labeled.mov");

            MediaProperties ma = new MediaProperties(@"D:\\lbox.mkv");

            
            var scanner = new FFMediaScanner();

            //Scan two minutes at center of source...
            //These can run at the same time.
            
            //Parallel.Invoke
           // (
            //    () => {scanner.DetectCombing(ma, Convert.ToInt32(ma.MediaDuration / 2 - 60), 120);},
           //     () => {scanner.DetectLetterbox(ma, Convert.ToInt32(ma.MediaDuration / 2 - 60), 120);}
           // );

            scanner.DetectLetterbox(ma, Convert.ToInt32(ma.MediaDuration / 2 - 15), 30);

            var VideoJob = new VideoEncodeJob("eng", "d:\\")
            {
                GopLengthSeconds = 4,
                LookAheadFrames = 48,
                Encoder = new FFVideoEncoder(FFVideoEncoder.AvailableEncoders.x265),
                //ColorSpec = VideoEncodeJob.OutputColorSpec.REC709,
                Preset = VideoEncodeJob.EncodeSpeed.Faster,
                PixelFmt = VideoEncodeJob.PixelFormat.YUV420p10,
                AutoCrop = true,
                //BurnSubs = @"D:\TestMedia\NormalizerTest\SUB_BURN\TestSubs.ass",
                //MatchRate = 50,
                //MatchWidth = 1280,
                //MatchHeight = 960
            };

            //Video encoder may only have one input. 
            VideoJob.AddInputMedia(ma);
        
            //Define output streams
            VideoOutputDefinition vod = new VideoOutputDefinition
            {
                Width = 852,
                TargetBitrate = 1000,
                PeakBitrate = 2000,
                VBVBufferSize = 5000,
                NumPasses = 1,
                AllowSceneDetection = true,

                Role = OutputDefinition.StreamRole.Download,
                StreamName = "DTO_VIDEO_1",
                OutputFileName = "TestFile1_WOOT.mkv"
            };
           
            //Override encoder preset defaults
            //Can be done on a per-output basis.
            //vod.EncoderOptionOverride.Add("bframes=4");
       
            VideoJob.AddOutputMedia(vod);
           
           
            VideoJob.Runjob();
        }
    }
}
