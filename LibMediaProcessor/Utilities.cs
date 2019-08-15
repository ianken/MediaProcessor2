using System;
using System.Diagnostics;
using System.IO;

namespace LibMediaProcessor
{
    /// <summary>
    /// Wrapper for console output
    /// </summary>
    public class ConsoleOutput
    {
        public string StdErr { get; }
        public string StdOut { get; }

        public ConsoleOutput(string stdOut, string stdErr)
        {
            this.StdOut = stdOut;
            this.StdErr = stdErr;
        }
    }

    /// <summary>
    /// An exception that is thrown when errors are the result of
    /// issues that must be addressed by the content provider.
    /// IE: bad audio channel configuration or media with multiple video streams.
    /// </summary>

    public class ProviderTargetedException : Exception
    {
        public ProviderTargetedException()
        {
        }

        public ProviderTargetedException(string message)
            : base(message)
        {
        }

        public ProviderTargetedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    /// <summary>
    /// Assorted utilities.
    /// Run executables, verify file existence, etc
    /// </summary>
    public class Utilities
    {
        //Locations of command line tools...
        public readonly ToolBins mediaTools;
        //Callback for console parsing.
        public delegate void ProgressParser(object sendingProcess, DataReceivedEventArgs errLine);

        public enum LogLevel
        {
            Info=1,
            Warning,
            ExceptionDetails,
            AppError,
            ProviderError
        }

        public Utilities()
        {
            //Get tools
            this.mediaTools = new ToolBins(this);
        }

        /// <summary>
        /// Verifies existence of file. Throws if it's missing
        /// </summary>
        /// <param name="filePath"> path to file.</param>
        public void ValidateFile(string filePath)
        {
            if (!File.Exists(filePath.Replace("\"", "")))
            {
                throw new FileNotFoundException($"File {filePath} not found.");
            }
        }

