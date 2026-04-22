using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Services
{
    public static class Validator
    {
        public static bool ValidateTask(string name, string progressText, out int progress, out string error)
        {
            error = null;
            progress = 0;

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
            return true;

        }
    }
}
