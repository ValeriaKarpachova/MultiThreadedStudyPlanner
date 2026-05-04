using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WpfApp2.Services;

namespace WpfApp2.Views
{
    public partial class MainWindow : Window
    {
        private readonly TaskManager manager;
        private readonly BackgroundProcessor _backgroundProcessor;
        private readonly DispatcherTimer _workdayTimer = new() { Interval = TimeSpan.FromMinutes(1) };
        private DateTime _workdayStart;
        private Button? _activeNav;

        public static TrayService? Tray { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            var db = new DatabaseService();
            db.InitializeDatabase();

            manager = new TaskManager(db);
            manager.Load();

            _backgroundProcessor = new BackgroundProcessor(manager);
            _backgroundProcessor.Start();

            Navigate(new TasksView(manager, TaskViewMode.Home), BtnHome, "Головна");

            Tray = new TrayService();
            Tray.OpenRequested += () =>
            {
                Show();
                WindowState = WindowState.Maximized;
                Activate();
            };

            _workdayStart = DateTime.Now;
            _workdayTimer.Tick += WorkdayTimer_Tick;
            _workdayTimer.Start();
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddTaskWindow();
            if (window.ShowDialog() == true)
                manager.AddTask(window.NewTask);
        }

        private void Home_Click(object s, RoutedEventArgs e) =>
            Navigate(new TasksView(manager, TaskViewMode.Home), BtnHome, "Головна");

        private void Today_Click(object s, RoutedEventArgs e)
        {
            CheckTodayOverloadWarning();
            Navigate(new TasksView(manager, TaskViewMode.Today), BtnToday, "Сьогодні");
        }

        private void Completed_Click(object s, RoutedEventArgs e) =>
            Navigate(new TasksView(manager, TaskViewMode.Completed), BtnCompleted, "Виконані");

        private void Subjects_Click(object s, RoutedEventArgs e) =>
            Navigate(new SubjectsView(manager), BtnSubjects, "Предмети");

        private void Calendar_Click(object s, RoutedEventArgs e) =>
            Navigate(new CalendarView(manager), BtnCalendar, "Календар");

        private void Statistics_Click(object s, RoutedEventArgs e) =>
            Navigate(new StatisticsView(manager), BtnStatistics, "Статистика");

        private void CheckTodayOverloadWarning()
        {
            var today = DateTime.Today;
            var tasks = manager.Tasks.ToList();

            double totalHours = 0;
            var todayTasks = new System.Collections.Generic.List<TaskItem>();

            foreach (var t in tasks)
            {
                if (t.IsCompleted) continue;

                if (t.IsSplit)
                {
                    var subs = t.SubTasks
                        .Where(s => s.Deadline.HasValue &&
                                    s.Deadline.Value.Date == today &&
                                    !s.IsChecked)
                        .ToList();
                    if (subs.Any())
                    {
                        totalHours += subs.Sum(s => s.EstimatedHours);
                        todayTasks.Add(t);
                    }
                }
                else
                {
                    if (t.Deadline.HasValue && t.Deadline.Value.Date == today)
                    {
                        totalHours += t.EstimatedHours * (1 - t.Progress / 100.0);
                        todayTasks.Add(t);
                    }
                }
            }

            if (todayTasks.Any() && totalHours > PlannerService.DailyLimit)
            {
                var dialog = new OverloadDialog(todayTasks, totalHours, manager)
                {
                    Owner = this
                };
                dialog.ShowDialog();
            }
        }

        public enum TaskViewMode { Home, Today, Completed }

        private void WorkdayTimer_Tick(object? s, EventArgs e)
        {
            var elapsed = DateTime.Now - _workdayStart;
            if (elapsed.TotalHours >= 6 && (int)elapsed.TotalMinutes % 30 == 0)
            {
                Tray?.Notify("⚠️ Перевантаження",
                    $"Ви працюєте вже {(int)elapsed.TotalHours} год. Зробіть тривалу перерву!",
                    System.Windows.Forms.ToolTipIcon.Warning);
            }
        }

        private void Navigate(object content, Button btn, string title = "")
        {
            MainContent.Content = content;
            if (_activeNav != null) _activeNav.Tag = null;
            _activeNav = btn;
            btn.Tag = "active";
            if (PageTitle != null) PageTitle.Text = title;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
