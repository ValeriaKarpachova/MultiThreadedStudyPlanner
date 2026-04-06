using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private TaskManager manager = new TaskManager();

        public MainWindow()
        {
            InitializeComponent();

            TasksGrid.ItemsSource = manager.Tasks;
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddTaskWindow();

            if (window.ShowDialog() == true)
            {
                manager.AddTask(window.NewTask);
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

            var window = new AddTaskWindow(task);

            if (window.ShowDialog() == true)
            {
                TasksGrid.Items.Refresh();
            }
        }
    }
}
