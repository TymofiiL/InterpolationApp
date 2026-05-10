namespace InterpolationApp;

public partial class App : Application
{
    private readonly IServiceProvider _sp;

    public App(IServiceProvider sp)
    {
        _sp = sp;

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            File.WriteAllText(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "crash.txt"),
                e.ExceptionObject?.ToString() ?? "null");

        TaskScheduler.UnobservedTaskException += (_, e) =>
            File.WriteAllText(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "crash_task.txt"),
                e.Exception?.ToString() ?? "null");

        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Pages are constructed here, after InitializeComponent() populated resources.
        MainPage = _sp.GetRequiredService<AppShell>();
        var window = base.CreateWindow(activationState);
        window.MinimumWidth = 900;
        window.MinimumHeight = 620;
        window.Width = 1100;
        window.Height = 760;
        return window;
    }
}
