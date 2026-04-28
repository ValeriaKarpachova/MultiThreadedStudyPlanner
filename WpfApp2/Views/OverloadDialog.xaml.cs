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
        private TaskItem _taskToSplit;

        public OverloadDialog(List<TaskItem> tasks, double hours, TaskManager taskManager)
        {
            InitializeComponent();

            _tasks = tasks;
            _taskManager = taskManager;

            HoursText.Text = $"Запланировано {hours:F1} ч. при лімите {PlannerService.DailyLimit} ч.";

            // Карточка "Перенести"
            var toMove = GetTasksToMove();
            MoveText.Text = toMove.Any()
                ? "На завтра перейдут:\n" + string.Join("\n", toMove.Select(t => $"• {t.Name}"))
                : "Нечего переносить.";

            // Карточка "Разбить" — ищем задание дольше 6 часов с дедлайном > сегодня
            _taskToSplit = _tasks.FirstOrDefault(t =>
                t.Deadline.HasValue &&
                t.Deadline.Value.Date >= DateTime.Today &&
                !t.IsCompleted &&
                t.EstimatedHours > PlannerService.DailyLimit &&
                (t.Deadline.Value.Date - DateTime.Today).TotalDays >= 1);

            if (_taskToSplit != null)
            {
                int parts = (int)Math.Ceiling(_taskToSplit.EstimatedHours / PlannerService.DailyLimit);
                int daysAvailable = (int)(_taskToSplit.Deadline!.Value.Date - DateTime.Today).TotalDays + 1;
                parts = Math.Min(parts, daysAvailable);

                SplitCard.Visibility = Visibility.Visible;
                SplitColumn.Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
                SplitText.Text = $"«{_taskToSplit.Name}» ({_taskToSplit.EstimatedHours} ч.) " +
                                 $"будет разбито на {parts} части по ~" +
                                 $"{Math.Round(_taskToSplit.EstimatedHours / parts, 1)} ч./день до дедлайна.";
            }
        }

        private List<TaskItem> GetTasksToMove()
        {
            var today = DateTime.Today;
            var todayTasks = _tasks
                .Where(t => t.Deadline.HasValue && t.Deadline.Value.Date == today && !t.IsCompleted)
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
            if (_taskToSplit == null) return;

            var parts = PlannerService.SplitTask(_taskToSplit);
            if (!parts.Any())
            {
                MessageBox.Show("Недостаточно дней до дедлайна для разбивки.");
                return;
            }

            // Удаляем оригинальное задание
            _taskManager.DeleteTask(_taskToSplit);

            // Добавляем части (без повторного показа диалога)
            foreach (var part in parts)
                _taskManager.AddTaskSilent(part);

            DialogResult = true;
        }

        private void Ignore_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}