using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace WpfApp2.Services
{
    public static class OverloadService
    {
        private const double DAILY_LIMIT = 6;

        private static bool _toastShown;

        public static void CheckAndNotify(List<TaskItem> tasks, bool isEditing = false)
        {
            if (isEditing)
                return;

            var result = PlannerService.BuildDayPlan(tasks);

            if (!result.isOverloaded)
                return;

            var answer = MessageBox.Show(
                $"Planned: {result.usedHours:F1}h (limit 6)\nRebuild plan?",
                "Overload warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (answer == MessageBoxResult.Yes)
            {
                PlannerService.RebuildPlan(tasks);
            }
        }
    }
}