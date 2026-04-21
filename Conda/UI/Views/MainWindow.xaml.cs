using Conda.Core.Environment;
using Conda.Core.ProjectSystem;
using Conda.UI.Views;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using ProgressBar = System.Windows.Controls.ProgressBar;
using ListBox = System.Windows.Controls.ListBox;
using Orientation = System.Windows.Controls.Orientation;
using Application = System.Windows.Application;

namespace Conda
{
    public partial class MainWindow : Window
    {
        private bool isFullScreen = false;
        private WindowState previousState;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckPython();
        }

        private static async System.Threading.Tasks.Task CheckPython()
        {
            if (!PythonService.IsPythonInstalled())
            {
                var result = System.Windows.MessageBox.Show(
                    "Python is not installed or not added to PATH.\n\nPlease install Python to use Conda IDE.\n\nDo you want to open the Python download page?",
                    "Python Not Found",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://www.python.org/downloads/",
                        UseShellExecute = true
                    });
                }
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        public void OnNewProjectClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "New Project",
                Width = 500,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Project Name
            var nameLabel = new TextBlock
            {
                Text = "Project Name:",
                Foreground = Brushes.White,
                Margin = new Thickness(15, 20, 15, 5),
                FontSize = 14
            };
            Grid.SetRow(nameLabel, 0);
            grid.Children.Add(nameLabel);

            var textBox = new TextBox
            {
                Text = "MyGame",
                Margin = new Thickness(15, 5, 15, 10),
                FontSize = 14,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
            };
            Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            // Location Option
            var locationLabel = new TextBlock
            {
                Text = "Save Location:",
                Foreground = Brushes.White,
                Margin = new Thickness(15, 10, 15, 5),
                FontSize = 14
            };
            Grid.SetRow(locationLabel, 2);
            grid.Children.Add(locationLabel);

            var locationPanel = new Grid();
            locationPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            locationPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(locationPanel, 3);
            
            var locationTextBox = new TextBox
            {
                Margin = new Thickness(15, 5, 5, 10),
                FontSize = 13,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CondaProjects")
            };
            Grid.SetColumn(locationTextBox, 0);
            
            var browseBtn = new Button
            {
                Content = "Browse...",
                Width = 80,
                Height = 35,
                Margin = new Thickness(5, 5, 15, 10),
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = Brushes.White,
                FontSize = 12
            };
            Grid.SetColumn(browseBtn, 1);
            
            locationPanel.Children.Add(locationTextBox);
            locationPanel.Children.Add(browseBtn);
            grid.Children.Add(locationPanel);
            
            // Use default checkbox
            var useDefaultCheck = new CheckBox
            {
                Content = "Use default location (CondaProjects folder in user home directory)",
                Margin = new Thickness(15, 5, 15, 10),
                Foreground = Brushes.White,
                FontSize = 12,
                IsChecked = true
            };
            Grid.SetRow(useDefaultCheck, 4);
            grid.Children.Add(useDefaultCheck);
            
            browseBtn.Click += (s, args) =>
            {
                var dialogFolder = new System.Windows.Forms.FolderBrowserDialog();
                if (dialogFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    locationTextBox.Text = dialogFolder.SelectedPath;
                    useDefaultCheck.IsChecked = false;
                }
            };
            
            useDefaultCheck.Checked += (s, args) =>
            {
                if (useDefaultCheck.IsChecked == true)
                {
                    locationTextBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CondaProjects");
                    locationTextBox.IsEnabled = false;
                }
                else
                {
                    locationTextBox.IsEnabled = true;
                }
            };
            
            useDefaultCheck.IsChecked = true;
            locationTextBox.IsEnabled = false;

