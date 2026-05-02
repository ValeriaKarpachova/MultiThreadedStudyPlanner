using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

public class TaskItem : INotifyPropertyChanged
{
    private string? name;
    private string? description;
    private DateTime? deadline;
    private string? taskType;
    private int priority;
    private double estimatedHours;
    private bool isChecked;

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

    public DateTime? Deadline
    {
        get => deadline;
        set { deadline = value; OnPropertyChanged(nameof(Deadline)); }
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
                // Для обычных заданий отмечаем как выполненные/невыполненные
                if (!IsSplit)
                {
                    OnPropertyChanged(nameof(Progress));
                    OnPropertyChanged(nameof(IsCompleted));
                    OnPropertyChanged(nameof(RemainingHours));
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
    }
}