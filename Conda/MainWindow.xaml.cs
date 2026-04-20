using Conda.Core.Env;
using Conda.Core.ProjectSystem;
using Conda.UI.Views;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Conda
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckPython();
        }

        private async void OnNewProjectClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "New Project",
                Width = 450,
                Height = 250,
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

            var label = new TextBlock
            {
                Text = "Enter project name:",
                Foreground = Brushes.White,
                Margin = new Thickness(15, 20, 15, 5),
                FontSize = 14
            };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);

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

            var progressBar = new ProgressBar
            {
                Margin = new Thickness(15, 10, 15, 10),
                Height = 25,
                Visibility = Visibility.Collapsed
            };
            Grid.SetRow(progressBar, 2);
            grid.Children.Add(progressBar);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(15, 10, 15, 15)
            };
            Grid.SetRow(buttonPanel, 3);

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

                if (string.IsNullOrWhiteSpace(projectName))
                {
                    MessageBox.Show("Project name cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                createBtn.IsEnabled = false;
                cancelBtn.IsEnabled = false;
                progressBar.Visibility = Visibility.Visible;
                progressBar.IsIndeterminate = true;

                ProjectCreator creator = new ProjectCreator();
                var progress = new Progress<string>(msg => { });
                string creationResult = await creator.CreateProjectWithVenvAsync(projectName, progress);

                createBtn.IsEnabled = true;
                cancelBtn.IsEnabled = true;
                progressBar.Visibility = Visibility.Collapsed;

                MessageBox.Show(creationResult, "Conda", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void OnOpenProjectClicked(object sender, RoutedEventArgs e)
        {
            ProjectManager manager = new ProjectManager();
            var projects = manager.GetAllProjects();

            if (projects.Count == 0)
            {
                MessageBox.Show("No projects found. Create a new project first.", "Conda", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Window
            {
                Title = "Open Project",
                Width = 400,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                Left = this.Left + 10,
                Top = this.Top + 10
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
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

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(15, 10, 15, 15)
            };
            Grid.SetRow(buttonPanel, 2);

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

            openBtn.Click += (s, args) =>
            {
                if (selectedProject != null)
                    dialog.DialogResult = true;
                else
                    MessageBox.Show("Please select a project.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    Width = 1200,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    MinWidth = 800,
                    MinHeight = 500,
                    Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/logo/FirstIcon.png"))
                };
                editorWindow.Show();
                this.Hide();
                editorWindow.Closed += (s, args) => this.Show();
            }
        }

        private async System.Threading.Tasks.Task CheckPython()
        {
            var service = new PythonService();

            if (!service.IsPythonInstalled())
            {
                var result = MessageBox.Show(
                    "Python is not installed or not added to PATH.\n\nPlease install Python to use Conda IDE.\n\nDo you want to open the Python download page?",
                    "Python Not Found",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://www.python.org/downloads/",
                        UseShellExecute = true
                    });
                }
            }
        }
    }
}
