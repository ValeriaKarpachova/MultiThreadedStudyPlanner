using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfApp2.Services;

namespace WpfApp2.Views
{
    public partial class StatisticsView : UserControl
    {
        public StatisticsView(TaskManager manager)
        {
            InitializeComponent();
            var tasks    = manager.Tasks.ToList();
            var report   = ProductivityService.Analyze(tasks);
            var subjects = new SubjectService().GetAll();

            BuildKpiCards(report, tasks);
            BuildByTypeChart(tasks);
            BuildStatusPie(report);
            BuildWeekLoadChart(tasks);
            BuildTopTasksList(tasks);
            BuildDeadlineChart(tasks);
            BuildSubjectAccordion(tasks, subjects);
        }

        // KPI
        private void BuildKpiCards(ProductivityReport r, List<TaskItem> tasks)
        {
            SetCard(CardTotal,        "Всього задач",   r.TotalTasks.ToString(),        "#534AB7");
            SetCard(CardDone,         "Виконано",       r.CompletedTasks.ToString(),     "#22A06B");
            SetCard(CardOverdue,      "Прострочено",    r.OverdueTasks.ToString(),       "#E2483D");
            SetCard(CardProductivity, "Продуктивність", $"{r.ProductivityPercent:F1}%",  "#0065FF");

            double todayH = PlannerService.GetTodayHours(tasks);
            SetCard(CardTotalHours,  "Загальний обсяг", $"{r.TotalHours:F1} год",        "#7B68EE");
            SetCard(CardDoneHours,   "Виконано годин",  $"{r.CompletedHours:F1} год",    "#22A06B");
            SetCard(CardAvgProgress, "Сер. прогрес",    $"{r.AverageProgress:F0}%",      "#FF8C00");
            SetCard(CardTodayHours,  "Сьогодні (год)",
                todayH > PlannerService.DailyLimit ? $"⚠ {todayH:F1}" : $"{todayH:F1}",
                todayH > PlannerService.DailyLimit ? "#E2483D" : "#0065FF");
        }

