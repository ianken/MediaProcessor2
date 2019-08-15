using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace LibMediaProcessor
{
    public enum StreamType
    {
        Video,
        Audio,
    };

    /// <summary>
    /// MediaStream describes a discrete stream within a scanned file. 
    /// </summary>
    public class MediaStream
    {
        public int BitRate;
        public int AudioChannelCount;
        public string AudioChannelLayout;
        public int AudioSampleRate;
        public int AudioSampleSize;
        public string CodecName;
        public string CodecFormat;
        public decimal Duration;
        public int StreamIndex;
        public StreamType StreamType;
        public double VideoDisplayAspect;
        public decimal VideoFrameRate;
        public Int32 VideoFrameCount;
        public double VideoPixelAspect;
        public string VideoPixelFormat;
        public Int32 VideoPixelBitDepth;
        public string VideoPixelChromaFormat;
        public int VideoWidth;
        public int VideoHeight;
        public int SquareVideoWidth;
        public int SquareVideoHeight;
        public MasteringDisplayPrimaries MasteringDisplayPrimaries;
        public MasteringDisplayLuminance MasteringDisplayLuminance;
        public Cea8613HdrData Cea8613HdrData;
        public VideoColorProperties VideoColorProperties;
    }
    public class CropValue
    {
        public int XOffset, XExtent;
        public int YOffset, YExtent;

        public CropValue(int yOffset, int yExtent, int xOffset, int xExtent)
        {
            this.YExtent = yExtent;
            this.YOffset = yOffset;
            this.XExtent = xExtent;
            this.XOffset = xOffset;
        }
    }

    /// <summary>
    /// MediaAttributes fetches media attributes and performs
    /// scanning via FFMPEG to detect combing and telecine content.
    /// </summary>
    public class MediaProperties
    {
        #region Global Media Attributes

        public int AudioStreamCount { get; private set; }
        public int VideoStreamCount { get; private set; }
        public bool BadDelivery { get; set; }
        public string FFCropFilter { get; set; }
        public bool HasAtmos { get; private set; }
        public bool HasCombing { get; set; }
        public bool HasTelecine { get; set; }
        public bool IsPureFilm { get; set; }
        public bool IsPureVideo { get; set; }
        public bool IsMixedFilmVideo { get; set; }
        public bool HasLetterbox { get; set; }
        public bool HasEDL { get; private set; }
        public bool HasHDR { get; private set; }
        public int MaxAudioChannelCount { get; private set; }
        public decimal MediaDuration { get; private set; }
        public string MediaFile { get; }
        public int FirstVideoIndex { get; private set; } = -1;
        public CropValue cropValue { get; set; }


        #endregion

        //Tools and utilities
        private readonly ToolBins mediaTools = new ToolBins();
        private readonly Utilities utils = new Utilities();

        //All streams in scanned media.
        public List<MediaStream> Streams = new List<MediaStream>();
        //Detected crop information
       
        /// <summary>
        /// Fetches initial properties of a piece of media.
        /// Does not perform deep analysis (combing detection or audio level measurement, etc).
        /// </summary>
        /// <param name="mediaFile"> The media file to be analyzed.</param>
        public MediaProperties(string mediaFile)
        {
            this.utils.ValidateFile(mediaFile);
            this.MediaFile = mediaFile;
            this.GetMediaAttribites();
        }

        /// <summary>
        /// Executes MEDIAINFO.EXE and FFPROBE.EXE to fetch attributes of MediaFile
        /// </summary>
        private void GetMediaAttribites()
        {
            
            var mediaInfoBlob = this.utils.ExecuteCommand_StdOut(this.mediaTools.MediaInfoPath, $" --output=XML --Language=raw -f {this.MediaFile} ");
            var ffProbeBlob = this.utils.ExecuteCommand_StdErr(this.mediaTools.FfProbePath, $"  -probesize 50000000  {this.MediaFile} ");

            if (string.IsNullOrEmpty(ffProbeBlob))
            {
                this.utils.LogProxy($"FFPROBE data is empty. File {this.MediaFile} may be invalid or corrupt.",  Utilities.LogLevel.AppError);
            }

            //This information does not show up in the XML payload, so it needs to be obtained here.
            //At this time FFMPEG cannot processes MOV with edit lists, so this is used to detect that.
            if (ffProbeBlob.Contains("multiple edit list entries"))
            {
                this.HasEDL = true;
            }
              
            //Fetch XML probe data...
            ffProbeBlob = this.utils.ExecuteCommand_StdOut(this.mediaTools.FfProbePath, $"  -v quiet -probesize 50000000 -print_format xml=x=1 -noprivate -show_format -show_streams  {this.MediaFile}");

            var mediaInfoSer = new XmlSerializer(typeof(Mediainfo));
            var ffProbeInfoSer = new XmlSerializer(typeof(ffprobeType));

            Mediainfo mediaInfoResult;
            ffprobeType probeResult;

            using (TextReader reader = new StringReader(mediaInfoBlob))
            {
                try
                {
                    mediaInfoResult = (Mediainfo)mediaInfoSer.Deserialize(reader);
                }
                catch (Exception ex)
                {
                    this.utils.LogProxy($"Exception deserializing mediaInfo data...{ex.Message}", Utilities.LogLevel.ExceptionDetails);
                    throw;
                }
            }

            using (TextReader reader = new StringReader(ffProbeBlob))
            {
                try
                {
                    probeResult = (ffprobeType) ffProbeInfoSer.Deserialize(reader);
                }
                catch (Exception ex)
                {
                    this.utils.LogProxy($"Exception deserializing FFProbe data...{ex.Message}", Utilities.LogLevel.ExceptionDetails);
                    throw;
                }
            }

            this.ParseMediaInfoResult(mediaInfoResult);
            this.ParseFfProbeResult(probeResult);

        }

        /// <summary>
        /// Parses the deserialized XML describing the supplied media. 
        /// Specific attributes of interest are extracted for easier access.
        /// Use FFPROBE color space and other data for compatibility with FFMPEG
        /// Assumes mediainfo has already been run and parsed to fill out stream count
        /// and other properties.
        /// </summary>
        /// <param name="probeResult"> A FFProbe object that describes the media.</param>
        private void ParseFfProbeResult(ffprobeType probeResult)
        {
            var index = 0;

            foreach (streamType s in probeResult.streams)
            {

                switch (s.codec_type)
                {
                    case "video":
                        if (s.color_primaries != null && s.color_transfer != null && s.color_space != null)
                        {
                            this.Streams.FirstOrDefault(i => i.StreamIndex == index).VideoColorProperties =
                                new VideoColorProperties(s.color_primaries, s.color_transfer, s.color_space);
                        }
                    break;

                    case "audio":
                        this.Streams.FirstOrDefault(i => i.StreamIndex == index).AudioChannelLayout = s.channel_layout;
                    break;

                }

                index++;
            }
        }

        /// <summary>
        /// Parses the deserialized XML describing the supplied media. 
        /// Specific attributes of interest are extracted for easier access.
        /// </summary>
        /// <param name="mediaInfoResult"> A Media-info object that describes the media.</param>
        private void ParseMediaInfoResult(Mediainfo mediaInfoResult)
        {
            foreach (MediainfoFileTrack track in mediaInfoResult.File)
            {
                var m = new MediaStream();

                switch (track.type.ToLower())
                {
                    case "general":
                        //Media Duration in seconds.
                        this.MediaDuration = Convert.ToDecimal(track.Duration)/1000;
                        break;

                    case "video":
                        m.StreamType = StreamType.Video;
                        m.StreamIndex = Convert.ToInt32(track.StreamOrder);
                        m.CodecName = track.Codec;
                        m.CodecFormat = track.Format;
                        m.BitRate = Convert.ToInt32(track.BitRate);
                        m.Duration = Convert.ToDecimal(track.Duration) / 1000;
                        m.VideoFrameRate = Convert.ToDecimal(track.FrameRate);
                        m.VideoFrameCount = Convert.ToInt32(track.FrameCount);
                        m.VideoDisplayAspect = Convert.ToDouble(track.DisplayAspectRatio);
                        m.VideoPixelAspect = Convert.ToDouble(track.PixelAspectRatio);
                        m.VideoPixelFormat = track.ColorSpace;
                        m.VideoPixelBitDepth = Convert.ToInt32(track.BitDepth);
                        m.VideoPixelChromaFormat = track.ChromaSubsampling;
                        m.VideoWidth = Convert.ToInt32(track.Width);
                        m.VideoHeight = Convert.ToInt32(track.Height);
                        m.SquareVideoWidth = Convert.ToInt32(m.VideoWidth * m.VideoPixelAspect);
                        m.SquareVideoHeight = m.VideoHeight;
                        
                        if (track.MasteringDisplay_ColorPrimaries != null && track.MasteringDisplay_Luminance != null)
                        {

                            this.HasHDR = true;

                            m.MasteringDisplayPrimaries = new MasteringDisplayPrimaries(track.MasteringDisplay_ColorPrimaries);
                            m.MasteringDisplayLuminance = new MasteringDisplayLuminance((track.MasteringDisplay_Luminance));

                            //If CEA 861.2 data is present, fetch it. It is not required.
                            if (track.MaxCLL != null)
                            {
                                m.Cea8613HdrData = new Cea8613HdrData(track.MaxCLL,track.MaxFALL);
                            }
                        }

                        this.FirstVideoIndex = m.StreamIndex;
                        this.VideoStreamCount++;
                        this.Streams.Add(m);
                        break;

                    case "audio":
                        m.StreamType = StreamType.Audio;

                        if (track.Format_Profile?.IndexOf("atmos", StringComparison.CurrentCultureIgnoreCase) >= 0)
                        {
                            this.HasAtmos = true;
                            //Media info reports bitrate for both the EC3 core data
                            //as well as the aggregate atmos+ec3 bit-rate.
                            //This extracts the latter.
                            m.AudioChannelCount = Convert.ToInt32(track.Channel_s_.Split('/')[1].Replace(" ", ""));
                            m.BitRate = Convert.ToInt32(track.BitRate.Split('/')[0].Replace(" ", ""));
                        }
                        else
                        {
                            m.AudioChannelCount = Convert.ToInt32(track.Channel_s_);
                            m.BitRate = Convert.ToInt32(track.BitRate);
                        }

                        if (m.AudioChannelCount < this.MaxAudioChannelCount)
                        {
                            this.MaxAudioChannelCount = m.AudioChannelCount;
                        }

                        m.StreamIndex = Convert.ToInt32(track.StreamOrder);
                        //m.AudioChannelLayout = track.ChannelLayout;
                        m.AudioSampleRate = Convert.ToInt32(track.SamplingRate);
                        m.AudioSampleSize = Convert.ToInt32(track.BitDepth);
                        m.CodecName = track.Codec;
                        m.CodecFormat = track.Format;
                        m.Duration = Convert.ToDecimal(track.Duration) / 1000;

                        this.AudioStreamCount++;
                        this.Streams.Add(m);
                        break;
                }

                
            }
        }
    }
}
