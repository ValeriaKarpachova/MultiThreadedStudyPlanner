using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace WpfApp2.Views
{
    public partial class AddSubjectDialog : Window
    {
        public string SubjectName { get; private set; } = "";
        public string SubjectColor { get; private set; } = "#534AB7";

        public AddSubjectDialog() => InitializeComponent();

        private void ColorRadio_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton rb &&
                rb.Tag?.ToString() is string color)
                SubjectColor = color;
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Введіть назву предмету");
                return;
            }

            SubjectName = NameBox.Text.Trim();

            var rb = ColorPicker.Children
                .OfType<System.Windows.Controls.RadioButton>()
                .FirstOrDefault(r => r.IsChecked == true);

            SubjectColor = rb?.Tag?.ToString() ?? "#534AB7";
            DialogResult = true;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}