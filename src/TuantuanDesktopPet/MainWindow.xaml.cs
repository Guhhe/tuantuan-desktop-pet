using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using TuantuanDesktopPet.Core;

namespace TuantuanDesktopPet;

public partial class MainWindow : Window
{
    private const string ApplicationName = "团团桌宠";
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(33);
    private SpriteAtlas _atlas;
    private PetDescriptor _selectedPet;
    private readonly PetCatalog _petCatalog;
    private readonly PetController _controller = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly AutoStartService _autoStart = new();
    private readonly DesktopBoundsService _desktop = new();
    private readonly FullscreenWatcher _fullscreenWatcher = new();
    private readonly DispatcherTimer _animationTimer;
    private readonly DispatcherTimer _fullscreenTimer;
    private readonly DispatcherTimer _singleClickTimer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly PetSettings _settings;
    private readonly TrayService _tray;
    private nint _hwnd;
    private HwndSource? _source;
    private long _lastTickMilliseconds;
    private SpriteFrame? _displayedFrame;
    private FrameData? _frameData;
    private NativeMethods.Point _mouseDownCursor;
    private NativeMethods.Point _lastDragCursor;
    private NativeMethods.Rect _mouseDownWindow;
    private int _mouseDownClickCount;
    private bool _mouseDown;
    private bool _dragging;
    private bool _resumePausedAfterDrag;
    private bool _fullscreenHidden;
    private bool _exiting;

