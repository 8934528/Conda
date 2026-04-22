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
using System.Threading.Tasks;
using System.Windows.Media.Animation;

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
            var showAnimation = FindResource("ShowDialogAnimation") as Storyboard;
            if (showAnimation != null)
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

            // Set icon based on type
            switch (icon)
            {
                case DialogIcon.Info:
                    dialog.TitleIcon.Text = "ℹ️";
                    break;
                case DialogIcon.Success:
                    dialog.TitleIcon.Text = "✅";
                    break;
                case DialogIcon.Warning:
                    dialog.TitleIcon.Text = "⚠️";
                    break;
                case DialogIcon.Error:
                    dialog.TitleIcon.Text = "❌";
                    break;
                case DialogIcon.Question:
                    dialog.TitleIcon.Text = "❓";
                    break;
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
            dialog.TitleIcon.Text = "⚙️";

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
            var button = sender as System.Windows.Controls.Button;
            result = button == Button1;

            var hideAnimation = FindResource("HideDialogAnimation") as Storyboard;
            if (hideAnimation != null)
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
            var hideAnimation = FindResource("HideDialogAnimation") as Storyboard;
            if (hideAnimation != null)
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
