using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WpfApp2.Services;

namespace WpfApp2.Views
{
    public partial class SplitPartDialog : Window
    {
        private readonly TaskItem _sub;     
        private readonly TaskItem _parent;   
        private readonly TaskManager _manager;
        private bool _initialized;
        public ObservableCollection<SubTaskEntry> Entries { get; } = new();

        public SplitPartDialog(TaskItem sub, TaskManager manager, TaskItem parent)
        {
            InitializeComponent();
            _sub = sub;
            _manager = manager;
            _parent = parent;

            TitleText.Text = $"Розбити: {sub.Name}";
            InfoText.Text = $"{sub.EstimatedHours} ч. • дедлайн: {sub.Deadline:dd.MM.yyyy}";

            SubList.ItemsSource = Entries;
            PartsLabel.Text = "2";
            _initialized = true;
            UpdateEntries(2);
        }

        private void PartsSlider_ValueChanged(object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_initialized) return;
            int parts = (int)e.NewValue;
            PartsLabel.Text = parts.ToString();
            UpdateEntries(parts);
        }

        private void UpdateEntries(int parts)
        {
            double h = Math.Round(_sub.EstimatedHours / parts, 1);
            Entries.Clear();
            for (int i = 0; i < parts; i++)
            {
                double hours = i == parts - 1
                    ? Math.Round(_sub.EstimatedHours - h * (parts - 1), 1)
                    : h;
                Entries.Add(new SubTaskEntry
                {
                    Index = i + 1,
                    Name = $"{_sub.Name} ({i + 1}/{parts})",
                    Hours = hours.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var baseDate = _sub.Deadline ?? DateTime.Today;

            _parent.SubTasks.Remove(_sub);
            _manager.DeleteSubTask(_sub);

            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                var newSub = new TaskItem
                {
                    Name = entry.Name,
                    TaskType = _sub.TaskType,
                    Priority = _sub.Priority,
                    ParentId = _parent.Id,
                    Deadline = baseDate.AddDays(i),
                    EstimatedHours = entry.ParsedHours,
                    IsChecked = false
                };
                _manager.AddSubTask(_parent, newSub);
            }

            _parent.EstimatedHours = _parent.SubTasks.Sum(s => s.EstimatedHours);
            _manager.UpdateTask(_parent);

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;
    }
}