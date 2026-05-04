using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp2.Services
{
    public class BackgroundProcessor
    {
        private readonly TaskManager _manager;
        private CancellationTokenSource _cts;

        public BackgroundProcessor(TaskManager manager)
        {
            _manager = manager;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            MoveOverdueTasks();
                            _manager.Recalculate();
                        });
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"BackgroundProcessor error: {ex.Message}");
                    }

                    try { await Task.Delay(TimeSpan.FromMinutes(1), token); }
                    catch (OperationCanceledException) { break; }
                }
            }, token);
        }

        private void MoveOverdueTasks()
        {
            var tomorrow = DateTime.Today.AddDays(1);

            foreach (var task in _manager.Tasks.ToList())
            {
                if (!task.Deadline.HasValue) continue;
                if (task.IsCompleted) continue;

                // Перевіряємо точний момент дедлайну (дата + час якщо є)
                // Якщо час не вказано — вважаємо кінець дня (23:59:59),
                // тобто задача "на сьогодні без часу" не переноситься до кінця дня
                if (!task.DeadlineDateTime.HasValue) continue;
                if (task.DeadlineDateTime.Value >= DateTime.Now) continue;

                // Переносимо тільки ті що вже стали просроченими
                if (task.ParentId.HasValue)
                {
                    task.Deadline = tomorrow;
                    _manager.UpdateTask(task);
                    continue;
                }

                if (!task.IsSplit)
                {
                    task.Deadline = tomorrow;
                    _manager.UpdateTask(task);
                }
            }
        }

        public void Stop() => _cts?.Cancel();
    }
}
