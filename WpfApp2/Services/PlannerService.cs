using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp2.Services
{
    public static class PlannerService
    {
        public const double DailyLimit = 6.0;

        public static List<TaskItem> GetTodayTasks(List<TaskItem> tasks)
        {
            var today = DateTime.Today;

            return tasks
                .Where(t =>
                    t.Deadline.HasValue &&
                    t.Deadline.Value.Date == today &&
                    !t.IsCompleted)
                .ToList();
        }

        public static double GetTodayHours(List<TaskItem> tasks)
        {
            return GetTodayTasks(tasks)
                .Sum(t => t.EstimatedHours * (1 - t.Progress / 100.0));
        }

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

        public static List<TaskItem> SplitTask(TaskItem task)
        {
            var today = DateTime.Today;
            var deadline = task.Deadline!.Value.Date;
            int daysAvailable = (int)(deadline - today).TotalDays + 1; // включая день дедлайна

            if (daysAvailable <= 1)
                return new List<TaskItem>(); // нет смысла разбивать — до дедлайна 0-1 день

            // Сколько частей нужно (каждая часть <= DailyLimit часов)
            int parts = (int)Math.Ceiling(task.EstimatedHours / DailyLimit);
            parts = Math.Min(parts, daysAvailable); // не больше чем дней до дедлайна

            double hoursPerPart = Math.Round(task.EstimatedHours / parts, 1);
            var result = new List<TaskItem>();

            for (int i = 0; i < parts; i++)
            {
                result.Add(new TaskItem
                {
                    Name = $"{task.Name} (часть {i + 1}/{parts})",
                    Description = task.Description,
                    TaskType = task.TaskType,
                    Priority = task.Priority,
                    Deadline = today.AddDays(i),
                    EstimatedHours = i == parts - 1
                        ? Math.Round(task.EstimatedHours - hoursPerPart * (parts - 1), 1) // остаток
                        : hoursPerPart,
                    Progress = 0
                });
            }

            return result;
        }
    }
}