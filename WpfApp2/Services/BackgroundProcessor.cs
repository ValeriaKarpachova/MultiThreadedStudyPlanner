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
                            _manager.Recalculate();
                        });
                    }
                    catch (OperationCanceledException) { break; } // ← нормальное завершение
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"BackgroundProcessor error: {ex.Message}");
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), token);
                    }
                    catch (OperationCanceledException) { break; }
                }
            }, token);
        }

        public void Stop()
        {
            _cts?.Cancel();
        }
    }
}