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
                .Sum(t => t.RemainingHours);
        }

        public static void CalculatePriorities(List<TaskItem> tasks)
        {
            foreach (var task in tasks)
            {
                double urgency = 0;

                if (task.Deadline.HasValue)
                {
                    double daysLeft = (task.Deadline.Value - DateTime.Now).TotalDays;

                    if (daysLeft <= 0)
                    {
                        urgency = 1.0 + Math.Abs(daysLeft);
                    }
                    else
                    {
                        urgency = 1.0 / (daysLeft + 1.0);
                    }
                }

                double completion = (100.0 - task.Progress) / 100.0;
                double importance = TaskTypeService.GetImportance(task.TaskType);

                task.Priority =
                    (int)(
                        0.3 * urgency +
                        0.2 * importance +
                        0.5 * completion ) * 100;
            }
        }
    }   
}

