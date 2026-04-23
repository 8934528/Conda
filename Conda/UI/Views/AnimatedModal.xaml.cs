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

namespace Conda.UI.Views
{
    public partial class AnimatedModal : Window
    {
        private readonly object? result = null;
        private bool isConfirmed = false;

        public AnimatedModal()
        {
            InitializeComponent();
            Loaded += AnimatedModal_Loaded;
        }

        private void AnimatedModal_Loaded(object sender, RoutedEventArgs e)
        {
            if (FindResource("ShowModalAnimation") is Storyboard showAnimation)
            {
                showAnimation.Begin(this);
            }
        }

        public static async Task<(bool confirmed, object? result)> ShowCustomModalAsync(
            Window owner,
            string title,
            UIElement content,
            string confirmText = "Create",
            string cancelText = "Cancel")
        {
            var modal = new AnimatedModal
            {
                Owner = owner,
                Title = title
            };

            modal.TitleText.Text = title;
            modal.ContentPanel.Children.Clear();
            modal.ContentPanel.Children.Add(content);

            // Add buttons
            var confirmButton = new System.Windows.Controls.Button
            {
                Content = confirmText,
                Width = 100,
                Height = 32,
                Margin = new Thickness(5, 0, 0, 0),
                Background = System.Windows.Media.Brushes.DodgerBlue,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            confirmButton.Click += (s, e) => modal.Confirm();

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = cancelText,
                Width = 100,
                Height = 32,
                Margin = new Thickness(5, 0, 0, 0),
                Background = System.Windows.Media.Brushes.Gray,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            cancelButton.Click += (s, e) => modal.Cancel();

            modal.ButtonsPanel.Children.Add(cancelButton);
            modal.ButtonsPanel.Children.Add(confirmButton);

            modal.ShowDialog();
            await Task.Delay(10);
            return (modal.isConfirmed, modal.result);
        }

        private async void Confirm()
        {
            isConfirmed = true;
            await CloseWithAnimation();
        }

        private async void Cancel()
        {
            isConfirmed = false;
            await CloseWithAnimation();
        }

        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            isConfirmed = false;
            await CloseWithAnimation();
        }

        private async Task CloseWithAnimation()
        {
            if (FindResource("HideModalAnimation") is Storyboard hideAnimation)
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
