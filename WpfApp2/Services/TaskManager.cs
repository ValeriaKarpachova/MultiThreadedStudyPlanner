using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using WpfApp2.Services;

namespace WpfApp2.Services
{
    public class TaskManager
    {
        public ObservableCollection<TaskItem> Tasks { get; } = new();

        private readonly DatabaseService _db;

        private SubjectService? _subjectSvc;
        private SubjectService SubjectSvc => _subjectSvc ??= new SubjectService();

        private static readonly string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tasks.db");

        public bool IsEditing { get; set; }
        public event Action? TasksChanged;

        public TaskManager(DatabaseService db)
        {
            _db = db;
            AppLogger.Info("TaskManager", "Ініціалізовано");
        }

        public void Load()
        {
            AppLogger.Info("TaskManager", "Завантаження задач...");

            List<TaskItem> tasks;

            if (File.Exists(DbPath))
            {
                _db.InitializeDatabase();
                tasks = _db.LoadTasks();
                var allForSpd = tasks.Concat(tasks.SelectMany(t => t.SubTasks)).ToList();
                Task.Run(() => DataStorageService.Save(allForSpd));
            }
            else
            {
                AppLogger.Warning("TaskManager", "tasks.db не знайдено — спроба завантажити з SPD резервної копії");
                tasks = DataStorageService.Load();

                if (tasks.Count > 0)
                {
                    AppLogger.Info("TaskManager", $"Знайдено {tasks.Count} задач у SPD, відновлення БД...");

                    _db.InitializeDatabase();

                    var subjects = DataStorageService.LoadSubjects();
                    foreach (var s in subjects)
                        SubjectSvc.Restore(s);

                    AppLogger.Info("TaskManager", $"Відновлено {subjects.Count} предметів з SPD");

                    foreach (var t in tasks)
                    {
                        _db.AddTask(t);
                        foreach (var sub in t.SubTasks)
                        {
                            sub.ParentId = t.Id;
                            _db.AddTask(sub);
                        }
                    }
                    AppLogger.Info("TaskManager", "БД успішно відновлено з SPD");
                }
                else
                {
                    AppLogger.Warning("TaskManager", "SPD також порожній або відсутній — починаємо з нуля");
                    _db.InitializeDatabase();
                }
            }

            Tasks.Clear();
            foreach (var t in tasks)
            {
                Subscribe(t);
                SubscribeToSubTasks(t);
                Tasks.Add(t);
            }
            Recalculate();
            RefreshSubjectColors();

            AppLogger.Info("TaskManager", $"Завантажено {tasks.Count} задач");
        }

        public void AddTask(TaskItem task)
        {
            _db.AddTask(task);
            DataStorageService.LogChange("ADD", task, $"Дедлайн: {task.Deadline:yyyy-MM-dd}, Тип: {task.TaskType}");

            Application.Current.Dispatcher.Invoke(() =>
            {
                Tasks.Add(task);
                RefreshSubjectColors();
                Subscribe(task);
                Recalculate();
                TasksChanged?.Invoke();
                SaveToSpd();
            });

            AppLogger.Info("TaskManager", $"Додано задачу \"{task.Name}\" (#{task.Id})");
        }

        public void UpdateTask(TaskItem task)
        {
            _db.UpdateTask(task);
            DataStorageService.LogChange("UPDATE", task, $"Дедлайн: {task.Deadline:yyyy-MM-dd}, Прогрес: {task.Progress}%");

            Application.Current.Dispatcher.Invoke(() =>
            {
                Recalculate();
                RefreshSubjectColors();
                TasksChanged?.Invoke();
                SaveToSpd();
            });
        }

