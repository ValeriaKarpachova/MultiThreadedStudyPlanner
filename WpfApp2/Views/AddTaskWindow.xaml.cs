using System;
using System.Windows;
using System.Windows.Controls;
using WpfApp2.Services;

namespace WpfApp2
{
    public partial class AddTaskWindow : Window
    {
        public TaskItem NewTask { get; private set; }

        public AddTaskWindow(TaskItem task = null)
        {
            InitializeComponent();

            NewTask = task ?? new TaskItem();

            if (task != null)
            {
                NameBox.Text = task.Name;
                DeadlinePicker.SelectedDate = task.Deadline;
                EstimatedBox.Text = task.EstimatedHours.ToString();
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (!Validator.ValidateTask(
                    NameBox.Text,
                    EstimatedBox.Text,
                    out double hours,
                    out string error))
            {
                MessageBox.Show(error);
                return;
            }

            NewTask.Name = NameBox.Text;
            NewTask.Description = Description.Text;
            NewTask.Deadline = DeadlinePicker.SelectedDate;
            NewTask.EstimatedHours = hours;

            var selectedType = (TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString()
                              ?? "Без типу";

            NewTask.TaskType = selectedType;

            DialogResult = true;
        }
    }
}