using System;
using System.Diagnostics;

namespace Conda.Core.Environment
{
    public class PythonService
    {
        public static bool IsPythonInstalled()
        {
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "python",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process? process = Process.Start(psi);

                if (process == null)
                    return false;

                process.WaitForExit(5000);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public static string GetPythonVersion()
        {
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "python",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process? process = Process.Start(psi);
                if (process == null) return "Unknown";

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);
                return output.Trim();
            }
            catch
            {
                return "Not installed";
            }
        }
    }
}
