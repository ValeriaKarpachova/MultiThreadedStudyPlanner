using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApp2.Services;

namespace WpfApp2.Views
{
    public partial class CalendarView : UserControl
    {
        private readonly TaskManager _manager;
        private DateTime _month;

        public CalendarView(TaskManager manager)
        {
            InitializeComponent();
            _manager = manager;
            _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            Render();
        }

        private void Render()
        {
            MonthLabel.Text = _month.ToString("MMMM yyyy", new CultureInfo("uk-UA"));
            CalGrid.Children.Clear();

            int startDow = ((int)_month.DayOfWeek + 6) % 7;
            int daysInMonth = DateTime.DaysInMonth(_month.Year, _month.Month);
            var today = DateTime.Today;
            var tasks = _manager.Tasks.ToList();

            for (int i = 0; i < startDow; i++)
                CalGrid.Children.Add(new Border());

            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(_month.Year, _month.Month, d);
                var dayTasks = tasks.Where(t =>
                    t.Deadline.HasValue &&
                    t.Deadline.Value.Date == date).ToList();

                bool isToday = date == today;
                bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday ||
                                  date.DayOfWeek == DayOfWeek.Sunday;

                var border = new Border
                {
                    Margin = new Thickness(2),
                    CornerRadius = new CornerRadius(6),
                    BorderThickness = new Thickness(isToday ? 2 : 1),
                    BorderBrush = isToday
                        ? new SolidColorBrush(Color.FromRgb(83, 74, 183))
                        : new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    Background = isToday
                        ? new SolidColorBrush(Color.FromRgb(237, 235, 254))
                        : Brushes.White,
                    Padding = new Thickness(4)
                };

                var panel = new StackPanel();

                var dayNum = new TextBlock
                {
                    Text = d.ToString(),
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Foreground = isWeekend
                        ? new SolidColorBrush(Color.FromRgb(224, 112, 112))
                        : isToday
                            ? new SolidColorBrush(Color.FromRgb(83, 74, 183))
                            : Brushes.Black
                };

                // ← виправлення: System.Windows.HorizontalAlignment замість просто HorizontalAlignment
                var addBtn = new Button
                {
                    Content = "+",
                    FontSize = 10,
                    Width = 16,
                    Height = 16,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = date
                };
                addBtn.Click += AddTaskOnDate_Click;

                var header = new Grid();
                header.ColumnDefinitions.Add(new ColumnDefinition
                { Width = new GridLength(1, GridUnitType.Star) });
                header.ColumnDefinitions.Add(new ColumnDefinition
                { Width = GridLength.Auto });
                Grid.SetColumn(dayNum, 0);
                Grid.SetColumn(addBtn, 1);
                header.Children.Add(dayNum);
                header.Children.Add(addBtn);
                panel.Children.Add(header);

                const int maxShow = 3;
                bool hasMore = dayTasks.Count > maxShow;

                foreach (var t in dayTasks.Take(maxShow))
                    panel.Children.Add(MakeTaskBadge(t, date < today));

                if (hasMore)
                {
                    var expandBtn = new Button
                    {
                        Content = $"▼ ще {dayTasks.Count - maxShow}",
                        FontSize = 9,
                        Padding = new Thickness(2, 1, 2, 1),
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Tag = (date, dayTasks, panel, maxShow)
                    };
                    expandBtn.Click += ExpandDay_Click;
                    panel.Children.Add(expandBtn);
                }

                border.Child = panel;
                CalGrid.Children.Add(border);
            }

            int filled = startDow + daysInMonth;
            for (int i = filled; i < 42; i++)
                CalGrid.Children.Add(new Border());
        }

        private static Border MakeTaskBadge(TaskItem t, bool overdue) =>
            new Border
            {
                Background = overdue && !t.IsCompleted
                    ? new SolidColorBrush(Color.FromRgb(254, 226, 226))
                    : t.IsCompleted
                        ? new SolidColorBrush(Color.FromRgb(220, 252, 231))
                        : new SolidColorBrush(Color.FromRgb(237, 235, 254)),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(3, 1, 3, 1),
                Child = new TextBlock
                {
                    Text = t.Name,
                    FontSize = 10,
                    TextTrimming = TextTrimming.CharacterEllipsis
                }
            };

        private void ExpandDay_Click(object s, RoutedEventArgs e)
        {
            var btn = s as Button;
            if (btn?.Tag is not (DateTime date, List<TaskItem> all,
                StackPanel panel, int shown)) return;

            panel.Children.Remove(btn);
            foreach (var t in all.Skip(shown))
                panel.Children.Add(MakeTaskBadge(t, date < DateTime.Today));
        }

        private void AddTaskOnDate_Click(object s, RoutedEventArgs e)
        {
            var date = (DateTime)((Button)s).Tag;
            var dlg = new AddTaskWindow();
            dlg.SetDeadline(date);
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true)
            {
                _manager.AddTask(dlg.NewTask);
                Render();
            }
        }

        private void PrevMonth_Click(object s, RoutedEventArgs e)
        {
            _month = _month.AddMonths(-1);
            Render();
        }

        private void NextMonth_Click(object s, RoutedEventArgs e)
        {
            _month = _month.AddMonths(1);
            Render();
        }
    }
}