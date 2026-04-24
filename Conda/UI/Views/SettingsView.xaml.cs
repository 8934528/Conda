using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using Conda.Core.Settings;
using Microsoft.Win32;

using MediaColor = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;

namespace Conda.UI.Views
{
    public partial class SettingsView : Page
    {
        private Border? currentSelectedNav;
        private readonly Dictionary<string, StackPanel> sections = [];
        private readonly SettingsModel originalSettings;

        private readonly FolderSettings? _folderSettings;

        public SettingsView()
        {
            InitializeComponent();
            InitializeSections();
            
            // Global Settings
            originalSettings = LoadCurrentSettings(SettingsManager.Instance.CurrentSettings);
            DataContext = SettingsManager.Instance.CurrentSettings;
        }

        public SettingsView(FolderSettings folderSettings)
        {
            InitializeComponent();
            InitializeSections();
            
            // Folder Specific Settings
            _folderSettings = folderSettings;
            originalSettings = LoadCurrentSettings(folderSettings.Settings);
            DataContext = folderSettings.Settings;

            // Change header to reflect project settings
            if (FindName("MainHeader") is TextBlock mainHeader)
                mainHeader.Text = "⚙️ Project Settings";
            if (FindName("SubHeader") is TextBlock subHeader)
                subHeader.Text = "Customize settings for this project only";
        }

        private static SettingsModel LoadCurrentSettings(SettingsModel current)
        {
            // Simple clone
            return new SettingsModel
            {
                Language = current.Language,
                StartupBehavior = current.StartupBehavior,
                AutoSave = current.AutoSave,
                AutoSaveInterval = current.AutoSaveInterval,
                CheckForUpdates = current.CheckForUpdates,
                Telemetry = current.Telemetry,
                Theme = current.Theme,
                AccentColor = current.AccentColor,
                FontFamily = current.FontFamily,
                FontSize = current.FontSize,
                LineSpacing = current.LineSpacing,
                ShowMinimap = current.ShowMinimap,
                WordWrap = current.WordWrap,
                ShowLineNumbers = current.ShowLineNumbers,
                PythonInterpreter = current.PythonInterpreter,
                AutoCreateVenv = current.AutoCreateVenv,
                PipIndexUrl = current.PipIndexUrl,
                DefaultPackages = current.DefaultPackages,
                AutoCompletion = current.AutoCompletion,
                Linting = current.Linting
            };
        }

        private void InitializeSections()
        {
            sections.Add("General", GeneralSection);
            sections.Add("Theme", ThemeSection);
            sections.Add("Python", PythonSection);
            sections.Add("Editor", EditorSection);
            sections.Add("GameEngine", GameEngineSection);
            sections.Add("Tools", ToolsSection);
            sections.Add("Extensions", ExtensionsSection);
            sections.Add("Keymap", KeymapSection);
            sections.Add("About", AboutSection);
            
            currentSelectedNav = NavGeneral;
        }

        private void NavItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedNav)
            {
                string? tag = clickedNav.Tag?.ToString();
                if (!string.IsNullOrEmpty(tag) && sections.ContainsKey(tag))
                {
                    if (currentSelectedNav != null)
                    {
                        currentSelectedNav.Background = MediaBrushes.Transparent;
                    }
                    clickedNav.Background = new SolidColorBrush(MediaColor.FromRgb(45, 45, 45));
                    currentSelectedNav = clickedNav;

                    SwitchSection(tag);
                }
            }
        }

        private async void SwitchSection(string sectionName)
        {
            if (FindResource("FadeOutAnimation") is Storyboard fadeOut)
            {
                fadeOut.Completed += (s, _) =>
                {
                    foreach (var section in sections.Values)
                    {
                        section.Visibility = Visibility.Collapsed;
                    }

                    if (sections.TryGetValue(sectionName, out var selectedSection))
                    {
                        selectedSection.Visibility = Visibility.Visible;

                        if (FindResource("FadeInAnimation") is Storyboard fadeIn)
                        {
                            fadeIn.Begin(ContentContainer);
                        }
                    }
                };
                fadeOut.Begin(ContentContainer);
                await Task.Delay(150);
            }
            else
            {
                foreach (var section in sections.Values)
                {
                    section.Visibility = Visibility.Collapsed;
                }

                if (sections.TryGetValue(sectionName, out var selectedSection))
                {
                    selectedSection.Visibility = Visibility.Visible;
                }
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_folderSettings != null)
            {
                _folderSettings.Save();
            }
            else
            {
                SettingsManager.Instance.Save();
            }
            
            // In a real app, you'd trigger theme changes here
            System.Windows.MessageBox.Show("Settings applied successfully!", "Settings", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            
            if (NavigationService != null && NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Revert changes by copying back original values
            var current = SettingsManager.Instance.CurrentSettings;
            current.Language = originalSettings.Language;
            current.StartupBehavior = originalSettings.StartupBehavior;
            current.AutoSave = originalSettings.AutoSave;
            current.AutoSaveInterval = originalSettings.AutoSaveInterval;
            current.CheckForUpdates = originalSettings.CheckForUpdates;
            current.Telemetry = originalSettings.Telemetry;
            current.Theme = originalSettings.Theme;
            current.AccentColor = originalSettings.AccentColor;
            current.FontFamily = originalSettings.FontFamily;
            current.FontSize = originalSettings.FontSize;
            current.LineSpacing = originalSettings.LineSpacing;
            current.ShowMinimap = originalSettings.ShowMinimap;
            current.WordWrap = originalSettings.WordWrap;
            current.ShowLineNumbers = originalSettings.ShowLineNumbers;
            current.PythonInterpreter = originalSettings.PythonInterpreter;
            current.AutoCreateVenv = originalSettings.AutoCreateVenv;
            current.PipIndexUrl = originalSettings.PipIndexUrl;
            current.DefaultPackages = originalSettings.DefaultPackages;
            current.AutoCompletion = originalSettings.AutoCompletion;
            current.Linting = originalSettings.Linting;

            if (NavigationService != null && NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show("Are you sure you want to reset all settings to defaults?", "Reset Settings", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes)
            {
                SettingsManager.Instance.Reset();
                DataContext = SettingsManager.Instance.CurrentSettings;
            }
        }

        private void BrowsePythonButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Python Executable (python.exe)|python.exe|All files (*.*)|*.*",
                Title = "Select Python Interpreter"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SettingsManager.Instance.CurrentSettings.PythonInterpreter = openFileDialog.FileName;
            }
        }
    }
}
