using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GW2RTTFTimer;

public partial class MainWindow : Window
{
    private static readonly TimeSpan ReadyAutoHideDelay = TimeSpan.FromSeconds(10);

    private readonly RttfTimerState _timer = new(RttfTimerState.DefaultDuration);
    private readonly DispatcherTimer _uiTimer;
    private readonly DispatcherTimer _hotkeyTimer;
    private readonly Dictionary<int, bool> _keyHeld = new();
    private readonly Dictionary<int, DateTime> _lastHotkeyUtc = new();

    private bool _readySoundPlayed;

    public MainWindow()
    {
        InitializeComponent();

        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _uiTimer.Tick += (_, _) => RefreshOverlay();

        _hotkeyTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _hotkeyTimer.Tick += (_, _) => PollHotkeys();

        Loaded += MainWindow_Loaded;
        Closed += (_, _) =>
        {
            _uiTimer.Stop();
            _hotkeyTimer.Stop();
        };
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        InitHotkeys();
        PlaceLowerRight();
        RefreshOverlay();
        _uiTimer.Start();
        _hotkeyTimer.Start();
    }

    private void InitHotkeys()
    {
        foreach (int vk in new[] { Win32Interop.VkF8, Win32Interop.VkF11 })
        {
            _keyHeld[vk] = false;
            _lastHotkeyUtc[vk] = DateTime.MinValue;
        }
    }

    private void PollHotkeys()
    {
        TryHotkey(Win32Interop.VkF8, TimeSpan.FromMilliseconds(250), StartOrResetTimer);
        TryHotkey(Win32Interop.VkF11, TimeSpan.FromMilliseconds(250), ToggleOverlayVisible);
    }

    private void TryHotkey(int vk, TimeSpan cooldown, Action handler)
    {
        short keyState = Win32Interop.GetAsyncKeyState(vk);
        bool down = (keyState & 0x8000) != 0;
        bool wasDown = _keyHeld.GetValueOrDefault(vk);

        if (down && !wasDown)
        {
            DateTime now = DateTime.UtcNow;
            if (now - _lastHotkeyUtc.GetValueOrDefault(vk, DateTime.MinValue) >= cooldown)
            {
                _lastHotkeyUtc[vk] = now;
                handler();
            }
        }

        _keyHeld[vk] = down;
    }

    private void StartOrResetTimer()
    {
        _timer.StartOrReset(DateTime.UtcNow);
        _readySoundPlayed = false;
        ShowOverlay();
        RefreshOverlay();
    }

    private void ToggleOverlayVisible()
    {
        if (IsVisible)
            Hide();
        else
            ShowOverlay();
    }

    private void ShowOverlay()
    {
        if (!IsVisible)
            Show();

        WindowState = WindowState.Normal;
        PlaceLowerRight();
        Topmost = false;
        Topmost = true;
    }

    private void RefreshOverlay()
    {
        DateTime now = DateTime.UtcNow;
        bool becameReady = _timer.Tick(now);

        if (becameReady && !_readySoundPlayed)
        {
            _readySoundPlayed = true;
            System.Media.SystemSounds.Asterisk.Play();
        }

        if (_timer.IsReady
            && _timer.ReadySinceUtc is { } readySince
            && now - readySince >= ReadyAutoHideDelay)
        {
            _timer.Hide();
            Hide();
            return;
        }

        bool finalMinute = _timer.IsFinalMinute(now);
        RootBorder.Background = (Brush)FindResource(_timer.IsReady ? "Brush.SurfaceReady" : "Brush.Surface");
        RootBorder.BorderBrush = (Brush)FindResource(finalMinute || _timer.IsReady ? "Brush.BorderWarn" : "Brush.Border");
        RootBorder.BorderThickness = new Thickness(finalMinute || _timer.IsReady ? 4 : 3);
        RootBorder.Opacity = finalMinute
            ? 0.94 + (Math.Sin(Environment.TickCount64 / 260.0) * 0.04)
            : 1;

        TimerText.Text = _timer.DisplayText(now);
        TimerText.FontSize = _timer.IsReady ? 34 : 39;
        HintText.Text = _timer.IsReady
            ? "Ready   F8 restart"
            : _timer.IsRunning
                ? "F8 reset   F11 hide"
                : "F8 start   F11 hide";
        UpdateProgressBar(now);
    }

    private void UpdateProgressBar(DateTime nowUtc)
    {
        double width = ProgressTrack.ActualWidth * _timer.Progress(nowUtc);
        ProgressFill.Width = double.IsFinite(width) ? Math.Max(0, width) : 0;
    }

    private void PlaceLowerRight()
    {
        const double margin = 28;
        double left;
        double top;
        DpiScale dpi = VisualTreeHelper.GetDpi(this);

        if (Win32Interop.FindGw2ClientScreenRect() is { } gw2Rect)
        {
            left = (gw2Rect.Left / dpi.DpiScaleX) + (gw2Rect.Width / dpi.DpiScaleX) - Width - margin;
            top = (gw2Rect.Top / dpi.DpiScaleY) + (gw2Rect.Height / dpi.DpiScaleY) - Height - margin;
        }
        else
        {
            Rect workArea = SystemParameters.WorkArea;
            left = workArea.Right - Width - margin;
            top = workArea.Bottom - Height - margin;
        }

        Left = Math.Max(0, left);
        Top = Math.Max(0, top);
    }

    private void ProgressTrack_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateProgressBar(DateTime.UtcNow);
    }

    private void RootBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void QuitButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}