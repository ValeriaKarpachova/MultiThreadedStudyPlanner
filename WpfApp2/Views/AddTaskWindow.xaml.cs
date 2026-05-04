using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

            for (int h = 0; h <= 23; h++)
                HourBox.Items.Add(h.ToString("D2"));

            for (int m = 0; m <= 55; m += 5)
                MinuteBox.Items.Add(m.ToString("D2"));

            HourBox.SelectedIndex = 9; 
            MinuteBox.SelectedIndex = 0;

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

                if (task.DeadlineTime.HasValue)
                {
                    TimeEnabledCheck.IsChecked = true;
                    HourBox.SelectedItem = task.DeadlineTime.Value.Hours.ToString("D2");
                    MinuteBox.SelectedItem = task.DeadlineTime.Value.Minutes.ToString("D2");
                }
            }

            UpdateTimeState();
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

        public void SetDeadline(DateTime date)
        {
            DeadlinePicker.SelectedDate = date;
        }

        private void TimeEnabledCheck_Changed(object sender, RoutedEventArgs e)
        {
            UpdateTimeState();
        }

        private void UpdateTimeState()
        {
            bool enabled = TimeEnabledCheck.IsChecked == true;
            HourBox.Opacity = enabled ? 1.0 : 0.4;
            MinuteBox.Opacity = enabled ? 1.0 : 0.4;
        }

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

            if (TimeEnabledCheck.IsChecked == true &&
                HourBox.SelectedItem != null && MinuteBox.SelectedItem != null)
            {
                int h = int.Parse((string)HourBox.SelectedItem);
                int m = int.Parse((string)MinuteBox.SelectedItem);
                NewTask.DeadlineTime = new TimeSpan(h, m, 0);
            }
            else
            {
                NewTask.DeadlineTime = null;
            }

            DialogResult = true;
        }
    }
}