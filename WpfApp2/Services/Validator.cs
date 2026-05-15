using System;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApp2.Services
{
    public static class Validator
    {
        public static bool ValidateTask(
            string name,
            string estimatedText,
            out double estimatedHours,
            out string error)
        {
            estimatedHours = 0;
            error = null;

            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Назва завдання не може бути порожньою";
                return false;
            }

            if (string.IsNullOrWhiteSpace(estimatedText))
            {
                estimatedHours = 1;
                return true;
            }

            if (!double.TryParse(
                    estimatedText,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out estimatedHours))
            {
                error = "Орієнтовна кількість годин має бути числом (наприклад: 2 або 1,5)";
                return false;
            }

            if (estimatedHours <= 0)
            {
                error = "Орієнтовна кількість годин має бути більшою за 0";
                return false;
            }

            if (estimatedHours > 200)
            {
                error = "Очікуваний час роботи завеликий";
                return false;
            }

            return true;
        }

        public static bool ValidateSubject(string name, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Введіть назву предмету";
                return false;
            }

            return true;
        }

        public static bool ValidatePartDeletion(int totalParts, out string error)
        {
            error = null;

            if (totalParts <= 1)
            {
                error = "Не можна видалити останню частину.\nВидаліть усе завдання цілком.";
                return false;
            }

            return true;
        }

        public static bool ValidateTaskSelected(object selectedItem, out string error)
        {
            error = null;

            if (selectedItem == null)
            {
                error = "Оберіть завдання для розбиття";
                return false;
            }

            return true;
        }

        public static Task SafeRunAsync(Func<Task> action, string logCategory, string logMessage)
        {
            return Task.Run(async () =>
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    AppLogger.Error(logCategory, logMessage, ex);
                }
            });
        }
        public static Task SafeRunAsync(Action action, string logCategory, string logMessage)
        {
            return Task.Run(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    AppLogger.Error(logCategory, logMessage, ex);
                }
            });
        }

        public static ImageSource TryLoadImage(string fileName)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/Images/{fileName}");
                return BitmapFrame.Create(uri);
            }
            catch (Exception ex)
            {
                AppLogger.Warning("Validator", $"Не вдалося завантажити зображення '{fileName}': {ex.Message}");
                return null;
            }
        }
    }
}