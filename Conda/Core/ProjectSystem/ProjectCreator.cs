using System;
using System.Diagnostics;
using System.IO;

namespace Conda.Core.ProjectSystem
{
    public class ProjectCreator
    {
        public static string CreateProject(string projectName)
        {
            try
            {
                string basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
                string projectsPath = Path.Combine(basePath, "CondaProjects");

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
                string projectsPath = Path.Combine(basePath, "CondaProjects");

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

                progress?.Report("Creating virtual environment...");
                await CreateVirtualEnvironmentAsync(projectPath);

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
    }
}
