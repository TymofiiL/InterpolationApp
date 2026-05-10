using CommunityToolkit.Mvvm.Messaging;
using InterpolationApp.Messages;
using InterpolationApp.ViewModels;

namespace InterpolationApp.Views;

public partial class ComplexityPage : ContentPage
{
    private readonly IMessenger _messenger;

    public ComplexityPage(ComplexityViewModel vm, IMessenger messenger)
    {
        InitializeComponent();
        BindingContext = vm;
        _messenger = messenger;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _messenger.Register<InvalidateComplexityChartMessage>(this, (_, _) =>
            MainThread.BeginInvokeOnMainThread(() => ComplexityChartView.Invalidate()));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _messenger.Unregister<InvalidateComplexityChartMessage>(this);
    }
}
