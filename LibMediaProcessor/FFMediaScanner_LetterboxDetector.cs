using System;
using System.Collections.Generic;
using System.Linq;

namespace LibMediaProcessor
{
    public partial class FFMediaScanner
    {
        /// <summary>
        /// Scan media, finding black bars and selecting the most common result.
        /// Used for automated cropping
        /// </summary>
        /// <param name="ma"> MediaAttributes.</param>
        /// <param name="startSeconds"></param>
        /// <param name="scanDuration"></param>
        public void DetectLetterbox(MediaProperties ma, int startSeconds = 0, int scanDuration = 0)
        {
            var filterCropdetect = "cropdetect=0.1:2:0";
            var filterChain = new FilterChain();
            var videoStream = ma.Streams.FirstOrDefault(m => m.StreamType == StreamType.Video);
            filterChain.AddFilter(filterCropdetect);

            this.utils.LogProxy($"Starting letterbox detection...",  Utilities.LogLevel.Info);
            var scanData = this.RunScan(ma, filterChain, startSeconds, scanDuration);

            ma.cropValue = this.GetCommonCropValue(scanData);

            int bottomCrop = videoStream.VideoHeight - ma.cropValue.YExtent - ma.cropValue.YOffset;

            //If the bars at the top differ in size from those at the bottom
            //then re-scan because this is generally the result of noise at the top
            //of the video frame that breaks bar detection.
            if (Math.Abs(bottomCrop - ma.cropValue.YOffset) > 16)
            {
                filterChain.DeleteAll();

                //Different masking values are used for suspected PAL and NSTC content.
                //The crop filter syntax is counter-intuitive. It describes the visible region of media
                //IE: crop=width:height:x-offset:y-offset <- this is the size of the output. Not the mask.
                if (videoStream.VideoHeight < 650 && videoStream.VideoHeight > 525)
                {
                    filterChain.AddFilter($"crop={videoStream.VideoWidth.ToString()}:{videoStream.VideoHeight - 32}:0:{32}");
                }
                else if (videoStream.VideoHeight <= 525)
                {
                    filterChain.AddFilter($"crop={videoStream.VideoWidth.ToString()}:{videoStream.VideoHeight - 16}:0:{16}");
                }

                filterChain.AddFilter(filterCropdetect);

                this.utils.LogProxy($"Starting second letterbox detection scan due to detected noise...",
                     Utilities.LogLevel.Info);
                scanData = this.RunScan(ma, filterChain, startSeconds, scanDuration);

                ma.cropValue = this.GetCommonCropValue(scanData);

                if (videoStream.VideoHeight < 650 && videoStream.VideoHeight > 525)
                {
                    ma.cropValue.YOffset += 32;
                }
                else if (videoStream.VideoHeight <= 525)
                {
                    ma.cropValue.YOffset += 16;
                }

            }

            //Only crop if we have a minimum of letterbox coverage
            if (Math.Abs(ma.cropValue.YExtent - videoStream.VideoHeight) > 16)
            {
                ma.HasLetterbox = true;
                //Crop to even values...
                if (ma.cropValue.YExtent % 2 != 0)
                {
                    ma.cropValue.YExtent--;
                }

                //Save pre-built crop filter for later use if desired.
                ma.FFCropFilter = $"crop={videoStream.VideoWidth.ToString()}:{ma.cropValue.YExtent.ToString()}:0:{ma.cropValue.YOffset.ToString()}";
            }

        }

        /// <summary>
        /// Parse the output of the crop-detect filter and return the most common result.
        /// </summary>
        private CropValue GetCommonCropValue(string scanData)
        {
            string[] scanDataLines = scanData.Split(new[]
            {
                "\n",
                "\r\n"
            }, StringSplitOptions.RemoveEmptyEntries);

            var cropValues = new List<CropValue>();

            foreach (string s in scanDataLines)
            {
                if (s.Contains("Parsed_cropdetect"))
                {
                    string[] crop = s.Split(' ')[13].Split(':');
                    if (crop.Length == 4)
                    {
                        crop[0] = crop[0].Split('=')[1];
                        cropValues.Add(new CropValue(Convert.ToInt16(crop[3]), Convert.ToInt16(crop[1]), Convert.ToInt16(crop[2]),
                            Convert.ToInt16(crop[0])));
                    }
                }
            }

            //Sort by YExtent...
            var commonResult = cropValues.GroupBy(item => item.YExtent).OrderByDescending(g => g.Count()).Select(g => new
            {
                YExtent = g.Key,
                g.First().YOffset,
                g.First().XOffset,
                g.First().XExtent
             }).First();

            return new CropValue(commonResult.YOffset,commonResult.YExtent,commonResult.YOffset,commonResult.XExtent);
        }

       
    }
}