        public void DeleteTask(TaskItem task)
        {
            AppLogger.Warning("TaskManager", $"Видалення задачі \"{task.Name}\" (#{task.Id})");
            DataStorageService.LogChange("DELETE", task, $"Видалено задачу з {task.SubTasks.Count} підзадачами");

            Application.Current.Dispatcher.Invoke(() =>
            {
                Tasks.Remove(task);
                TasksChanged?.Invoke();
                SaveToSpd(); 
            });

            Validator.SafeRunAsync(
                () => _db.DeleteTask(task.Id),
                "TaskManager", $"Помилка видалення задачі #{task.Id}");

            Recalculate();
        }

        private bool _isRecalculating = false;

        public void Recalculate()
        {
            _isRecalculating = true;
            try
            {
                var snapshot = Tasks.ToList();
                PlannerService.CalculatePriorities(snapshot);
                ApplyUpdatedPriorities(snapshot);
            }
            finally { _isRecalculating = false; }
        }

        public void DeleteSubTask(TaskItem sub)
        {
            AppLogger.Debug("TaskManager", $"Видалення підзадачі \"{sub.Name}\" (#{sub.Id})");
            DataStorageService.LogChange("DELETE", sub, "Видалено підзадачу");

            Validator.SafeRunAsync(
                () => _db.DeleteSubTask(sub.Id),
                "TaskManager", $"Помилка видалення підзадачі #{sub.Id}");
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

        public void AddSubTask(TaskItem parent, TaskItem sub)
        {
            sub.ParentId = parent.Id;
            _db.AddTask(sub);
            DataStorageService.LogChange("ADD", sub, $"Підзадача для #{parent.Id} \"{parent.Name}\"");

            Application.Current.Dispatcher.Invoke(() =>
            {
                parent.SubTasks.Add(sub);
                Subscribe(sub);
                SaveToSpd(); 

                sub.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName != nameof(TaskItem.IsChecked)) return;

                    parent.RefreshParent();

                    if (IsEditing) return;

                    Validator.SafeRunAsync(() =>
                    {
                        _db.UpdateTask(sub);
                        _db.UpdateTask(parent);
                        DataStorageService.LogChange("UPDATE", sub, "Зміна статусу підзадачі");
                    }, "TaskManager", "Помилка оновлення підзадачі");

                    SaveToSpd();
                };
            });
        }

        public void SubscribeToSubTasks(TaskItem parent)
        {
            foreach (var sub in parent.SubTasks)
            {
                var captured = sub;
                captured.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName != nameof(TaskItem.IsChecked)) return;

                    parent.RefreshParent();

                    if (IsEditing) return;

                    Validator.SafeRunAsync(() =>
                    {
                        _db.UpdateTask(captured);
                        _db.UpdateTask(parent);
                    }, "TaskManager", "Помилка синхронізації підзадачі");

                    SaveToSpd();
                };
            }
        }

        private void Subscribe(TaskItem task)
        {
            task.PropertyChanged += (s, e) =>
            {
                if (IsEditing || _isRecalculating) return;

                if (e.PropertyName == nameof(TaskItem.Deadline))
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                        TasksChanged?.Invoke());
                }

                Validator.SafeRunAsync(() =>
                {
                    _db.UpdateTask(task);
                    DataStorageService.LogChange("UPDATE", task, $"Змінено поле: {e.PropertyName}");
                }, "TaskManager", $"Помилка збереження задачі #{task.Id}");

                SaveToSpd();
            };
        }

        public void RefreshSubjectColors()
        {
            var subjects = SubjectSvc.GetAll();
            var map = subjects.ToDictionary(s => s.Id, s => s.Color);
            foreach (var t in Tasks)
            {
                t.SubjectColor = (t.SubjectId.HasValue && map.TryGetValue(t.SubjectId.Value, out var c))
                    ? c : "Transparent";
                foreach (var sub in t.SubTasks)
                    sub.SubjectColor = t.SubjectColor;
            }
        }

        private void SaveToSpd()
        {
            var all = Tasks.ToList();
            var subjects = SubjectSvc.GetAll();
            Task.Run(() => {
                DataStorageService.Save(all);
                DataStorageService.SaveSubjects(subjects);
            });
        }
    }
}