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
                ProgressBox.Text = task.Progress.ToString();
                DeadlinePicker.SelectedDate = task.Deadline;
                EstimatedBox.Text = task.EstimatedHours.ToString();
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (!Validator.ValidateTask(
                    NameBox.Text,
                    ProgressBox.Text,
                    EstimatedBox.Text,
                    out int progress,
                    out double hours,
                    out string error))
            {
                MessageBox.Show(error);
                return;
            }

            NewTask.Name = NameBox.Text;
            NewTask.Progress = progress;
            NewTask.Deadline = DeadlinePicker.SelectedDate;

            var selectedType = (TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            NewTask.TaskType = selectedType == "Без типу" ? null : selectedType;

            NewTask.EstimatedHours = hours;

            PlannerService.CalculatePriorities(new List<TaskItem> { NewTask });

            DialogResult = true;

        }
    }
}