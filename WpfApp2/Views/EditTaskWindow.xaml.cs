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

            _taskManager.IsEditing = true;

            NameBox.Text = Task.Name;
            Description.Text = Task.Description;
            ProgressBox.Text = Task.Progress.ToString();
            EstimatedBox.Text = Task.EstimatedHours.ToString();
            DeadlinePicker.SelectedDate = Task.Deadline;

            foreach (ComboBoxItem item in TypeBox.Items)
            {
                if ((string)item.Content == Task.TaskType)
                {
                    TypeBox.SelectedItem = item;
                    break;
                }
            }
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

            Task.TaskType = selectedType == "Без типу" || string.IsNullOrWhiteSpace(selectedType)
                ? null
                : selectedType;

            _taskManager.IsEditing = true;

            _taskManager.UpdateTask(Task);

            _taskManager.IsEditing = false;

            DialogResult = true;
        }
    }
}