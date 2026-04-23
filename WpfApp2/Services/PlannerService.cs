using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp2.Services
{
    public static class PlannerService
    {
        private const double DAILY_LIMIT = 6;

        public static (List<TaskItem> plan, double usedHours, bool isOverloaded)
            BuildDayPlan(List<TaskItem> tasks)
        {
            double used = 0;

            var sorted = tasks
                .Where(t => !t.IsCompleted)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.Deadline)
                .ToList();

            var result = new List<TaskItem>();

            foreach (var task in sorted)
            {
                if (used + task.RemainingHours > DAILY_LIMIT)
                    return (result, used, true);

                result.Add(task);
                used += task.RemainingHours;
            }

            return (result, used, false);
        }

        public static bool IsOverloaded(List<TaskItem> tasks)
        {
            var (_, _, isOverloaded) = BuildDayPlan(tasks);
            return isOverloaded;
        }

        public static void CalculatePriorities(List<TaskItem> tasks)
        {
            foreach (var task in tasks)
            {
                double urgency = 0;

                if (task.Deadline.HasValue)
                {
                    double daysLeft = (task.Deadline.Value - DateTime.Now).TotalDays;
                    urgency = 1.0 / (daysLeft + 1.0);
                }

                double completion = (100.0 - task.Progress) / 100.0;
                double importance = TaskTypeService.GetImportance(task.TaskType);

                task.Priority =
                    (int)(
                        0.3 * urgency +
                        0.2 * importance +
                        0.5 * completion
                    ) * 100;
            }
        }

        public static void RebuildPlan(List<TaskItem> tasks)
        {
            double used = 0;

            var sorted = tasks
                .Where(t => !t.IsCompleted)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.Deadline)
                .ToList();

            foreach (var task in sorted)
            {
                if (used + task.RemainingHours <= DAILY_LIMIT)
                {
                    used += task.RemainingHours;
                }
                else
                {
                    task.Deadline = (task.Deadline ?? DateTime.Now).AddDays(1);
                }
            }
        }
    }
}