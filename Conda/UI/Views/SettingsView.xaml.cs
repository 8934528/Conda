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

using MediaColor = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;

namespace Conda.UI.Views
{
    public partial class SettingsView : Page
    {
        private Border? currentSelectedNav;
        private readonly Dictionary<string, StackPanel> sections = new();

        public SettingsView()
        {
            InitializeComponent();
            InitializeSections();
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
            var fadeOut = FindResource("FadeOutAnimation") as Storyboard;
            if (fadeOut != null)
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

                        var fadeIn = FindResource("FadeInAnimation") as Storyboard;
                        if (fadeIn != null)
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
    }
}
