using System.Collections.Generic;

namespace LibMediaProcessor
{
    public class EncodeJob
    {
        public enum Encoders
        {
            None = 0,
            FFVideoEncoder_x264,
            FFVideoEncoder_x265,
            DolbyMediaGenerator_AAC,
            DolbyMediaGenerator_HEAAC,
            DolbyMediaGenerator_EAC3,
            DolbyEncodingEngine
        }

        public string Language { get; } 
        public string OutputFolder { get; }
        public Encoders Encoder { get; set; }

        //List of media to be processed.
        public List<MediaProperties> InputMedia = new List<MediaProperties>();
        protected readonly Utilities utils = new Utilities();
        
        /// <summary>
        /// Collection of media and general options for encoding media
        /// </summary>
        /// <param name="language">Three letter language code of content.</param>
        /// <param name="outputFolder">Destination of encode output</param>
        public EncodeJob(string language, string  outputFolder)
        {
            this.Language = language;
            this.OutputFolder = outputFolder;

            this.utils.ValidateDirectory(this.OutputFolder);
        }   
    }
}
