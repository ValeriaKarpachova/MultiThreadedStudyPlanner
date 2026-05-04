using System;
using System.Collections.Generic;

namespace WpfApp2.Services
{
    public class ProductivityReport
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double AverageProgress { get; set; }
        public double TotalHours { get; set; }
        public double CompletedHours { get; set; }
        public double ProductivityPercent { get; set; }
    }

    public static class ProductivityService
    {
        public static ProductivityReport Analyze(List<TaskItem> tasks)
        {
            var now = DateTime.Now;

            var total     = tasks.Count;
            var completed = tasks.Count(t => t.IsCompleted);
            var overdue = tasks.Count(t => t.IsOverdue);

            var avgProgress   = total == 0 ? 0 : tasks.Average(t => t.Progress);
            var totalHours     = tasks.Sum(t => t.EstimatedHours);
            var completedHours = tasks.Sum(t => t.EstimatedHours * (t.Progress / 100.0));
            var productivity   = totalHours == 0 ? 0 : (completedHours / totalHours) * 100;

            return new ProductivityReport
            {
                TotalTasks          = total,
                CompletedTasks      = completed,
                OverdueTasks        = overdue,
                AverageProgress     = avgProgress,
                TotalHours          = totalHours,
                CompletedHours      = completedHours,
                ProductivityPercent = productivity
            };
        }
    }
}
