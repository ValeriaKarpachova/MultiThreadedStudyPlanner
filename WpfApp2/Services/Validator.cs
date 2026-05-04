using System;

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
    }
}