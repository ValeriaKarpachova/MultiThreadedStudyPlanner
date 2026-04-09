using System.Collections.ObjectModel;
using WpfApp1.Services;

namespace WpfApp1
{
    public class TaskManager
    {
        public ObservableCollection<TaskItem> Tasks { get; set; } = new ObservableCollection<TaskItem>();
        private readonly DatabaseService _db;

        public TaskManager(DatabaseService db)
        {
            _db = db;
        }

        public void Load()
        {
            var tasks = _db.LoadTasks();

            Tasks.Clear();

            foreach (var t in tasks)
                Tasks.Add(t);
        }

        public void AddTask(TaskItem task)
        {
            Tasks.Add(task);
            _db.AddTask(task);
        }

        public void DeleteTask(TaskItem task)
        {
            Tasks.Remove(task);
            _db.DeleteTask(task.Id);
        }

        public void UpdateTask(TaskItem task)
        {
            _db.UpdateTask(task);
        }
    }
}
