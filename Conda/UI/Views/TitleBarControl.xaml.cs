using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Conda.UI.Views
{
    public partial class TitleBarControl : System.Windows.Controls.UserControl
    {
        public event EventHandler<RoutedEventArgs> NewProjectClicked = delegate { };
        public event EventHandler<RoutedEventArgs> OpenProjectClicked = delegate { };
        public event EventHandler<RoutedEventArgs> ExitClicked = delegate { };
        public event EventHandler<RoutedEventArgs> PreferencesClicked = delegate { };
        public event EventHandler<RoutedEventArgs> SettingsClicked = delegate { };
        public event EventHandler<RoutedEventArgs> ToggleFullScreenClicked = delegate { };
        public event EventHandler<RoutedEventArgs> ResetLayoutClicked = delegate { };
        public event EventHandler<RoutedEventArgs> RecentProjectsClicked = delegate { };
        public event EventHandler<RoutedEventArgs> ProjectSettingsClicked = delegate { };
        public event EventHandler<RoutedEventArgs> BuildClicked = delegate { };
        public event EventHandler<RoutedEventArgs> ExportClicked = delegate { };
        public event EventHandler<RoutedEventArgs> PackageManagerClicked = delegate { };
        public event EventHandler<RoutedEventArgs> ExtensionsClicked = delegate { };
        public event EventHandler<RoutedEventArgs> OpenTerminalClicked = delegate { };
        public event EventHandler<RoutedEventArgs> RunCommandClicked = delegate { };
        public event EventHandler<RoutedEventArgs> DocumentationClicked = delegate { };
        public event EventHandler<RoutedEventArgs> AboutClicked = delegate { };
        public event EventHandler<RoutedEventArgs> SettingsIconClicked = delegate { };

        public TitleBarControl()
        {
            InitializeComponent();
        }

        private void OnNewProjectClicked(object sender, RoutedEventArgs e)
            => NewProjectClicked?.Invoke(sender, e);

        private void OnOpenProjectClicked(object sender, RoutedEventArgs e)
            => OpenProjectClicked?.Invoke(sender, e);

        private void OnExitClicked(object sender, RoutedEventArgs e)
            => ExitClicked?.Invoke(sender, e);

        private void OnPreferencesClicked(object sender, RoutedEventArgs e)
            => PreferencesClicked?.Invoke(sender, e);

        private void OnSettingsClicked(object sender, RoutedEventArgs e)
            => SettingsClicked?.Invoke(sender, e);

        private void OnToggleFullScreenClicked(object sender, RoutedEventArgs e)
            => ToggleFullScreenClicked?.Invoke(sender, e);

        private void OnResetLayoutClicked(object sender, RoutedEventArgs e)
            => ResetLayoutClicked?.Invoke(sender, e);

        private void OnRecentProjectsClicked(object sender, RoutedEventArgs e)
            => RecentProjectsClicked?.Invoke(sender, e);

        private void OnProjectSettingsClicked(object sender, RoutedEventArgs e)
            => ProjectSettingsClicked?.Invoke(sender, e);

        private void OnBuildClicked(object sender, RoutedEventArgs e)
            => BuildClicked?.Invoke(sender, e);

        private void OnExportClicked(object sender, RoutedEventArgs e)
            => ExportClicked?.Invoke(sender, e);

        private void OnPackageManagerClicked(object sender, RoutedEventArgs e)
            => PackageManagerClicked?.Invoke(sender, e);

        private void OnExtensionsClicked(object sender, RoutedEventArgs e)
            => ExtensionsClicked?.Invoke(sender, e);

        private void OnOpenTerminalClicked(object sender, RoutedEventArgs e)
            => OpenTerminalClicked?.Invoke(sender, e);

        private void OnRunCommandClicked(object sender, RoutedEventArgs e)
            => RunCommandClicked?.Invoke(sender, e);

        private void OnDocumentationClicked(object sender, RoutedEventArgs e)
            => DocumentationClicked?.Invoke(sender, e);

        private void OnAboutClicked(object sender, RoutedEventArgs e)
            => AboutClicked?.Invoke(sender, e);

        private void OnSettingsIconClicked(object sender, RoutedEventArgs e)
            => SettingsIconClicked?.Invoke(sender, e);
    }
}
