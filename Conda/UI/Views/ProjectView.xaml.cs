using Conda.Core.Environment;
using Conda.Core.ProjectSystem;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;


namespace Conda.UI.Views
{
    public partial class ProjectView : Page
    {
        private readonly ProjectManager projectManager;
        private readonly ObservableCollection<ProjectModel> recentProjects;


        public string ProjectsPath { get; set; }

        public ProjectView()
        {
            InitializeComponent();
            projectManager = new ProjectManager();
            recentProjects = [];
            RecentProjectsList.ItemsSource = recentProjects;
            
            ProjectsPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "CondaProjects");
            DataContext = this;
            
            Loaded += ProjectView_Loaded;
        }

        private async void ProjectView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProjects();
            await CheckPythonStatus();
        }

        private async System.Threading.Tasks.Task LoadProjects()
        {
            var projects = projectManager.GetAllProjects();
            recentProjects.Clear();
            foreach (var project in projects)
            {
                recentProjects.Add(project);
            }
            ProjectCountText.Text = recentProjects.Count.ToString();
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task CheckPythonStatus()
        {
            if (PythonService.IsPythonInstalled())
            {
                string version = PythonService.GetPythonVersion();

                PythonStatusText.Text = $"✓ {version}";
                PythonStatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                PythonStatusText.Text = "✗ Python not found. Please install Python and add to PATH.";
                PythonStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private void OnNewProjectClicked(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this);
            if (mainWindow is MainWindow main)
            {
                main.OnNewProjectClicked(sender, e);
            }
        }

        private void OnOpenProjectClicked(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this);
            if (mainWindow is MainWindow main)
            {
                main.OnOpenProjectClicked(sender, e);
            }
        }

        private void OnProjectSelected(object sender, SelectionChangedEventArgs e)
        {
            // Open button
        }

        private async void OnOpenSelectedProject(object sender, RoutedEventArgs e)
        {
            if (RecentProjectsList.SelectedItem is ProjectModel project)
            {
                var editorView = new EditorView(project.Path);
                var editorWindow = new Window
                {
                    Title = $"Conda Editor - {project.Name}",
                    Content = editorView,
                    Width = 1400,
                    Height = 900,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    MinWidth = 1000,
                    MinHeight = 700,
                    WindowState = WindowState.Maximized
                };
                editorWindow.Show();
                Window.GetWindow(this)?.Close();
            }
            else
            {
                System.Windows.MessageBox.Show("Please select a project to open.", "No Selection", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private async void OnRefreshClicked(object sender, RoutedEventArgs e)
        {
            await LoadProjects();
            await CheckPythonStatus();
            System.Windows.MessageBox.Show("Projects refreshed!", "Refresh", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void OnInstallDepsClicked(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("To install dependencies:\n\n1. Open a project\n2. Navigate to the Editor\n3. Click 'Install Dependencies' button\n\nOr run: pip install -r requirements.txt in terminal", 
                "Install Dependencies", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void OnCheckPythonClicked(object sender, RoutedEventArgs e)
        {
            _ = CheckPythonStatus();
        }

        private void OnDocumentationClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.pygame.org/docs/",
                UseShellExecute = true
            });
        }

        private void OnAboutClicked(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Conda IDE\nVersion 1.0\n\nA Python Game Development Environment\n\nFeatures:\n• Project Management\n• Code Editor with Monaco\n• Python Virtual Environment Support\n• Pygame Integration\n• File Explorer\n• Output Console", 
                "About Conda IDE", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void OnSettingsClicked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings Panel\n\nConfigure:\n• Python Interpreter Path\n• Theme Preferences\n• Editor Font Size\n• Keybindings", 
                "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnExitClicked(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void OnPreferencesClicked(object sender, RoutedEventArgs e) => MessageBox.Show("Preferences dialog would open here.", "Preferences", MessageBoxButton.OK);
        private void OnToggleFullScreenClicked(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                if (window.WindowStyle != WindowStyle.None)
                {
                    window.WindowStyle = WindowStyle.None;
                    window.WindowState = WindowState.Maximized;
                }
                else
                {
                    window.WindowStyle = WindowStyle.SingleBorderWindow;
                    window.WindowState = WindowState.Normal;
                }
            }
        }
        private void OnResetLayoutClicked(object sender, RoutedEventArgs e) => MessageBox.Show("Layout reset.", "Reset Layout", MessageBoxButton.OK);
        private void OnRecentProjectsClicked(object sender, RoutedEventArgs e) => MessageBox.Show("Recent projects list would appear here.", "Recent Projects", MessageBoxButton.OK);
        private void OnProjectSettingsClicked(object sender, RoutedEventArgs e) => MessageBox.Show("Project settings dialog would open here.", "Project Settings", MessageBoxButton.OK);
        private void OnBuildClicked(object sender, RoutedEventArgs e) => MessageBox.Show("Build process would start here.", "Build", MessageBoxButton.OK);
        private void OnExportClicked(object sender, RoutedEventArgs e) => MessageBox.Show("Export options would appear here.", "Export", MessageBoxButton.OK);
        private void OnPackageManagerClicked(object sender, RoutedEventArgs e) => MessageBox.Show("Package manager would open here.", "Package Manager", MessageBoxButton.OK);
        private void OnExtensionsClicked(object sender, RoutedEventArgs e) => MessageBox.Show("Extensions manager would open here.", "Extensions", MessageBoxButton.OK);
        private void OnOpenTerminalClicked(object sender, RoutedEventArgs e) => MessageBox.Show("Terminal would open here.", "Terminal", MessageBoxButton.OK);
        private void OnRunCommandClicked(object sender, RoutedEventArgs e) => MessageBox.Show("Command runner would open here.", "Run Command", MessageBoxButton.OK);
        private void OnSettingsIconClicked(object sender, RoutedEventArgs e) => MessageBox.Show("Settings panel would open here.", "Settings", MessageBoxButton.OK);
    }
}
