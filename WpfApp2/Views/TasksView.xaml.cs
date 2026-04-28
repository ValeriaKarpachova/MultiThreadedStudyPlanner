using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfApp2.Services;

namespace WpfApp2.Views
{
    public partial class TasksView : UserControl
    {
        private readonly TaskManager manager;
        private readonly MainWindow.TaskViewMode viewMode;

        public TasksView(TaskManager manager, MainWindow.TaskViewMode mode)
        {
            InitializeComponent();

            this.manager = manager;
            this.viewMode = mode;

            TasksGrid.ItemsSource = manager.Tasks;

            var view = CollectionViewSource.GetDefaultView(TasksGrid.ItemsSource);

            view.SortDescriptions.Add(
                new SortDescription(nameof(TaskItem.Priority), ListSortDirection.Descending));

            view.Filter = TaskFilter;

            Subscribe();
        }

        private void Subscribe()
        {
            foreach (var task in manager.Tasks)
                task.PropertyChanged += Task_PropertyChanged;

            manager.Tasks.CollectionChanged += (s, e) =>
            {
                if (e.NewItems == null) return;

                foreach (TaskItem t in e.NewItems)
                    t.PropertyChanged += Task_PropertyChanged;
            };
        }

        private void Task_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskItem.Priority))
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            Dispatcher.Invoke(() =>
            {
                CollectionViewSource.GetDefaultView(TasksGrid.ItemsSource)?.Refresh();
            });
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button)?.DataContext as TaskItem;
            if (task == null) return;

            manager.DeleteTask(task);
            Refresh();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button)?.DataContext as TaskItem;
            if (task == null) return;

            var window = new EditTaskWindow(task, manager);

            if (window.ShowDialog() == true)
            {
                Refresh();
            }
        }

        private bool TaskFilter(object t)
        {
            var task = t as TaskItem;
            if (task == null) return false;

            var today = DateTime.Today;

            return viewMode switch
            {
                MainWindow.TaskViewMode.Home => !task.IsCompleted,

                MainWindow.TaskViewMode.Today =>
                    task.Deadline.HasValue &&
                    task.Deadline.Value.Date == today &&
                    !task.IsCompleted,

                MainWindow.TaskViewMode.Completed => task.IsCompleted,

                _ => true
            };
        }
    }
}