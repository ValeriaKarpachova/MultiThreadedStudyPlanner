using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using WpfApp2.Views;
using WpfApp2;
using WpfApp2.Services;

namespace WpfApp2.Services
{
    public class BackgroundProcessor
    {
        private readonly TaskManager _taskManager;
        private CancellationTokenSource _cts;
        private readonly TimeSpan _interval;

        public BackgroundProcessor(TaskManager taskManager, TimeSpan? interval = null)
        {
            _taskManager = taskManager;
            _interval = interval ?? TimeSpan.FromMinutes(1); // Проверка каждые 1 минуту по умолчанию
        }

        // Запуск фонового процесса
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
                        CheckDeadlinesAndPriorities();
                    }
                    catch (Exception ex)
                    {
                        // Логирование ошибок фонового процесса
                        Application.Current.Dispatcher.Invoke(() =>
                            MessageBox.Show("BackgroundProcessor error: " + ex.Message));
                    }

                    await Task.Delay(_interval, token);
                }
            }, token);
        }

        // Остановка фонового процесса
        public void Stop()
        {
            _cts?.Cancel();
        }

        // Проверка дедлайнов и пересчет приоритетов
        private void CheckDeadlinesAndPriorities()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var task in _taskManager.Tasks)
                {
                    task.CalculatePriority();
                }

                var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                mainWindow?.RefreshTasksView();
            });
        }
    }
}
