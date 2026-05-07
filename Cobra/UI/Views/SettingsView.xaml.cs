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
using Cobra.Core.Settings;
using Microsoft.Win32;

using MediaColor = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;

namespace Cobra.UI.Views
{
    public partial class SettingsView : Page
    {
        private Border? currentSelectedNav;
        private readonly Dictionary<string, StackPanel> sections = [];
        private SettingsModel originalSettings;

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
                mainHeader.Text = "Project Settings";
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
                Linting = current.Linting,
                DefaultResolution = current.DefaultResolution,
                FullscreenDefault = current.FullscreenDefault,
                VSync = current.VSync,
                FrameRateLimit = current.FrameRateLimit,
                PhysicsEngine = current.PhysicsEngine,
                TerminalPath = current.TerminalPath,
                ExternalEditor = current.ExternalEditor,
                BuildTool = current.BuildTool,
                KeymapPreset = current.KeymapPreset,
                TranslationApiKey = current.TranslationApiKey
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

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_folderSettings != null)
            {
                _folderSettings.Save();
            }
            else
            {
                SettingsManager.Instance.Save();
                SettingsManager.Instance.NotifySettingsUpdated();
            }
            
            var owner = Window.GetWindow(this);
            await CustomDialog.ShowAsync(owner, "Settings applied successfully!", "Settings Applied", DialogIcon.Success);
            
            if (NavigationService != null && NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var owner = Window.GetWindow(this);
            bool discard = await CustomDialog.ShowAsync(owner, "Are you sure you want to discard unsaved changes?", "Discard Changes", DialogIcon.Warning, "Discard", "Cancel");

            if (discard)
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
                current.DefaultResolution = originalSettings.DefaultResolution;
                current.FullscreenDefault = originalSettings.FullscreenDefault;
                current.VSync = originalSettings.VSync;
                current.FrameRateLimit = originalSettings.FrameRateLimit;
                current.PhysicsEngine = originalSettings.PhysicsEngine;
                current.TerminalPath = originalSettings.TerminalPath;
                current.ExternalEditor = originalSettings.ExternalEditor;
                current.BuildTool = originalSettings.BuildTool;
                current.KeymapPreset = originalSettings.KeymapPreset;
                current.TranslationApiKey = originalSettings.TranslationApiKey;

                if (NavigationService != null && NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
        }

        private async void CheckNowButton_Click(object sender, RoutedEventArgs e)
        {
            var owner = Window.GetWindow(this);
            await CustomDialog.ShowAsync(owner, "Checking for updates...\n\nYou are running the latest version of Cobra IDE (v1.0.0).", "Update Check", DialogIcon.Info);
        }

        private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var owner = Window.GetWindow(this);
            bool confirm = await CustomDialog.ShowAsync(owner, "Are you sure you want to reset all settings to defaults?", "Reset Settings", DialogIcon.Warning, "Yes", "No");
            
            if (confirm)
            {
                SettingsManager.Instance.Reset();
                DataContext = SettingsManager.Instance.CurrentSettings;
                originalSettings = LoadCurrentSettings(SettingsManager.Instance.CurrentSettings);
                SettingsManager.Instance.NotifySettingsUpdated();
                
                await CustomDialog.ShowAsync(owner, "Settings have been reset to defaults.", "Reset Complete", DialogIcon.Info);
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

        private void AutoSaveToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
        }

        private async void AutoSaveToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            var owner = Window.GetWindow(this);
            
            var textBlock = new TextBlock
            {
                Text = "Turning off Auto Save might result in data loss if the application crashes. Do you want to proceed?",
                Foreground = new SolidColorBrush(MediaColor.FromRgb(224, 224, 224)),
                TextWrapping = TextWrapping.Wrap
            };
            
            var result = await AnimatedModal.ShowCustomModalAsync(owner, "Warning: Auto Save Disabled", textBlock, "Proceed", "Keep Auto Save");
            if (!result.confirmed)
            {
                // Revert
                if (sender is System.Windows.Controls.Primitives.ToggleButton toggle)
                {
                    toggle.IsChecked = true;
                }
            }
        }

        private void AutoSaveIntervalCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
        }



        private void AccentColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsLoaded) return;
            if (sender is Border border && border.Tag is string colorName)
            {
                SettingsManager.Instance.CurrentSettings.AccentColor = colorName;
            }
        }

        private void FontFamilyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
        }

        private void LineSpacingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
        }

        private void MinimapToggle_Checked(object sender, RoutedEventArgs e) { if (!IsLoaded) return; }
        private void MinimapToggle_Unchecked(object sender, RoutedEventArgs e) { if (!IsLoaded) return; }
        
        private void WordWrapToggle_Checked(object sender, RoutedEventArgs e) { if (!IsLoaded) return; }
        private void WordWrapToggle_Unchecked(object sender, RoutedEventArgs e) { if (!IsLoaded) return; }
        
        private void LineNumbersToggle_Checked(object sender, RoutedEventArgs e) { if (!IsLoaded) return; }
        private void LineNumbersToggle_Unchecked(object sender, RoutedEventArgs e) { if (!IsLoaded) return; }

        private void GenericSetting_Changed(object sender, EventArgs e)
        {
            if (!IsLoaded) return;
        }

        private void BrowseTerminalButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select Terminal Emulator"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SettingsManager.Instance.CurrentSettings.TerminalPath = openFileDialog.FileName;
            }
        }

        private void BrowseExternalEditorButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select External Editor"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SettingsManager.Instance.CurrentSettings.ExternalEditor = openFileDialog.FileName;
            }
        }
    }
}
