using System;

namespace WpfApp2.Services
{
    public static class Validator
    {
        public static bool ValidateTask(
            string name,
            string progressText,
            string estimatedText,
            out int progress,
            out double estimatedHours,
            out string error)
        {
            progress = 0;
            estimatedHours = 0;
            error = null;

            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Task name cannot be empty";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(progressText))
            {
                if (!int.TryParse(progressText, out progress))
                {
                    error = "Progress must be a number";
                    return false;
                }

                if (progress < 0 || progress > 100)
                {
                    error = "Progress must be between 0 and 100";
                    return false;
                }
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