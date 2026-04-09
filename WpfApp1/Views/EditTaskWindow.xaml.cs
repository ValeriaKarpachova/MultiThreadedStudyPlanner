using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfApp1.Services;

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
            if (!Validator.ValidateTask(NameBox.Text, ProgressBox.Text, out int progress, out string error))
            {
                MessageBox.Show(error);
                return;
            }

            try
            {
                Task.Name = NameBox.Text;
                Task.Description = Description.Text;
                Task.Progress = progress;
                Task.Deadline = DeadlinePicker.SelectedDate;

                var selectedType = (TypeBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                
                if (selectedType == "Без типу")
                    Task.TaskType = null;
                else
                    Task.TaskType = selectedType;

                Task.CalculateImportance();
                Task.CalculatePriority();

                var mainWindow = Application.Current.Windows.OfType<Views.MainWindow>().FirstOrDefault();
                mainWindow?.RefreshTasksView();

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error: " + ex.Message);
            }
        }
    }
}