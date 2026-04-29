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
            var tomorrow = today.AddDays(1);

            return tasks
                .Where(t =>
                    t.Deadline.HasValue &&
                    t.Deadline.Value >= today &&    
                    t.Deadline.Value < tomorrow &&    
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
    }
}