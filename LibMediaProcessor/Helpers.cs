using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace LibMediaProcessor
{
    class Helpers
    {
        public static string ExecuteCommand_StdOut(string cmd1, string cmd2)
        {
            Console.WriteLine("Executing command: {0} {1}", cmd1, cmd2);

            //Setup font configuration environment for FFMPEG
            var di = new DirectoryInfo(Assembly.GetExecutingAssembly().Location);
            string fontsdir = Path.Combine(Path.GetDirectoryName(di.FullName),"tools", "fonts");

            //remove existing environment variables so that we can add it cleanly
            Environment.SetEnvironmentVariable("FONTCONFIG_FILE", null);
            Environment.SetEnvironmentVariable("FC_CONFIG_DIR", null);
            Environment.SetEnvironmentVariable("FONTCONFIG_PATH", null);

            var procStartInfo = new ProcessStartInfo(cmd1, cmd2)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = false,
                EnvironmentVariables =
                    {
                        {
                            "FONTCONFIG_FILE", Path.Combine(fontsdir, "fonts.conf")
                        },
                        {
                            "FC_CONFIG_DIR", fontsdir
                        },
                        {
                            "FONTCONFIG_PATH", fontsdir
                        }
                    }
            };

            // Create process and execute
            var proc = new Process
            {
                StartInfo = procStartInfo
            };

            proc.Start();

            // Get the console output...blocking until execution completes
            string result = proc.StandardOutput.ReadToEnd();

            if (proc.ExitCode != 0)
            {
                throw new ApplicationException($"CMD {cmd1 + " " + cmd2} failed with error code: {result}");
            }
            return result.ToString();
        }
    }
}
