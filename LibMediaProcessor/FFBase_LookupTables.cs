using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LibMediaProcessor
{
    /// <summary>
    /// Lookup tables that map generalized settings to FFMEPG/X264/X265 specific parameters.
    /// </summary>
    public partial class FFBase
    {
        //Maps color scheme enum to FFMPEG specific color space syntax
        protected Dictionary<VideoEncodeJob.OutputColorSpec, string> ColorSpaceMap = new Dictionary<VideoEncodeJob.OutputColorSpec, string>()
        {
            {
                VideoEncodeJob.OutputColorSpec.REC709, "colorprim=bt709:transfer=bt709:colormatrix=bt709"
            },
            {
                VideoEncodeJob.OutputColorSpec.REC601, "colorprim=smpte170m:transfer=smpte170m:colormatrix=smpte170m"
            },
            {
                VideoEncodeJob.OutputColorSpec.HDR10, "colorprim=bt2020:transfer=smpte2084:colormatrix=bt2020nc"
            },

        };

        //Maps speed setting to FFMPEG/x265/x264 preset strings
        protected Dictionary<VideoEncodeJob.EncodeSpeed, string> EncoderPresetMap = new Dictionary<VideoEncodeJob.EncodeSpeed, string>()
        {
            {
                VideoEncodeJob.EncodeSpeed.Slow, "slow"
            },
            {
                VideoEncodeJob.EncodeSpeed.Fast, "fast"
            },
            {
                VideoEncodeJob.EncodeSpeed.Faster, "superfast"
            },
        };

        //Maps pixel format to FFMPEG specific settings
        protected Dictionary<VideoEncodeJob.PixelFormat, string> PixelFormatMap = new Dictionary<VideoEncodeJob.PixelFormat, string>()
        {
            {
                VideoEncodeJob.PixelFormat.YUV420p, "yuv420p"
            },
            {
                VideoEncodeJob.PixelFormat.YUV420p10, "yuv420p10"
            }
            ,
            {
                VideoEncodeJob.PixelFormat.YUV422p10, "yuv422p10"
            }
        };

        protected Dictionary<VideoEncodeJob.DeinterlaceOverride, string> DeinterlaceOverrideMap =
            new Dictionary<VideoEncodeJob.DeinterlaceOverride, string>()
            {
                {
                    VideoEncodeJob.DeinterlaceOverride.PureTelecine, FFBase.DeintFilter_PureTelecine

                },
                {
                    VideoEncodeJob.DeinterlaceOverride.PureVideoHD  , FFBase.DeintFilter_PureVideoHD
                },
                {
                    VideoEncodeJob.DeinterlaceOverride.PureVideoSD  , FFBase.DeintFilter_PureVideoSD
                },
                {
                    VideoEncodeJob.DeinterlaceOverride.Mixed_FilmBias  , FFBase.DeintFilter_FilmBias
                },
                {
                    VideoEncodeJob.DeinterlaceOverride.Mixed_VideoBias  , FFBase.DeintFilter_VideoBias
                },

            };

        protected Dictionary<FFVideoEncoder.AvailableEncoders, string> EncoderMap =
            new Dictionary<FFVideoEncoder.AvailableEncoders, string>()
            {
                {
                    FFVideoEncoder.AvailableEncoders.x264, "x264"
                },
                {
                    FFVideoEncoder.AvailableEncoders.x265, "x265"
                }
            };


    }
}