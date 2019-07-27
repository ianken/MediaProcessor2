using System;
using System.Text.RegularExpressions;

namespace LibMediaProcessor
{
    /// <summary>
    /// Video color data tools
    /// </summary>
    public class VideoColorProperties
    {
        public string VideoColorPrimaries { get; private set; }
        public string VideoTransferCharacteristics { get; private set; }
        public string VideoMatrixCoefficients { get; private set; }
    
        //Color space data as reported by FFPROBE
        public VideoColorProperties(string videoColorPrimaries, string videoTransferCharacteristics, string videoMatrixCoefficients)
        {
            this.VideoColorPrimaries = videoColorPrimaries;
            this.VideoTransferCharacteristics = videoTransferCharacteristics;
            this.VideoMatrixCoefficients = videoMatrixCoefficients;
        }
}

    //HDR metadata as reported by MediaInfo
    public class MasteringDisplayPrimaries
    {
        public decimal Rx { get; private set; }
        public decimal Ry { get; private set; }
        public decimal Gx { get; private set; }
        public decimal Gy { get; private set; }
        public decimal Bx { get; private set; }
        public decimal By { get; private set; }
        public decimal Wpx { get; private set; }
        public decimal Wpy { get; private set; }
        
        public MasteringDisplayPrimaries(string masteringDisplayPrimaries)
        {

            var rVal = masteringDisplayPrimaries.Split(',')[0];
            var gVal = masteringDisplayPrimaries.Split(',')[1];
            var bVal = masteringDisplayPrimaries.Split(',')[2];
            var wPVal = masteringDisplayPrimaries.Split(',')[3];

            this.Rx = Convert.ToDecimal(Regex.Match(rVal, @"x=(.*)y").Groups[1].Value);
            this.Ry = Convert.ToDecimal(Regex.Match(rVal, @"y=(.*)").Groups[1].Value);

            this.Gx = Convert.ToDecimal(Regex.Match(gVal, @"x=(.*)y").Groups[1].Value);
            this.Gy = Convert.ToDecimal(Regex.Match(gVal, @"y=(.*)").Groups[1].Value);

            this.Bx = Convert.ToDecimal(Regex.Match(bVal, @"x=(.*)y").Groups[1].Value);
            this.By = Convert.ToDecimal(Regex.Match(bVal, @"y=(.*)").Groups[1].Value);

            this.Wpx = Convert.ToDecimal(Regex.Match(wPVal, @"x=(.*)y").Groups[1].Value);
            this.Wpy = Convert.ToDecimal(Regex.Match(wPVal, @"y=(.*)").Groups[1].Value);
        }

        public MasteringDisplayPrimaries(decimal Rx, decimal Ry,
            decimal Gx, decimal Gy,
            decimal Bx, decimal By,
            decimal Wpx, decimal Wpy)
        {

            this.Rx = Rx;
            this.Ry = Ry;
            this.Gx = Gx;
            this.Gy = Gy;
            this.Bx = Bx;
            this.By = By;
            this.Wpx = Wpx;
            this.Wpy = Wpy;
        }
    }

    public class MasteringDisplayLuminance
    {
        public decimal Min { get; private set; }
        public decimal Max { get; private set; }

        public MasteringDisplayLuminance(string masteringDisplayLuminance)
        {
            this.Min = Convert.ToDecimal(Regex.Match(masteringDisplayLuminance, @"min:(.*)cd\/m2,").Groups[1].Value);
            this.Max = Convert.ToDecimal(Regex.Match(masteringDisplayLuminance, @"max:(.*)cd").Groups[1].Value);
        }
    }

    public class Cea8613HdrData
    {
        public int MaxFALL { get; private set; }
        public int MaxCLL { get; private set; }

        public Cea8613HdrData(string maxCLL, string maxFALL)
        {
            this.MaxFALL = Convert.ToInt32(Regex.Match(maxFALL, @"(.*)cd").Groups[1].Value);
            this.MaxCLL = Convert.ToInt32(Regex.Match(maxCLL, @"(.*)cd").Groups[1].Value);
        }
    }

}