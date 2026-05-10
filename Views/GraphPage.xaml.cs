using CommunityToolkit.Mvvm.Messaging;
using InterpolationApp.Messages;
using InterpolationApp.ViewModels;

namespace InterpolationApp.Views;

public partial class GraphPage : ContentPage
{
    private readonly IMessenger _messenger;

    public GraphPage(GraphViewModel vm, IMessenger messenger)
    {
        InitializeComponent();
        BindingContext = vm;
        _messenger = messenger;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Subscribe: when ViewModel updates chart data, redraw the GraphicsView
        _messenger.Register<InvalidateChartMessage>(this, (_, _) =>
            MainThread.BeginInvokeOnMainThread(() => ChartView.Invalidate()));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _messenger.Unregister<InvalidateChartMessage>(this);
    }
}
