using System;
using System.Windows;
using System.Windows.Controls;
using WpfApp1.Services;
using static WpfApp1.TaskItem;

namespace WpfApp1
{
    public partial class AddTaskWindow : Window
    {
        public TaskItem NewTask { get; private set; }
        
        public AddTaskWindow(TaskItem task = null)
        {
            InitializeComponent();

            if (task != null)
            {
                NameBox.Text = task.Name;
                ProgressBox.Text = task.Progress.ToString();
                DeadlinePicker.SelectedDate = task.Deadline;

                NewTask = task; 
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (!Validator.ValidateTask(NameBox.Text, ProgressBox.Text, out int progress, out string error))
            {
                MessageBox.Show(error);
                return;
            }

            try
            {
                if (NewTask == null)
                    NewTask = new TaskItem();

                NewTask.Name = NameBox.Text;
                NewTask.Progress = progress;
                NewTask.Deadline = DeadlinePicker.SelectedDate;

                var selectedType = (TypeBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                NewTask.TaskType = selectedType == "Без типу" ? null : selectedType;

                NewTask.CalculateImportance();
                NewTask.CalculatePriority();

                DialogResult = true;
            }
            catch
            {
                MessageBox.Show("Invalid input");
            }
        }

    }
}
