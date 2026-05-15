using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp2.Services;

namespace WpfApp2.Views
{
    public partial class OverloadDialog : Window
    {
        private readonly List<TaskItem> _tasks;
        private readonly TaskManager _taskManager;

        public OverloadDialog(List<TaskItem> tasks, double hours, TaskManager taskManager)
        {
            InitializeComponent();
            _tasks = tasks;
            _taskManager = taskManager;

            HoursText.Text = $"Заплановано {hours:F1} ч. при ліміті {PlannerService.DailyLimit} ч.";

            var toMove = GetTasksToMove();
            MoveText.Text = toMove.Any()
                ? "На завтра перейдуть:\n" + string.Join("\n", toMove.Select(t => $"• {t.Name}"))
                : "Нічого переносити.";

            var today = DateTime.Today;
            var splittable = _tasks
                .Where(t => !t.IsCompleted
                         && !t.IsSplit
                         && t.EstimatedHours > 0
                         && t.Deadline.HasValue
                         && t.Deadline.Value.Date == today)
                .OrderByDescending(t => t.EstimatedHours)
                .ToList();

            SplitTaskCombo.ItemsSource = splittable;
            if (splittable.Any())
                SplitTaskCombo.SelectedIndex = 0;
        }

        private List<TaskItem> GetTasksToMove()
        {
            var today = DateTime.Today;
            var todayTasks = _tasks
                .Where(t => !t.IsCompleted && t.Deadline.HasValue &&
                            t.Deadline.Value.Date == today)
                .OrderBy(t => t.Priority)
                .ToList();

            double totalHours = todayTasks.Sum(t => GetEffectiveTodayHours(t));
            var toMove = new List<TaskItem>();

            foreach (var task in todayTasks)
            {
                if (totalHours <= PlannerService.DailyLimit) break;
                totalHours -= GetEffectiveTodayHours(task);
                toMove.Add(task);
            }
            return toMove;
        }

        private static double GetEffectiveTodayHours(TaskItem t)
        {
            if (!t.IsSplit)
                return t.EstimatedHours * (1 - t.Progress / 100.0);

            var today = DateTime.Today;
            return t.SubTasks
                .Where(s => s.Deadline.HasValue &&
                            s.Deadline.Value.Date == today &&
                            !s.IsChecked)
                .Sum(s => s.EstimatedHours);
        }

        private void Move_Click(object sender, RoutedEventArgs e)
        {
            var tomorrow = DateTime.Today.AddDays(1);
            foreach (var task in GetTasksToMove())
                task.Deadline = tomorrow;
            DialogResult = true;
        }

        private void Split_Click(object sender, RoutedEventArgs e)
        {
            if (!Validator.ValidateTaskSelected(SplitTaskCombo.SelectedItem, out string error))
            {
                MessageBox.Show(error);
                return;
            }

            var selected = (TaskItem)SplitTaskCombo.SelectedItem;
            var dialog = new SplitTaskDialog(selected, _taskManager) { Owner = this };
            if (dialog.ShowDialog() == true)
                DialogResult = true;
        }

        private void Ignore_Click(object sender, RoutedEventArgs e) => Close();

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