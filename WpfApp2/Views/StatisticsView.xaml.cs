using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            BuildTopTasksChart(tasks);
            // PriorityChart — ВИДАЛЕНО
            BuildDeadlineChart(tasks);
            BuildSubjectAccordion(tasks, subjects);
            BuildChangelogTable();
        }

        // ─── KPI ──────────────────────────────────────────────────────────────
        private void BuildKpiCards(ProductivityReport r, List<TaskItem> tasks)
        {
            SetCard(CardTotal,        "Всього задач",      r.TotalTasks.ToString(),            "#534AB7");
            SetCard(CardDone,         "Виконано",          r.CompletedTasks.ToString(),         "#22A06B");
            SetCard(CardOverdue,      "Прострочено",       r.OverdueTasks.ToString(),           "#E2483D");
            SetCard(CardProductivity, "Продуктивність",    $"{r.ProductivityPercent:F1}%",      "#0065FF");

            double todayHours = PlannerService.GetTodayHours(tasks);
            SetCard(CardTotalHours,  "Загальний обсяг",    $"{r.TotalHours:F1} год",            "#7B68EE");
            SetCard(CardDoneHours,   "Виконано годин",     $"{r.CompletedHours:F1} год",        "#22A06B");
            SetCard(CardAvgProgress, "Сер. прогрес",       $"{r.AverageProgress:F0}%",          "#FF8C00");
            SetCard(CardTodayHours,  "Сьогодні (год)",
                todayHours > PlannerService.DailyLimit
                    ? $"⚠ {todayHours:F1}"
                    : $"{todayHours:F1}",
                todayHours > PlannerService.DailyLimit ? "#E2483D" : "#0065FF");
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

        // ─── Задачі за типом ─────────────────────────────────────────────────
        private void BuildByTypeChart(List<TaskItem> tasks)
        {
            var groups = tasks
                .GroupBy(t => string.IsNullOrEmpty(t.TaskType) ? "Інше" : t.TaskType)
                .OrderByDescending(g => g.Count())
                .Take(8)
                .ToList();

            ByTypeChart.Series = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Values = groups.Select(g => g.Count()).ToArray(),
                    Fill   = new SolidColorPaint(SKColor.Parse("#534AB7")),
                    Stroke = null,
                    MaxBarWidth = 40,
                    DataLabelsPaint    = new SolidColorPaint(SKColor.Parse("#534AB7")),
                    DataLabelsSize     = 11,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                    DataLabelsFormatter = p => p.Coordinate.PrimaryValue.ToString()
                }
            };

            var labels = groups.Select(g =>
            {
                var n = g.Key;
                return n.Length > 12 ? n[..11] + "…" : n;
            }).ToArray();

            ByTypeChart.XAxes = new[] { new Axis { Labels = labels, LabelsRotation = -35, TextSize = 11, MinStep = 1 } };
            ByTypeChart.YAxes = new[] { new Axis { MinLimit = 0, MinStep = 1, TextSize = 11 } };
            ByTypeChart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Hidden;
        }

        // ─── Кругова: статус ─────────────────────────────────────────────────
        private void BuildStatusPie(ProductivityReport r)
        {
            int active = Math.Max(0, r.TotalTasks - r.CompletedTasks - r.OverdueTasks);
            StatusPie.Series = new ISeries[]
            {
                new PieSeries<int> { Name = "Виконано",    Values = new[] { r.CompletedTasks },
                    Fill = new SolidColorPaint(SKColor.Parse("#22A06B")) },
                new PieSeries<int> { Name = "Активні",     Values = new[] { active },
                    Fill = new SolidColorPaint(SKColor.Parse("#534AB7")) },
                new PieSeries<int> { Name = "Прострочені", Values = new[] { r.OverdueTasks },
                    Fill = new SolidColorPaint(SKColor.Parse("#E2483D")) },
            };

            var legend = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };

            void AddDot(string text, string hex)
            {
                var col = (Color)ColorConverter.ConvertFromString(hex);
                legend.Children.Add(new Border
                {
                    Width = 10, Height = 10, CornerRadius = new CornerRadius(5),
                    Background = new SolidColorBrush(col), Margin = new Thickness(0,0,4,0),
                    VerticalAlignment = VerticalAlignment.Center
                });
                legend.Children.Add(new TextBlock
                {
                    Text = text, FontSize = 11, Foreground = Brushes.Gray,
                    Margin = new Thickness(0,0,14,0), VerticalAlignment = VerticalAlignment.Center
                });
            }
            AddDot($"Виконано ({r.CompletedTasks})", "#22A06B");
            AddDot($"Активні ({active})",            "#534AB7");
            AddDot($"Прострочені ({r.OverdueTasks})", "#E2483D");
            LegendPanel.Content = legend;
        }

        // ─── Навантаження по тижнях ──────────────────────────────────────────
        private void BuildWeekLoadChart(List<TaskItem> tasks)
        {
            var today  = DateTime.Today;
            var labels = new List<string>();
            var hours  = new List<double>();
            var doneH  = new List<double>();

            for (int w = -3; w <= 4; w++)
            {
                int dow = (int)today.DayOfWeek;
                if (dow == 0) dow = 7;
                var ws = today.AddDays(-(dow - 1) + w * 7);
                var we = ws.AddDays(7);
                labels.Add(ws.ToString("dd.MM"));

                var week = tasks.Where(t =>
                    t.Deadline.HasValue &&
                    t.Deadline.Value.Date >= ws &&
                    t.Deadline.Value.Date < we).ToList();

                hours.Add(Math.Round(week.Sum(t => t.EstimatedHours), 1));
                doneH.Add(Math.Round(week.Sum(t => t.EstimatedHours * t.Progress / 100.0), 1));
            }

            WeekLoadChart.Series = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name   = "Заплановано",
                    Values = hours,
                    Fill   = new SolidColorPaint(SKColor.Parse("#534AB720")),
                    Stroke = new SolidColorPaint(SKColor.Parse("#534AB7")) { StrokeThickness = 2 },
                    GeometrySize = 8
                },
                new LineSeries<double>
                {
                    Name   = "Виконано",
                    Values = doneH,
                    Fill   = new SolidColorPaint(SKColor.Parse("#22A06B20")),
                    Stroke = new SolidColorPaint(SKColor.Parse("#22A06B")) { StrokeThickness = 2 },
                    GeometrySize = 6
                }
            };
            WeekLoadChart.XAxes = new[] { new Axis { Labels = labels, TextSize = 10 } };
            WeekLoadChart.YAxes = new[] { new Axis { MinLimit = 0, TextSize = 10 } };
            WeekLoadChart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Bottom;
        }

        // ─── Топ задач — горизонтальні бари + WPF легенда зверху ────────────
        private void BuildTopTasksChart(List<TaskItem> tasks)
        {
            var top = tasks.Where(t => !t.IsSplit)
                           .OrderByDescending(t => t.EstimatedHours)
                           .Take(8).ToList();

            var plannedValues = top.Select(t => t.EstimatedHours).ToArray();
            var doneValues    = top.Select(t => Math.Round(t.EstimatedHours * t.Progress / 100.0, 1)).ToArray();

            TopTasksChart.Series = new ISeries[]
            {
                new RowSeries<double>
                {
                    Name   = "Заплановано",
                    Values = plannedValues,
                    Fill   = new SolidColorPaint(SKColor.Parse("#534AB780")),
                    Stroke = null,
                    MaxBarWidth = 18,
                    DataLabelsPaint    = new SolidColorPaint(SKColor.Parse("#534AB7")),
                    DataLabelsSize     = 10,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                    DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue:F1} г."
                },
                new RowSeries<double>
                {
                    Name   = "Виконано",
                    Values = doneValues,
                    Fill   = new SolidColorPaint(SKColor.Parse("#22A06B")),
                    Stroke = null,
                    MaxBarWidth = 18,
                    DataLabelsPaint    = new SolidColorPaint(SKColor.Parse("#22A06B")),
                    DataLabelsSize     = 10,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                    DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue:F1} г."
                }
            };

            TopTasksChart.YAxes = new[] { new Axis
            {
                Labels = top.Select(t =>
                {
                    var n = t.Name ?? "";
                    return n.Length > 22 ? n[..21] + "…" : n;
                }).ToArray(),
                TextSize = 11
            }};
            TopTasksChart.XAxes = new[] { new Axis { MinLimit = 0, TextSize = 11 } };

            // Легенда — власна WPF (не LiveCharts) для читабельності
            TopTasksChart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Hidden;

            var legendPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 4)
            };

            void AddLegendItem(string label, string hex)
            {
                var col = (Color)ColorConverter.ConvertFromString(hex);
                legendPanel.Children.Add(new Border
                {
                    Width = 14, Height = 14, CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush(col),
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
                legendPanel.Children.Add(new TextBlock
                {
                    Text = label, FontSize = 12,
                    Foreground = Brushes.DimGray,
                    Margin = new Thickness(0, 0, 16, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            AddLegendItem("Заплановано", "#534AB780");
            AddLegendItem("Виконано",    "#22A06B");
            TopTasksLegend.Content = legendPanel;
        }

        // ─── Навантаження на 14 днів ─────────────────────────────────────────
        private void BuildDeadlineChart(List<TaskItem> tasks)
        {
            var today  = DateTime.Today;
            var labels = new List<string>();
            var values = new List<double>();

            for (int d = 0; d < 14; d++)
            {
                var date = today.AddDays(d);
                labels.Add(date.ToString("dd.MM"));

                double h = 0;
                foreach (var t in tasks)
                {
                    if (t.IsCompleted) continue;
                    if (t.IsSplit)
                        h += t.SubTasks
                            .Where(s => s.Deadline.HasValue && s.Deadline.Value.Date == date && !s.IsChecked)
                            .Sum(s => s.EstimatedHours);
                    else if (t.Deadline.HasValue && t.Deadline.Value.Date == date)
                        h += t.EstimatedHours * (1 - t.Progress / 100.0);
                }
                values.Add(Math.Round(h, 1));
            }

            var safeIdx = new List<double?>();
            var warnIdx = new List<double?>();
            var overIdx = new List<double?>();

            for (int i = 0; i < values.Count; i++)
            {
                double v = values[i];
                if (v > PlannerService.DailyLimit)
                { safeIdx.Add(null); warnIdx.Add(null); overIdx.Add(v); }
                else if (v > PlannerService.DailyLimit * 0.75)
                { safeIdx.Add(null); warnIdx.Add(v); overIdx.Add(null); }
                else
                { safeIdx.Add(v); warnIdx.Add(null); overIdx.Add(null); }
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

        // ─── Статистика по предметах ─────────────────────────────────────────
        private void BuildSubjectAccordion(List<TaskItem> tasks, List<Subject> subjects)
        {
            if (!subjects.Any()) return;
            var now   = DateTime.Now;
            var panel = new StackPanel();

            foreach (var s in subjects)
            {
                var st      = tasks.Where(t => t.SubjectId == s.Id).ToList();
                int done    = st.Count(t => t.IsCompleted);
                int overdue = st.Count(t => t.Deadline < now && !t.IsCompleted);
                int active  = st.Count(t => !t.IsCompleted && !(t.Deadline < now));
                double totalH = st.Sum(t => t.EstimatedHours);
                double doneH  = st.Sum(t => t.EstimatedHours * t.Progress / 100.0);
                double pct    = totalH == 0 ? 0 : doneH / totalH * 100;

                var col   = (Color)ColorConverter.ConvertFromString(s.Color);
                var brush = new SolidColorBrush(col);

                var headerGrid = new Grid();
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                var colorBar = new Border
                {
                    Width = 6, Background = brush, CornerRadius = new CornerRadius(3),
                    Margin = new Thickness(0,2,10,2),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment   = VerticalAlignment.Stretch
                };
                Grid.SetColumn(colorBar, 0);

                var nameBlock = new TextBlock
                {
                    Text = s.Name, FontSize = 14, FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(nameBlock, 1);

                // Summary: total hours with "г."
                var summaryBlock = new TextBlock
                {
                    FontSize = 12, Foreground = Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,8,0)
                };
                summaryBlock.Inlines.Add(new System.Windows.Documents.Run($"{st.Count} задач · {totalH:F0} г."));
                Grid.SetColumn(summaryBlock, 2);

                var miniProgress = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                miniProgress.Children.Add(new System.Windows.Controls.ProgressBar
                {
                    Value = pct, Maximum = 100, Height = 6, Width = 70,
                    Foreground = brush,
                    Background = new SolidColorBrush(Color.FromArgb(40, col.R, col.G, col.B)),
                    BorderThickness = new Thickness(0)
                });
                miniProgress.Children.Add(new TextBlock
                {
                    Text = $"{pct:F0}%", FontSize = 10, Foreground = Brushes.Gray,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                });
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

                    var barBorder = new Border
                    {
                        Height = 12,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex2)),
                        CornerRadius = new CornerRadius(3),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                        Width = Math.Max(4, (double)count / maxCount * 200)
                    };
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
                        // Завдання: назва займає більшу частину, години і дедлайн — компактно поруч
                        var taskRow = new Grid { Margin = new Thickness(0,2,0,0) };
                        taskRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        // Hours + deadline side by side, closer to name
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

                        // Inline hours + deadline, right after name
                        var metaBlock = new TextBlock
                        {
                            FontSize = 11, Foreground = Brushes.Gray,
                            Margin = new Thickness(8, 0, 0, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        };

                        // Hours with "г."
                        metaBlock.Inlines.Add(new System.Windows.Documents.Run($"{t.EstimatedHours:F1} г."));

                        if (t.Deadline.HasValue)
                        {
                            bool isOverdue = t.Deadline.Value < DateTime.Now && !t.IsCompleted;
                            metaBlock.Inlines.Add(new System.Windows.Documents.Run("  "));
                            metaBlock.Inlines.Add(new System.Windows.Documents.Run(t.Deadline.Value.ToString("dd.MM.yyyy"))
                            {
                                Foreground = isOverdue
                                    ? new SolidColorBrush(Color.FromRgb(226, 72, 61))
                                    : Brushes.Gray
                            });
                        }
                        else
                        {
                            metaBlock.Inlines.Add(new System.Windows.Documents.Run("  —"));
                        }

                        Grid.SetColumn(metaBlock, 1);
                        taskRow.Children.Add(tName);
                        taskRow.Children.Add(metaBlock);
                        detailPanel.Children.Add(taskRow);
                    }

                    if (st.Count > 5)
                        detailPanel.Children.Add(new TextBlock
                        {
                            Text = $"… та ще {st.Count - 5} завдань",
                            FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0,4,0,0)
                        });
                }

                var expander = new Expander
                {
                    Header = headerGrid, Content = detailPanel, IsExpanded = false,
                    Margin = new Thickness(0,0,0,6)
                };
                panel.Children.Add(expander);
            }

            SubjectAccordion.Content = panel;
        }

        // ─── Журнал змін ─────────────────────────────────────────────────────
        private void BuildChangelogTable()
        {
            try
            {
                var entries = DataStorageService.LoadJournalFromFile()
                    .OrderByDescending(e => e.Timestamp)
                    .Take(30)
                    .ToList();

                if (!entries.Any())
                    entries = DataStorageService.Journal
                        .OrderByDescending(e => e.Timestamp)
                        .Take(30)
                        .ToList();
            }
            catch { }
        }
    }
}
