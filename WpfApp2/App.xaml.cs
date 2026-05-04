using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using WpfApp2.Services;

namespace WpfApp2
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppLogger.MinLevel = LogLevel.Debug;
            AppLogger.Info("App", $"=== Student Planner запущено | .NET {Environment.Version} ===");
            AppLogger.Info("App", $"Директорія: {AppDomain.CurrentDomain.BaseDirectory}");

            DataStorageService.EnsureDirectory();

            // Глобальні обробники помилок
            DispatcherUnhandledException += OnDispatcherException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainException;
            TaskScheduler.UnobservedTaskException += OnTaskException;

            AppLogger.Info("App", "Глобальні обробники помилок зареєстровано");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppLogger.Info("App", $"=== Student Planner завершено (код {e.ApplicationExitCode}) ===");
            base.OnExit(e);
        }

        // UI-потік
        private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs ex)
        {
            AppLogger.Fatal("App", "Необроблений виняток у UI-потоці", ex.Exception);
            Debug.WriteLine($"UI Exception: {ex.Exception.Message}\n{ex.Exception.StackTrace}");
            ex.Handled = true;
        }

        // Фоновий потік
        private void OnDomainException(object sender, UnhandledExceptionEventArgs ex)
        {
            var exception = ex.ExceptionObject as Exception;
            AppLogger.Fatal("App", $"Критичний виняток (IsTerminating={ex.IsTerminating})", exception);
            Debug.WriteLine($"Fatal Exception: {exception?.Message}");
        }

        // Task виняток
        private void OnTaskException(object? sender, UnobservedTaskExceptionEventArgs ex)
        {
            AppLogger.Error("App", "Необроблений виняток у Task", ex.Exception);
            Debug.WriteLine($"Task Exception: {ex.Exception.Message}");
            ex.SetObserved();
        }
    }
}
