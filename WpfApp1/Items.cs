using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class TaskItem : INotifyPropertyChanged
    {
        private string name;
        private string description;
        private DateTime deadline;
        private string taskType;
        private int priority;
        private int progress;
        private bool isSelected;

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

        public DateTime Deadline
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

        public bool IsSelected
        {
            get => isSelected;
            set { isSelected = value; OnPropertyChanged("IsSelected"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public void CalculateImportance()
        {
            switch (TaskType)
            {
                case "Exam":
                case "Diploma Project":
                    Importance = 10;
                    break;

                case "Test":
                    Importance = 9;
                    break;

                case "Thesis":
                case "Internship":
                    Importance = 9;
                    break;

                case "Course Project":
                case "Research Paper":
                    Importance = 8;
                    break;

                case "Laboratory Work":
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
                    Importance = 5;
                    break;
            }
        }

        public void CalculatePriority()
        {
            CalculateImportance();

            double daysLeft = (Deadline - DateTime.Now).TotalDays;

            double urgency;

            if (daysLeft >= 0)
            {
                //(экспоненциальное затухание)
                urgency = Math.Exp(-0.1 * daysLeft);
            }
            else
            {
                double overdueDays = Math.Abs(daysLeft);

                urgency = 1 + Math.Log(overdueDays + 1);
            }

            double completionFactor = (100.0 - Progress) / 100.0;
            double importance = Importance / 10.0;

            double result =
                0.5 * urgency +          
                0.4 * completionFactor + 
                0.1 * importance; 

            Priority = (int)(result * 100);
        }
    }
}
