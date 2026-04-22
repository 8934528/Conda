using Conda.Core.Environment;
using Conda.Core.ProjectSystem;
using Conda.UI.Views;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;

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
        private readonly ProjectManager projectManager;
        private readonly ObservableCollection<ProjectModel> recentProjects;

        public string ProjectsPath { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            projectManager = new ProjectManager();
            recentProjects = [];
            RecentProjectsList.ItemsSource = recentProjects;

            ProjectsPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "CondaProjects");
            DataContext = this;

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckPython();
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

        private static async System.Threading.Tasks.Task CheckPython()
        {
            if (!PythonService.IsPythonInstalled())
            {
                var result = await CustomDialog.ShowAsync(
                    Application.Current.MainWindow,
                    "Python is not installed or not added to PATH.\n\nPlease install Python to use Conda IDE.\n\nDo you want to open the Python download page?",
                    "Python Not Found",
                    DialogIcon.Warning,
                    "Yes",
                    "No");

                if (result)
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

        public async void OnNewProjectClicked(object sender, RoutedEventArgs e)
        {
            // Create custom content for the modal
            var contentGrid = new Grid();
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Project Name
            var nameLabel = new TextBlock
            {
                Text = "Project Name:",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetRow(nameLabel, 0);
            contentGrid.Children.Add(nameLabel);

            var textBox = new TextBox
            {
                Text = "MyGame",
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 13,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 0, 10, 0)
            };
            Grid.SetRow(textBox, 1);
            contentGrid.Children.Add(textBox);

            // Location
            var locationLabel = new TextBlock
            {
                Text = "Save Location:",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetRow(locationLabel, 2);
            contentGrid.Children.Add(locationLabel);

            var locationPanel = new Grid();
            locationPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            locationPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(locationPanel, 3);

            var locationTextBox = new TextBox
            {
                Margin = new Thickness(0, 0, 8, 0),
                FontSize = 12,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 0, 10, 0),
                Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CondaProjects")
            };
            Grid.SetColumn(locationTextBox, 0);

            var browseBtn = new Button
            {
                Content = "Browse...",
                Width = 85,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            Grid.SetColumn(browseBtn, 1);

            locationPanel.Children.Add(locationTextBox);
            locationPanel.Children.Add(browseBtn);
            contentGrid.Children.Add(locationPanel);

            // Use default checkbox
            var useDefaultCheck = new CheckBox
            {
                Content = "Use default location (CondaProjects folder in user home directory)",
                Margin = new Thickness(0, 15, 0, 0),
                Foreground = Brushes.White,
                FontSize = 11,
                IsChecked = true
            };
            Grid.SetRow(useDefaultCheck, 4);
            contentGrid.Children.Add(useDefaultCheck);

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

            // Show the modal
            var (confirmed, _) = await AnimatedModal.ShowCustomModalAsync(this, "Create New Project", contentGrid, "Create", "Cancel");

            if (confirmed)
            {
                string projectName = textBox.Text.Trim();
                string location = locationTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(projectName))
                {
                    await CustomDialog.ShowAsync(this, "Project name cannot be empty.", "Error", DialogIcon.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(location))
                {
                    await CustomDialog.ShowAsync(this, "Location cannot be empty.", "Error", DialogIcon.Error);
                    return;
                }

                string projectPath = Path.Combine(location, projectName);

                if (Directory.Exists(projectPath))
                {
                    await CustomDialog.ShowAsync(this, "Project already exists at this location!", "Error", DialogIcon.Error);
                    return;
                }

                // Show progress dialog - Fixed version without CornerRadius on Window
                var progressDialog = new Window
                {
                    Title = "Creating Project",
                    Width = 400,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent
                };

                // Create a border for the rounded corners
                var mainBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(0)
                };

                var progressGrid = new Grid();
                progressGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                progressGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var progressText = new TextBlock
                {
                    Text = "Creating project...",
                    Foreground = Brushes.White,
                    FontSize = 14,
                    Margin = new Thickness(20, 20, 20, 10),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                };
                Grid.SetRow(progressText, 0);
                progressGrid.Children.Add(progressText);

                var progressBar = new ProgressBar
                {
                    Height = 4,
                    Margin = new Thickness(20, 0, 20, 20),
                    IsIndeterminate = true,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 122, 204))
                };
                Grid.SetRow(progressBar, 1);
                progressGrid.Children.Add(progressBar);

                mainBorder.Child = progressGrid;
                progressDialog.Content = mainBorder;
                progressDialog.Show();

                var progress = new Progress<string>(msg =>
                {
                    progressText.Text = msg;
                });

                string creationResult = await ProjectCreator.CreateProjectWithVenvAtPathAsync(projectPath, progress);

                progressDialog.Close();

                if (creationResult.StartsWith("Error"))
                {
                    await CustomDialog.ShowAsync(this, creationResult, "Error", DialogIcon.Error);
                }
                else
                {
                    await CustomDialog.ShowAsync(this, "Project created successfully!", "Success", DialogIcon.Success);
                    ShowToast("Project created successfully!");
                    OpenProjectEditor(projectPath, projectName);
                }
            }
        }

        public async void OnOpenProjectClicked(object sender, RoutedEventArgs e)
        {
            ProjectManager manager = new();
            var projects = manager.GetAllProjects();

            if (projects.Count == 0)
            {
                await CustomDialog.ShowAsync(this, "No projects found. Create a new project first.", "Information", DialogIcon.Info);
                return;
            }

            // Create project selection UI
            var selectionPanel = new StackPanel();

            var label = new TextBlock
            {
                Text = "Select a project to open:",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            };
            selectionPanel.Children.Add(label);

            var listBox = new ListBox
            {
                Height = 250,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                FontSize = 13
            };

            foreach (var project in projects)
            {
                listBox.Items.Add(project.Name);
            }
            selectionPanel.Children.Add(listBox);

            var browseOtherBtn = new Button
            {
                Content = "📁 Browse Other Location...",
                Margin = new Thickness(0, 15, 0, 0),
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            selectionPanel.Children.Add(browseOtherBtn);

            ProjectModel? selectedProject = null;

            listBox.SelectionChanged += (s, args) =>
            {
                if (listBox.SelectedItem != null)
                {
                    selectedProject = projects.FirstOrDefault(p => p.Name == listBox.SelectedItem.ToString());
                }
            };

            browseOtherBtn.Click += async (s, args) =>
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
                        OpenProjectEditor(selectedProject.Path, selectedProject.Name);
                        // Close any open dialogs
                        var window = Window.GetWindow(browseOtherBtn);
                        if (window is AnimatedModal modal)
                        {
                            modal.Close();
                        }
                    }
                    else
                    {
                        await CustomDialog.ShowAsync(this, "Selected folder does not contain a valid Conda project (main.py not found).", "Invalid Project", DialogIcon.Warning);
                    }
                }
            };

            var (confirmed, _) = await AnimatedModal.ShowCustomModalAsync(this, "Open Project", selectionPanel, "Open", "Cancel");

            if (confirmed && selectedProject != null)
            {
                OpenProjectEditor(selectedProject.Path, selectedProject.Name);
            }
        }

        // Navigation Menu Handlers
        private void OnExitClicked(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private async void OnPreferencesClicked(object sender, RoutedEventArgs e) => await CustomDialog.ShowAsync(this, "Preferences dialog would open here.", "Preferences", DialogIcon.Info);
        private async void OnSettingsClicked(object sender, RoutedEventArgs e) => await CustomDialog.ShowAsync(this, "Settings dialog would open here.", "Settings", DialogIcon.Info);
        private void OnToggleFullScreenClicked(object sender, RoutedEventArgs e) => ToggleFullScreen();
        private async void OnResetLayoutClicked(object sender, RoutedEventArgs e) => await CustomDialog.ShowAsync(this, "Layout reset.", "Reset Layout", DialogIcon.Info);
        private async void OnRecentProjectsClicked(object sender, RoutedEventArgs e) => await CustomDialog.ShowAsync(this, "Recent projects list would appear here.", "Recent Projects", DialogIcon.Info);
        private async void OnProjectSettingsClicked(object sender, RoutedEventArgs e) => await CustomDialog.ShowAsync(this, "Project settings dialog would open here.", "Project Settings", DialogIcon.Info);
        private async void OnBuildClicked(object sender, RoutedEventArgs e) => await CustomDialog.ShowAsync(this, "Build process would start here.", "Build", DialogIcon.Info);
        private async void OnExportClicked(object sender, RoutedEventArgs e) => await CustomDialog.ShowAsync(this, "Export options would appear here.", "Export", DialogIcon.Info);
        private async void OnPackageManagerClicked(object sender, RoutedEventArgs e) => await CustomDialog.ShowAsync(this, "Package manager would open here.", "Package Manager", DialogIcon.Info);
        private async void OnExtensionsClicked(object sender, RoutedEventArgs e) => await CustomDialog.ShowAsync(this, "Extensions manager would open here.", "Extensions", DialogIcon.Info);
        private async void OnOpenTerminalClicked(object sender, RoutedEventArgs e) => await CustomDialog.ShowAsync(this, "Terminal would open here.", "Terminal", DialogIcon.Info);
        private async void OnRunCommandClicked(object sender, RoutedEventArgs e) => await CustomDialog.ShowAsync(this, "Command runner would open here.", "Run Command", DialogIcon.Info);

        private void OnSettingsIconClicked(object sender, RoutedEventArgs e)
        {
            var settingsView = new SettingsView();
            var settingsWindow = new Window
            {
                Title = "Conda IDE - Settings",
                Content = settingsView,
                Width = 1200,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            };
            settingsWindow.ShowDialog();
        }

        // Dashboard Event Handlers
        private void OnProjectSelected(object sender, SelectionChangedEventArgs e)
        {
            // Open button 
        }

        private async void OnOpenSelectedProject(object sender, RoutedEventArgs e)
        {
            if (RecentProjectsList.SelectedItem is ProjectModel project)
            {
                OpenProjectEditor(project.Path, project.Name);
            }
            else
            {
                await CustomDialog.ShowAsync(this, "Please select a project to open.", "No Selection", DialogIcon.Info);
            }
        }

        private async void OnRefreshClicked(object sender, RoutedEventArgs e)
        {
            await LoadProjects();
            await CheckPythonStatus();
            await CustomDialog.ShowAsync(this, "Projects refreshed successfully!", "Success", DialogIcon.Success);
        }

        private async void OnInstallDepsClicked(object sender, RoutedEventArgs e)
        {
            await CustomDialog.ShowAsync(this,
                "To install dependencies:\n\n1. Open a project\n2. Navigate to the Editor\n3. Click 'Install Dependencies' button\n\nOr run: pip install -r requirements.txt in terminal",
                "Install Dependencies",
                DialogIcon.Info);
        }

        private void OnCheckPythonClicked(object sender, RoutedEventArgs e)
        {
            _ = CheckPythonStatus();
        }

        public void OnDocumentationClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "#",
                UseShellExecute = true
            });
        }

        public async void OnAboutClicked(object sender, RoutedEventArgs e)
        {
            await CustomDialog.ShowAsync(this, "Conda IDE\nVersion 1.0\n\nA modern environment for Python development.", "About Conda", DialogIcon.Info);
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

        private void ShowToast(string message, bool isSuccess = true)
        {
            ToastMessage.Text = message;
            ToastIcon.Text = isSuccess ? "✅" : "❌";
            ToastOverlay.Visibility = Visibility.Visible;

            DoubleAnimation slideUp = new()
            {
                From = 100,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            DoubleAnimation fadeIn = new()
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400)
            };

            ToastTranslate.BeginAnimation(TranslateTransform.YProperty, slideUp);
            ToastOverlay.BeginAnimation(OpacityProperty, fadeIn);

            // Hide after delay
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (s, args) =>
            {
                DoubleAnimation slideDown = new()
                {
                    To = 100,
                    Duration = TimeSpan.FromMilliseconds(400),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                DoubleAnimation fadeOut = new()
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(400)
                };
                slideDown.Completed += (s2, args2) => ToastOverlay.Visibility = Visibility.Collapsed;
                ToastTranslate.BeginAnimation(TranslateTransform.YProperty, slideDown);
                ToastOverlay.BeginAnimation(OpacityProperty, fadeOut);
                timer.Stop();
            };
            timer.Start();
        }

        public void OpenProjectEditor(string projectPath, string projectName)
        {
            var editorView = new EditorView(projectPath);
            var editorWindow = new Window
            {
                Title = $"Conda Editor - {projectName}",
                Content = editorView,
                Width = 1400,
                Height = 900,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowState = WindowState.Maximized
            };
            editorWindow.Show();
            this.Hide();
            editorWindow.Closed += (s, args) => this.Show();
        }
    }
}