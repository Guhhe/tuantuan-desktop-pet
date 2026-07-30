using System.Threading;
using System.Windows;

namespace TuantuanDesktopPet;

public partial class App : System.Windows.Application
{
    private const string MutexName = @"Local\TuantuanDesktopPet.8F50A4A9-1E7C-47BC-B5AA-CA059E2681F0";
    private Mutex? _singleInstanceMutex;
    private bool _ownsSingleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, MutexName, out var createdNew);
        _ownsSingleInstanceMutex = createdNew;
        if (!createdNew)
        {
            NativeMethods.BroadcastWakeExistingInstance();
            Shutdown();
            return;
        }

        try
        {
            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show(
                $"团团桌宠无法启动。\n\n{exception.Message}",
                "团团桌宠",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsSingleInstanceMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
