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

            var today = DateTime.Today;

            view.Filter = t =>
            {
                var task = t as TaskItem;

                return currentView switch
                {
                    TaskViewMode.Home => !task.IsCompleted,

                    TaskViewMode.Today =>
                        task.Deadline.HasValue &&
                        task.Deadline.Value.Date == today &&
                        !task.IsCompleted,

                    TaskViewMode.Completed => task.IsCompleted,

                    _ => true
                };
            };

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
                RefreshTasksView();
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

            manager.IsEditing = true; 

            var window = new EditTaskWindow(task, manager);

            if (window.ShowDialog() == true)
            {
                manager.IsEditing = false; 
                manager.UpdateTask(task); 
                RefreshTasksView();
            }
            else
            {
                manager.IsEditing = false; 
            }
        }

        private void TaskCompletedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is TaskItem task)
            {
                task.Progress = cb.IsChecked == true ? 100 : 0;

                manager.UpdateTask(task);

                RefreshTasksView();
            }
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            currentView = TaskViewMode.Home;
            RefreshTasksView();
        }

        private void Today_Click(object sender, RoutedEventArgs e)
        {
            currentView = TaskViewMode.Today;
            RefreshTasksView();
        }

        private void Completed_Click(object sender, RoutedEventArgs e)
        {
            currentView = TaskViewMode.Completed;
            RefreshTasksView();
        }

        private void Calendar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Тут будет календарь");
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