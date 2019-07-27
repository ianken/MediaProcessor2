using System.IO;
using System.Reflection;

namespace LibMediaProcessor
{
    /// <summary>
    /// The ToolBin class is intended to manage all external command line tools, such as FFMPEG
    /// that are called by LibMediaProcessor.
    /// </summary>
  
    public class ToolBin
    {
        public string FfEncPath { get; private set; }
        public  string FfProbePath { get; private set; }
        public  string FfFontsDir { get; private set; }
        public  string FfFontsConf { get; private set; }
        public  string MediaInfoPath { get; private set; }
  

        /// <summary>
        /// The ToolBin class is intended to manage all external command line tools, such as FFMPEG
        /// that are called by LibMediaProcessor.
        /// </summary>
        /// <param name="utilities">Optionally provide utilities helper</param>
        /// <param name="toolsRootPath"> Optionally override location of tools.</param>
        public ToolBin(Utilities utilities = null, string toolsRootPath = null)
        {
            var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var toolsRoot = toolsRootPath ?? Path.Combine(assemblyFolder, "TOOLS");
            var utils = utilities ?? new Utilities();
            
            //Public build of FFMPEG
            this.FfEncPath = Path.Combine(toolsRoot, "FFMPEG", "PUBLIC", "FFMPEG.EXE");
            utils.ValidateFile(this.FfEncPath);

            //Public build of FFPROBE
            this.FfProbePath = Path.Combine(toolsRoot, "FFMPEG", "PUBLIC", "FFPROBE.EXE");
            utils.ValidateFile(this.FfProbePath);

            //MediaInfo
            this.MediaInfoPath = Path.Combine(toolsRoot, "MEDIAINFO", "MEDIAINFO.EXE");
            utils.ValidateFile(this.MediaInfoPath);

            //FONTS - path to FONTS folder and config file used by FFMPEG when burning subtitles.
            //These are used by the helper when setting up the environment for burning subtitles.
            this.FfFontsDir = Path.Combine(toolsRoot, "FONTS");
            this.FfFontsConf = Path.Combine(this.FfFontsDir, "FONTS.CONF");
            utils.ValidateFile(this.FfFontsConf);
        }

    }
}