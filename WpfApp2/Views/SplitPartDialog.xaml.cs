using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfApp2.Services;

namespace WpfApp2.Views
{
    public class SubTaskEntrySplit : INotifyPropertyChanged
    {
        public int Index { get; set; }

        private string _name = "";
        public string Name
        {
            get => _name;
            set { _name = value; Notify(nameof(Name)); }
        }

        private string _hours = "1";
        public string Hours
        {
            get => _hours;
            set { _hours = value; Notify(nameof(Hours)); }
        }

        public double ParsedHours =>
            double.TryParse(_hours,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var h) && h > 0 ? h : 1.0;

        private void Notify(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public partial class SplitPartDialog : Window
    {
        private readonly TaskItem _sub;
        private readonly TaskItem _parent;
        private readonly TaskManager _manager;
        private bool _initialized;
        public ObservableCollection<SubTaskEntrySplit> Entries { get; } = new();

        public SplitPartDialog(TaskItem sub, TaskManager manager, TaskItem parent)
        {
            InitializeComponent();
            _sub = sub;
            _manager = manager;
            _parent = parent;

            TitleText.Text = $"Розбити: {sub.Name}";
            InfoText.Text = $"{sub.EstimatedHours:F1} ч. • дедлайн: {sub.Deadline:dd.MM.yyyy}";

            SubTasksList.ItemsSource = Entries; 
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
                Entries.Add(new SubTaskEntrySplit
                {
                    Index = i + 1,
                    Name = $"{_sub.Name} ({i + 1}/{parts})",
                    Hours = hours.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)
                });
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var baseDate = _sub.Deadline ?? DateTime.Today;
            int subCount = Entries.Count;
            int splitPartId = _sub.Id;

            _parent.SubTasks.Remove(_sub);
            _manager.DeleteSubTask(_sub);

            for (int i = 0; i < subCount; i++)
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

            PlannerService.ShiftSiblingDeadlines(_parent, splitPartId, subCount);

            foreach (var sibling in _parent.SubTasks)
                _manager.UpdateTask(sibling);

            _parent.EstimatedHours = Math.Round(_parent.SubTasks.Sum(s => s.EstimatedHours), 1);
            _manager.UpdateTask(_parent);

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}