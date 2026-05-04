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
            var result = new List<TaskItem>();

            foreach (var t in tasks)
            {
                if (t.IsCompleted) continue;

                if (t.IsSplit)
                {
                    var todaySubs = t.SubTasks
                        .Where(s => s.Deadline.HasValue &&
                                    s.Deadline.Value.Date == today &&
                                    !s.IsChecked)
                        .ToList();
                    if (todaySubs.Any()) result.Add(t);
                }
                else
                {
                    if (t.Deadline.HasValue && t.Deadline.Value.Date == today)
                        result.Add(t);
                }
            }
            return result;
        }

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

        public static void CalculatePriorities(List<TaskItem> tasks)
        {
            var now = DateTime.Now;

            foreach (var task in tasks)
            {
                double urgency = 0;

                if (task.DeadlineDateTime.HasValue)
                {
                    double hoursLeft = (task.DeadlineDateTime.Value - now).TotalHours;

                    if (hoursLeft <= 0)
                    {
                        urgency = 100.0 + Math.Abs(hoursLeft) * 2;
                    }
                    else
                    {
                        // Ключова формула: urgency обернено пропорційна годинам
                        // до дедлайну. Чим менше годин — тим вищий urgency.
                        // Множник 24 нормалізує: 24 год → urgency≈50, 1 год → urgency≈96
                        urgency = 100.0 / (1.0 + hoursLeft / 24.0);
                    }
                }

                double importance  = TaskTypeService.GetImportance(task.TaskType);
                double completion  = (100.0 - task.Progress) / 100.0;

                task.Priority = (int)(
                    urgency      * 0.5   // 50% — терміновість (з урахуванням часу)
                    + importance * 5.0   // вага типу завдання
                    + completion * 15.0  // незавершеність
                );
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
                    Name           = subTaskNames[i],
                    Description    = "",
                    TaskType       = task.TaskType,
                    Priority       = task.Priority,
                    ParentId       = task.Id,
                    Deadline       = today.AddDays(i),
                    EstimatedHours = i == parts - 1
                        ? Math.Round(task.EstimatedHours - hoursPerPart * (parts - 1), 1)
                        : hoursPerPart,
                    IsChecked = false
                });
            }

            return result;
        }

        public static void ShiftSiblingDeadlines(
            TaskItem parent,
            int      splitPartId,
            int      subCount)
        {
            if (subCount <= 1) return;

            int shift = subCount - 1;

            var ordered = parent.SubTasks
                .OrderBy(s => s.Deadline ?? DateTime.MaxValue)
                .ToList();

            int splitIdx = ordered.FindIndex(s => s.Id == splitPartId);
            if (splitIdx < 0) return;

            for (int i = splitIdx + 1; i < ordered.Count; i++)
            {
                var sibling = ordered[i];
                if (sibling.IsChecked) continue;
                if (sibling.Deadline.HasValue)
                    sibling.Deadline = sibling.Deadline.Value.AddDays(shift);
            }
        }
    }
}
