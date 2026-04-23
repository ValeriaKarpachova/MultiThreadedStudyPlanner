using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp2.Services;

namespace WpfApp2
{
    public partial class EditTaskWindow : Window
    {
        public TaskItem Task { get; private set; }
        private readonly TaskManager _taskManager;

        public EditTaskWindow(TaskItem task, TaskManager taskManager)
        {
            InitializeComponent();

            Task = task;
            _taskManager = taskManager;
            taskManager.IsEditing = true;

            NameBox.Text = Task.Name;
            Description.Text = Task.Description;
            ProgressBox.Text = Task.Progress.ToString();
            EstimatedBox.Text = Task.EstimatedHours.ToString();
            DeadlinePicker.SelectedDate = Task.Deadline;
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
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

            Task.Name = NameBox.Text;
            Task.Description = Description.Text;
            Task.Progress = progress;
            Task.Deadline = DeadlinePicker.SelectedDate;
            Task.EstimatedHours = hours;

            var selectedType = (TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            Task.TaskType = selectedType == "Без типу" ? null : selectedType;

            PlannerService.CalculatePriorities(_taskManager.Tasks.ToList());

            _taskManager.IsEditing = false;
            _taskManager.UpdateTask(Task);

            DialogResult = true;
        }
    }
}