using System;
using System.Text.RegularExpressions;

namespace LibMediaProcessor
{
    /// <summary>
    /// FFMediaScanner is used to detect media properties like combing and letter-boxing.
    /// </summary>
    public partial class FFMediaScanner
    {
        /// <summary>
        /// Scan media, looking for combing artifacts. 
        /// Used for guiding de-interlacing and inverse-telecine filter selection.
        /// </summary>
        /// <param name="ma"> MediaAttributes.</param>
        /// <param name="startSeconds"></param>
        /// <param name="scanDuration"></param>
        public void DetectCombing(MediaProperties ma, int startSeconds = 0, int scanDuration = 0)
        {
            //Settings for detecting combing and telecine patterns.
            var filterIdet = "idet";
            var filterFieldMatch = "fieldmatch=order=auto:combmatch=full:cthresh=12";

            var filterChain = new FilterChain();
         
            filterChain.AddFilter(filterIdet);
            filterChain.AddFilter(filterFieldMatch);

            this.utils.LogProxy($"Starting combing/telecine detection...", Utilities.LogLevel.Info);
            var scanData = this.RunScan(ma, filterChain, startSeconds, scanDuration);

            this.ParseCombScanData(ma, scanData);
        }

        ///  <summary>
        ///  Parse the scan results to detect combed video or film content...
        ///  </summary>
        /// <param name="ma"> MediaAttributes.</param>
        /// <param name="scanData">Results of FFMPEG run</param>
        private void ParseCombScanData(MediaProperties ma, string scanData)
        {
            string[] sfd = null;
            string[] mfd = null;
            string[] rfd = null;
            var fieldMatchFailures = 0;

            string[] scanDataLines = scanData.Split(new[]
            {
                "\n",
                "\r\n"
            }, StringSplitOptions.RemoveEmptyEntries);

            //Extract field and frame info
            foreach (string s in scanDataLines)
            {
                if (s.Contains("Repeated Fields"))
                {
                    //remove dupe spaces
                    string t = Regex.Replace(s, @"\s+", " ").Replace(@": ", @":");
                    rfd = t.Split(' ');
                }

                if (s.Contains("Single frame detection"))
                {
                    //remove dupe spaces
                    string t = Regex.Replace(s, @"\s+", " ").Replace(@": ", @":");
                    sfd = t.Split(' ');
                }

                if (s.Contains("Multi frame detection"))
                {
                    //remove dupe spaces
                    string t = Regex.Replace(s, @"\s+", " ").Replace(@": ", @":");
                    t = t.Replace(": ", ":");
                    mfd = t.Split(' ');
                }

                if (s.Contains("still interlaced"))
                {
                    fieldMatchFailures++;
                }
            }

            if (sfd != null && mfd != null)
                this.DoFrameAndFieldAnalysis(ma, mfd, rfd, fieldMatchFailures);
        }

