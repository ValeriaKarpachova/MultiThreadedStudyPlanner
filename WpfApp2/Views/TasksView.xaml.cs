using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApp2.Services;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

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

            RefreshTree();

            manager.Tasks.CollectionChanged += (s, e) => RefreshTree();

            foreach (var task in manager.Tasks)
            {
                SubscribeToTask(task);
            }
        }

        private void SubscribeToTask(TaskItem task)
        {
            task.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TaskItem.IsChecked) ||
                    e.PropertyName == nameof(TaskItem.Progress))
                {
                    RefreshTree(); 
                }
            };
            
            foreach (var sub in task.SubTasks)
            {
                sub.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(TaskItem.IsChecked))
                    {
                        task.RefreshParent(); 
                        RefreshTree();
                    }
                };
            }
        }

        private void RefreshTree()
        {
            var today = System.DateTime.Today;

            var filtered = manager.Tasks.Where(t => viewMode switch
            {
                MainWindow.TaskViewMode.Today =>
                    t.IsSplit
                        ? t.SubTasks.Any(s =>
                            s.Deadline.HasValue &&
                            s.Deadline.Value.Date == today &&
                            !s.IsChecked)
                        : t.Deadline.HasValue &&
                          t.Deadline.Value.Date == today &&
                          !t.IsCompleted,

                MainWindow.TaskViewMode.Completed => t.IsCompleted,
                _ => !t.IsCompleted
            }).OrderByDescending(t => t.Priority).ToList();

            TasksGrid.ItemsSource = filtered;
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button)?.DataContext as TaskItem;
            if (task == null) return;

            var window = new EditTaskWindow(task, manager);
            if (window.ShowDialog() == true)
                RefreshTree();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button)?.DataContext as TaskItem;
            if (task == null) return;

            manager.DeleteTask(task);
            RefreshTree();
        }

        private void Split_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button)?.DataContext as TaskItem;
            if (task == null) return;

            var dialog = new SplitTaskDialog(task, manager)
            {
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() == true)
                RefreshTree();
        }

        private void ExpanderToggle_Checked(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleButton;
            var task = toggle?.Tag as TaskItem;
            if (task == null || !task.IsSplit) return;

            var row = FindParentDataGridRow(toggle);
            if (row != null)
            {
                row.DetailsVisibility = Visibility.Visible;
            }
        }

        private void ExpanderToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleButton;
            var task = toggle?.Tag as TaskItem;
            if (task == null || !task.IsSplit) return;

            var row = FindParentDataGridRow(toggle);
            if (row != null)
            {
                row.DetailsVisibility = Visibility.Collapsed;
            }
        }

        private DataGridRow FindParentDataGridRow(DependencyObject child)
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is DataGridRow row) return row;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

    }
}