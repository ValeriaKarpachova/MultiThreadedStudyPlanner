using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class AddTaskWindow : Window
    {
        public string TaskName { get; set; }
        public string TaskDescription { get; set; }
        public DateTime TaskDeadline { get; set; }
        public int TaskPriority { get; set; }

        public AddTaskWindow()
        {
            InitializeComponent();
            DeadlinePicker.SelectedDate = DateTime.Now;
            PriorityComboBox.SelectedIndex = 0;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            TaskName = NameTextBox.Text;
            TaskDescription = DescriptionTextBox.Text;
            TaskDeadline = DeadlinePicker.SelectedDate ?? DateTime.Now;
            TaskPriority = int.Parse((PriorityComboBox.SelectedItem as ComboBoxItem).Content.ToString());

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