        ///  <summary>
        ///  Parse the scan results to detect combed video or film content...
        ///  </summary>
        /// <param name="ma"> MediaAttributes.</param>
        /// <param name="mfd">Multi-frame detection data from FFMPEG IDET filter run</param>
        /// <param name="rfd">Repeat fields data from FFMPEG IDET filter run</param>
        /// <param name="fieldMatchFailures">Field match failures from FIELDMATCH filter run</param>
        private void DoFrameAndFieldAnalysis(MediaProperties ma, string[] mfd, string[] rfd, int fieldMatchFailures)
        {
            //Detection settings
            //If Progressive frame count falls below this level, combing
            //is assumed...
            decimal progressiveFrameThreshold = 95;
            decimal ProgFramePercentage = 0;
            decimal RepeatFieldPercentage = 0;
            decimal TfFtoBffRatio = 0;

            decimal mfdTff = 1, mfdBff = 1, mfdProg = 1;
            decimal rfdNone = 1, rfdTop = 1, rfdBottom = 1;
            foreach (string s in mfd)

            {
                if (s.Contains("TFF"))
                    mfdTff = Convert.ToDecimal(s.Split(':')[2]);
                if (s.Contains("BFF"))
                    mfdBff = Convert.ToDecimal(s.Split(':')[1]);
                if (s.Contains("Progressive"))
                    mfdProg = Convert.ToDecimal(s.Split(':')[1]);
            }

            foreach (string s in rfd)
            {
                if (s.Contains("Neither"))
                    rfdNone = Convert.ToDecimal(s.Split(':')[2]);
                if (s.Contains("Top"))
                    rfdTop = Convert.ToDecimal(s.Split(':')[1]);
                if (s.Contains("Bottom"))
                    rfdBottom = Convert.ToDecimal(s.Split(':')[1]);
            }

            //total number of identified frames.
            decimal processedFrames = rfdBottom + rfdNone + rfdTop;
        
            this.utils.LogProxy($"Total Scanned Frames (from IDET): {processedFrames}", Utilities.LogLevel.Info);
            this.utils.LogProxy($"Fieldmatch Failures: {fieldMatchFailures}", Utilities.LogLevel.Info);

            //Ratio of frames with repeated fields to the total number of frames identified
            RepeatFieldPercentage = ((rfdTop + rfdBottom) / processedFrames) * 100;
            this.utils.LogProxy($"RepeatFieldPercentage : {RepeatFieldPercentage:N2}", Utilities.LogLevel.Info);

            //Ratio of detected progressive frames to total frames identified
            ProgFramePercentage = (mfdProg / processedFrames) * 100;
            this.utils.LogProxy($"Progressive frame percentage : {ProgFramePercentage:N2}", Utilities.LogLevel.Info);

            //Compute ratio of TFF vs BFF frames.
            if (mfdTff == 0 || mfdBff == 0)
                TfFtoBffRatio = 0;
            else
                TfFtoBffRatio = Math.Max(mfdTff, mfdBff) / Math.Min(mfdTff, mfdBff);

            this.utils.LogProxy($"TFF:BFF ratio : {mfdTff}:{mfdBff}", Utilities.LogLevel.Info);

            //Once the detected progressive frame count falls below a threshold assume interlaced
            if (ProgFramePercentage < progressiveFrameThreshold)
            {
                ma.HasCombing = true;
                this.utils.LogProxy($"Combing detected...", Utilities.LogLevel.Info);
            }

            //If content is combed (low progressive frame count) and there are
            //repeated fields, video probably has some telecine content
            if (ma.HasCombing && (RepeatFieldPercentage > Convert.ToDecimal(5)))
            {
                this.utils.LogProxy($"Telecine content detected...", Utilities.LogLevel.Info);
                ma.HasTelecine = true;
            }
            
            //If fieldmatch failures are very low, indicating either clean progressive media
            //OR a clean telecine AND the progressive frame percentage is very low THEN
            //flag media a cleanly telecined. 
            if (fieldMatchFailures / (double)processedFrames < 0.01 && ProgFramePercentage < 1)
            {
                ma.IsPureFilm = true;
                this.utils.LogProxy($"Clean Telecine content detected...", Utilities.LogLevel.Info);
            }

            ma.IsPureVideo = (ma.HasCombing && !ma.HasTelecine);
            ma.IsMixedFilmVideo = (ma.HasCombing && ma.HasTelecine && !ma.IsPureFilm);

            this.utils.LogProxy($"PureVideo:{ma.IsPureVideo}", Utilities.LogLevel.Info);
            this.utils.LogProxy($"Mixed Film and Video:{ma.IsMixedFilmVideo}", Utilities.LogLevel.Info);

            //If media is combed but the TFF:BFF ratio approaches 1:1 this could indicate
            //a potentially bad delivery. 
            if (ma.HasCombing && (TfFtoBffRatio < Convert.ToDecimal(1.25) && TfFtoBffRatio > Convert.ToDecimal(0.75)))
            {
                ma.BadDelivery = true;
                this.utils.LogProxy($"Ratio of TFF and BFF frames of {TfFtoBffRatio:F}:1 indicates possibly poor delivery.", Utilities.LogLevel.Warning);
            }

        }
    }
}
