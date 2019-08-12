using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Properties;
using LibMediaProcessor;

namespace Tests
{
    [TestClass]
    public class MediaScannerTests
    {
        //Minimal ATMOS existence test
        [TestMethod]
        [TestCategory("Smoke")]
        [TestCategory("Functional")]
        public void MediaAttributes_MEDIA_ATMOS()
        {
            MediaProperties ma = new MediaProperties(Settings.Default.AtmosES);
            Assert.IsTrue(ma.HasAtmos);
        }

        [TestMethod]
        [TestCategory("Smoke")]
        [TestCategory("Functional")]
        public void EncodeJob_ConstrcutorTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), "Dir With Spaces");

            EncodeJob job = new EncodeJob("eng", testDir);
            Assert.IsTrue(Directory.Exists(job.OutputFolder));
            Directory.Delete(job.OutputFolder);
        }

        //Regressions check for Deluxe flavor Atmos with timecode payload
        [TestMethod]
        [TestCategory("Smoke")]
        [TestCategory("Functional")]
        public void MediaAttributes_MEDIA_ATMOS_TIMECODE()
        {
            MediaProperties ma = new MediaProperties(Settings.Default.AtmosESTC);
            Assert.IsTrue(ma.HasAtmos);
        }

        //General audio attributes test
        //Also makes sure items with spaces in the path work.
        [TestMethod]
        [TestCategory("Smoke")]
        [TestCategory("Functional")]
        public void MediaAttributes_MEDIA_VIDEO_ATTRIBUTES()
        {
            MediaProperties ma = new MediaProperties(Settings.Default.Mov6Ch);
            foreach (MediaStream ms in ma.Streams)
            {
                if(ms.StreamType == StreamType.Video)
                { 
                    Assert.IsTrue(ms.CodecFormat.ToLower().Contains("prores"));
                }
            }
        }
        
        //Extract HDR and other color metadata test
        [TestMethod]
        [TestCategory("Smoke")]
        [TestCategory("Functional")]
        public void MediaAttributes_MEDIA_HDR_ATTRIBUTES()
        {
            MediaProperties mi = new MediaProperties(Settings.Default.HDRMP4);
            Assert.IsTrue(mi.HasHDR);
        }

        //Test callback functionality of FFBase.ExecuteFFMpeg_LogProgress
        //Scans two seconds of video. Ensures StdErr contains data.
        [TestMethod]
        [TestCategory("Smoke")]
        [TestCategory("Functional")]
        public void FFMediaScanner_StatusCallbackTest()
        {
            MediaProperties ma = new MediaProperties(Settings.Default.HDRMP4);
            FFMediaScanner ffsMedia = new FFMediaScanner();
            var result = ffsMedia.ExecuteFFMpeg_LogProgress($" -i {ma.MediaFile} -t 2 -y -an -vf cropdetect -f rawvideo NUL ", ma.MediaDuration);

            Assert.IsFalse(String.IsNullOrEmpty(result.StdErr));
        }

        //Test combing and telecine detection
        [TestMethod]
        [TestCategory("Functional")]
        [TestCategory("Medium Duration - Less than 5 minutes")]
        public void FFMediaScanner_MEDIA_DETECT_COMBING()
        {
            var ma = new MediaProperties(Settings.Default.Artifacts_CleanTelecine);
            var scanner = new FFMediaScanner();
            scanner.DetectCombing(ma);
            Assert.IsTrue(ma.IsPureFilm);
        }

        //Test letterbox detection
        [TestMethod]
        [TestCategory("Functional")]
        [TestCategory("Medium Duration - Less than 5 minutes")]
        public void FFMediaScanner_MEDIA_DETECT_LETTERBOX()
        {
            var ma = new MediaProperties("D:\\lbox.mkv");
            var scanner = new FFMediaScanner();
            scanner.DetectLetterbox(ma);
            Assert.IsTrue(ma.HasLetterbox);
        }

        //Test letterbox detection two pass adaptive
        [TestMethod]
        [TestCategory("Functional")]
        [TestCategory("Medium Duration - Less than 5 minutes")]
        public void FFMediaScanner_MEDIA_DETECT_LETTERBOX_NOISE()
        {
            var ma = new MediaProperties(Settings.Default.Artifacts_Letterbox_Noise);
            var scanner = new FFMediaScanner();
            scanner.DetectLetterbox(ma);
            Assert.IsTrue(ma.HasLetterbox);
        }
    }
}
