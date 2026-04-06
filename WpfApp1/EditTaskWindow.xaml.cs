using System;
using System.Windows;

namespace WpfApp1
{
    public partial class EditTaskWindow : Window
    {
        public TaskItem Task { get; private set; }

        public EditTaskWindow(TaskItem task)
        {
            InitializeComponent();

            Task = task;

            NameBox.Text = Task.Name;
            ProgressBox.Text = Task.Progress.ToString();
            DeadlinePicker.SelectedDate = Task.Deadline;
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Task.Name = NameBox.Text;
                Task.Progress = int.Parse(ProgressBox.Text);
                Task.Deadline = DeadlinePicker.SelectedDate ?? DateTime.Now;

                DialogResult = true;
            }
            catch
            {
                MessageBox.Show("Invalid input");
            }
        }
    }
}