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
            var tasks = manager.Tasks.ToList();
            var report = ProductivityService.Analyze(tasks);
            var subjects = new SubjectService().GetAll();

            BuildKpiCards(report);
            BuildByTypeChart(tasks);
            BuildStatusPie(report);
            BuildWeekLoadChart(tasks);
            BuildTopTasksChart(tasks);
            BuildSubjectChart(tasks, subjects);
        }

        private void BuildKpiCards(ProductivityReport r)
        {
            SetCard(CardTotal, "Всього задач", r.TotalTasks.ToString(), "#534AB7");
            SetCard(CardDone, "Виконано", r.CompletedTasks.ToString(), "#22A06B");
            SetCard(CardOverdue, "Прострочено", r.OverdueTasks.ToString(), "#E2483D");
            SetCard(CardProductivity, "Продуктивність", $"{r.ProductivityPercent:F1}%", "#0065FF");
        }

        private static void SetCard(
            System.Windows.Controls.Border b,
            string label, string value, string hex)
        {
            var col = (Color)ColorConverter.ConvertFromString(hex);
            b.Background = new SolidColorBrush(Color.FromArgb(25, col.R, col.G, col.B));
            b.Child = new StackPanel
            {
                Children = {
                    new TextBlock { Text=value, FontSize=28, FontWeight=FontWeights.Bold,
                        Foreground = new SolidColorBrush(col) },
                    new TextBlock { Text=label, FontSize=12,
                        Foreground = Brushes.Gray }
                }
            };
        }

        private void BuildByTypeChart(List<TaskItem> tasks)
        {
            var groups = tasks
                .GroupBy(t => string.IsNullOrEmpty(t.TaskType) ? "Інше" : t.TaskType)
                .OrderByDescending(g => g.Count()).Take(8).ToList();

            ByTypeChart.Series = groups.Select(g => (ISeries)new ColumnSeries<int>
            {
                Name = g.Key,
                Values = new[] { g.Count() }
            }).ToArray();
            ByTypeChart.XAxes = new[] { new Axis { Labels = groups.Select(g => g.Key).ToArray() } };
            ByTypeChart.YAxes = new[] { new Axis { MinLimit = 0 } };
        }

        private void BuildStatusPie(ProductivityReport r)
        {
            int active = Math.Max(0, r.TotalTasks - r.CompletedTasks - r.OverdueTasks);
            StatusPie.Series = new ISeries[] {
                new PieSeries<int> { Name="Виконано",
                    Values=new[]{r.CompletedTasks},
                    Fill=new SolidColorPaint(SKColor.Parse("#22A06B")) },
                new PieSeries<int> { Name="Активні",
                    Values=new[]{active},
                    Fill=new SolidColorPaint(SKColor.Parse("#534AB7")) },
                new PieSeries<int> { Name="Прострочені",
                    Values=new[]{r.OverdueTasks},
                    Fill=new SolidColorPaint(SKColor.Parse("#E2483D")) },
            };
        }

        private void BuildWeekLoadChart(List<TaskItem> tasks)
        {
            var today = DateTime.Today;
            var labels = new List<string>();
            var hours = new List<double>();
            for (int w = -4; w <= 4; w++)
            {
                var ws = today.AddDays(-(int)today.DayOfWeek + 1 + w * 7);
                var we = ws.AddDays(7);
                labels.Add(ws.ToString("dd.MM"));
                hours.Add(Math.Round(tasks
                    .Where(t => t.Deadline.HasValue
                             && t.Deadline.Value.Date >= ws
                             && t.Deadline.Value.Date < we)
                    .Sum(t => t.EstimatedHours), 1));
            }
            WeekLoadChart.Series = new ISeries[] {
                new LineSeries<double> {
                    Name="Годин", Values=hours,
                    Fill  =new SolidColorPaint(SKColor.Parse("#534AB720")),
                    Stroke=new SolidColorPaint(SKColor.Parse("#534AB7")){ StrokeThickness=2 },
                    GeometrySize=8
                }
            };
            WeekLoadChart.XAxes = new[] { new Axis { Labels = labels } };
            WeekLoadChart.YAxes = new[] { new Axis { MinLimit = 0 } };
        }

        private void BuildTopTasksChart(List<TaskItem> tasks)
        {
            var top = tasks.Where(t => !t.IsSplit)
                           .OrderByDescending(t => t.EstimatedHours)
                           .Take(8).ToList();
            TopTasksChart.Series = new ISeries[] {
                new RowSeries<double> {
                    Name="Годин", Values=top.Select(t=>t.EstimatedHours).ToArray()
                }
            };
            TopTasksChart.YAxes = new[] { new Axis { Labels = top.Select(t => t.Name ?? "").ToArray() } };
            TopTasksChart.XAxes = new[] { new Axis { MinLimit = 0 } };
        }

        private void BuildSubjectChart(
            List<TaskItem> tasks, List<Subject> subjects)
        {
            if (!subjects.Any()) return;
            var now = DateTime.Now;

            var done = new List<int>();
            var overdue = new List<int>();
            var active = new List<int>();
            var labels = new List<string>();

            foreach (var s in subjects)
            {
                var st = tasks.Where(t => t.SubjectId == s.Id).ToList();
                labels.Add(s.Name);
                done.Add(st.Count(t => t.IsCompleted));
                overdue.Add(st.Count(t => t.Deadline < now && !t.IsCompleted));
                active.Add(st.Count(t => !t.IsCompleted && !(t.Deadline < now)));
            }

            SubjectChart.Series = new ISeries[] {
                new StackedRowSeries<int> {
                    Name="Виконані", Values=done,
                    Fill=new SolidColorPaint(SKColor.Parse("#22A06B")) },
                new StackedRowSeries<int> {
                    Name="Прострочені", Values=overdue,
                    Fill=new SolidColorPaint(SKColor.Parse("#E2483D")) },
                new StackedRowSeries<int> {
                    Name="Активні", Values=active,
                    Fill=new SolidColorPaint(SKColor.Parse("#534AB7")) },
            };
            SubjectChart.YAxes = new[] { new Axis { Labels = labels } };
            SubjectChart.XAxes = new[] { new Axis { MinLimit = 0 } };
        }
    }
}