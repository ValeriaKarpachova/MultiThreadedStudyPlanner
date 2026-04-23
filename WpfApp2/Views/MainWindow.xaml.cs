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

        public MainWindow()
        {
            InitializeComponent();

            var db = new DatabaseService();
            db.InitializeDatabase();

            manager = new TaskManager(db);
            manager.Load();

            TasksGrid.ItemsSource = manager.Tasks;

            SubscribeToTasks();

            _backgroundProcessor = new BackgroundProcessor(manager, TimeSpan.FromSeconds(30));
            _backgroundProcessor.Start();

            RefreshTasksView();
        }

        public void RefreshTasksView()
        {
            var view = CollectionViewSource.GetDefaultView(TasksGrid.ItemsSource);

            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(
                new SortDescription(nameof(TaskItem.Priority), ListSortDirection.Descending));

            view.Filter = t => !(t as TaskItem).IsCompleted;

            view.Refresh();
        }

        private void SubscribeToTasks()
        {
            foreach (var task in manager.Tasks)
                task.PropertyChanged += Task_PropertyChanged;

            manager.Tasks.CollectionChanged += (s, e) =>
            {
                if (e.NewItems == null) return;

                foreach (TaskItem newTask in e.NewItems)
                    newTask.PropertyChanged += Task_PropertyChanged;
            };
        }

        private void Task_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskItem.IsCompleted))
            {
                RefreshTasksView();
            }
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddTaskWindow();

            if (window.ShowDialog() == true)
            {
                manager.AddTask(window.NewTask);
                RefreshTasksView();
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button)?.DataContext as TaskItem;
            if (task == null) return;

            manager.DeleteTask(task);
            RefreshTasksView();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button)?.DataContext as TaskItem;
            if (task == null) return;

            var window = new EditTaskWindow(task, manager);

            if (window.ShowDialog() == true)
            {
                RefreshTasksView();
            }
        }

        private void TaskCompletedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is TaskItem task)
            {
                task.Progress = cb.IsChecked == true ? 100 : task.Progress;

                manager.UpdateTask(task);

                RefreshTasksView();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _backgroundProcessor.Stop();
            base.OnClosing(e);
        }
    }
}