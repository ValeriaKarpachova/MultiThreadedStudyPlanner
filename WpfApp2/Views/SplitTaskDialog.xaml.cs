using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using WpfApp2.Services;

namespace WpfApp2.Views
{
    public class SubTaskEntry : INotifyPropertyChanged
    {
        public int Index { get; set; }
        private string _name = "";
        public string Name
        {
            get => _name;
            set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public partial class SplitTaskDialog : Window
    {
        private readonly TaskItem _task;
        private readonly TaskManager _manager;
        private bool _initialized = false; // ← прапор готовності
        public ObservableCollection<SubTaskEntry> Entries { get; } = new();

        public SplitTaskDialog(TaskItem task, TaskManager manager)
        {
            InitializeComponent();
            _task = task;
            _manager = manager;

            TitleText.Text = task.Name;
            InfoText.Text = $"{task.EstimatedHours} ч. • дедлайн: {task.Deadline:dd.MM.yyyy}";

            SubTasksList.ItemsSource = Entries;
            PartsLabel.Text = "3";

            _initialized = true; // ← тільки після прив'язки елементів
            UpdateEntries(3);
        }

        private void PartsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_initialized) return; // ← ігноруємо виклик під час XAML-ініціалізації

            int parts = (int)e.NewValue;
            PartsLabel.Text = parts.ToString();
            UpdateEntries(parts);
        }

        private void UpdateEntries(int parts)
        {
            var names = TaskTypeService.GetDefaultSubTaskNames(_task.TaskType, parts);
            Entries.Clear();
            for (int i = 0; i < parts; i++)
                Entries.Add(new SubTaskEntry { Index = i + 1, Name = names[i] });
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            int parts = (int)PartsSlider.Value;
            var names = Entries.Select(en => en.Name).ToList();
            var subTasks = PlannerService.SplitTask(_task, parts, names);

            foreach (var sub in subTasks)
            {
                sub.ParentId = _task.Id;
                _manager.AddSubTask(_task, sub);
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}