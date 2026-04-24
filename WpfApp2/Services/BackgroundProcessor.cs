using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
            _interval = interval ?? TimeSpan.FromMinutes(1);
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
                        var tasksCopy = _taskManager.Tasks.ToList();

                        PlannerService.CalculatePriorities(tasksCopy);

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _taskManager.ApplyUpdatedPriorities(tasksCopy);
                        });
                    }
                    catch (Exception ex)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                            MessageBox.Show("Background error: " + ex.Message));
                    }

                    await Task.Delay(_interval, token);
                }
            }, token);
        }

        public void Stop()
        {
            _cts?.Cancel();
        }
    }
}