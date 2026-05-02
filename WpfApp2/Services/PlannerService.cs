using System;
using System.Collections.Generic;
using System.Linq;
using WpfApp2.Services;

namespace WpfApp2.Services
{
    public static class PlannerService
    {
        public const double DailyLimit = 6.0;

        // ── Задачі на сьогодні ─────────────────────────────────────────────────
        // Для розбитих задач — враховуємо тільки підзадачі з дедлайном сьогодні
        public static List<TaskItem> GetTodayTasks(List<TaskItem> tasks)
        {
            var today = DateTime.Today;
            var result = new List<TaskItem>();

            foreach (var t in tasks)
            {
                if (t.IsCompleted) continue;

                if (t.IsSplit)
                {
                    // Тільки незавершені частини з дедлайном сьогодні
                    var todaySubs = t.SubTasks
                        .Where(s => s.Deadline.HasValue &&
                                    s.Deadline.Value.Date == today &&
                                    !s.IsChecked)
                        .ToList();

                    if (todaySubs.Any())
                        result.Add(t); // додаємо батька, але години рахуємо через TodayHours
                }
                else
                {
                    if (t.Deadline.HasValue && t.Deadline.Value.Date == today)
                        result.Add(t);
                }
            }

            return result;
        }

        // ── Годин на сьогодні ──────────────────────────────────────────────────
        // Для розбитих — сума годин ТІЛЬКИ тих підзадач, що мають дедлайн сьогодні
        public static double GetTodayHours(List<TaskItem> tasks)
        {
            var today = DateTime.Today;
            double total = 0;

            foreach (var t in tasks)
            {
                if (t.IsCompleted) continue;

                if (t.IsSplit)
                {
                    total += t.SubTasks
                        .Where(s => s.Deadline.HasValue &&
                                    s.Deadline.Value.Date == today &&
                                    !s.IsChecked)
                        .Sum(s => s.EstimatedHours);
                }
                else
                {
                    if (t.Deadline.HasValue && t.Deadline.Value.Date == today)
                        total += t.EstimatedHours * (1 - t.Progress / 100.0);
                }
            }

            return total;
        }

        // ── Пріоритети ─────────────────────────────────────────────────────────
        public static void CalculatePriorities(List<TaskItem> tasks)
        {
            foreach (var task in tasks)
            {
                double urgency = 0;

                if (task.Deadline.HasValue)
                {
                    double daysLeft = (task.Deadline.Value - DateTime.Now).TotalDays;
                    urgency = daysLeft <= 0
                        ? 1.0 + Math.Abs(daysLeft)
                        : 1.0 / (daysLeft + 1.0);
                }

                double completion = (100.0 - task.Progress) / 100.0;
                double importance = TaskTypeService.GetImportance(task.TaskType);

                task.Priority = (int)(
                    0.3 * urgency +
                    0.2 * importance +
                    0.5 * completion) * 100;
            }
        }

        // ── Розбиття задачі на частини ─────────────────────────────────────────
        public static List<TaskItem> SplitTask(TaskItem task, int parts, List<string> subTaskNames)
        {
            var today = DateTime.Today;
            double hoursPerPart = Math.Round(task.EstimatedHours / parts, 1);
            var result = new List<TaskItem>();

            for (int i = 0; i < parts; i++)
            {
                result.Add(new TaskItem
                {
                    Name = subTaskNames[i],
                    Description = "",
                    TaskType = task.TaskType,
                    Priority = task.Priority,
                    ParentId = task.Id,
                    Deadline = today.AddDays(i),
                    EstimatedHours = i == parts - 1
                        ? Math.Round(task.EstimatedHours - hoursPerPart * (parts - 1), 1)
                        : hoursPerPart,
                    IsChecked = false
                });
            }

            return result;
        }

        // ── Зсув дедлайнів після розбиття підзадачі ───────────────────────────
        // Коли частина з індексом splitIndex розбивається на subCount підчастин,
        // всі наступні (незавершені) частини зсуваються вперед на (subCount - 1) днів.
        public static void ShiftSiblingDeadlines(
            TaskItem parent,
            int      splitPartId,   // Id підзадачі яку розбиваємо
            int      subCount)      // на скільки підчастин розбиваємо
        {
            if (subCount <= 1) return;

            int shift = subCount - 1; // розбиваємо 1 частину → +N нових → зсуваємо на N-1

            // Беремо підзадачі, відсортовані по дедлайну
            var ordered = parent.SubTasks
                .OrderBy(s => s.Deadline ?? DateTime.MaxValue)
                .ToList();

            // Знаходимо індекс розбитої частини
            int splitIdx = ordered.FindIndex(s => s.Id == splitPartId);
            if (splitIdx < 0) return;

            // Зсуваємо всі наступні незавершені частини
            for (int i = splitIdx + 1; i < ordered.Count; i++)
            {
                var sibling = ordered[i];
                if (sibling.IsChecked) continue; // завершені не чіпаємо
                if (sibling.Deadline.HasValue)
                    sibling.Deadline = sibling.Deadline.Value.AddDays(shift);
            }
        }
    }
}
