using System;
using System.Windows;
using System.Windows.Controls;
using WpfApp2.Services;

namespace WpfApp2.Views
{
    public partial class PomodoroWidget : UserControl
    {
        public static readonly PomodoroService Pomodoro = new();
        private int _totalSeconds;

        public PomodoroWidget()
        {
            InitializeComponent();
            Pomodoro.Tick += UpdateUI;
            Pomodoro.PhaseChanged += OnPhaseChanged;
            _totalSeconds = (int)TimeSpan.FromMinutes(Pomodoro.WorkMinutes).TotalSeconds;
            UpdateUI();
        }

        private void UpdateUI()
        {
            var r = Pomodoro.Remaining;
            TimerLabel.Text = $"{(int)r.TotalMinutes:D2}:{r.Seconds:D2}";
            TimerBar.Maximum = _totalSeconds;
            TimerBar.Value = _totalSeconds - (int)r.TotalSeconds;
            CountLabel.Text = $"Циклів: {Pomodoro.Completed}";
            PhaseLabel.Text = Pomodoro.Phase switch
            {
                PomodoroPhase.Work => "Робота",
                PomodoroPhase.ShortBreak => "Коротка пауза",
                _ => "Довга пауза"
            };
        }

        private void OnPhaseChanged(PomodoroPhase phase)
        {
            _totalSeconds = (int)TimeSpan.FromMinutes(
                phase == PomodoroPhase.Work ? Pomodoro.WorkMinutes :
                phase == PomodoroPhase.ShortBreak ? Pomodoro.ShortBreakMinutes :
                                                    Pomodoro.LongBreakMinutes).TotalSeconds;

            string msg = phase switch
            {
                PomodoroPhase.ShortBreak => "☕ Час зробити перерву 5 хв!",
                PomodoroPhase.LongBreak => "🛋 Довга перерва — відпочиньте 15 хв!",
                _ => "🍅 Час повертатися до роботи!"
            };

            string title = phase switch
            {
                PomodoroPhase.ShortBreak => "Час перерви",
                PomodoroPhase.LongBreak => "Довга перерва",
                _ => "Повернення до роботи"
            };

            MainWindow.Tray?.Notify(title, msg);

            UpdateUI();
            Pomodoro.Start();
        }

        private void Start_Click(object s, RoutedEventArgs e) => Pomodoro.Start();
        private void Pause_Click(object s, RoutedEventArgs e) => Pomodoro.Pause();
        private void Reset_Click(object s, RoutedEventArgs e) { Pomodoro.Reset(); UpdateUI(); }
        private void Skip_Click(object s, RoutedEventArgs e) => Pomodoro.Skip();
    }
}