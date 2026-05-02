using System.Windows;
using System.Windows.Controls;
using WpfApp2.Services;

namespace WpfApp2
{
    public partial class EditTaskWindow : Window
    {
        public TaskItem Task { get; private set; }
        private readonly TaskManager _taskManager;
        private readonly SubjectService _subjectSvc = new();

        public EditTaskWindow(TaskItem task, TaskManager taskManager)
        {
            InitializeComponent();
            Task = task;
            _taskManager = taskManager;
            _taskManager.IsEditing = true;

            // Поля задачі
            NameBox.Text = Task.Name;
            Description.Text = Task.Description;
            EstimatedBox.Text = Task.EstimatedHours.ToString();
            DeadlinePicker.SelectedDate = Task.Deadline;

            // Тип
            foreach (ComboBoxItem item in TypeBox.Items)
                if ((string)item.Content == Task.TaskType)
                { TypeBox.SelectedItem = item; break; }

            // Предмети
            var subjects = _subjectSvc.GetAll();
            SubjectBox.ItemsSource = subjects;
            SubjectBox.DisplayMemberPath = "Name";
            SubjectBox.SelectedValuePath = "Id";
            if (Task.SubjectId.HasValue)
                SubjectBox.SelectedValue = Task.SubjectId.Value;
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (!Validator.ValidateTask(
                    NameBox.Text, EstimatedBox.Text,
                    out double hours, out string error))
            { MessageBox.Show(error); return; }

            Task.Name = NameBox.Text;
            Task.Description = Description.Text;
            Task.Deadline = DeadlinePicker.SelectedDate;
            Task.EstimatedHours = hours;
            Task.SubjectId = SubjectBox.SelectedValue is int id ? id : (int?)null;

            var sel = (TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            Task.TaskType = sel == "Без типу" || string.IsNullOrWhiteSpace(sel)
                ? null : sel;

            _taskManager.IsEditing = true;
            _taskManager.UpdateTask(Task);
            _taskManager.IsEditing = false;
            DialogResult = true;
        }
    }
}