using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public TaskManager manager = new TaskManager();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            AddTaskWindow addWindow = new AddTaskWindow();

            if (addWindow.ShowDialog() == true)
            {
                TaskItem newTask = new TaskItem
                {
                    Id = manager.Tasks.Count + 1,
                    Name = addWindow.TaskName,
                    Description = addWindow.TaskDescription,
                    Deadline = addWindow.TaskDeadline,
                    Priority = addWindow.TaskPriority,
                    Progress = 0,
                    Status = "Not Started"
                };

                manager.AddTask(newTask);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button).DataContext as TaskItem;
            manager.DeleteTask(task);
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button).DataContext as TaskItem;
            manager.UpdateTask(task, "Edited task");
        }
    }
}
