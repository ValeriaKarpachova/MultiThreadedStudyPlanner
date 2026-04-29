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
                error = "Task name cannot be empty";
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
                error = "Estimated hours must be a number (example: 2 or 1.5)";
                return false;
            }

            if (estimatedHours <= 0)
            {
                error = "Estimated hours must be greater than 0";
                return false;
            }

            if (estimatedHours > 200)
            {
                error = "Estimated hours seems too large";
                return false;
            }

            return true;
        }
    }
}