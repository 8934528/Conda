using System;
using System.Collections.Generic;
using System.IO;

namespace Conda.Core.ProjectSystem
{
    public class ProjectManager
    {
        private string basePath;

        public ProjectManager()
        {
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        private string GetProjectsPath()
        {
            return Path.Combine(basePath, "CondaProjects");
        }

        public List<ProjectModel> GetAllProjects()
        {
            List<ProjectModel> projects = new List<ProjectModel>();
            string projectsPath = GetProjectsPath();

            if (!Directory.Exists(projectsPath))
                return projects;

            var dirs = Directory.GetDirectories(projectsPath);
            foreach (var dir in dirs)
            {
                projects.Add(new ProjectModel
                {
                    Name = Path.GetFileName(dir),
                    Path = dir
                });
            }
            return projects;
        }
    }
}
