using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp2.Services;

namespace WpfApp2.Views
{
    public partial class NotesPanel : UserControl
    {
        private readonly NotesService _svc = new();
        private readonly ObservableCollection<NoteItem> _items = new();
        private bool _saving;

        public NotesPanel()
        {
            InitializeComponent();
            foreach (var n in _svc.LoadAll()) _items.Add(n);
            NotesList.ItemsSource = _items;
        }

        private void AddNote_Click(object s, RoutedEventArgs e)
        {
            var id = _svc.Add();
            _items.Add(new NoteItem { Id = id });
        }

        private void DeleteNote_Click(object s, RoutedEventArgs e)
        {
            var note = (s as Button)?.DataContext as NoteItem;
            if (note == null) return;
            _svc.Delete(note.Id);
            _items.Remove(note);
        }

        private void Color_Click(object s, RoutedEventArgs e)
        {
            var btn = s as Button;
            var note = btn?.DataContext as NoteItem;
            if (note == null || btn?.Tag is not string color) return;
            note.Color = color;
            _svc.Update(note);
       
            var idx = _items.IndexOf(note);
            if (idx >= 0) { _items[idx] = null!; _items[idx] = note; }
        }

        private void NoteText_Changed(object s, TextChangedEventArgs e)
        {
            if (_saving) return;
            var note = (s as TextBox)?.DataContext as NoteItem;
            if (note == null) return;
            _saving = true;
            _svc.Update(note);
            _saving = false;
        }
    }
}