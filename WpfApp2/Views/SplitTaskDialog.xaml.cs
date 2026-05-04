using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
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

    public partial class SplitTaskDialog : Window
    {
        private readonly TaskItem _task;
        private readonly TaskManager _manager;
        private bool _initialized = false;
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

            _initialized = true;
            UpdateEntries(3);
        }

        // Для перетягування вікна
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
            var names = TaskTypeService.GetDefaultSubTaskNames(_task.TaskType, parts);
            double hoursPerPart = Math.Round(_task.EstimatedHours / parts, 1);

            Entries.Clear();
            for (int i = 0; i < parts; i++)
            {
                double h = i == parts - 1
                    ? Math.Round(_task.EstimatedHours - hoursPerPart * (parts - 1), 1)
                    : hoursPerPart;

                Entries.Add(new SubTaskEntry
                {
                    Index = i + 1,
                    Name = names[i],
                    Hours = h.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            int parts = (int)PartsSlider.Value;
            var today = DateTime.Today;

            var subTasks = Entries.Select((en, i) => new TaskItem
            {
                Name = en.Name,
                Description = "",
                TaskType = _task.TaskType,
                Priority = _task.Priority,
                ParentId = _task.Id,
                Deadline = today.AddDays(i),
                EstimatedHours = en.ParsedHours,
                IsChecked = false
            }).ToList();

            foreach (var sub in subTasks)
                _manager.AddSubTask(_task, sub);

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;
    }
}