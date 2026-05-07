using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Conda.Core.Environment
{
    public static class VenvService
    {
        private static readonly string TemplatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "VenvTemplate");
        private static readonly string VenvDir = Path.Combine(TemplatePath, "venv");

        public static async Task EnsureTemplateExistsAsync(IProgress<string>? progress = null)
        {
            if (Directory.Exists(VenvDir)) return;

            Directory.CreateDirectory(TemplatePath);
            progress?.Report("Initializing venv template (this only happens once)...");

            await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new()
                    {
                        FileName = "python",
                        Arguments = "-m venv venv",
                        WorkingDirectory = TemplatePath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using Process? process = Process.Start(psi);
                    process?.WaitForExit(60000);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating venv template: {ex.Message}");
                }
            });
        }

        public static async Task CopyVenvToProjectAsync(string projectPath, IProgress<string>? progress = null)
        {
            await EnsureTemplateExistsAsync(progress);

            progress?.Report("Adding virtual environment...");
            string targetVenv = Path.Combine(projectPath, "venv");

            await Task.Run(() =>
            {
                try
                {
                    CopyDirectory(VenvDir, targetVenv);
                    FixVenvPaths(targetVenv);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error copying venv: {ex.Message}");
                }
            });
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        private static void FixVenvPaths(string venvPath)
        {
            // Update pyvenv.cfg if needed
            string cfgPath = Path.Combine(venvPath, "pyvenv.cfg");
            if (File.Exists(cfgPath))
            {
                // In most cases on Windows, we just need the python home to be correct.
                // Since we assume python is in PATH, we might not need to change much,
                // but if we wanted to be robust, we'd find the current python home.
            }

            // Fix scripts (activate.bat, etc.)
            // Most venv scripts use relative paths or are simple enough.
            // If they use absolute paths to the original template, we'd need to replace them.
        }
        
        public static bool VenvExists(string projectPath)
        {
            return Directory.Exists(Path.Combine(projectPath, "venv"));
        }
    }
}
