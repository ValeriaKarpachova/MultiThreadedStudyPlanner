using System.Windows;
using System.Windows.Input;

namespace WpfApp2.Views
{
    public partial class ConfirmDialog : Window
    {
        public bool IsConfirmed { get; private set; } = false;

        public ConfirmDialog(string message, string title = "Підтвердження")
        {
            InitializeComponent();
            MessageText.Text = message;
            Title = title;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            Close();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            Close();
        }

        public static bool Show(string message, string title = "Підтвердження")
        {
            var dialog = new ConfirmDialog(message, title);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
            return dialog.IsConfirmed;
        }
    }
}