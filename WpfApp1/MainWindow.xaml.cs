using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private TaskManager manager = new TaskManager();

        public MainWindow()
        {
            InitializeComponent();

            TasksGrid.ItemsSource = manager.Tasks;

            var view = CollectionViewSource.GetDefaultView(TasksGrid.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("Priority", ListSortDirection.Descending));
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddTaskWindow();

            if (window.ShowDialog() == true)
            {
                manager.AddTask(window.NewTask);

                window.NewTask.CalculatePriority();

                CollectionViewSource.GetDefaultView(TasksGrid.ItemsSource).Refresh();
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button).DataContext as TaskItem;
            manager.DeleteTask(task);
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var task = button.DataContext as TaskItem;

            var window = new EditTaskWindow(task);

            if (window.ShowDialog() == true)
            {
                task.CalculatePriority(); 

                CollectionViewSource.GetDefaultView(TasksGrid.ItemsSource).Refresh();
            }
        }
    }
}
