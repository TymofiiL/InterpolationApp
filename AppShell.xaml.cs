using InterpolationApp.Views;

namespace InterpolationApp;

public partial class AppShell : TabbedPage
{
    public AppShell(ComputePage computePage, GraphPage graphPage, ComplexityPage complexityPage)
    {
        InitializeComponent();
        Children.Add(computePage);
        Children.Add(graphPage);
        Children.Add(complexityPage);
    }
}
