using System;
using System.Windows.Threading;

namespace WpfApp2.Services
{
    public enum PomodoroPhase { Work, ShortBreak, LongBreak }

    public class PomodoroService
    {
        public int WorkMinutes { get; set; } = 25;
        public int ShortBreakMinutes { get; set; } = 5;
        public int LongBreakMinutes { get; set; } = 15;
        public int LongBreakAfter { get; set; } = 4;

        public PomodoroPhase Phase { get; private set; } = PomodoroPhase.Work;
        public int Completed { get; private set; }
        public bool IsRunning { get; private set; }
        public TimeSpan Remaining { get; private set; }

        public event Action<PomodoroPhase>? PhaseChanged;
        public event Action? Tick;

        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

        public PomodoroService() { _timer.Tick += OnTick; Reset(); }

        public void Start() { IsRunning = true; _timer.Start(); }
        public void Pause() { IsRunning = false; _timer.Stop(); }
        public void Reset()
        {
            _timer.Stop(); IsRunning = false;
            Remaining = TimeSpan.FromMinutes(WorkMinutes);
            Phase = PomodoroPhase.Work;
        }

        public void Skip()
        {
            _timer.Stop(); IsRunning = false;
            Advance();
        }

        private void OnTick(object? s, EventArgs e)
        {
            Remaining -= TimeSpan.FromSeconds(1);
            Tick?.Invoke();
            if (Remaining <= TimeSpan.Zero) { _timer.Stop(); IsRunning = false; Advance(); }
        }

        private void Advance()
        {
            if (Phase == PomodoroPhase.Work)
            {
                Completed++;
                Phase = (Completed % LongBreakAfter == 0)
                    ? PomodoroPhase.LongBreak
                    : PomodoroPhase.ShortBreak;
            }
            else
            {
                Phase = PomodoroPhase.Work;
            }
            Remaining = TimeSpan.FromMinutes(
                Phase == PomodoroPhase.Work ? WorkMinutes :
                Phase == PomodoroPhase.ShortBreak ? ShortBreakMinutes :
                                                    LongBreakMinutes);
            PhaseChanged?.Invoke(Phase);
        }
    }
}