        private static void SetCard(Border b, string label, string value, string hex)
        {
            var col = (Color)ColorConverter.ConvertFromString(hex);
            b.Background = new SolidColorBrush(Color.FromArgb(25, col.R, col.G, col.B));
            b.Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = value, FontSize = 26, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(col) },
                    new TextBlock { Text = label, FontSize = 11, Foreground = Brushes.Gray }
                }
            };
        }

        // Задачі за типом 
        private void BuildByTypeChart(List<TaskItem> tasks)
        {
            var groups = tasks
                .GroupBy(t => string.IsNullOrEmpty(t.TaskType) ? "Інше" : t.TaskType)
                .OrderByDescending(g => g.Count()).Take(8).ToList();

            ByTypeChart.Series = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Values = groups.Select(g => g.Count()).ToArray(),
                    Fill = new SolidColorPaint(SKColor.Parse("#534AB7")), Stroke = null,
                    MaxBarWidth = 40,
                    DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#534AB7")),
                    DataLabelsSize = 11,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                    DataLabelsFormatter = p => p.Coordinate.PrimaryValue.ToString()
                }
            };
            var labels = groups.Select(g => g.Key.Length > 12 ? g.Key[..11] + "…" : g.Key).ToArray();
            ByTypeChart.XAxes = new[] { new Axis { Labels = labels, LabelsRotation = -35, TextSize = 11, MinStep = 1 } };
            ByTypeChart.YAxes = new[] { new Axis { MinLimit = 0, MinStep = 1, TextSize = 11 } };
            ByTypeChart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Hidden;
        }

        // Кругова: статус
        private void BuildStatusPie(ProductivityReport r)
        {
            int active = Math.Max(0, r.TotalTasks - r.CompletedTasks - r.OverdueTasks);
            StatusPie.Series = new ISeries[]
            {
                new PieSeries<int> { Name = "Виконано",    Values = new[] { r.CompletedTasks }, Fill = new SolidColorPaint(SKColor.Parse("#22A06B")) },
                new PieSeries<int> { Name = "Активні",     Values = new[] { active },           Fill = new SolidColorPaint(SKColor.Parse("#534AB7")) },
                new PieSeries<int> { Name = "Прострочені", Values = new[] { r.OverdueTasks },   Fill = new SolidColorPaint(SKColor.Parse("#E2483D")) },
            };
            var legend = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            void AddDot(string text, string hex)
            {
                var col = (Color)ColorConverter.ConvertFromString(hex);
                legend.Children.Add(new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(5), Background = new SolidColorBrush(col), Margin = new Thickness(0,0,4,0), VerticalAlignment = VerticalAlignment.Center });
                legend.Children.Add(new TextBlock { Text = text, FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0,0,14,0), VerticalAlignment = VerticalAlignment.Center });
            }
            AddDot($"Виконано ({r.CompletedTasks})", "#22A06B");
            AddDot($"Активні ({active})",            "#534AB7");
            AddDot($"Прострочені ({r.OverdueTasks})", "#E2483D");
            LegendPanel.Content = legend;
        }

        // Навантаження по тижнях
        private void BuildWeekLoadChart(List<TaskItem> tasks)
        {
            var today = DateTime.Today;
            var labels = new List<string>(); var hours = new List<double>(); var doneH = new List<double>();
            for (int w = -3; w <= 4; w++)
            {
                int dow = (int)today.DayOfWeek; if (dow == 0) dow = 7;
                var ws = today.AddDays(-(dow - 1) + w * 7); var we = ws.AddDays(7);
                labels.Add(ws.ToString("dd.MM"));
                var week = tasks.Where(t => t.Deadline.HasValue && t.Deadline.Value.Date >= ws && t.Deadline.Value.Date < we).ToList();
                hours.Add(Math.Round(week.Sum(t => t.EstimatedHours), 1));
                doneH.Add(Math.Round(week.Sum(t => t.EstimatedHours * t.Progress / 100.0), 1));
            }
            WeekLoadChart.Series = new ISeries[]
            {
                new LineSeries<double> { Name = "Заплановано", Values = hours, Fill = new SolidColorPaint(SKColor.Parse("#534AB720")), Stroke = new SolidColorPaint(SKColor.Parse("#534AB7")) { StrokeThickness = 2 }, GeometrySize = 8 },
                new LineSeries<double> { Name = "Виконано",    Values = doneH, Fill = new SolidColorPaint(SKColor.Parse("#22A06B20")), Stroke = new SolidColorPaint(SKColor.Parse("#22A06B")) { StrokeThickness = 2 }, GeometrySize = 6 }
            };
            WeekLoadChart.XAxes = new[] { new Axis { Labels = labels, TextSize = 10 } };
            WeekLoadChart.YAxes = new[] { new Axis { MinLimit = 0, TextSize = 10 } };
            WeekLoadChart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Bottom;
        }

        private void BuildTopTasksList(List<TaskItem> tasks)
        {
            var top = tasks
                .Where(t => !t.IsSplit)
                .OrderByDescending(t => t.EstimatedHours)
                .Take(10)
                .ToList();

            double maxH = top.FirstOrDefault()?.EstimatedHours ?? 1;

            var panel = new StackPanel();

            for (int i = 0; i < top.Count; i++)
            {
                var t = top[i];
                double fillPct = maxH > 0 ? t.EstimatedHours / maxH : 0;
                bool done = t.IsCompleted;

                var barColor = done
                    ? Color.FromRgb(34, 160, 107)  
                    : Color.FromRgb(83, 74, 183);   

                var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });  
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); 
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });   

                FrameworkElement rankIcon;

                if (i < 3)
                {
                    string medalImage = i == 0 ? "gold_medal.png" : (i == 1 ? "silver_medal.png" : "bronze_medal.png");
                    var img = new System.Windows.Controls.Image  
                    {
                        Source = LoadImageFromResources(medalImage),
                        Width = 24,
                        Height = 24,
                        Stretch = Stretch.Uniform
                    };
                    rankIcon = img;
                }
                else
                {
                    rankIcon = new TextBlock
                    {
                        Text = $"#{i + 1}",
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Gray,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    };
                }

                Grid.SetColumn(rankIcon, 0);

                var nameAndBar = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

                var nameBlock = new TextBlock
                {
                    Text = t.Name ?? "",
                    FontSize = 13,
                    FontWeight = i < 3 ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = done ? Brushes.Gray : Brushes.Black,
                    TextDecorations = done ? TextDecorations.Strikethrough : null,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 0, 0, 3)
                };
                nameAndBar.Children.Add(nameBlock);

                var track = new Border
                {
                    Height = 6,
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush(Color.FromArgb(30, barColor.R, barColor.G, barColor.B))
                };
                var fill = new Border
                {
                    Height = 6,
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush(barColor),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    Width = double.NaN
                };

                var barGrid = new Grid();
                barGrid.Children.Add(track);
                barGrid.Children.Add(fill);

                double pct = fillPct;
                track.SizeChanged += (s, e) =>
                {
                    fill.Width = e.NewSize.Width * pct;
                };

                nameAndBar.Children.Add(barGrid);
                Grid.SetColumn(nameAndBar, 1);

                var hoursBlock = new TextBlock
                {
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(barColor),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    TextAlignment = TextAlignment.Right,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                hoursBlock.Inlines.Add(new Run($"{t.EstimatedHours:F1}"));
                hoursBlock.Inlines.Add(new Run(" г.") { Foreground = Brushes.Gray, FontWeight = FontWeights.Normal, FontSize = 11 });
                Grid.SetColumn(hoursBlock, 2);

                row.Children.Add(rankIcon);
                row.Children.Add(nameAndBar);
                row.Children.Add(hoursBlock);

                var separator = new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)),
                    Margin = new Thickness(32, 4, 0, 0)
                };

                panel.Children.Add(row);
                if (i < top.Count - 1)
                    panel.Children.Add(separator);
            }

            TopTasksList.Content = panel;
        }

        private static System.Windows.Media.ImageSource LoadImageFromResources(string fileName)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/Images/{fileName}");
                return System.Windows.Media.Imaging.BitmapFrame.Create(uri);
            }
            catch
            {
                return null;
            }
        }

        // Навантаження на 14 днів 
        private void BuildDeadlineChart(List<TaskItem> tasks)
        {
            var today = DateTime.Today;
            var labels = new List<string>();
            var safeIdx = new List<double?>(); var warnIdx = new List<double?>(); var overIdx = new List<double?>();
            for (int d = 0; d < 14; d++)
            {
                var date = today.AddDays(d); labels.Add(date.ToString("dd.MM"));
                double h = 0;
                foreach (var t in tasks)
                {
                    if (t.IsCompleted) continue;
                    if (t.IsSplit) h += t.SubTasks.Where(s => s.Deadline.HasValue && s.Deadline.Value.Date == date && !s.IsChecked).Sum(s => s.EstimatedHours);
                    else if (t.Deadline.HasValue && t.Deadline.Value.Date == date) h += t.EstimatedHours * (1 - t.Progress / 100.0);
                }
                h = Math.Round(h, 1);
                if (h > PlannerService.DailyLimit)              { safeIdx.Add(null); warnIdx.Add(null); overIdx.Add(h); }
                else if (h > PlannerService.DailyLimit * 0.75)  { safeIdx.Add(null); warnIdx.Add(h); overIdx.Add(null); }
                else                                             { safeIdx.Add(h); warnIdx.Add(null); overIdx.Add(null); }
            }
            DeadlineChart.Series = new ISeries[]
            {
                new ColumnSeries<double?> { Name = "Норма",             Values = safeIdx, Fill = new SolidColorPaint(SKColor.Parse("#22A06B")), Stroke = null, MaxBarWidth = 30 },
                new ColumnSeries<double?> { Name = "Близько до ліміту", Values = warnIdx, Fill = new SolidColorPaint(SKColor.Parse("#FF8C00")), Stroke = null, MaxBarWidth = 30 },
                new ColumnSeries<double?> { Name = "Перевантаження",    Values = overIdx, Fill = new SolidColorPaint(SKColor.Parse("#E2483D")), Stroke = null, MaxBarWidth = 30 },
            };
            DeadlineChart.XAxes = new[] { new Axis { Labels = labels, TextSize = 10, LabelsRotation = -35 } };
            DeadlineChart.YAxes = new[] { new Axis { MinLimit = 0, TextSize = 10 } };
            DeadlineChart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Bottom;
        }

        // Статистика по предметах
        private void BuildSubjectAccordion(List<TaskItem> tasks, List<Subject> subjects)
        {
            if (!subjects.Any()) return;
            var now = DateTime.Now;
            var panel = new StackPanel();

            foreach (var s in subjects)
            {
                var st = tasks.Where(t => t.SubjectId == s.Id).ToList();
                int done = st.Count(t => t.IsCompleted);
                int overdue = st.Count(t => t.Deadline < now && !t.IsCompleted);
                int active  = st.Count(t => !t.IsCompleted && !(t.Deadline < now));
                double totalH = st.Sum(t => t.EstimatedHours);
                double doneH  = st.Sum(t => t.EstimatedHours * t.Progress / 100.0);
                double pct    = totalH == 0 ? 0 : doneH / totalH * 100;

                var col = (Color)ColorConverter.ConvertFromString(s.Color);
                var brush = new SolidColorBrush(col);

                var headerGrid = new Grid();
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                var colorBar = new Border { Width = 5, Background = brush, CornerRadius = new CornerRadius(3), Margin = new Thickness(0,2,8,2), VerticalAlignment = VerticalAlignment.Stretch };
                Grid.SetColumn(colorBar, 0);

                var nameBlock = new TextBlock { Text = s.Name, FontSize = 14, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(nameBlock, 1);

                var summaryBlock = new TextBlock { Text = $"{st.Count} задач · {totalH:F0} г.", FontSize = 12, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,8,0) };
                Grid.SetColumn(summaryBlock, 2);

                var miniProgress = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                miniProgress.Children.Add(new System.Windows.Controls.ProgressBar { Value = pct, Maximum = 100, Height = 6, Width = 70, Foreground = brush, Background = new SolidColorBrush(Color.FromArgb(40, col.R, col.G, col.B)), BorderThickness = new Thickness(0) });
                miniProgress.Children.Add(new TextBlock { Text = $"{pct:F0}%", FontSize = 10, Foreground = Brushes.Gray, HorizontalAlignment = System.Windows.HorizontalAlignment.Center });
                Grid.SetColumn(miniProgress, 3);

                headerGrid.Children.Add(colorBar);
                headerGrid.Children.Add(nameBlock);
                headerGrid.Children.Add(summaryBlock);
                headerGrid.Children.Add(miniProgress);

                var detailPanel = new StackPanel { Margin = new Thickness(16,4,4,8) };
                int maxCount = Math.Max(st.Count, 1);

                void AddBar(string barLabel, int count, string hex2)
                {
                    if (count == 0) return;
                    var row = new Grid { Margin = new Thickness(0,3,0,0) };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
                    var lbl = new TextBlock { Text = barLabel, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
                    Grid.SetColumn(lbl, 0);
                    var barBorder = new Border { Height = 12, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex2)), CornerRadius = new CornerRadius(3), HorizontalAlignment = System.Windows.HorizontalAlignment.Left, Width = Math.Max(4, (double)count / maxCount * 200) };
                    Grid.SetColumn(barBorder, 1);
                    var cnt = new TextBlock { Text = count.ToString(), FontSize = 12, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                    Grid.SetColumn(cnt, 2);
                    row.Children.Add(lbl); row.Children.Add(barBorder); row.Children.Add(cnt);
                    detailPanel.Children.Add(row);
                }
                AddBar("Виконано",    done,    "#22A06B");
                AddBar("Прострочено", overdue, "#E2483D");
                AddBar("Активні",     active,  "#534AB7");

                if (st.Any())
                {
                    detailPanel.Children.Add(new Separator { Margin = new Thickness(0,6,0,4) });
                    foreach (var t in st.OrderByDescending(x => x.Priority).Take(5))
                    {
                        var taskRow = new Grid { Margin = new Thickness(0,2,0,0) };
                        taskRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        taskRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var tName = new TextBlock
                        {
                            Text = t.Name ?? "", FontSize = 12,
                            Foreground = t.IsCompleted ? Brushes.Gray : Brushes.Black,
                            TextDecorations = t.IsCompleted ? TextDecorations.Strikethrough : null,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(tName, 0);

                        var meta = new TextBlock { FontSize = 11, Margin = new Thickness(6,0,0,0), VerticalAlignment = VerticalAlignment.Center };
                        meta.Inlines.Add(new Run($"{t.EstimatedHours:F1} г.") { Foreground = Brushes.Gray });
                        if (t.Deadline.HasValue)
                        {
                            bool isOv = t.Deadline.Value < now && !t.IsCompleted;
                            meta.Inlines.Add(new Run("  "));
                            meta.Inlines.Add(new Run(t.Deadline.Value.ToString("dd.MM.yy"))
                            {
                                Foreground = isOv ? new SolidColorBrush(Color.FromRgb(226,72,61)) : Brushes.Gray
                            });
                        }
                        Grid.SetColumn(meta, 1);
                        taskRow.Children.Add(tName); taskRow.Children.Add(meta);
                        detailPanel.Children.Add(taskRow);
                    }
                    if (st.Count > 5)
                        detailPanel.Children.Add(new TextBlock { Text = $"… та ще {st.Count - 5} завдань", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0,4,0,0) });
                }

                panel.Children.Add(new Expander { Header = headerGrid, Content = detailPanel, IsExpanded = false, Margin = new Thickness(0,0,0,6) });
            }
            SubjectAccordion.Content = panel;
        }
    }
}
