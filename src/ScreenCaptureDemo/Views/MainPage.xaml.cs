using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using ScreenCaptureDemo.ViewModels;

namespace ScreenCaptureDemo.Views;

internal sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainPageViewModel>();
        this.InitializeComponent();
    }
}
