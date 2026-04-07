using System;
using System.Windows;
using System.Windows.Controls;
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
            try
            {
                if (NewTask == null)
                    NewTask = new TaskItem();

                NewTask.Name = NameBox.Text;
                NewTask.Progress = int.Parse(ProgressBox.Text);
                NewTask.Deadline = DeadlinePicker.SelectedDate ?? DateTime.Now;

                var selectedType = (TypeBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                NewTask.TaskType = selectedType ?? "Lecture"; 

                NewTask.CalculateImportance();

                DialogResult = true;
            }
            catch
            {
                MessageBox.Show("Invalid input");
            }
        }

    }
}
