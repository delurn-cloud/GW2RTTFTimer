namespace GW2TimerCore;

public enum CountdownPhase
{
    Idle,
    Running,
    Ready,
}

public sealed class CountdownTimerState
{
    private DateTime _startedUtc;
    private DateTime? _readySinceUtc;

    public CountdownTimerState(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");

        Duration = duration;
    }

    public TimeSpan Duration { get; }

    public CountdownPhase Phase { get; private set; } = CountdownPhase.Idle;

    public bool IsRunning => Phase == CountdownPhase.Running;

    public bool IsReady => Phase == CountdownPhase.Ready;

    public DateTime? ReadySinceUtc => _readySinceUtc;

    public bool IsInFinalWindow(DateTime nowUtc, TimeSpan finalWindow) =>
        IsRunning && Remaining(nowUtc) <= finalWindow;

    public void StartOrReset(DateTime nowUtc)
    {
        _startedUtc = nowUtc;
        _readySinceUtc = null;
        Phase = CountdownPhase.Running;
    }

    public void ResetToIdle()
    {
        Phase = CountdownPhase.Idle;
        _readySinceUtc = null;
    }

    public TimeSpan Remaining(DateTime nowUtc)
    {
        if (!IsRunning)
            return TimeSpan.Zero;

        TimeSpan remaining = Duration - (nowUtc - _startedUtc);
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public double Progress(DateTime nowUtc)
    {
        if (IsReady || !IsRunning)
            return 1;

        double remaining = Remaining(nowUtc).TotalMilliseconds;
        return Math.Clamp(remaining / Duration.TotalMilliseconds, 0, 1);
    }

    public string DisplayText(DateTime nowUtc)
    {
        if (IsReady)
            return "READY";
        if (!IsRunning)
            return FormatDuration(Duration);

        TimeSpan remaining = Remaining(nowUtc);
        int totalSeconds = (int)Math.Ceiling(remaining.TotalSeconds);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    public bool Tick(DateTime nowUtc)
    {
        if (!IsRunning || Remaining(nowUtc) > TimeSpan.Zero)
            return false;

        Phase = CountdownPhase.Ready;
        _readySinceUtc = nowUtc;
        return true;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        int totalSeconds = (int)Math.Ceiling(duration.TotalSeconds);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }
}