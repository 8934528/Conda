using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Conda.Core.Env
{
    public class PythonService
    {
        public bool IsPythonInstalled()
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

        public string GetPythonVersion()
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
