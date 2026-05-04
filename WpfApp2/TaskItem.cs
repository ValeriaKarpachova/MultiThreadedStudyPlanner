using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

public class TaskItem : INotifyPropertyChanged
{
    private string? name;
    private string? description;
    private DateTime? deadline;
    private TimeSpan? deadlineTime;   // null = час не вказано
    private string? taskType;
    private int priority;
    private double estimatedHours;
    private bool isChecked;
    private string _subjectColor = "Transparent";

    public int Id { get; set; }
    public int? ParentId { get; set; }

    public string? Name
    {
        get => name;
        set { name = value; OnPropertyChanged(nameof(Name)); }
    }

    public string? Description
    {
        get => description;
        set { description = value; OnPropertyChanged(nameof(Description)); }
    }

    /// <summary>Дата дедлайну (без часу).</summary>
    public DateTime? Deadline
    {
        get => deadline;
        set
        {
            deadline = value?.Date;
            OnPropertyChanged(nameof(Deadline));
            OnPropertyChanged(nameof(DeadlineDateTime));
            OnPropertyChanged(nameof(DeadlineDisplay));
            OnPropertyChanged(nameof(IsOverdue));
        }
    }

    /// <summary>
    /// Необов'язковий час здачі. Якщо null — час не вказано,
    /// дедлайн вважається кінцем дня (23:59:59).
    /// </summary>
    public TimeSpan? DeadlineTime
    {
        get => deadlineTime;
        set
        {
            deadlineTime = value;
            OnPropertyChanged(nameof(DeadlineTime));
            OnPropertyChanged(nameof(DeadlineDateTime));
            OnPropertyChanged(nameof(DeadlineDisplay));
            OnPropertyChanged(nameof(DeadlineTimeDisplay));
            OnPropertyChanged(nameof(IsOverdue));
        }
    }

    /// <summary>
    /// Точний момент дедлайну: дата + час (або 23:59:59 якщо час не задано).
    /// Якщо дата не вказана — null.
    /// </summary>
    public DateTime? DeadlineDateTime
    {
        get
        {
            if (!Deadline.HasValue) return null;
            return DeadlineTime.HasValue
                ? Deadline.Value.Add(DeadlineTime.Value)
                : Deadline.Value.AddDays(1).AddSeconds(-1); // кінець дня
        }
    }

    /// <summary>Рядок для відображення: "дд.мм.рррр ГГ:хх" або "дд.мм.рррр".</summary>
    public string DeadlineDisplay
    {
        get
        {
            if (!Deadline.HasValue) return "—";
            return DeadlineTime.HasValue
                ? $"{Deadline.Value:dd.MM.yyyy} {DeadlineTime.Value:hh\\:mm}"
                : $"{Deadline.Value:dd.MM.yyyy}";
        }
    }

    /// <summary>Рядок часу для UI: "ГГ:хх" або "" якщо не вказано.</summary>
    public string DeadlineTimeDisplay =>
        DeadlineTime.HasValue ? DeadlineTime.Value.ToString(@"hh\:mm") : "";

    /// <summary>
    /// Рядок для серіалізації в БД: "ГГ:хх" або пусто.
    /// </summary>
    public string DeadlineTimeString
    {
        get => DeadlineTime.HasValue
            ? DeadlineTime.Value.ToString(@"hh\:mm")
            : string.Empty;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                DeadlineTime = null;
            else if (TimeSpan.TryParseExact(value, @"hh\:mm",
                         System.Globalization.CultureInfo.InvariantCulture, out var ts))
                DeadlineTime = ts;
            else
                DeadlineTime = null;
        }
    }

    public string? TaskType
    {
        get => taskType;
        set { taskType = value; OnPropertyChanged(nameof(TaskType)); }
    }

    public int Priority
    {
        get => priority;
        set { priority = value; OnPropertyChanged(nameof(Priority)); }
    }

    public double EstimatedHours
    {
        get => estimatedHours;
        set { estimatedHours = value; OnPropertyChanged(nameof(EstimatedHours)); }
    }

    public bool IsChecked
    {
        get => isChecked;
        set
        {
            if (isChecked != value)
            {
                isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
                if (!IsSplit)
                {
                    OnPropertyChanged(nameof(Progress));
                    OnPropertyChanged(nameof(IsCompleted));
                    OnPropertyChanged(nameof(RemainingHours));
                    OnPropertyChanged(nameof(IsOverdue));
                }
            }
        }
    }

    public ObservableCollection<TaskItem> SubTasks { get; set; } = new();

    public bool IsSplit => SubTasks.Any();

    public int Progress
    {
        get
        {
            if (IsSplit)
            {
                double totalHours = SubTasks.Sum(s => s.EstimatedHours);
                if (totalHours == 0) return 0;
                double done = SubTasks
                    .Where(s => s.IsChecked)
                    .Sum(s => s.EstimatedHours);
                return (int)Math.Round(done / totalHours * 100);
            }
            return IsChecked ? 100 : 0;
        }
    }

    public bool IsCompleted => Progress >= 100;

    /// <summary>
    /// Завдання прострочено якщо:
    ///  - не виконано, І
    ///  - точний момент дедлайну (дата + час) вже минув.
    /// Завдання "на сьогодні, час ще не настав" — НЕ прострочено.
    /// </summary>
    public bool IsOverdue
    {
        get
        {
            if (IsCompleted) return false;
            if (!DeadlineDateTime.HasValue) return false;
            return DateTime.Now > DeadlineDateTime.Value;
        }
    }

    public double RemainingHours => EstimatedHours * (1.0 - Progress / 100.0);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public int? SubjectId { get; set; }

    public void RefreshParent()
    {
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(RemainingHours));
        OnPropertyChanged(nameof(IsOverdue));
    }

    public string SubjectColor
    {
        get => _subjectColor;
        set { _subjectColor = value; OnPropertyChanged(nameof(SubjectColor)); }
    }
}
