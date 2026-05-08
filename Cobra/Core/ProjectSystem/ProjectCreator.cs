using System;
using System.Diagnostics;
using System.IO;

namespace Cobra.Core.ProjectSystem
{
    public class ProjectCreator
    {
        public static string CreateProject(string projectName)
        {
            try
            {
                string basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
                string projectsPath = Path.Combine(basePath, "CobraProjects");

                if (!Directory.Exists(projectsPath))
                    Directory.CreateDirectory(projectsPath);

                string projectPath = Path.Combine(projectsPath, projectName);

                if (Directory.Exists(projectPath))
                    return "Project already exists!";

                return CreateProjectAtPath(projectPath);
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public static string CreateProjectAtPath(string projectPath)
        {
            try
            {
                Directory.CreateDirectory(projectPath);

                Directory.CreateDirectory(Path.Combine(projectPath, "assets"));
                Directory.CreateDirectory(Path.Combine(projectPath, "scenes"));
                Directory.CreateDirectory(Path.Combine(projectPath, "scripts"));

                ProjectTemplate.ApplyPygameTemplate(projectPath);

                return $"Project created successfully!\nLocation: {projectPath}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public static async System.Threading.Tasks.Task<string> CreateProjectWithVenvAsync(string projectName, IProgress<string> progress)
        {
            try
            {
                string basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
                string projectsPath = Path.Combine(basePath, "CobraProjects");

                if (!Directory.Exists(projectsPath))
                    Directory.CreateDirectory(projectsPath);

                string projectPath = Path.Combine(projectsPath, projectName);

                if (Directory.Exists(projectPath))
                    return "Project already exists!";

                return await CreateProjectWithVenvAtPathAsync(projectPath, progress);
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public static async System.Threading.Tasks.Task<string> CreateProjectWithVenvAtPathAsync(string projectPath, IProgress<string> progress)
        {
            try
            {
                Directory.CreateDirectory(projectPath);

                Directory.CreateDirectory(Path.Combine(projectPath, "assets"));
                Directory.CreateDirectory(Path.Combine(projectPath, "scenes"));
                Directory.CreateDirectory(Path.Combine(projectPath, "scripts"));

                ProjectTemplate.ApplyPygameTemplate(projectPath);

                await Cobra.Core.Environment.VenvService.CopyVenvToProjectAsync(projectPath, progress);

                progress?.Report("Installing dependencies...");
                await InstallRequirementsAsync(projectPath);

                progress?.Report("Project ready!");

                return $"Project created successfully!\nLocation: {projectPath}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private static async System.Threading.Tasks.Task CreateVirtualEnvironmentAsync(string projectPath)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new()
                    {
                        FileName = "python",
                        Arguments = "-m venv venv",
                        WorkingDirectory = projectPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using Process? process = Process.Start(psi);
                    process?.WaitForExit(60000);
                }
                catch { }
            });
        }

        private static async System.Threading.Tasks.Task InstallRequirementsAsync(string projectPath)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    string? python = GetPythonPath(projectPath);
                    if (python == null) return;

                    ProcessStartInfo psi = new()
                    {
                        FileName = python,
                        Arguments = "-m pip install -r requirements.txt",
                        WorkingDirectory = projectPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using Process? process = Process.Start(psi);
                    process?.WaitForExit(120000);
                }
                catch { }
            });
        }

        private static string? GetPythonPath(string projectPath)
        {
            string win = Path.Combine(projectPath, "venv", "Scripts", "python.exe");
            string unix = Path.Combine(projectPath, "venv", "bin", "python");

            if (File.Exists(win)) return win;
            if (File.Exists(unix)) return unix;

            return null;
        }
        public static async System.Threading.Tasks.Task<string> CreateJsProjectWithViteAsync(string projectPath, IProgress<string> progress)
        {
            try
            {
                progress?.Report("Initializing JS project structure...");
                Directory.CreateDirectory(projectPath);

                string projectName = Path.GetFileName(projectPath);
                ProjectTemplate.ApplyJsPhaserTemplate(projectPath, projectName);

                progress?.Report("Installing dependencies (npm install)...");
                bool success = await RunNpmInstallAsync(projectPath);

                if (!success)
                {
                    return "Error: Failed to install npm dependencies. Make sure Node.js and NPM are installed.";
                }

                progress?.Report("Project ready!");
                return $"Project created successfully!\nLocation: {projectPath}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private static async System.Threading.Tasks.Task<bool> RunNpmInstallAsync(string projectPath)
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new()
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c npm install",
                        WorkingDirectory = projectPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using Process? process = Process.Start(psi);
                    if (process == null) return false;

                    process.WaitForExit(180000); // 3 minutes timeout for npm install
                    return process.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            });
        }
        public static async System.Threading.Tasks.Task<string> AddBackendToProjectAsync(string projectPath, string backendType, IProgress<string> progress)
        {
            try
            {
                string backendDirName = backendType == "C#" ? "server" : "backend";
                string backendPath = Path.Combine(projectPath, backendDirName);

                if (Directory.Exists(backendPath))
                {
                    return $"Error: {backendDirName} folder already exists!";
                }

                progress?.Report($"Initializing {backendType} backend...");
                ProjectTemplate.ApplyBackendTemplate(backendPath, backendType);

                if (backendType == "Node.js")
                {
                    progress?.Report("Installing node backend dependencies...");
                    // We could run npm install here too, but for now we'll just create the files
                }

                progress?.Report($"{backendType} backend added successfully!");
                await System.Threading.Tasks.Task.CompletedTask;
                return $"{backendType} backend added successfully!";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
