using InterpolationApp.ViewModels;

namespace InterpolationApp.Views;

public partial class ComputePage : ContentPage
{
    public ComputePage(ComputeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
