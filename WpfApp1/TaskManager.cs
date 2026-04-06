using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class TaskManager
    {
        public ObservableCollection<TaskItem> Tasks { get; set; } = new ObservableCollection<TaskItem>();

        public void AddTask(TaskItem task)
        {
            Tasks.Add(task);
        }

        public void DeleteTask(TaskItem task)
        {
            Tasks.Remove(task);
        }

        public void UpdateTask(TaskItem task, string newName)
        {
            task.Name = newName;
        }
    }
}
