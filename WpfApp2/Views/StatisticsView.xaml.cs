using System.Linq;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using WpfApp2.Services;

namespace WpfApp2.Views
{
    public partial class StatisticsView : UserControl
    {
        public StatisticsView(TaskManager manager)
        {
            InitializeComponent();

            var report = ProductivityService.Analyze(manager.Tasks.ToList());

            SummaryText.Text =
                $"Всього завдань: {report.TotalTasks}\n" +
                $"Виконано: {report.CompletedTasks}\n" +
                $"Прострочені: {report.OverdueTasks}\n" +
                $"Середній прогрес: {report.AverageProgress:F1}%\n" +
                $"Продуктивність: {report.ProductivityPercent:F1}%";

            TasksChart.Series = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Values = new[]
                    {
                        report.CompletedTasks,
                        report.TotalTasks - report.CompletedTasks,
                        report.OverdueTasks
                    }
                }
            };

            TasksChart.XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = new[] { "Виконані", "Активні", "Прострочені" }
                }
            };
        }
    }
}