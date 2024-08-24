using Microsoft.UI.Xaml.Controls;

using ScreenCaptureDemo.ViewModels;

namespace ScreenCaptureDemo.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }
}
