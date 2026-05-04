using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            Task         = task;
            _taskManager = taskManager;
            _taskManager.IsEditing = true;

            // Заповнюємо списки годин (00–23) і хвилин (00, 05, 10 ... 55)
            for (int h = 0; h <= 23; h++)
                HourBox.Items.Add(h.ToString("D2"));

            for (int m = 0; m <= 55; m += 5)
                MinuteBox.Items.Add(m.ToString("D2"));

            // Заповнюємо поля з існуючого завдання
            NameBox.Text                = Task.Name;
            Description.Text            = Task.Description;
            EstimatedBox.Text           = Task.EstimatedHours.ToString();
            DeadlinePicker.SelectedDate = Task.Deadline;

            foreach (ComboBoxItem item in TypeBox.Items)
                if ((string)item.Content == Task.TaskType)
                { TypeBox.SelectedItem = item; break; }

            var subjects = _subjectSvc.GetAll();
            SubjectBox.ItemsSource       = subjects;
            SubjectBox.DisplayMemberPath = "Name";
            SubjectBox.SelectedValuePath = "Id";
            if (Task.SubjectId.HasValue)
                SubjectBox.SelectedValue = Task.SubjectId.Value;

            // Відновлюємо час здачі якщо він був задан
            if (Task.DeadlineTime.HasValue)
            {
                TimeEnabledCheck.IsChecked = true;

                string hStr = Task.DeadlineTime.Value.Hours.ToString("D2");
                string mStr = (Task.DeadlineTime.Value.Minutes / 5 * 5).ToString("D2");

                HourBox.SelectedItem   = hStr;
                MinuteBox.SelectedItem = mStr;
            }
            else
            {
                TimeEnabledCheck.IsChecked = false;
                HourBox.SelectedIndex   = 9;   // 09:00 за замовчуванням
                MinuteBox.SelectedIndex = 0;
            }

            UpdateTimeState();
        }

        private void TimeEnabledCheck_Changed(object sender, RoutedEventArgs e)
        {
            UpdateTimeState();
        }

        private void UpdateTimeState()
        {
            bool enabled = TimeEnabledCheck.IsChecked == true;
            HourBox.Opacity   = enabled ? 1.0 : 0.4;
            MinuteBox.Opacity = enabled ? 1.0 : 0.4;
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (!Validator.ValidateTask(
                    NameBox.Text, EstimatedBox.Text,
                    out double hours, out string error))
            { MessageBox.Show(error); return; }

            Task.Name           = NameBox.Text;
            Task.Description    = Description.Text;
            Task.Deadline       = DeadlinePicker.SelectedDate;
            Task.EstimatedHours = hours;
            Task.SubjectId      = SubjectBox.SelectedValue is int id ? id : (int?)null;

            var sel = (TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            Task.TaskType = sel == "Без типу" || string.IsNullOrWhiteSpace(sel)
                ? null : sel;

            // Час здачі
            if (TimeEnabledCheck.IsChecked == true &&
                HourBox.SelectedItem != null && MinuteBox.SelectedItem != null)
            {
                int h = int.Parse((string)HourBox.SelectedItem);
                int m = int.Parse((string)MinuteBox.SelectedItem);
                Task.DeadlineTime = new TimeSpan(h, m, 0);
            }
            else
            {
                Task.DeadlineTime = null;
            }

            _taskManager.IsEditing = true;
            _taskManager.UpdateTask(Task);
            _taskManager.IsEditing = false;
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
