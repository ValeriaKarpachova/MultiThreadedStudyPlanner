using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfApp1.Services;

namespace WpfApp1.Views
{
    public partial class MainWindow : Window
    {
        private TaskManager manager;
        private BackgroundProcessor _backgroundProcessor;

        public MainWindow()
        {
            InitializeComponent();

            var db = new DatabaseService();
            db.InitializeDatabase();

            manager = new TaskManager(db);
            manager.Load();

            TasksGrid.ItemsSource = manager.Tasks;

            foreach (var task in manager.Tasks)
                task.PropertyChanged += Task_PropertyChanged;

            manager.Tasks.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (TaskItem newTask in e.NewItems)
                        newTask.PropertyChanged += Task_PropertyChanged;
                }
            };

            _backgroundProcessor = new BackgroundProcessor(manager, TimeSpan.FromSeconds(30));
            _backgroundProcessor.Start();

            RefreshTasksView();
        }

        public void RefreshTasksView()
        {
            var view = CollectionViewSource.GetDefaultView(TasksGrid.ItemsSource);

            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(
                new SortDescription("Priority", ListSortDirection.Descending));

            view.Filter = t => !(t as TaskItem).IsCompleted;

            view.Refresh();
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
            var task = (sender as Button).DataContext as TaskItem;

            manager.DeleteTask(task);

            RefreshTasksView();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button).DataContext as TaskItem;

            var window = new EditTaskWindow(task);

            if (window.ShowDialog() == true)
            {
                manager.UpdateTask(task);
                RefreshTasksView();
            }
        }

        private void TaskCompletedChanged(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (TasksGrid.CommitEdit(DataGridEditingUnit.Row, true))
                    RefreshTasksView();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void Task_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskItem.IsCompleted))
                TaskCompletedChanged(sender, null);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _backgroundProcessor.Stop();
            base.OnClosing(e);
        }
    }
}