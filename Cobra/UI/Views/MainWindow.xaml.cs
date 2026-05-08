using Cobra.Core.Environment;
using Cobra.Core.ProjectSystem;
using Cobra.Core.Settings;
using Cobra.UI.Views;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;

using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using ProgressBar = System.Windows.Controls.ProgressBar;
using ListBox = System.Windows.Controls.ListBox;
using Orientation = System.Windows.Controls.Orientation;
using Application = System.Windows.Application;

namespace Cobra
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

            ProjectsPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "CobraProjects");
            DataContext = this;

            Loaded += MainWindow_Loaded;
            SettingsManager.Instance.SettingsUpdated += (s, e) => ApplySettings();
            ApplySettings();
        }

        private void ApplySettings()
        {
            var settings = SettingsManager.Instance.CurrentSettings;
            
            // Apply Theme colors
            if (settings.Theme == "Light")
            {
                this.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            }
            else
            {
                this.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckPython();
            await LoadProjects();
            await CheckPythonStatus();
            await CheckNodeStatus();
            await CheckNpmStatus();
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

                PythonStatusText.Text = version;
                PythonStatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                PythonStatusText.Text = "Python not found. Please install Python and add to PATH.";
                PythonStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task CheckNodeStatus()
        {
            if (FindName("NodeStatusText") is not TextBlock nodeText) return;

            if (NodeService.IsNodeInstalled())
            {
                string version = NodeService.GetNodeVersion();
                nodeText.Text = version;
                nodeText.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                nodeText.Text = "Node.js not found. Please install Node.js.";
                nodeText.Foreground = System.Windows.Media.Brushes.Red;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task CheckNpmStatus()
        {
            if (FindName("NpmStatusText") is not TextBlock npmText) return;

            if (NodeService.IsNpmInstalled())
            {
                string version = NodeService.GetNpmVersion();
                npmText.Text = version;
                npmText.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                npmText.Text = "NPM not found. Please install NPM.";
                npmText.Foreground = System.Windows.Media.Brushes.Red;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private static async System.Threading.Tasks.Task CheckPython()
        {
            if (!PythonService.IsPythonInstalled())
            {
                var result = await CustomDialog.ShowAsync(
                    Application.Current.MainWindow,
                    "Python is not installed or not added to PATH.\n\nPlease install Python to use Cobra IDE.\n\nDo you want to open the Python download page?",
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
            var mainContentGrid = new Grid();
            mainContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Language Tabs
            mainContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Name Label
            mainContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Name Input
            mainContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Location Label
            mainContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Location Input
            mainContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Default Checkbox
            mainContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Config Section (Venv or JS)

            // Custom Style for TabItems to make them look premium
            var tabItemStyle = new Style(typeof(System.Windows.Controls.TabItem));
            tabItemStyle.Setters.Add(new Setter(System.Windows.Controls.TabItem.BackgroundProperty, Brushes.Transparent));
            tabItemStyle.Setters.Add(new Setter(System.Windows.Controls.TabItem.BorderThicknessProperty, new Thickness(0)));
            tabItemStyle.Setters.Add(new Setter(System.Windows.Controls.TabItem.ForegroundProperty, new SolidColorBrush(Color.FromRgb(170, 170, 170))));
            tabItemStyle.Setters.Add(new Setter(System.Windows.Controls.TabItem.TemplateProperty, CreateTabItemTemplate()));

            var langTabControl = new System.Windows.Controls.TabControl
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 20),
                ItemContainerStyle = tabItemStyle
            };
            Grid.SetRow(langTabControl, 0);

            var pythonTab = new System.Windows.Controls.TabItem
            {
                Header = "PYTHON",
                Padding = new Thickness(20, 10, 20, 10),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            
            var jsTab = new System.Windows.Controls.TabItem
            {
                Header = "JS + NPM",
                Padding = new Thickness(20, 10, 20, 10),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };

            langTabControl.Items.Add(pythonTab);
            langTabControl.Items.Add(jsTab);
            mainContentGrid.Children.Add(langTabControl);

            // Project Name
            var nameLabel = new TextBlock
            {
                Text = "Project Name:",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetRow(nameLabel, 1);
            mainContentGrid.Children.Add(nameLabel);

            var textBox = new TextBox
            {
                Text = "MyGame",
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 14,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 0, 10, 0)
            };
            Grid.SetRow(textBox, 2);
            mainContentGrid.Children.Add(textBox);

            // Location
            var locationLabel = new TextBlock
            {
                Text = "Save Location:",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetRow(locationLabel, 3);
            mainContentGrid.Children.Add(locationLabel);

            var locationPanel = new Grid();
            locationPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            locationPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(locationPanel, 4);

            var locationTextBox = new TextBox
            {
                Margin = new Thickness(0, 0, 8, 0),
                FontSize = 14,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 0, 10, 0),
                Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CobraProjects")
            };
            Grid.SetColumn(locationTextBox, 0);

            var browseBtn = new Button
            {
                Content = "Browse...",
                Width = 85,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                FontSize = 14,
                Cursor = System.Windows.Input.Cursors.Hand,
                Style = (Style)FindResource("CommonRoundedButtonStyle")
            };
            Grid.SetColumn(browseBtn, 1);

            locationPanel.Children.Add(locationTextBox);
            locationPanel.Children.Add(browseBtn);
            mainContentGrid.Children.Add(locationPanel);

            // Use default checkbox
            var useDefaultCheck = new CheckBox
            {
                Content = "Use default location (CobraProjects folder in user home directory)",
                Margin = new Thickness(0, 15, 0, 15),
                Foreground = Brushes.White,
                FontSize = 13,
                IsChecked = true
            };
            Grid.SetRow(useDefaultCheck, 5);
            mainContentGrid.Children.Add(useDefaultCheck);

            // Venv Section (For Python)
            var venvPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Padding = new Thickness(15),
                CornerRadius = new CornerRadius(5),
                BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 5, 0, 0),
                Visibility = Visibility.Visible
            };
            Grid.SetRow(venvPanel, 6);
            
            var venvContent = new Grid();
            venvContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            venvContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            var venvText = new TextBlock
            {
                Text = "Add a virtual environment",
                Foreground = Brushes.White,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Medium
            };
            Grid.SetColumn(venvText, 0);
            
            bool addVenv = false;
            var venvBtn = new Button
            {
                Content = "Add Venv",
                Padding = new Thickness(25, 10, 25, 10),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand,
                Style = (Style)FindResource("CommonRoundedButtonStyle"),
                Opacity = 0.6
            };
            Grid.SetColumn(venvBtn, 1);
            
            venvBtn.Click += (s, args) =>
            {
                addVenv = !addVenv;
                venvBtn.Foreground = addVenv ? Brushes.White : new SolidColorBrush(Color.FromRgb(150, 150, 150));
                venvBtn.Opacity = addVenv ? 1.0 : 0.6;
                venvBtn.Background = addVenv ? new SolidColorBrush(Color.FromRgb(255, 140, 0)) : Brushes.Transparent;
            };
            
            venvContent.Children.Add(venvText);
            venvContent.Children.Add(venvBtn);
            venvPanel.Child = venvContent;
            mainContentGrid.Children.Add(venvPanel);

            // JS Config Section (For JS+NPM)
            var jsConfigPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Padding = new Thickness(15),
                CornerRadius = new CornerRadius(5),
                BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 5, 0, 0),
                Visibility = Visibility.Collapsed
            };
            Grid.SetRow(jsConfigPanel, 6);

            var jsInfoText = new TextBlock
            {
                Text = "Project will be initialized with Vite and Phaser engine.\nScalable architecture with scenes, systems, and entities.",
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            };
            jsConfigPanel.Child = jsInfoText;
            mainContentGrid.Children.Add(jsConfigPanel);

            // Tab selection logic
            langTabControl.SelectionChanged += (s, args) =>
            {
                if (langTabControl.SelectedItem == pythonTab)
                {
                    venvPanel.Visibility = Visibility.Visible;
                    jsConfigPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    venvPanel.Visibility = Visibility.Collapsed;
                    jsConfigPanel.Visibility = Visibility.Visible;
                }
            };

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
                    locationTextBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CobraProjects");
                    locationTextBox.IsEnabled = false;
                }
                else
                {
                    locationTextBox.IsEnabled = true;
                }
            };
            
            useDefaultCheck.Unchecked += (s, args) =>
            {
                locationTextBox.IsEnabled = true;
            };

            useDefaultCheck.IsChecked = true;
            locationTextBox.IsEnabled = false;

            // Show the modal
            var (confirmed, _) = await AnimatedModal.ShowCustomModalAsync(this, "Create New Project", mainContentGrid, "Create", "Cancel");

            if (confirmed)
            {
                string projectName = textBox.Text.Trim();
                string location = locationTextBox.Text.Trim();
                bool isJsProject = langTabControl.SelectedItem == jsTab;

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

                // Show progress dialog
                var progressDialog = new Window
                {
                    Title = "Creating Project",
                    Width = 450,
                    Height = 180,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent
                };

                var mainBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    CornerRadius = new CornerRadius(10),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    BorderThickness = new Thickness(1),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 15, Opacity = 0.3 }
                };

                var progressGrid = new Grid();
                progressGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                progressGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                progressGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var progressText = new TextBlock
                {
                    Text = isJsProject ? "Setting up JS+NPM Project..." : "Preparing project setup...",
                    Foreground = Brushes.White,
                    FontSize = 15,
                    Margin = new Thickness(25, 25, 25, 10),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                };
                Grid.SetRow(progressText, 0);
                progressGrid.Children.Add(progressText);

                var progressBar = new ProgressBar
                {
                    Height = 10,
                    Margin = new Thickness(25, 10, 25, 10),
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                    BorderThickness = new Thickness(0),
                    Foreground = new LinearGradientBrush(
                        isJsProject ? Color.FromRgb(203, 56, 55) : Color.FromRgb(255, 140, 0), // NPM Red or Cobra Orange
                        isJsProject ? Color.FromRgb(255, 100, 100) : Color.FromRgb(255, 165, 0),
                        0)
                };
                Grid.SetRow(progressBar, 1);
                progressGrid.Children.Add(progressBar);
                
                var statusText = new TextBlock
                {
                    Text = "Starting...",
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    FontSize = 12,
                    Margin = new Thickness(25, 0, 25, 25),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                };
                Grid.SetRow(statusText, 2);
                progressGrid.Children.Add(statusText);

                mainBorder.Child = progressGrid;
                progressDialog.Content = mainBorder;
                progressDialog.Show();

                // Animate progress bar
                var animation = new DoubleAnimation
                {
                    From = 0,
                    To = 100,
                    Duration = TimeSpan.FromSeconds(isJsProject ? 5 : (addVenv ? 3 : 2)),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                progressBar.BeginAnimation(ProgressBar.ValueProperty, animation);

                var progressReporter = new Progress<string>(msg =>
                {
                    statusText.Text = msg;
                });

                string result;
                if (isJsProject)
                {
                    result = await ProjectCreator.CreateJsProjectWithViteAsync(projectPath, progressReporter);
                }
                else if (addVenv)
                {
                    result = await ProjectCreator.CreateProjectWithVenvAtPathAsync(projectPath, progressReporter);
                }
                else
                {
                    result = await Task.Run(() => ProjectCreator.CreateProjectAtPath(projectPath));
                }

                // Wait for animation to finish if it's faster
                await Task.Delay(500);
                progressDialog.Close();

                if (result.StartsWith("Error"))
                {
                    await CustomDialog.ShowAsync(this, result, "Error", DialogIcon.Error);
                }
                else
                {
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
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            selectionPanel.Children.Add(label);

            var listBox = new ListBox
            {
                Height = 250,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                FontSize = 14
            };

            foreach (var project in projects)
            {
                listBox.Items.Add(project.Name);
            }
            selectionPanel.Children.Add(listBox);

            var browseOtherBtn = new Button
            {
                Content = "Browse Other Location...",
                Margin = new Thickness(0, 15, 0, 0),
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                FontSize = 14,
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
                        await CustomDialog.ShowAsync(this, "Selected folder does not contain a valid Cobra project (main.py not found).", "Invalid Project", DialogIcon.Warning);
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
        private void OnPreferencesClicked(object sender, RoutedEventArgs e) => OnSettingsIconClicked(sender, e);
        private void OnSettingsClicked(object sender, RoutedEventArgs e) => OnSettingsIconClicked(sender, e);
        private void OnToggleFullScreenClicked(object sender, RoutedEventArgs e) => ToggleFullScreen();
        private async void OnResetLayoutClicked(object sender, RoutedEventArgs e) => await CustomDialog.ShowAsync(this, "Layout reset.", "Reset Layout", DialogIcon.Info);
        private async void OnRecentProjectsClicked(object sender, RoutedEventArgs e) => await CustomDialog.ShowAsync(this, "Recent projects list would appear here.", "Recent Projects", DialogIcon.Info);
        private async void OnProjectSettingsClicked(object sender, RoutedEventArgs e)
        {
            if (RecentProjectsList.SelectedItem is ProjectModel project)
            {
                var folderSettings = new FolderSettings(project.Path);
                var settingsView = new SettingsView(folderSettings);
                var settingsWindow = new Window
                {
                    Title = $"Project Settings - {project.Name}",
                    Content = settingsView,
                    Width = 1200,
                    Height = 800,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    WindowStyle = WindowStyle.None
                };
                WindowChrome.SetWindowChrome(settingsWindow, new WindowChrome { CaptionHeight = 35, ResizeBorderThickness = new Thickness(5), GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(0) });
                settingsWindow.ShowDialog();
            }
            else
            {
                await CustomDialog.ShowAsync(this, "Please select a project from the list to view its settings.", "No Selection", DialogIcon.Info);
            }
        }
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
                Title = "Cobra IDE - Settings",
                Content = settingsView,
                Width = 1200,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                WindowStyle = WindowStyle.None
            };
            WindowChrome.SetWindowChrome(settingsWindow, new WindowChrome { CaptionHeight = 35, ResizeBorderThickness = new Thickness(5), GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(0) });
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
                "To install dependencies, please follow these steps:\n\n1. Open your project.\n2. In the explorer, click on the requirements.txt file.\n3. Click the 'Install Requirements' button that appears.",
                "Install Dependencies",
                DialogIcon.Info);
        }

        private async void OnCheckPythonClicked(object sender, RoutedEventArgs e)
        {
            string version = PythonService.GetPythonVersion();
            await CustomDialog.ShowAsync(this,
                $"Installed Python Version:\n\n{version}",
                "Python Status",
                DialogIcon.Info);
        }

        private async void OnCheckNodeClicked(object sender, RoutedEventArgs e)
        {
            string version = NodeService.GetNodeVersion();
            await CustomDialog.ShowAsync(this,
                $"Installed Node.js Version:\n\n{version}",
                "Node.js Status",
                DialogIcon.Info);
        }

        private async void OnCheckNpmClicked(object sender, RoutedEventArgs e)
        {
            string version = NodeService.GetNpmVersion();
            await CustomDialog.ShowAsync(this,
                $"Installed NPM Version:\n\n{version}",
                "NPM Status",
                DialogIcon.Info);
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
            await CustomDialog.ShowAsync(this, "Cobra IDE\nVersion 1.0\n\nA modern environment for Python development.", "About Cobra", DialogIcon.Info);
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
            ToastIcon.Kind = isSuccess ? PackIconMaterialKind.CheckCircle : PackIconMaterialKind.CloseCircle;
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
                Title = $"Cobra Editor - {projectName}",
                Content = editorView,
                Width = 1400,
                Height = 900,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowState = WindowState.Maximized,
                WindowStyle = WindowStyle.None
            };
            WindowChrome.SetWindowChrome(editorWindow, new WindowChrome { CaptionHeight = 35, ResizeBorderThickness = new Thickness(5), GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(0) });
            editorWindow.Show();
            this.Hide();
            editorWindow.Closed += (s, args) => this.Show();
        }
        private static ControlTemplate CreateTabItemTemplate()
        {
            var template = new ControlTemplate(typeof(System.Windows.Controls.TabItem));
            
            var border = new FrameworkElementFactory(typeof(Border)) { Name = "Border" };
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(System.Windows.Controls.TabItem.BackgroundProperty));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 0, 2));
            border.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(System.Windows.Controls.TabItem.PaddingProperty));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.ContentSourceProperty, "Header");
            
            border.AppendChild(contentPresenter);
            template.VisualTree = border;

            // Triggers
            var selectedTrigger = new Trigger { Property = System.Windows.Controls.TabItem.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(System.Windows.Controls.TabItem.ForegroundProperty, Brushes.White));
            selectedTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(255, 140, 0)), "Border"));
            template.Triggers.Add(selectedTrigger);

            var mouseOverTrigger = new Trigger { Property = System.Windows.Controls.TabItem.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(System.Windows.Controls.TabItem.ForegroundProperty, Brushes.White));
            template.Triggers.Add(mouseOverTrigger);

            return template;
        }
    }
}
