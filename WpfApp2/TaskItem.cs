using System;
using System.ComponentModel;

public class TaskItem : INotifyPropertyChanged
{
    private string? name;
    private string? description;
    private DateTime? deadline;
    private string? taskType;
    private int priority;
    private int progress;
    private double estimatedHours;

    public int Id { get; set; }

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

    public int Progress
    {
        get => progress;
        set
        {
            progress = value;
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(IsCompleted));
        }
    }

    public double EstimatedHours
    {
        get => estimatedHours;
        set
        {
            estimatedHours = value;
            OnPropertyChanged(nameof(EstimatedHours));
        }
    }

    public bool IsCompleted => Progress >= 100;

    public double RemainingHours =>
        EstimatedHours * (1.0 - Progress / 100.0);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}