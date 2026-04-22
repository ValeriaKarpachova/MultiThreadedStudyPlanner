using System.Collections.ObjectModel;
using System.Windows.Controls;
using WpfApp2.Services;

namespace WpfApp2
{
    public class TaskManager
    {
        public ObservableCollection<TaskItem> Tasks { get; set; } = new ObservableCollection<TaskItem>();
        private readonly DatabaseService _db;

        public TaskManager(DatabaseService db)
        {
            _db = db;
        }

        private void Subscribe(TaskItem task)
        {
            task.PropertyChanged += (s, e) =>
            {
                _db.UpdateTask(task);
            };
        }

        public void Load()
        {
            var tasks = _db.LoadTasks();

            Tasks.Clear();

            foreach (var t in tasks)
            {
                Subscribe(t);
                Tasks.Add(t);
            }
        }

        public void AddTask(TaskItem task)
        {
            _db.AddTask(task);
            Subscribe(task);
            Tasks.Add(task);
        }

        public void DeleteTask(TaskItem task)
        {
            Tasks.Remove(task);
            _db.DeleteTask(task.Id);
        }
    }
}