    public MainWindow()
    {
        InitializeComponent();
        RenderOptions.SetBitmapScalingMode(SpriteImage, BitmapScalingMode.HighQuality);

        _settings = _settingsStore.Load();
        _petCatalog = new PetCatalog();
        _selectedPet = _petCatalog.Find(_settings.SelectedPetId) ?? _petCatalog.BuiltIn;
        try
        {
            _atlas = SpriteAtlas.Load(_petCatalog.Load(_selectedPet), _selectedPet.IsBuiltIn);
        }
        catch (Exception exception) when (!_selectedPet.IsBuiltIn)
        {
            System.Windows.MessageBox.Show(
                $"上次选择的宠物无法加载，已恢复为内置团团。\n\n{exception.Message}",
                ApplicationName,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            _selectedPet = _petCatalog.BuiltIn;
            _settings.SelectedPetId = _selectedPet.Id;
            _atlas = SpriteAtlas.Load(_petCatalog.Load(_selectedPet), isBuiltIn: true);
        }

        Title = ApplicationName;
        Width = AnimationCatalog.CellWidth * _settings.Scale;
        Height = AnimationCatalog.CellHeight * _settings.Scale;
        Topmost = _settings.Topmost;
        _controller.SetWalkingEnabled(_settings.WalkingEnabled);

        _animationTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = FrameInterval
        };
        _animationTimer.Tick += OnAnimationTick;

        _fullscreenTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _fullscreenTimer.Tick += (_, _) => CheckFullscreen();

        _singleClickTimer = new DispatcherTimer(DispatcherPriority.Input)
        {
            Interval = TimeSpan.FromMilliseconds(System.Windows.Forms.SystemInformation.DoubleClickTime)
        };
        _singleClickTimer.Tick += (_, _) =>
        {
            _singleClickTimer.Stop();
            _controller.TriggerClickReaction();
        };

        _tray = new TrayService(
            TogglePaused,
            ToggleTopmost,
            ToggleMouseFollow,
            ToggleWalking,
            SetScale,
            ToggleAutoStart,
            ToggleAutoHideFullscreen,
            ImportPet,
            SelectPet,
            ResetPosition,
            ExitApplication,
            WakeWindow);
        UpdateTray();

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closed += OnClosed;
        PreviewMouseLeftButtonDown += OnLeftButtonDown;
        PreviewMouseMove += OnMouseMove;
        PreviewMouseLeftButtonUp += OnLeftButtonUp;
        PreviewMouseRightButtonUp += (_, eventArgs) =>
        {
            _tray.ShowMenu();
            eventArgs.Handled = true;
        };
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.Loaded, ClampToCurrentMonitor);
    }

    private void OnSourceInitialized(object? sender, EventArgs eventArgs)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WindowProcedure);

        var style = NativeMethods.GetExtendedStyle(_hwnd);
        style |= NativeMethods.WsExToolWindow | NativeMethods.WsExNoActivate;
        NativeMethods.SetExtendedStyle(_hwnd, style);

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, PlaceInitialOrSavedPosition);
    }

    private void OnLoaded(object sender, RoutedEventArgs eventArgs)
    {
        try
        {
            if (_settings.AutoStartEnabled && (!_autoStart.IsCurrentPathRegistered() || _settingsStore.IsFirstRun))
            {
                _autoStart.Apply(true);
            }
        }
        catch (Exception exception)
        {
            _settings.AutoStartEnabled = false;
            System.Windows.MessageBox.Show(
                $"无法设置开机启动，其他功能仍可正常使用。\n\n{exception.Message}",
                "团团桌宠",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        _controller.SetPaused(_settings.Paused);
        if (!_settings.Paused)
        {
            _controller.StartStartupGreeting();
        }

        _lastTickMilliseconds = _clock.ElapsedMilliseconds;
        RenderCurrentFrame();
        _animationTimer.Start();
        _fullscreenTimer.Start();
        SaveSettings();
        UpdateTray();
    }

    private void OnAnimationTick(object? sender, EventArgs eventArgs)
    {
        if (_hwnd == nint.Zero || _fullscreenHidden)
        {
            return;
        }

        var now = _clock.ElapsedMilliseconds;
        var elapsed = Math.Clamp(now - _lastTickMilliseconds, 1, 250);
        _lastTickMilliseconds = now;

        if (!NativeMethods.GetWindowRect(_hwnd, out var window))
        {
            return;
        }

        var monitor = _desktop.GetForWindow(_hwnd);
        var pixelsPerDip = GetPixelsPerDip();
        var availableLeft = (window.Left - monitor.Work.Left) / pixelsPerDip;
        var availableRight = (monitor.Work.Right - window.Right) / pixelsPerDip;
        var tick = _controller.Tick(elapsed, availableLeft, availableRight);

        if (Math.Abs(tick.MoveXDips) > 0.001)
        {
            var nextLeft = window.Left + (int)Math.Round(tick.MoveXDips * pixelsPerDip);
            _ = NativeMethods.SetWindowPos(
                _hwnd,
                nint.Zero,
                nextLeft,
                window.Top,
                0,
                0,
                NativeMethods.SwpMoveOnly);
        }

        RenderCurrentFrame();
    }

    private void RenderCurrentFrame()
    {
        var frame = GetVisualFrame();
        if (_displayedFrame == frame)
        {
            return;
        }

        _frameData = _atlas.GetFrame(frame);
        SpriteImage.Source = _frameData.Image;
        _displayedFrame = frame;
    }

    private SpriteFrame GetVisualFrame()
    {
        if (!_settings.MouseFollowEnabled ||
            _controller.State != PetState.Idle ||
            !NativeMethods.GetCursorPos(out var cursor) ||
            !NativeMethods.GetWindowRect(_hwnd, out var window))
        {
            return _controller.CurrentFrame;
        }

        var headX = window.Left + (window.Width * 0.5);
        var headY = window.Top + (window.Height * 0.36);
        var dx = cursor.X - headX;
        var dy = cursor.Y - headY;
        var pixelsPerDip = GetPixelsPerDip();
        var distance = Math.Sqrt((dx * dx) + (dy * dy));
        if (distance > 640 * pixelsPerDip)
        {
            return _controller.CurrentFrame;
        }

        var direction = DirectionMapper.Map(dx, dy, 24 * pixelsPerDip);
        if (direction is null)
        {
            return _controller.CurrentFrame;
        }

        return new SpriteFrame(direction.Value.Row, direction.Value.Column);
    }

    private void OnLeftButtonDown(object sender, MouseButtonEventArgs eventArgs)
    {
        if (!NativeMethods.GetCursorPos(out _mouseDownCursor) ||
            !NativeMethods.GetWindowRect(_hwnd, out _mouseDownWindow) ||
            !IsVisiblePixel(_mouseDownCursor, _mouseDownWindow))
        {
            return;
        }

        _mouseDown = true;
        _dragging = false;
        _mouseDownClickCount = eventArgs.ClickCount;
        if (_mouseDownClickCount >= 2)
        {
            _singleClickTimer.Stop();
        }
        _ = Mouse.Capture(this);
        eventArgs.Handled = true;
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs eventArgs)
    {
        if (!_mouseDown || eventArgs.LeftButton != MouseButtonState.Pressed ||
            !NativeMethods.GetCursorPos(out var cursor))
        {
            return;
        }

        var pixelsPerDip = GetPixelsPerDip();
        var deltaX = cursor.X - _mouseDownCursor.X;
        var deltaY = cursor.Y - _mouseDownCursor.Y;
        if (!_dragging &&
            (Math.Abs(deltaX) >= SystemParameters.MinimumHorizontalDragDistance * pixelsPerDip ||
             Math.Abs(deltaY) >= SystemParameters.MinimumVerticalDragDistance * pixelsPerDip))
        {
            _dragging = true;
            _singleClickTimer.Stop();
            _lastDragCursor = _mouseDownCursor;
            _resumePausedAfterDrag = _settings.Paused;
            if (_resumePausedAfterDrag)
            {
                _controller.SetPaused(false);
            }
            _controller.BeginDrag();
        }

        if (_dragging)
        {
            _controller.UpdateDragDirection((cursor.X - _lastDragCursor.X) / pixelsPerDip);
            _lastDragCursor = cursor;
            _ = NativeMethods.SetWindowPos(
                _hwnd,
                nint.Zero,
                _mouseDownWindow.Left + deltaX,
                _mouseDownWindow.Top + deltaY,
                0,
                0,
                NativeMethods.SwpMoveOnly);
        }
    }

    private void OnLeftButtonUp(object sender, MouseButtonEventArgs eventArgs)
    {
        if (!_mouseDown)
        {
            return;
        }

        _mouseDown = false;
        Mouse.Capture(null);
        if (_dragging)
        {
            _dragging = false;
            _controller.EndDrag();
            if (_resumePausedAfterDrag)
            {
                _controller.SetPaused(true);
                _resumePausedAfterDrag = false;
            }
            ClampToCurrentMonitor();
            SaveCurrentPosition();
            SaveSettings();
        }
        else if (_mouseDownClickCount >= 2)
        {
            _singleClickTimer.Stop();
            _controller.TriggerJump();
        }
        else
        {
            _singleClickTimer.Stop();
            _singleClickTimer.Start();
        }

        eventArgs.Handled = true;
    }

    private bool IsVisiblePixel(NativeMethods.Point cursor, NativeMethods.Rect window)
    {
        if (_frameData is null ||
            window.Width <= 0 ||
            window.Height <= 0)
        {
            return false;
        }

        var localX = cursor.X - window.Left;
        var localY = cursor.Y - window.Top;
        if (localX < 0 || localY < 0 || localX >= window.Width || localY >= window.Height)
        {
            return false;
        }

        var sourceX = Math.Clamp(
            localX * AnimationCatalog.CellWidth / window.Width,
            0,
            AnimationCatalog.CellWidth - 1);
        var sourceY = Math.Clamp(
            localY * AnimationCatalog.CellHeight / window.Height,
            0,
            AnimationCatalog.CellHeight - 1);
        return _frameData.AlphaMask[(sourceY * AnimationCatalog.CellWidth) + sourceX] >= 16;
    }

    private void CheckFullscreen()
    {
        var shouldHide = _settings.AutoHideFullscreen &&
                         _fullscreenWatcher.IsForeignFullscreenWindow(_hwnd);
        if (shouldHide == _fullscreenHidden)
        {
            return;
        }

        _fullscreenHidden = shouldHide;
        if (shouldHide && _dragging)
        {
            _mouseDown = false;
            _dragging = false;
            _resumePausedAfterDrag = false;
            Mouse.Capture(null);
            SaveCurrentPosition();
        }
        _controller.SetFullscreenHidden(shouldHide);
        if (shouldHide)
        {
            Hide();
        }
        else
        {
            Show();
            Topmost = _settings.Topmost;
            if (_settings.Paused)
            {
                _controller.SetPaused(true);
            }
            ClampToCurrentMonitor();
            _lastTickMilliseconds = _clock.ElapsedMilliseconds;
            RenderCurrentFrame();
        }
    }

    private void PlaceInitialOrSavedPosition()
    {
        if (!NativeMethods.GetWindowRect(_hwnd, out var window))
        {
            return;
        }

        MonitorArea monitor;
        int left;
        int top;
        if (_settings.LeftPx is int savedLeft && _settings.TopPx is int savedTop)
        {
            monitor = _desktop.FindSavedMonitor(_settings.MonitorDeviceName, savedLeft, savedTop);
            left = savedLeft;
            top = savedTop;
        }
        else
        {
            monitor = _desktop.GetAll().First(area => area.IsPrimary);
            left = monitor.Work.Right - window.Width - 24;
            top = monitor.Work.Bottom - window.Height - 24;
        }

        var clamped = _desktop.Clamp(monitor, left, top, window.Width, window.Height);
        _ = NativeMethods.SetWindowPos(
            _hwnd,
            nint.Zero,
            clamped.Left,
            clamped.Top,
            0,
            0,
            NativeMethods.SwpMoveOnly);
        SaveCurrentPosition();
    }

    private void ClampToCurrentMonitor()
    {
        if (!NativeMethods.GetWindowRect(_hwnd, out var window))
        {
            return;
        }

        var monitor = _desktop.GetForWindow(_hwnd);
        var clamped = _desktop.Clamp(monitor, window.Left, window.Top, window.Width, window.Height);
        _ = NativeMethods.SetWindowPos(
            _hwnd,
            nint.Zero,
            clamped.Left,
            clamped.Top,
            0,
            0,
            NativeMethods.SwpMoveOnly);
        SaveCurrentPosition();
    }

    private void ResetPosition()
    {
        if (!NativeMethods.GetWindowRect(_hwnd, out var window))
        {
            return;
        }

        var monitor = _desktop.GetAll().First(area => area.IsPrimary);
        var left = monitor.Work.Right - window.Width - 24;
        var top = monitor.Work.Bottom - window.Height - 24;
        var clamped = _desktop.Clamp(monitor, left, top, window.Width, window.Height);
        _ = NativeMethods.SetWindowPos(
            _hwnd,
            nint.Zero,
            clamped.Left,
            clamped.Top,
            0,
            0,
            NativeMethods.SwpMoveOnly);
        SaveCurrentPosition();
        SaveSettings();
    }

    private void SetScale(double scale)
    {
        if (Math.Abs(_settings.Scale - scale) < 0.001)
        {
            return;
        }

        _settings.Scale = scale;
        _settings.Normalize();
        Width = AnimationCatalog.CellWidth * _settings.Scale;
        Height = AnimationCatalog.CellHeight * _settings.Scale;
        Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            () =>
            {
                ClampToCurrentMonitor();
                SaveSettings();
                UpdateTray();
            });
    }

    private void TogglePaused()
    {
        _settings.Paused = !_settings.Paused;
        if (!_fullscreenHidden)
        {
            _controller.SetPaused(_settings.Paused);
        }
        SaveSettings();
        UpdateTray();
    }

    private void ToggleTopmost()
    {
        _settings.Topmost = !_settings.Topmost;
        Topmost = _settings.Topmost;
        SaveSettings();
        UpdateTray();
    }

    private void ToggleMouseFollow()
    {
        _settings.MouseFollowEnabled = !_settings.MouseFollowEnabled;
        _displayedFrame = null;
        RenderCurrentFrame();
        SaveSettings();
        UpdateTray();
    }

    private void ToggleWalking()
    {
        _settings.WalkingEnabled = !_settings.WalkingEnabled;
        _controller.SetWalkingEnabled(_settings.WalkingEnabled);
        _displayedFrame = null;
        RenderCurrentFrame();
        SaveSettings();
        UpdateTray();
    }

    private void ToggleAutoStart()
    {
        try
        {
            var desired = !_settings.AutoStartEnabled;
            _autoStart.Apply(desired);
            _settings.AutoStartEnabled = desired;
            SaveSettings();
            UpdateTray();
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show(
                $"无法修改开机启动设置。\n\n{exception.Message}",
                "团团桌宠",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void ToggleAutoHideFullscreen()
    {
        _settings.AutoHideFullscreen = !_settings.AutoHideFullscreen;
        SaveSettings();
        UpdateTray();
        CheckFullscreen();
    }

    private void ImportPet()
    {
        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Title = "导入团团桌宠素材",
            Filter =
                "桌宠包 (*.ttpet;*.zip)|*.ttpet;*.zip|" +
                "宠物配置或图集 (pet.json;*.webp)|pet.json;*.webp|" +
                "所有支持的文件|*.ttpet;*.zip;*.json;*.webp",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        try
        {
            var package = _petCatalog.ReadImport(dialog.FileName);
            // Decode and validate before anything is installed.
            using (SpriteAtlas.Load(package, isBuiltIn: false))
            {
            }

            var existing = _petCatalog.Find(package.Manifest.Id);
            var replace = false;
            if (existing is not null)
            {
                var result = System.Windows.MessageBox.Show(
                    $"已存在 id 为“{package.Manifest.Id}”的宠物“{existing.DisplayName}”。是否替换？",
                    "导入宠物",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
                replace = true;
            }

            var installed = _petCatalog.Install(package, replace);
            SelectPet(installed.Id, forceReload: true);
            System.Windows.MessageBox.Show(
                $"“{installed.DisplayName}”已导入并切换成功。",
                "导入宠物",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show(
                $"无法导入这个宠物包。\n\n{exception.Message}",
                "导入宠物",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SelectPet(string id) => SelectPet(id, forceReload: false);

    private void SelectPet(string id, bool forceReload)
    {
        var descriptor = _petCatalog.Find(id);
        if (descriptor is null ||
            (!forceReload &&
             string.Equals(descriptor.Id, _selectedPet.Id, StringComparison.OrdinalIgnoreCase)))
        {
            UpdateTray();
            return;
        }

        try
        {
            var nextAtlas = SpriteAtlas.Load(_petCatalog.Load(descriptor), descriptor.IsBuiltIn);
            var previousAtlas = _atlas;
            _atlas = nextAtlas;
            _selectedPet = descriptor;
            _settings.SelectedPetId = descriptor.Id;
            Title = ApplicationName;
            _displayedFrame = null;
            _frameData = null;
            RenderCurrentFrame();
            previousAtlas.Dispose();
            if (!_settings.Paused && !_fullscreenHidden)
            {
                _controller.TriggerWave();
            }
            SaveSettings();
            UpdateTray();
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show(
                $"无法切换到“{descriptor.DisplayName}”。\n\n{exception.Message}",
                "切换宠物",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void WakeWindow()
    {
        if (_fullscreenHidden)
        {
            return;
        }

        if (!IsVisible)
        {
            Show();
        }
        NativeMethods.RestoreWindow(_hwnd);
        Topmost = _settings.Topmost;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs eventArgs)
    {
        _ = Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            () =>
            {
                ClampToCurrentMonitor();
                SaveSettings();
            });
    }

    private nint WindowProcedure(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == NativeMethods.WakeMessage)
        {
            WakeWindow();
            handled = true;
        }
        return nint.Zero;
    }

    private double GetPixelsPerDip()
    {
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
    }

    private void SaveCurrentPosition()
    {
        if (!NativeMethods.GetWindowRect(_hwnd, out var window))
        {
            return;
        }

        var monitor = _desktop.GetForWindow(_hwnd);
        _settings.LeftPx = window.Left;
        _settings.TopPx = window.Top;
        _settings.MonitorDeviceName = monitor.DeviceName;
    }

    private void SaveSettings() => _settingsStore.Save(_settings);

    private void UpdateTray()
    {
        _tray.Update(
            _settings.Paused,
            _settings.Topmost,
            _settings.MouseFollowEnabled,
            _settings.WalkingEnabled,
            _settings.Scale,
            _settings.AutoStartEnabled,
            _settings.AutoHideFullscreen);
        _tray.UpdatePets(
            _petCatalog.GetPets(),
            _selectedPet.Id,
            _selectedPet.DisplayName);
    }

    private void ExitApplication()
    {
        _exiting = true;
        SaveCurrentPosition();
        SaveSettings();
        System.Windows.Application.Current.Shutdown();
    }

    private void OnClosed(object? sender, EventArgs eventArgs)
    {
        _animationTimer.Stop();
        _fullscreenTimer.Stop();
        _singleClickTimer.Stop();
        SaveCurrentPosition();
        SaveSettings();
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _source?.RemoveHook(WindowProcedure);
        _tray.Dispose();
        _atlas.Dispose();

        if (!_exiting)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
