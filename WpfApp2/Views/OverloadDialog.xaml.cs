using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
                .Where(t => t.Deadline.HasValue &&
                            t.Deadline.Value.Date == today &&
                            !t.IsCompleted)
                .OrderBy(t => t.Priority)
                .ToList();

            double totalHours = todayTasks.Sum(t => t.RemainingHours);
            var toMove = new List<TaskItem>();

            foreach (var task in todayTasks)
            {
                if (totalHours <= PlannerService.DailyLimit) break;
                totalHours -= task.RemainingHours;
                toMove.Add(task);
            }
            return toMove;
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
            var selected = SplitTaskCombo.SelectedItem as TaskItem;
            if (selected == null)
            {
                MessageBox.Show("Оберіть завдання для розбиття");
                return;
            }

            var dialog = new SplitTaskDialog(selected, _taskManager)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
                DialogResult = true;
        }

        private void Ignore_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}