using System;
using System.ComponentModel;

namespace WpfApp1
{
    public class TaskItem : INotifyPropertyChanged
    {
        private string name;
        private string description;
        private DateTime? deadline;
        private string taskType;
        private int priority;
        private int progress;
        private bool isCompleted;

        public int Id { get; set; }

        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged("Name"); }
        }

        public string Description
        {
            get => description;
            set { description = value; OnPropertyChanged("Description"); }
        }

        public DateTime? Deadline
        {
            get => deadline;
            set { deadline = value; OnPropertyChanged("Deadline"); }
        }

        public string TaskType
        {
            get => taskType;
            set { taskType = value; OnPropertyChanged("TaskType"); }
        }

        public int Importance { get; private set; }

        public int Priority
        {
            get => priority;
            set
            {
                if (priority != value)
                {
                    priority = value;
                    OnPropertyChanged("Priority"); 
                }
            }
        }

        public int Progress
        {
            get => progress;
            set { progress = value; OnPropertyChanged("Progress"); }
        }

        public bool IsCompleted
        {
            get => Progress >= 100;
            set
            {
                if (value)
                    Progress = 100;  
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(Progress));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public void CalculateImportance()
        {
            switch (TaskType ?? "")
            {
                case "Exam":
                case "Diploma Project":
                    Importance = 10;
                    break;

                case "Test":
                case "Internship":
                    Importance = 9;
                    break;

                case "Course Project":
                case "Research Paper":
                    Importance = 8;
                    break;

                case "Laboratory Work":
                case "Thesis":
                    Importance = 7;
                    break;

                case "Presentation":
                case "Essay":
                    Importance = 6;
                    break;

                case "Independent Study":
                case "Homework":
                    Importance = 5;
                    break;

                case "Practical Class":
                case "Seminar":
                    Importance = 4;
                    break;

                case "Lecture":
                case "Quiz":
                    Importance = 2;
                    break;

                default:
                    Importance = 3;
                    break;
            }
        }

        public void CalculatePriority()
        {
            CalculateImportance();

            double urgency = 0;

            if (Deadline.HasValue)
            {
                double daysLeft = (Deadline.Value - DateTime.Now).TotalDays;
                urgency = 1 / (daysLeft + 1); 
            }

            double completionFactor = (100.0 - Progress) / 100.0;

            double result = 
                0.3 * urgency + 
                0.2 * Importance + 
                0.5 * completionFactor;

            Priority = (int)(result * 100);
        }
    }
}
