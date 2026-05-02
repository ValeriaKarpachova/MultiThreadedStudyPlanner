using System;
using System.Windows;
using System.Windows.Controls;
using WpfApp2.Services;

namespace WpfApp2
{
    public partial class AddTaskWindow : Window
    {
        public TaskItem NewTask { get; private set; }
        private readonly SubjectService _subjectSvc = new();

        public AddTaskWindow(TaskItem? task = null)
        {
            InitializeComponent();
            NewTask = task ?? new TaskItem();

            var subjects = _subjectSvc.GetAll();
            SubjectBox.ItemsSource = subjects;
            SubjectBox.DisplayMemberPath = "Name";
            SubjectBox.SelectedValuePath = "Id";

            if (task != null)
            {
                NameBox.Text = task.Name;
                Description.Text = task.Description;
                DeadlinePicker.SelectedDate = task.Deadline;
                EstimatedBox.Text = task.EstimatedHours.ToString();

                if (task.SubjectId.HasValue)
                    SubjectBox.SelectedValue = task.SubjectId.Value;

                foreach (ComboBoxItem item in TypeBox.Items)
                    if ((string)item.Content == task.TaskType)
                    { TypeBox.SelectedItem = item; break; }
            }
        }

        public void SetDeadline(DateTime date) =>
            DeadlinePicker.SelectedDate = date;

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (!Validator.ValidateTask(
                    NameBox.Text, EstimatedBox.Text,
                    out double hours, out string error))
            { MessageBox.Show(error); return; }

            NewTask.Name = NameBox.Text;
            NewTask.Description = Description.Text;
            NewTask.Deadline = DeadlinePicker.SelectedDate;
            NewTask.EstimatedHours = hours;
            NewTask.SubjectId = SubjectBox.SelectedValue is int id ? id : (int?)null;

            var sel = (TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            NewTask.TaskType = sel == "Без типу" || string.IsNullOrWhiteSpace(sel)
                ? null : sel;

            DialogResult = true;
        }
    }
}