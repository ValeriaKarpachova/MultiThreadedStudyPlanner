using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace WpfApp2.Services
{
    public static class OverloadService
    {
        private static bool _isShown = false;
        public static void CheckAndNotify(List<TaskItem> tasks, bool isEditing)
        {
            double todayHours = PlannerService.GetTodayHours(tasks);

            if (todayHours > PlannerService.DailyLimit)
            {
                if (!isEditing && !_isShown)
                {
                    _isShown = true;

                    MessageBox.Show(
                        $"Перегрузка дня!\n" +
                        $"Запланировано: {todayHours:F1} ч.\n" +
                        $"Допустимо: {PlannerService.DailyLimit} ч.");
                }
            }
            else
            {
                _isShown = false;
            }
        }
    }
}