using GW2TimerCore;

namespace GW2RTTFTimer;

internal sealed class RttfTimerState
{
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(10);

    private readonly CountdownTimerState _timer;

    public RttfTimerState(TimeSpan duration)
    {
        _timer = new CountdownTimerState(duration);
    }

    public bool IsReady => _timer.IsReady;

    public bool IsRunning => _timer.IsRunning;

    public bool IsFinalMinute(DateTime nowUtc) =>
        _timer.IsInFinalWindow(nowUtc, TimeSpan.FromMinutes(1));

    public DateTime? ReadySinceUtc => _timer.ReadySinceUtc;

    public void StartOrReset(DateTime nowUtc) => _timer.StartOrReset(nowUtc);

    public void Hide() => _timer.ResetToIdle();

    public double Progress(DateTime nowUtc) => _timer.Progress(nowUtc);

    public string DisplayText(DateTime nowUtc) => _timer.DisplayText(nowUtc);

    public bool Tick(DateTime nowUtc) => _timer.Tick(nowUtc);
}
