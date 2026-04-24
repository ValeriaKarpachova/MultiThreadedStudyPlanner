using System.Collections.ObjectModel;
using System.Linq;
using WpfApp2.Services;
using System.Windows;

namespace WpfApp2.Services
{
    public class TaskManager
    {
        public ObservableCollection<TaskItem> Tasks { get; set; } = new();

        private readonly DatabaseService _db;

        public bool IsEditing { get; set; }

        public TaskManager(DatabaseService db)
        {
            _db = db;
        }

        private void Subscribe(TaskItem task)
        {
            task.PropertyChanged += (s, e) =>
            {
                _db.UpdateTask(task);

                if (!IsEditing &&
                    (e.PropertyName == nameof(TaskItem.Deadline) ||
                     e.PropertyName == nameof(TaskItem.EstimatedHours) ||
                     e.PropertyName == nameof(TaskItem.Progress)))
                {
                    CheckOverload();
                }
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

            Application.Current.Dispatcher.Invoke(() =>
            {
                Tasks.Add(task);
            });

            CheckOverload();
        }

        public void DeleteTask(TaskItem task)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Tasks.Remove(task);
            });

            _db.DeleteTask(task.Id);
        }

        public void UpdateTask(TaskItem task)
        {
            _db.UpdateTask(task);

            if (!IsEditing)
                CheckOverload();
        }

        public void ApplyUpdatedPriorities(List<TaskItem> updated)
        {
            foreach (var updatedTask in updated)
            {
                var existing = Tasks.FirstOrDefault(t => t.Id == updatedTask.Id);
                if (existing != null)
                {
                    existing.Priority = updatedTask.Priority;
                }
            }
        }

        private void CheckOverload()
        {
            OverloadService.CheckAndNotify(Tasks.ToList(), IsEditing);
        }
    }
}