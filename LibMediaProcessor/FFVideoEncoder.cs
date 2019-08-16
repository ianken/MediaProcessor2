using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LibMediaProcessor
{
    /// <summary>
    /// Encode a single source video to N output streams using FFMPEG.
    /// Supports x264 and x265
    /// </summary>
    public class FFVideoEncoder : FFBase , IVideoEncoder
    {
        public enum AvailableEncoders
        {
            x264,
            x265
        }

        private AvailableEncoders enc;

        public FFVideoEncoder(AvailableEncoders Enc)
        {
            enc = Enc;
        }

        /// <summary>
        /// Process video encoding job
        /// </summary>
        /// <param name="job">Job to encode</param>
        public void Execute(VideoEncodeJob job)
        {
            Parallel.ForEach(job.OutputMedia, (v, state, index) =>
                {
                    this.utils.LogProxy($"\r\nStarting FFVideoEncoder instance {index}", Utilities.LogLevel.Info);
                    this.EncodeX(v, job, this.EncoderMap[enc]);
                }
            );
        }

        /// <summary>
        /// Generate single encoded video stream based on JOB and OUTPUT DEFINITION
        /// </summary>
        /// <param name="v">Defines the video stream being generated</param>
        /// <param name="j">The overall job for this encode run</param>
        /// <param name="xEncoder">libx264 or libx265</param>
        public void EncodeX(VideoOutputDefinition v, VideoEncodeJob j, string xEncoder)
        {
            MediaProperties ma = j.InputMedia[0];
            MediaStream vs = ma.Streams[ma.FirstVideoIndex];

            //implicitly set frame rate.
            //To do: this needs to be rationals, not floats
            var rateString = vs.VideoFrameRate > 0 ? $"-r {((j.MatchRate != 0) ? j.MatchRate : vs.VideoFrameRate)}" : String.Empty ;

            //Select proper prefix based on input media.
            //MPEG files require a specific syntax on the input to prevent time-code issues.
            var FFRoot = this.MpegFiles.Contains(Path.GetExtension(ma.MediaFile).ToLower()) ? this.FFRootCmdMpg : this.FFRootCmdDefault;

            FilterChain f = this.ConfigureFilters(v, j);
            
            for (int pass = 1; pass <= v.NumPasses; pass++)
            {
                var FFCmd = $"{FFRoot} -i {j.InputMedia[0].MediaFile} {f.GetVideoFilters()} " +
                            $"-pix_fmt {this.PixelFormatMap[j.PixelFmt]} " +
                            $"-preset {this.EncoderPresetMap[j.Preset]} " +
                            $"-an -c:v lib{xEncoder} " +
                            $"-metadata:s:v:0 language={j.Language} " +
                            //ToDo: this needs to be a rational like "24000/1001" to prevent rate drift during sitching or concetnation.
                            $"{rateString} " +  
                            $"{this.BuildX26xEncOptions(v, j, pass, xEncoder)} " +
                            $"{Path.Combine(j.OutputFolder, v.OutputFileName)}";
                //Run
                this.utils.LogProxy($"\r\nSTARTING {xEncoder} Encode pass {pass} of {v.NumPasses}", Utilities.LogLevel.Info);
                this.ExecuteFFMpeg_LogProgress(FFCmd, ma.MediaDuration);
            }
        }

        /// <summary>
        /// Derives x264/x265 options from JOB and OUTPUT DEFINITION
        /// This only supports the parameters that are common between x264 and x265 and relies in their shared syntax.
        /// VideoOutputDefinition.EncoderOptionOverride.Add() is used for unique encoder specific overrides.
        /// </summary>
        /// <param name="v">Defines the video stream being generated</param>
        /// <param name="j">The overall job for this encode run</param>
        /// <param name="pass">pass number of multi pass encode</param>
        /// <param name="xEncoder">encoder to use</param>
        protected string BuildX26xEncOptions(VideoOutputDefinition v, VideoEncodeJob j, int pass, string xEncoder)
        {
            MediaProperties ma = j.InputMedia[0];
            MediaStream vs = ma.Streams[ma.FirstVideoIndex];
            var statsFile = $"{Path.GetFileNameWithoutExtension(v.OutputFileName)}_STATS";
       
            //Scene detection breaks GOP alignment across streams. Disable by default.
            //May be re-enabled at the stream level, for example for DONWLOAD encodes.
            var sceneDetection = v.AllowSceneDetection ? "" : "scenecut=0:";
            
            //If job does not have color space for output implicitly set, derive it from source media.
            var ColorArgs = (j.ColorSpec == VideoEncodeJob.OutputColorSpec.Unknown) ? this.getX26xColorSpaceArgs(vs) : this.ColorSpaceMap[j.ColorSpec];
           
            var args = $"-{xEncoder}-params \"" +
                       $"bitrate={v.TargetBitrate}:" +
                       $"vbv-maxrate={v.PeakBitrate}:" +
                       $"vbv-bufsize={v.VBVBufferSize}:" +
                       $"min-keyint={Convert.ToInt32(j.GopLengthSeconds * vs.VideoFrameRate)}:" +
                       $"keyint={Convert.ToInt32(j.GopLengthSeconds * vs.VideoFrameRate)}:" +
                       $"rc-lookahead={j.LookAheadFrames}:" +
                       "open-gop=0:" +  //GOPS must always be closed
                       $"{sceneDetection}" +  
                       $"pass={pass}:" +
                       $"stats=\"{statsFile}\":" +
                       $"{ColorArgs}";

            //handle HDR settings.
            //HDR is only supported when the output is HEVC

            if (xEncoder == "x265" && j.HdrMasteringDisplayPrimaries != null)
            {
                args += $":{this.Getx26xHdrString(j)}";
            }

            foreach (string overrideOption in v.EncoderOptionOverride)
            {
                args += $":{overrideOption}";
            }

            args += "\"";

            return args;
        }

        /// <summary>
        /// Generates HDR data string for x265
        /// </summary>
        /// <param name="j">Encode job, where HDR metadata is found</param>
        protected string Getx26xHdrString(VideoEncodeJob j)
        {
            var masterDisplay = $"master-display=\"G({Convert.ToInt32(j.HdrMasteringDisplayPrimaries.Gx * 50000)},{Convert.ToInt32(j.HdrMasteringDisplayPrimaries.Gy * 50000)})" +
                                $"B({Convert.ToInt32(j.HdrMasteringDisplayPrimaries.Bx * 50000)},{Convert.ToInt32(j.HdrMasteringDisplayPrimaries.By * 50000)})" +
                                $"R({Convert.ToInt32(j.HdrMasteringDisplayPrimaries.Rx * 50000)},{Convert.ToInt32(j.HdrMasteringDisplayPrimaries.Ry * 50000)})" +
                                $"WP({Convert.ToInt32(j.HdrMasteringDisplayPrimaries.Wpx * 50000)},{Convert.ToInt32(j.HdrMasteringDisplayPrimaries.Wpy * 50000)})" +
                                $"L({Convert.ToInt32(j.HdrMasteringDisplayLuminance.Max * 10000)},{Convert.ToInt32(j.HdrMasteringDisplayLuminance.Min * 10000)})\"";

            if (j.HdrCea8613HdrData != null)
            {
                var cea8613 = $"max-cll={j.HdrCea8613HdrData.MaxCLL},{j.HdrCea8613HdrData.MaxFALL}";
                return $"{masterDisplay}:{cea8613}";
            }
            else
            {
                return masterDisplay;
            }
        }
        
        /// <summary>
        /// Generate the color space arguments for encoding.
        /// Either fetch it directly from the source
        /// or derive it based on media dimensions, which may not be accurate
        /// but is unavoidable when source media lacks color space data.
        /// </summary>
        /// <param name="vs">The Video stream source</param>
        /// <param name="isHDR10">Signal HDR10 source</param>
        public string getX26xColorSpaceArgs(MediaStream vs, bool isHDR10 = false)
        {

            if (vs.VideoColorProperties != null)
            {
                return  $"colorprim={vs.VideoColorProperties.VideoColorPrimaries}:transfer={vs.VideoColorProperties.VideoTransferCharacteristics}:colormatrix={vs.VideoColorProperties.VideoMatrixCoefficients}";
            }
            else
            {
                if (vs.VideoWidth > 852)
                {
                    return isHDR10 ? this.ColorSpaceMap[VideoEncodeJob.OutputColorSpec.HDR10] : this.ColorSpaceMap[VideoEncodeJob.OutputColorSpec.REC709];
                }
                else
                {
                    return this.ColorSpaceMap[VideoEncodeJob.OutputColorSpec.REC601];
                }
            }
        }
       
        /// <summary>
        /// Manages filters used to process video.
        /// Scaling
        /// De-interlacing
        /// Cropping
        /// Matching to master (for stitching scenarios, IE: dub cards)
        /// Burning in sub-titles.
        /// </summary>
        /// <param name="outputVideo">Defines the video stream being generated</param>
        /// <param name="videoEncodeJob">The overall job for this encode run</param>
        protected FilterChain ConfigureFilters(VideoOutputDefinition outputVideo, VideoEncodeJob videoEncodeJob)
        {
            //Video stream
            MediaProperties videoProperties = videoEncodeJob.InputMedia[0];
            MediaStream inputVideo = videoProperties.Streams[videoProperties.FirstVideoIndex];
            FilterChain filters = new FilterChain();
       
            //Derived from desired width, source aspect and cropping options.
            var TargetOutputHeight = 0;
            
            if (!String.IsNullOrEmpty(videoProperties.FFCropFilter) && videoEncodeJob.AutoCrop)
            {
                //must crop before any scaling happens...
                filters.AddFilter(videoProperties.FFCropFilter);

                if (inputVideo.VideoPixelAspect != 1)
                {
                    inputVideo.SquareVideoWidth = Convert.ToInt32(videoProperties.cropValue.XExtent * inputVideo.VideoPixelAspect);
                    inputVideo.SquareVideoHeight = videoProperties.cropValue.YExtent;
                }
                else
                {
                    inputVideo.SquareVideoWidth = videoProperties.cropValue.XExtent;
                    inputVideo.SquareVideoHeight = videoProperties.cropValue.YExtent;
                }
            }
            else
            {
                if(inputVideo.VideoPixelAspect != 1)
                {
                    inputVideo.SquareVideoWidth = Convert.ToInt32(inputVideo.VideoWidth * inputVideo.VideoPixelAspect);
                }
            }

            //De-interlace filter behavior.
            //Must happen before any scaling operations.
            //Only invoke on HD and SD at non-film frame rates
            if (videoProperties.HasCombing && inputVideo.SquareVideoWidth <= 1920 && inputVideo.VideoFrameRate > 24)
            {
                if (videoEncodeJob.DeintOverride != VideoEncodeJob.DeinterlaceOverride.None)
                {
                    //The job manager may desire to override behavior.
                    //For example, in the event where the main feature is cleanly telecined film
                    //but the credits are combed video. HBO LatAm does this.
                    filters.AddFilter(this.DeinterlaceOverrideMap[videoEncodeJob.DeintOverride]);
                }
                else if (videoProperties.IsMixedFilmVideo)
                {
                    //Typical: 30i animation with some telecine segments.
                    //Preserve original frame rate. Inverse telecine film.
                    //De-comb video. There will be judder in the film segments.
                    filters.AddFilter(FFBase.DeintFilter_VideoBias);
                }
                else if (videoProperties.IsPureFilm)
                {
                    //Old telecined content. Rare.
                    filters.AddFilter(FFBase.DeintFilter_PureTelecine);
                }
                else if (videoProperties.IsPureVideo)
                {
                    //Talk shows. Next day TV. Old sitcoms.
                    //This filter setting is slow, but yields good results.
                    filters.AddFilter(FFBase.DeintFilter_PureVideoSD);
                }
            }
            
            if (videoEncodeJob.MatchWidth != 0 && videoEncodeJob.MatchHeight != 0)
            {
                //Job indicates matching to master source.
                filters.AddFilter(this.GetScaleMatchFilter(inputVideo.VideoPixelAspect == 1, videoEncodeJob.MatchWidth, videoEncodeJob.MatchHeight));
                TargetOutputHeight = outputVideo.Width * videoEncodeJob.MatchHeight / videoEncodeJob.MatchWidth;
                TargetOutputHeight = (TargetOutputHeight % 2 == 0) ? TargetOutputHeight : ++TargetOutputHeight;
                filters.AddFilter($"scale={outputVideo.Width}:{TargetOutputHeight}");
            }
            else if (outputVideo.Width != inputVideo.SquareVideoWidth || inputVideo.VideoPixelAspect != 1)
            {
                //Input is either non-square or does not match desired output
                TargetOutputHeight = outputVideo.Width * inputVideo.SquareVideoHeight / inputVideo.SquareVideoWidth;
                TargetOutputHeight = (TargetOutputHeight % 2 == 0) ? TargetOutputHeight : ++TargetOutputHeight;
                filters.AddFilter($"scale={outputVideo.Width}:{TargetOutputHeight}");
            }

            //Set source pixel aspect
            filters.AddFilter("setsar=1/1");

            //Sub titles go last to ensure some degree of readability on low res streams
            if (!String.IsNullOrEmpty(videoEncodeJob.BurnSubs))
            {
                //Need to escape characters so the ASS filter will be happy.
                videoEncodeJob.BurnSubs = videoEncodeJob.BurnSubs.Replace(@"\", @"\\");
                videoEncodeJob.BurnSubs = videoEncodeJob.BurnSubs.Replace(@":", @"\:");

                filters.AddFilter($"ass='{videoEncodeJob.BurnSubs}'");
            }

            return filters;
        }

        /// <summary>
        ///Helper to format scale/match filter
        /// </summary>
        /// <param name="isSquare"></param>
        /// <param name="targetWidth"></param>
        /// <param name="targetHeight"></param>
        protected string GetScaleMatchFilter(bool isSquare, int targetWidth, int targetHeight)
        {
            var filter = isSquare ? this.ScaleVideoToTargetSquare : this.ScaleVideoToTarget;
            return String.Format(filter, targetWidth, targetWidth);
        }
    }
}
