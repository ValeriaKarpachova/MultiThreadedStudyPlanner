using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp2.Services;

namespace WpfApp2.Views
{
    public class SubjectWithTasks
    {
        public Subject SubjectInfo { get; set; } = new();
        public int Id => SubjectInfo.Id;
        public string Name => SubjectInfo.Name;
        public string Color => SubjectInfo.Color;
        public List<TaskItem> Tasks { get; set; } = new();
    }

    public partial class SubjectsView : UserControl
    {
        private readonly TaskManager _manager;
        private readonly SubjectService _svc = new();

        public SubjectsView(TaskManager manager)
        {
            InitializeComponent();
            _manager = manager;
            Refresh();
        }

        private void Refresh()
        {
            var subjects = _svc.GetAll();
            var allTasks = _manager.Tasks.ToList();

            SubjectsList.ItemsSource = subjects
                .Select(s => new SubjectWithTasks
                {
                    SubjectInfo = s,
                    Tasks = allTasks.Where(t => t.SubjectId == s.Id).ToList()
                })
                .ToList();
        }

        private void AddSubject_Click(object s, RoutedEventArgs e)
        {
            var dlg = new AddSubjectDialog { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                _svc.Add(dlg.SubjectName, dlg.SubjectColor);
                Refresh();
            }
        }

        private void DeleteSubject_Click(object s, RoutedEventArgs e)
        {
            var item = (s as Button)?.DataContext as SubjectWithTasks;
            if (item == null) return;

            if (MessageBox.Show(
                    $"Видалити предмет «{item.Name}»?",
                    "Підтвердження",
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _svc.Delete(item.Id);
                Refresh();
            }
        }

        private void DeleteTask_Click(object s, RoutedEventArgs e)
        {
            var task = (s as Button)?.DataContext as TaskItem;
            if (task == null) return;
            _manager.DeleteTask(task);
            Refresh();
        }
    }
}