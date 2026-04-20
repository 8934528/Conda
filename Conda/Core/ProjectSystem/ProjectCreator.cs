using System;
using System.Diagnostics;
using System.IO;

namespace Conda.Core.ProjectSystem
{
    public class ProjectCreator
    {
        public string CreateProject(string projectName)
        {
            try
            {
                string basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string projectsPath = Path.Combine(basePath, "CondaProjects");

                if (!Directory.Exists(projectsPath))
                    Directory.CreateDirectory(projectsPath);

                string projectPath = Path.Combine(projectsPath, projectName);

                if (Directory.Exists(projectPath))
                    return "Project already exists!";

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

        public async System.Threading.Tasks.Task<string> CreateProjectWithVenvAsync(string projectName, IProgress<string> progress)
        {
            try
            {
                string basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string projectsPath = Path.Combine(basePath, "CondaProjects");

                if (!Directory.Exists(projectsPath))
                    Directory.CreateDirectory(projectsPath);

                string projectPath = Path.Combine(projectsPath, projectName);

                if (Directory.Exists(projectPath))
                    return "Project already exists!";

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

        private async System.Threading.Tasks.Task CreateVirtualEnvironmentAsync(string projectPath)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
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

        private async System.Threading.Tasks.Task InstallRequirementsAsync(string projectPath)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    string python = GetPythonPath(projectPath);
                    if (python == null) return;

                    ProcessStartInfo psi = new ProcessStartInfo
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

        private string? GetPythonPath(string projectPath)
        {
            string win = Path.Combine(projectPath, "venv", "Scripts", "python.exe");
            string unix = Path.Combine(projectPath, "venv", "bin", "python");

            if (File.Exists(win)) return win;
            if (File.Exists(unix)) return unix;

            return null;
        }
    }
}
