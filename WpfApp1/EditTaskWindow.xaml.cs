using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static WpfApp1.TaskItem;

namespace WpfApp1
{
    public partial class EditTaskWindow : Window
    {
        public TaskItem Task { get; private set; }

        public EditTaskWindow(TaskItem task)
        {
            InitializeComponent();

            Task = task;

            NameBox.Text = Task.Name;
            ProgressBox.Text = Task.Progress.ToString();
            DeadlinePicker.SelectedDate = Task.Deadline;

            foreach (ComboBoxItem item in TypeBox.Items)
            {
                if ((string)item.Content == Task.TaskType)
                {
                    TypeBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем корректность прогресса
            if (!int.TryParse(ProgressBox.Text, out int progress))
            {
                MessageBox.Show("Progress must be a number");
                return;
            }

            // Проверяем имя
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Task name cannot be empty");
                return;
            }

            try
            {
                Task.Name = NameBox.Text;
                Task.Description = Description.Text;
                Task.Progress = progress;
                Task.Deadline = DeadlinePicker.SelectedDate ?? DateTime.Now;

                var selectedType = (TypeBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                Task.TaskType = selectedType ?? Task.TaskType;

                Task.CalculateImportance();
                Task.CalculatePriority();

                // Обновляем DataGrid и пересортировку
                CollectionViewSource.GetDefaultView(Application.Current.Windows
                    .OfType<MainWindow>().First().TasksGrid.ItemsSource).Refresh();

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error: " + ex.Message);
            }
        }
    }
}