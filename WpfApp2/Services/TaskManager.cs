using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace WpfApp2.Services
{
    public class TaskManager
    {
        public ObservableCollection<TaskItem> Tasks { get; } = new();

        private readonly DatabaseService _db;

        public bool IsEditing { get; set; }

        public TaskManager(DatabaseService db)
        {
            _db = db;
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

            Recalculate();
        }

        public void AddTask(TaskItem task)
        {
            System.Diagnostics.Debug.WriteLine($"AddTask: {task.Name}, deadline={task.Deadline}, hours={task.EstimatedHours}");
            _db.AddTask(task);

            Application.Current.Dispatcher.Invoke(() =>
            {
                Tasks.Add(task);
                Subscribe(task);
                Recalculate();
                CheckOverload();
            });
        }

        public void UpdateTask(TaskItem task)
        {
            _db.UpdateTask(task);

            Application.Current.Dispatcher.Invoke(() =>
            {
                Recalculate();
                CheckOverload();
            });
        }

        public void DeleteTask(TaskItem task)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Tasks.Remove(task);
            });

            Task.Run(() => _db.DeleteTask(task.Id));
            Recalculate();
        }


        public void Recalculate()
        {
            var snapshot = Tasks.ToList();
            PlannerService.CalculatePriorities(snapshot);
            ApplyUpdatedPriorities(snapshot);
        }
        private void CheckOverload()
        {
            double hours = PlannerService.GetTodayHours(Tasks.ToList());

            // Временно — убедимся что метод вызывается и hours считается правильно
            System.Diagnostics.Debug.WriteLine($"CheckOverload: hours={hours}, limit={PlannerService.DailyLimit}");

            if (hours > PlannerService.DailyLimit)
            {
                System.Diagnostics.Debug.WriteLine("Showing OverloadDialog...");
                var dialog = new Views.OverloadDialog(Tasks.ToList(), hours, this);
                var result = dialog.ShowDialog();
                System.Diagnostics.Debug.WriteLine($"Dialog closed, result={result}");
            }
        }

        public void AddTaskSilent(TaskItem task)
        {
            _db.AddTask(task);

            Application.Current.Dispatcher.Invoke(() =>
            {
                Tasks.Add(task);
            });

            Subscribe(task);
            Recalculate();
        }

        private void ApplyUpdatedPriorities(List<TaskItem> updated)
        {
            foreach (var u in updated)
            {
                var existing = Tasks.FirstOrDefault(x => x.Id == u.Id);
                if (existing != null)
                    existing.Priority = u.Priority;
            }
        }

        private void Subscribe(TaskItem task)
        {
            task.PropertyChanged += (s, e) =>
            {
                if (!IsEditing)
                    Task.Run(() =>
                    {
                        try
                        {
                            _db.UpdateTask(task);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Subscribe UpdateTask error: {ex.Message}");
                        }
                    });
            };
        }

        public partial class App : Application
        {
            protected override void OnStartup(StartupEventArgs e)
            {
                base.OnStartup(e);

                DispatcherUnhandledException += (s, ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"UI Exception: {ex.Exception.Message}\n{ex.Exception.StackTrace}");
                    ex.Handled = true;
                };

                AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Fatal Exception: {(ex.ExceptionObject as Exception)?.Message}");
                };

                TaskScheduler.UnobservedTaskException += (s, ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Task Exception: {ex.Exception.Message}");
                    ex.SetObserved();
                };
            }
        }
    }
}