        /// <summary>
        /// If directory does not exist, create it.
        /// </summary>
        /// <param name="dirPath"> path to file.</param>
        public void ValidateDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath.Replace("\"", "")))
            {
                Directory.CreateDirectory(dirPath.Replace("\"", ""));
            }
        }

        /// <summary>
        /// A central place to log events.
        /// </summary>
        /// <param name="message"> message.</param>
        /// <param name="logLevel">Severity of message</param>
        /// <param name="newLine">Flag to prevent allow/disallow newline when emitting info.</param>
        public void LogProxy(string message, LogLevel logLevel, bool newLine = true)
        {
            switch (logLevel)
            {
                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"\r");
                    Console.Write($"INFO:");
                    Console.ResetColor();
                    if (newLine)
                    {
                        Console.WriteLine($"{message}");
                    }
                    else
                    {
                        Console.Write($"{message}");
                    }
                        
                    break;

                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"WARNING:");
                    Console.ResetColor();
                    Console.WriteLine($"{message}");
                    break;

                case LogLevel.ExceptionDetails:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"EXCEPTION DETAILS:");
                    Console.ResetColor();
                    Console.WriteLine($"{message}");
                    break;

                case LogLevel.AppError:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"APPLICATION ERROR:");
                    Console.ResetColor();
                    Console.WriteLine($"{message}");
                    throw new ApplicationException(message);

                case LogLevel.ProviderError:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"Content/Provider ERROR:");
                    Console.ResetColor();
                    Console.WriteLine($"{message}");
                    throw new ProviderTargetedException(message);
            }
        }
        
        /// <summary>
        /// Runs command and returns content of STDERR
        /// </summary>
        /// <param name="bin"> Executable to run.</param>
        /// <param name="args"> Arguments for executable.</param>
        public string ExecuteCommand_StdErr(string bin, string args)
        {
            this.LogProxy($"Executing command: {bin} {args}", LogLevel.Info);

            //remove existing environment variables so that we can add them cleanly
            Environment.SetEnvironmentVariable("FONTCONFIG_FILE", null);
            Environment.SetEnvironmentVariable("FC_CONFIG_DIR", null);
            Environment.SetEnvironmentVariable("FONTCONFIG_PATH", null);

            var procStartInfo = new ProcessStartInfo(bin, args)
            {
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false,
                EnvironmentVariables =
                    {
                        {
                            "FONTCONFIG_FILE", this.mediaTools.FfFontsConf
                        },
                        {
                            "FC_CONFIG_DIR", this.mediaTools.FfFontsDir
                        },
                        {
                            "FONTCONFIG_PATH", this.mediaTools.FfFontsDir
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
            var consoleOutput =  proc.StandardError.ReadToEnd();

            if (proc.ExitCode != 0)
            {
                this.LogProxy($"CMD {bin + " " + args} failed with error code: {proc.ExitCode}", LogLevel.AppError);
            }

            return consoleOutput;
        }

        /// <summary>
        /// Runs command and returns content of STDOUT to the caller
        /// </summary>
        /// <param name="bin"> Executable to run.</param>
        /// <param name="args"> Arguments for executable.</param>
        public string ExecuteCommand_StdOut(string bin, string args)
        {
            this.LogProxy($"Executing command: {bin} {args}", LogLevel.Info);

            //remove existing environment variables so that we can add them cleanly
            Environment.SetEnvironmentVariable("FONTCONFIG_FILE", null);
            Environment.SetEnvironmentVariable("FC_CONFIG_DIR", null);
            Environment.SetEnvironmentVariable("FONTCONFIG_PATH", null);

            var procStartInfo = new ProcessStartInfo(bin, args)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = false,
                EnvironmentVariables =
                {
                    {
                        "FONTCONFIG_FILE", this.mediaTools.FfFontsConf
                    },
                    {
                        "FC_CONFIG_DIR", this.mediaTools.FfFontsDir
                    },
                    {
                        "FONTCONFIG_PATH", this.mediaTools.FfFontsDir
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
            var consoleOutput = proc.StandardOutput.ReadToEnd();

            if (proc.ExitCode != 0)
            {
                this.LogProxy($"CMD {bin + " " + args} failed with error code: {proc.ExitCode}", LogLevel.AppError);
            }

            return consoleOutput;
        }

        /// <summary>
        /// Runs command with output parsed by specified progress parsers
        /// </summary>
        /// <param name="bin">The executable to run.</param>
        /// <param name="args">Command line arguments.</param>
        /// <param name="stdOutHandler">Callback to handle parsing console output.</param>
        /// <param name="stdErrHandler">Callback to handle parsing console output.</param>
        public void ExecuteCommand_Parser(string bin, string args, ProgressParser stdOutHandler, ProgressParser stdErrHandler)
        {
            this.LogProxy($"Executing command: {bin} {args}", LogLevel.Info);

            //remove existing environment variables so that we can add them cleanly
            Environment.SetEnvironmentVariable("FONTCONFIG_FILE", null);
            Environment.SetEnvironmentVariable("FC_CONFIG_DIR", null);
            Environment.SetEnvironmentVariable("FONTCONFIG_PATH", null);

            var procStartInfo = new ProcessStartInfo(bin, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false,
                EnvironmentVariables =
                {
                    {
                        "FONTCONFIG_FILE", this.mediaTools.FfFontsConf
                    },
                    {
                        "FC_CONFIG_DIR", this.mediaTools.FfFontsDir
                    },
                    {
                        "FONTCONFIG_PATH", this.mediaTools.FfFontsDir
                    }
                }
            };

            // Create process and execute
            var proc = new Process
            {
                StartInfo = procStartInfo
            };

            proc.OutputDataReceived += new DataReceivedEventHandler(stdOutHandler);
            proc.ErrorDataReceived += new DataReceivedEventHandler(stdErrHandler);

            proc.Start();
            
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
          
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                this.LogProxy($"CMD {bin + " " + args} failed with error code: {proc.ExitCode}", LogLevel.AppError);
            }
        }
    }
}
