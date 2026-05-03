using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfApp2.Services;

namespace WpfApp2.Services
{
    public class TaskManager
    {
        public ObservableCollection<TaskItem> Tasks { get; } = new();

        private readonly DatabaseService _db;

        public bool IsEditing { get; set; }

        // ← подія для підписників (CalendarView) про будь-яку зміну задач
        public event Action? TasksChanged;

        public TaskManager(DatabaseService db)
        {
            _db = db;
            AppLogger.Info("TaskManager", "Ініціалізовано");
        }

        public void Load()
        {
            AppLogger.Info("TaskManager", "Завантаження задач...");
            var tasks = _db.LoadTasks();
            Tasks.Clear();
            foreach (var t in tasks)
            {
                Subscribe(t);
                SubscribeToSubTasks(t);
                Tasks.Add(t);
            }
            Recalculate();
            AppLogger.Info("TaskManager", $"Завантажено {tasks.Count} задач");
        }

        public void AddTask(TaskItem task)
        {
            _db.AddTask(task);
            DataStorageService.LogChange("ADD", task, $"Дедлайн: {task.Deadline:yyyy-MM-dd}, Тип: {task.TaskType}");

            Application.Current.Dispatcher.Invoke(() =>
            {
                Tasks.Add(task);
                Subscribe(task);
                Recalculate();
                TasksChanged?.Invoke();
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
                // НЕ викликаємо CheckOverload тут — тільки при навігації на "Сьогодні"
                TasksChanged?.Invoke();
            });
        }

        private static bool IsToday(DateTime? deadline)
        {
            if (!deadline.HasValue) return false;
            var today = DateTime.Today;
            return deadline.Value >= today && deadline.Value < today.AddDays(1);
        }

        public void DeleteTask(TaskItem task)
        {
            AppLogger.Warning("TaskManager", $"Видалення задачі \"{task.Name}\" (#{task.Id})");
            DataStorageService.LogChange("DELETE", task, $"Видалено задачу з {task.SubTasks.Count} підзадачами");

            Application.Current.Dispatcher.Invoke(() =>
            {
                Tasks.Remove(task);
                TasksChanged?.Invoke();
            });

            Task.Run(() => _db.DeleteTask(task.Id));
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
            Task.Run(() => _db.DeleteSubTask(sub.Id));
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

                sub.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(TaskItem.IsChecked))
                    {
                        parent.RefreshParent();

                        if (!IsEditing)
                            Task.Run(() =>
                            {
                                try
                                {
                                    _db.UpdateTask(sub);
                                    _db.UpdateTask(parent);
                                    DataStorageService.LogChange("UPDATE", sub, "Зміна статусу підзадачі");
                                }
                                catch (Exception ex)
                                {
                                    AppLogger.Error("TaskManager", "Помилка оновлення підзадачі", ex);
                                }
                            });
                    }
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
                    if (e.PropertyName == nameof(TaskItem.IsChecked))
                    {
                        parent.RefreshParent();
                        if (!IsEditing)
                            Task.Run(() =>
                            {
                                try
                                {
                                    _db.UpdateTask(captured);
                                    _db.UpdateTask(parent);
                                }
                                catch (Exception ex)
                                {
                                    AppLogger.Error("TaskManager", "Помилка синхронізації підзадачі", ex);
                                }
                            });
                    }
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

                Task.Run(() =>
                {
                    try
                    {
                        _db.UpdateTask(task);
                        // Логуємо зміни властивостей
                        DataStorageService.LogChange("UPDATE", task,
                            $"Змінено поле: {e.PropertyName}");
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("TaskManager", $"Помилка збереження задачі #{task.Id}", ex);
                    }
                });
            };
        }
    }
}