            var progressBar = new ProgressBar
            {
                Margin = new Thickness(15, 10, 15, 10),
                Height = 25,
                Visibility = Visibility.Collapsed
            };
            Grid.SetRow(progressBar, 5);
            grid.Children.Add(progressBar);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(15, 20, 15, 15)
            };
            Grid.SetRow(buttonPanel, 6);

            var createBtn = new Button
            {
                Content = "Create",
                Width = 90,
                Height = 32,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = Brushes.White,
                FontSize = 13
            };

            var cancelBtn = new Button
            {
                Content = "Cancel",
                Width = 90,
                Height = 32,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                FontSize = 13
            };

            createBtn.Click += async (s, args) =>
            {
                string projectName = textBox.Text.Trim();
                string location = locationTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(projectName))
                {
                    System.Windows.MessageBox.Show("Project name cannot be empty.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(location))
                {
                    System.Windows.MessageBox.Show("Location cannot be empty.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                string projectPath = Path.Combine(location, projectName);

                if (Directory.Exists(projectPath))
                {
                    System.Windows.MessageBox.Show("Project already exists at this location!", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                createBtn.IsEnabled = false;
                cancelBtn.IsEnabled = false;
                progressBar.Visibility = Visibility.Visible;
                progressBar.IsIndeterminate = true;

                var progress = new Progress<string>(msg => { });
                string creationResult = await ProjectCreator.CreateProjectWithVenvAtPathAsync(projectPath, progress);

                createBtn.IsEnabled = true;
                cancelBtn.IsEnabled = true;
                progressBar.Visibility = Visibility.Collapsed;

                System.Windows.MessageBox.Show(creationResult, "Conda", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                dialog.DialogResult = true;
                dialog.Close();
            };

            cancelBtn.Click += (s, args) => { dialog.DialogResult = false; dialog.Close(); };

            buttonPanel.Children.Add(createBtn);
            buttonPanel.Children.Add(cancelBtn);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;
            dialog.ShowDialog();
        }

        public void OnOpenProjectClicked(object sender, RoutedEventArgs e)
        {
            ProjectManager manager = new();
            var projects = manager.GetAllProjects();

            if (projects.Count == 0)
            {
                System.Windows.MessageBox.Show("No projects found. Create a new project first.", "Conda", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var dialog = new Window
            {
                Title = "Open Project",
                Width = 450,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45))
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock
            {
                Text = "Select a project to open:",
                Foreground = Brushes.White,
                Margin = new Thickness(15, 20, 15, 10),
                FontSize = 14
            };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);

            var listBox = new ListBox
            {
                Margin = new Thickness(15, 5, 15, 10),
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                FontSize = 13
            };

            foreach (var project in projects)
            {
                listBox.Items.Add(project.Name);
            }
            Grid.SetRow(listBox, 1);
            grid.Children.Add(listBox);

            var browseOtherBtn = new Button
            {
                Content = "📁 Browse Other Location...",
                Margin = new Thickness(15, 5, 15, 10),
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                FontSize = 13
            };
            Grid.SetRow(browseOtherBtn, 2);
            grid.Children.Add(browseOtherBtn);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(15, 10, 15, 15)
            };
            Grid.SetRow(buttonPanel, 3);

            var openBtn = new Button
            {
                Content = "Open",
                Width = 90,
                Height = 32,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = Brushes.White,
                FontSize = 13
            };

            var cancelBtn = new Button
            {
                Content = "Cancel",
                Width = 90,
                Height = 32,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                FontSize = 13
            };

            ProjectModel? selectedProject = null;

            listBox.SelectionChanged += (s, args) =>
            {
                if (listBox.SelectedItem != null)
                {
                    selectedProject = projects.FirstOrDefault(p => p.Name == listBox.SelectedItem.ToString());
                }
            };
            
            browseOtherBtn.Click += (s, args) =>
            {
                var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;
                    if (Directory.Exists(selectedPath) && File.Exists(Path.Combine(selectedPath, "main.py")))
                    {
                        selectedProject = new ProjectModel
                        {
                            Name = Path.GetFileName(selectedPath),
                            Path = selectedPath
                        };
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Selected folder does not contain a valid Conda project (main.py not found).", "Invalid Project", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    }
                }
            };

            openBtn.Click += (s, args) =>
            {
                if (selectedProject != null)
                    dialog.DialogResult = true;
                else
                    System.Windows.MessageBox.Show("Please select a project.", "Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                dialog.Close();
            };

            cancelBtn.Click += (s, args) => { dialog.DialogResult = false; dialog.Close(); };

            buttonPanel.Children.Add(openBtn);
            buttonPanel.Children.Add(cancelBtn);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;
            var result = dialog.ShowDialog();

            if (result == true && selectedProject != null)
            {
                var editorView = new EditorView(selectedProject.Path);
                var editorWindow = new Window
                {
                    Title = $"Conda Editor - {selectedProject.Name}",
                    Content = editorView,
                    Width = 1400,
                    Height = 900,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    WindowState = WindowState.Maximized,
                    Icon = new BitmapImage(new Uri("pack://application:../../Assets/logo/FirstIcon.png", UriKind.RelativeOrAbsolute))
                };
                editorWindow.Show();
                this.Hide();
                editorWindow.Closed += (s, args) => this.Show();
            }
        }

        // Navigation Menu Handlers
        private void OnExitClicked(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void OnPreferencesClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Preferences dialog would open here.", "Preferences", System.Windows.MessageBoxButton.OK);
        private void OnSettingsClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Settings dialog would open here.", "Settings", System.Windows.MessageBoxButton.OK);
        private void OnToggleFullScreenClicked(object sender, RoutedEventArgs e) => ToggleFullScreen();
        private void OnResetLayoutClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Layout reset.", "Reset Layout", System.Windows.MessageBoxButton.OK);
        private void OnRecentProjectsClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Recent projects list would appear here.", "Recent Projects", System.Windows.MessageBoxButton.OK);
        private void OnProjectSettingsClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Project settings dialog would open here.", "Project Settings", System.Windows.MessageBoxButton.OK);
        private void OnBuildClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Build process would start here.", "Build", System.Windows.MessageBoxButton.OK);
        private void OnExportClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Export options would appear here.", "Export", System.Windows.MessageBoxButton.OK);
        private void OnPackageManagerClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Package manager would open here.", "Package Manager", System.Windows.MessageBoxButton.OK);
        private void OnExtensionsClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Extensions manager would open here.", "Extensions", System.Windows.MessageBoxButton.OK);
        private void OnOpenTerminalClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Terminal would open here.", "Terminal", System.Windows.MessageBoxButton.OK);
        private void OnRunCommandClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Command runner would open here.", "Run Command", System.Windows.MessageBoxButton.OK);
        private void OnSettingsIconClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Settings panel would open here.", "Settings", System.Windows.MessageBoxButton.OK);

        public void OnDocumentationClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://docs.anaconda.com/",
                UseShellExecute = true
            });
        }

        public void OnAboutClicked(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Conda IDE\nVersion 1.0\n\nA modern environment for Python development.", "About Conda", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        
        private void ToggleFullScreen()
        {
            if (!isFullScreen)
            {
                previousState = WindowState;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                ResizeMode = ResizeMode.NoResize;
                isFullScreen = true;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = previousState;
                ResizeMode = ResizeMode.CanResize;
                isFullScreen = false;
            }
        }
    }
}
