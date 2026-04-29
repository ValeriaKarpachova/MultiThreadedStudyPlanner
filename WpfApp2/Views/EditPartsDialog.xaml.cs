using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp2.Services;

namespace WpfApp2.Views
{
    public class PartEditEntry : INotifyPropertyChanged
    {
        public int SubTaskId { get; set; }
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

        private DateTime? _deadline;
        public DateTime? Deadline
        {
            get => _deadline;
            set { _deadline = value; Notify(nameof(Deadline)); }
        }

        public double ParsedHours =>
            double.TryParse(_hours,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var h) && h > 0 ? h : 1.0;

        void Notify(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public partial class EditPartsDialog : Window
    {
        private readonly TaskItem _task;
        private readonly TaskManager _manager;
        public ObservableCollection<PartEditEntry> Entries { get; } = new();

        public EditPartsDialog(TaskItem task, TaskManager manager)
        {
            InitializeComponent();
            _task = task;
            _manager = manager;

            LoadEntries();
            PartsList.ItemsSource = Entries;
        }

        // Завантажуємо підзавдання у список і оновлюємо заголовок
        private void LoadEntries()
        {
            Entries.Clear();
            TitleText.Text = _task.Name;
            InfoText.Text = $"Всього годин: {_task.EstimatedHours:F1} • Частин: {_task.SubTasks.Count}";

            int idx = 1;
            foreach (var sub in _task.SubTasks.OrderBy(s => s.Deadline))
                Entries.Add(new PartEditEntry
                {
                    SubTaskId = sub.Id,
                    Index = idx++,
                    Name = sub.Name ?? "",
                    Hours = sub.EstimatedHours.ToString(
                                    System.Globalization.CultureInfo.InvariantCulture),
                    Deadline = sub.Deadline
                });
        }

        // Перенумеровуємо після видалення/додавання
        private void Reindex()
        {
            for (int i = 0; i < Entries.Count; i++)
                Entries[i].Index = i + 1;
        }

        // ─── Видалити частину ───────────────────────────────────────────────
        private void DeletePart_Click(object sender, RoutedEventArgs e)
        {
            if (Entries.Count <= 1)
            {
                MessageBox.Show("Не можна видалити останню частину.\nВидаліть усе завдання цілком.",
                                "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var entry = (sender as Button)?.Tag as PartEditEntry;
            if (entry == null) return;

            var sub = _task.SubTasks.FirstOrDefault(s => s.Id == entry.SubTaskId);
            if (sub != null)
            {
                _task.SubTasks.Remove(sub);
                _manager.DeleteSubTask(sub);          // видаляємо з БД
            }

            Entries.Remove(entry);
            Reindex();

            // Перераховуємо години батька
            _task.EstimatedHours = _task.SubTasks.Sum(s => s.EstimatedHours);
            _manager.UpdateTask(_task);

            InfoText.Text = $"Всього годин: {_task.EstimatedHours:F1} • Частин: {_task.SubTasks.Count}";
        }

        // ─── Розбити частину ────────────────────────────────────────────────
        private void SplitPart_Click(object sender, RoutedEventArgs e)
        {
            var entry = (sender as Button)?.Tag as PartEditEntry;
            if (entry == null) return;

            var sub = _task.SubTasks.FirstOrDefault(s => s.Id == entry.SubTaskId);
            if (sub == null) return;

            // Відкриваємо SplitTaskDialog для цієї частини
            // Але частини не мають своїх підзавдань — тут спрощений діалог
            var dlg = new SplitPartDialog(sub, _manager, _task) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                // Перезавантажуємо список після розбиття
                LoadEntries();
                InfoText.Text = $"Всього годин: {_task.EstimatedHours:F1} • Частин: {_task.SubTasks.Count}";
            }
        }

        // ─── Зберегти всі зміни ─────────────────────────────────────────────
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            foreach (var entry in Entries)
            {
                var sub = _task.SubTasks.FirstOrDefault(s => s.Id == entry.SubTaskId);
                if (sub == null) continue;

                sub.Name = entry.Name;
                sub.EstimatedHours = entry.ParsedHours;
                sub.Deadline = entry.Deadline;
                _manager.UpdateTask(sub);
            }

            _task.EstimatedHours = _task.SubTasks.Sum(s => s.EstimatedHours);
            _manager.UpdateTask(_task);

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;
    }
}