using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfApp2.Services;

namespace WpfApp2.Views
{
    public partial class MainWindow : Window
    {
        private readonly TaskManager manager;
        private readonly BackgroundProcessor _backgroundProcessor;
        private TaskViewMode currentView = TaskViewMode.Home;

        public MainWindow()
        {
            InitializeComponent();

            var db = new DatabaseService();
            db.InitializeDatabase();

            manager = new TaskManager(db);
            manager.Load();

            _backgroundProcessor = new BackgroundProcessor(manager);
            _backgroundProcessor.Start();
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddTaskWindow();

            if (window.ShowDialog() == true)
            {
                manager.AddTask(window.NewTask);
            }
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new TasksView(manager, TaskViewMode.Home);
        }

        private void Today_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new TasksView(manager, TaskViewMode.Today);
        }

        private void Completed_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new TasksView(manager, TaskViewMode.Completed);
        }

        private void Calendar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Тут будет календарь");
        }

        private void Statistics_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new StatisticsView(manager);
        }

        public enum TaskViewMode
        {
            Home,
            Today,
            Completed
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _backgroundProcessor.Stop();
            base.OnClosing(e);
        }
    }
}