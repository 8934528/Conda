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
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;

using MediaColor = System.Windows.Media.Color;
using WpfButton = System.Windows.Controls.Button;

namespace Conda.UI.Views
{
    public enum DialogIcon
    {
        Info,
        Success,
        Warning,
        Error,
        Question
    }

    public partial class CustomDialog : Window
    {
        private bool result = false;

        public CustomDialog()
        {
            InitializeComponent();
            Loaded += CustomDialog_Loaded;
        }

        private void CustomDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (FindResource("ShowDialogAnimation") is Storyboard showAnimation)
            {
                showAnimation.Begin(this);
            }
        }

        public static async Task<bool> ShowAsync(
            Window owner,
            string message,
            string title = "Message",
            DialogIcon icon = DialogIcon.Info,
            string button1Text = "OK",
            string? button2Text = null)
        {
            var dialog = new CustomDialog
            {
                Owner = owner,
                Title = title
            };

            // Set title and message
            dialog.TitleText.Text = title;
            dialog.MessageText.Text = message;

            // Set icon based on type using PackIconMaterialKind
            if (dialog.TitleIcon is PackIconMaterial iconControl)
            {
                switch (icon)
                {
                    case DialogIcon.Info:
                        iconControl.Kind = PackIconMaterialKind.Information;
                        iconControl.Foreground = new SolidColorBrush(MediaColor.FromRgb(0, 122, 204)); // #007acc
                        break;
                    case DialogIcon.Success:
                        iconControl.Kind = PackIconMaterialKind.CheckCircle;
                        iconControl.Foreground = new SolidColorBrush(MediaColor.FromRgb(78, 201, 176)); // #4ec9b0
                        break;
                    case DialogIcon.Warning:
                        iconControl.Kind = PackIconMaterialKind.Alert;
                        iconControl.Foreground = new SolidColorBrush(MediaColor.FromRgb(255, 193, 7)); // #ffc107
                        break;
                    case DialogIcon.Error:
                        iconControl.Kind = PackIconMaterialKind.CloseCircle;
                        iconControl.Foreground = new SolidColorBrush(MediaColor.FromRgb(220, 53, 69)); // #dc3545
                        break;
                    case DialogIcon.Question:
                        iconControl.Kind = PackIconMaterialKind.HelpCircle;
                        iconControl.Foreground = new SolidColorBrush(MediaColor.FromRgb(0, 122, 204)); // #007acc
                        break;
                }
            }

            // Configure buttons
            dialog.Button1.Content = button1Text;

            if (!string.IsNullOrEmpty(button2Text))
            {
                dialog.Button2.Content = button2Text;
                dialog.Button2.Visibility = Visibility.Visible;
            }

            dialog.ShowDialog();

            // Wait for result
            await Task.Delay(10);
            return dialog.result;
        }

        public static async Task<bool?> ShowCustomAsync(
            Window owner,
            string title,
            UIElement customContent,
            string button1Text = "OK",
            string button2Text = "Cancel")
        {
            var dialog = new CustomDialog
            {
                Owner = owner,
                Title = title
            };

            dialog.TitleText.Text = title;

            // Set gear icon for custom dialog
            if (dialog.TitleIcon is PackIconMaterial customIconControl)
            {
                customIconControl.Kind = PackIconMaterialKind.Cog;
                customIconControl.Foreground = new SolidColorBrush(MediaColor.FromRgb(0, 122, 204)); // #007acc
            }

            // Add custom content
            dialog.CustomContent.Children.Clear();
            dialog.CustomContent.Children.Add(customContent);
            dialog.CustomContent.Visibility = Visibility.Visible;
            dialog.MessageText.Visibility = Visibility.Collapsed;

            // Configure buttons
            dialog.Button1.Content = button1Text;
            dialog.Button2.Content = button2Text;
            dialog.Button2.Visibility = Visibility.Visible;

            dialog.ShowDialog();

            await Task.Delay(10);
            return dialog.result;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as WpfButton;
            result = button == Button1;

            if (FindResource("HideDialogAnimation") is Storyboard hideAnimation)
            {
                hideAnimation.Completed += (s, _) => Close();
                hideAnimation.Begin(this);
            }
            else
            {
                Close();
            }
            await Task.CompletedTask;
        }

        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            result = false;
            if (FindResource("HideDialogAnimation") is Storyboard hideAnimation)
            {
                hideAnimation.Completed += (s, _) => Close();
                hideAnimation.Begin(this);
            }
            else
            {
                Close();
            }
            await Task.CompletedTask;
        }
    }
}
