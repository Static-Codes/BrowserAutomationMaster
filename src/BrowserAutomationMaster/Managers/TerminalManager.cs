using BrowserAutomationMaster.Messaging;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace BrowserAutomationMaster.Managers
{
    public partial class TerminalManager
    {
        private static readonly Regex colorRegex = ColorRegex();
        [GeneratedRegex(@"^declare\s--\scTcolors_[B|F]g_(?:Blu|Gre|Red)=""(.*\d)""$", RegexOptions.Compiled)]
        private static partial Regex ColorRegex();

        public static System.Drawing.Color FromColor(System.ConsoleColor c)
        {
            int cInt = (int)c;

            int brightnessCoefficient = ((cInt & 8) > 0) ? 2 : 1;
            int r = ((cInt & 4) > 0) ? 64 * brightnessCoefficient : 0;
            int g = ((cInt & 2) > 0) ? 64 * brightnessCoefficient : 0;
            int b = ((cInt & 1) > 0) ? 64 * brightnessCoefficient : 0;
            Console.WriteLine($"{r}, {g}, {b}");
            return Color.FromArgb(r, g, b);
        }

        [SupportedOSPlatform("linux")]
        public static Dictionary<int, Color> GetTerminalColors()
        {
            //var test = Console.ForegroundColor;
            //var test2 = Console.BackgroundColor;
            Console.WriteLine(FromColor(Console.ForegroundColor));
            Console.WriteLine(FromColor(Console.BackgroundColor));
            //Console.WriteLine(test2);

            string scriptFileContents = @"#!/bin/bash
                getTermRGB() {
                    local _array _string _fb=(F B) _i
                    for _i in 0 1; do
                        printf -v _string '\e]1%d;?\e\\' $_i
                        IFS= read -d \\ -srp ""$_string"" _string
                        IFS=$'\e/' read -ra _array <<<""${_string#*:}""
                        read -ra _array <<<""${_array[*]%??}""
                        printf -v _string '%d ' ""${_array[@]/#/0x}""
                        read -r ""${1:-TERM}_${_fb[_i]}""g_{Red,Gre,Blu} <<<""$_string""
                    done
                }
                getTermRGB cTcolors
                declare  -p ${!cTcolors*}".Replace("                ", "");


            string scriptDirectory = Path.GetTempPath(); // Creates a temp file for {scriptFileName}
            string scriptFileName = "colorcheck.sh";
            string scriptFilePath = Path.Combine(scriptDirectory, scriptFileName);

            try
            {
                File.WriteAllText(scriptFilePath, scriptFileContents);
                if (!File.Exists(scriptFilePath))
                {
                    Console.WriteLine("HUGE PROBLEM");
                    Environment.Exit(1);
                }

                ProcessStartInfo chmodStartInfo = new()
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"chmod +x \"{scriptFilePath}\"\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };

                Process chmodProcess = new() { StartInfo = chmodStartInfo };
                chmodProcess.Start();
                chmodProcess.WaitForExit();

                if (chmodProcess.ExitCode != 0)
                {
                    Errors.WriteErrorAndExit(
                        message: $"BAM Manager (BAMM) was unable to give {scriptFileName} executable permissions.\n\n" +
                                 $"If this continues, please make a bug report at {ConstantManager.ISSUES_LINK}\n\n" +
                                 $"Error log:\nchmod failed with exit code {chmodProcess.ExitCode}",
                        status: 1);
                }

                ProcessStartInfo sedProcessInfo = new()
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"sed -i 's/\\r$//' \"{scriptFilePath}\"\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true
                };

                Process sedProcess = new() { StartInfo = sedProcessInfo };
                sedProcess.Start();
                sedProcess.WaitForExit();

                if (sedProcess.ExitCode != 0)
                {
                    Errors.WriteErrorAndExit(
                        message: $"BAM Manager (BAMM) was unable to give {scriptFileName} executable permissions.\n\n" +
                                 $"If this continues, please make a bug report at {ConstantManager.ISSUES_LINK}\n\n" +
                                 $"Error log:\nsed failed with exit code {sedProcess.ExitCode}",
                        status: 1
                    );
                }


                ProcessStartInfo scriptRunInfo = new()
                {
                    FileName = "printf",
                    Arguments = "'\033]11;?\007'",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                Process? process = Process.Start(scriptRunInfo);

                if (process == null)
                {
                    Errors.WriteErrorAndExit(
                        message: $"BAM Manager (BAMM) was unable to determine the terminal colors for your system, please try again.\n\n" +
                                 $"If this continues, please make a bug report at {ConstantManager.ISSUES_LINK}\n\n" +
                                 $"Error log:\n" +
                                 $"Process associated with {scriptFileName} returned null, but it successfully received +x privileges.",
                        status: 1
                    );
                }

                string output = process!.StandardInput.ToString() ?? "No";
                //string output = process!.StandardOutput.ReadToEnd(); // Null check above thus the null forgiveness operator.
                string errorOutput = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Errors.WriteErrorAndExit(
                        message:
                            $"BAM Manager (BAMM) was unable to determine the terminal colors for your system, please try again.\n\n" +
                            $"If this continues, please make a bug report at {ConstantManager.ISSUES_LINK}\n\nError log:\n" +
                            $"{scriptFileName} returned the following error:\n{errorOutput}\nExit Code: {process.ExitCode}",
                        status: 1
                    );
                }

                // Handles the cross system issues caused by pasting a unix script on a windows machine
                var lines = output.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
                //foreach (string line in lines) { Console.WriteLine(line); } // Used for debug only do not forget to comment this out.

                if (lines.Length != 6) { return []; }

                if (byte.TryParse(lines[0], out byte red1) &&
                    byte.TryParse(lines[1], out byte green1) &&
                    byte.TryParse(lines[2], out byte blue1) &&
                    byte.TryParse(lines[3], out byte red2) &&
                    byte.TryParse(lines[4], out byte green2) &&
                    byte.TryParse(lines[5], out byte blue2)
                )
                {
                    return new Dictionary<int, Color>() {
                        { 0, Color.FromArgb(red1, green1, blue1) },
                        { 1, Color.FromArgb(red2, green2, blue2) }
                    };
                }
                Errors.WriteErrorAndExit(
                    message: $"BAM Manager (BAMM) was unable to determine the terminal colors for your system, please try again.\n\n" +
                    $"If this continues, please make a bug report at {ConstantManager.ISSUES_LINK}\n\n" +
                    $"Error log:\n{scriptFileName} returned the following error:\n{errorOutput}\nExit Code: {process.ExitCode}",
                    status: 1
                );
            }
            catch (Exception ex)
            {
                Errors.WriteErrorAndExit(
                    message: $"BAM Manager (BAMM) was unable to determine the terminal colors for your system, please try again.\n\n" +
                             $"If this continues, please make a bug report at {ConstantManager.ISSUES_LINK}\n\n" +
                             $"Error log:\n{ex.Message}",
                    status: 1);
            }

            return [];
        }
    }
}
