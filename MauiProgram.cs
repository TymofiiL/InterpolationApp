using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.Messaging;
using InterpolationApp.Services;
using InterpolationApp.ViewModels;
using InterpolationApp.Views;
namespace InterpolationApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

        // ── Messenger ──────────────────────────────────────────────
        builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        // ── Domain services (Singletons — one instance for whole app life) ──
        builder.Services.AddSingleton<InterpolationStateService>();
        builder.Services.AddSingleton<DataManager>();
        // ── ViewModels (Singletons so tab state is preserved) ──────
        builder.Services.AddSingleton<ComputeViewModel>();
        builder.Services.AddSingleton<GraphViewModel>();
        builder.Services.AddSingleton<ComplexityViewModel>();

        // ── Pages ──────────────────────────────────────────────────
        builder.Services.AddSingleton<ComputePage>();
        builder.Services.AddSingleton<GraphPage>();
        builder.Services.AddSingleton<ComplexityPage>();
        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}
