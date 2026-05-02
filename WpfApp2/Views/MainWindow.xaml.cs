using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        private bool _darkTheme = false;
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

            Navigate(new TasksView(manager, TaskViewMode.Home), BtnHome);

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
            {
                manager.AddTask(window.NewTask);
            }
        }

        private void Home_Click(object s, RoutedEventArgs e) =>
            Navigate(new TasksView(manager, TaskViewMode.Home), BtnHome);
        private void Today_Click(object s, RoutedEventArgs e) =>
            Navigate(new TasksView(manager, TaskViewMode.Today), BtnToday);
        private void Completed_Click(object s, RoutedEventArgs e) =>
            Navigate(new TasksView(manager, TaskViewMode.Completed), BtnCompleted);
        private void Subjects_Click(object s, RoutedEventArgs e) =>
            Navigate(new SubjectsView(manager), BtnSubjects);
        private void Calendar_Click(object s, RoutedEventArgs e) =>
            Navigate(new CalendarView(manager), BtnCalendar);
        private void Statistics_Click(object s, RoutedEventArgs e) =>
            Navigate(new StatisticsView(manager), BtnStatistics);


        public enum TaskViewMode
        {
            Home,
            Today,
            Completed
        }

        private void WorkdayTimer_Tick(object? s, EventArgs e)
        {
            var elapsed = DateTime.Now - _workdayStart;
            if (elapsed.TotalHours >= 6 &&
                (int)elapsed.TotalMinutes % 30 == 0) // нагадувати кожні 30хв після 6год
            {
                Tray?.Notify("⚠️ Перевантаження",
                    $"Ви працюєте вже {(int)elapsed.TotalHours} год. Зробіть тривалу перерву!",
                    System.Windows.Forms.ToolTipIcon.Warning);
            }
        }

        private void Theme_Click(object s, RoutedEventArgs e)
        {
            _darkTheme = !_darkTheme;
            BtnTheme.Content = _darkTheme ? "☀️  Світла тема" : "🌙  Темна тема";
            var bg = _darkTheme ? "#1E1E2E" : "#FFFFFF";
            var fg = _darkTheme ? "#CDD6F4" : "#000000";
            var col = _darkTheme ? MaterialDesignThemes.Wpf.BaseTheme.Dark
                                 : MaterialDesignThemes.Wpf.BaseTheme.Light;

            var paletteHelper = new MaterialDesignThemes.Wpf.PaletteHelper();
            var theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(col);
            paletteHelper.SetTheme(theme);
        }

        private void Navigate(object content, Button btn)
        {
            MainContent.Content = content;
            if (_activeNav != null) _activeNav.Tag = null;
            _activeNav = btn;
            btn.Tag = "active";
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;   
            Hide();             
            Tray?.Notify("Student Planner", "Програма згорнута в трей");
        }
    }
}