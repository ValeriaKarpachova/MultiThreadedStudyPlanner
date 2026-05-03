using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApp2.Views
{
    public partial class MiniCalendarPanel : UserControl
    {
        private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);

        public MiniCalendarPanel() { InitializeComponent(); Render(); }

        private void Render()
        {
            MonthLbl.Text = _month.ToString("MMM yyyy", new CultureInfo("uk-UA"));
            Grid.Children.Clear();

            string[] heads = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Нд" };
            foreach (var h in heads)
                Grid.Children.Add(new TextBlock
                {
                    Text = h,
                    FontSize = 8,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.Gray,
                    FontWeight = FontWeights.Bold
                });

            int startDow = ((int)_month.DayOfWeek + 6) % 7;
            int days = DateTime.DaysInMonth(_month.Year, _month.Month);
            var today = DateTime.Today;

            for (int i = 0; i < startDow; i++)
                Grid.Children.Add(new TextBlock());

            for (int d = 1; d <= days; d++)
            {
                var date = new DateTime(_month.Year, _month.Month, d);
                bool isToday = date == today;
                bool isWe = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

                var tb = new Border
                {
                    CornerRadius = new CornerRadius(3),
                    Background = isToday
                        ? new SolidColorBrush(Color.FromRgb(83, 74, 183))
                        : Brushes.Transparent,
                    Width = 22,
                    Height = 18,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = d.ToString(),
                        FontSize = 9,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        Foreground = isToday
                            ? Brushes.White
                            : isWe
                                ? new SolidColorBrush(Color.FromRgb(224, 112, 112))
                                : new SolidColorBrush(Color.FromRgb(44, 44, 42))
                    }
                };
                Grid.Children.Add(tb);
            }
        }

        private void Prev_Click(object s, RoutedEventArgs e) { _month = _month.AddMonths(-1); Render(); }
        private void Next_Click(object s, RoutedEventArgs e) { _month = _month.AddMonths(1); Render(); }
    }
}
