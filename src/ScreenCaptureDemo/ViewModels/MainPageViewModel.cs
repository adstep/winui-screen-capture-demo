using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using ScreenCaptureDemo.Helpers;
using ScreenCaptureDemo.Services;
using Windows.Graphics;
using Windows.Graphics.Display;

namespace ScreenCaptureDemo.ViewModels;

internal partial class MainPageViewModel : ObservableObject, INotifyPropertyChanged
{
    private readonly ScreenshotService _screenshot;

    [ObservableProperty]
    private ImageSource _screenshotImageSource;

    public MainPageViewModel(ScreenshotService screenshot)
    {
        _screenshot = screenshot;
    }

    [RelayCommand]
    public async Task CapturePrimaryDisplay()
    {
        Bitmap bitmap = await _screenshot.CaptureAllDisplays();

        ScreenshotImageSource = ToWriteableBitmap(bitmap);
    }

    private static WriteableBitmap ToWriteableBitmap(Bitmap bitmap)
    {
        using MemoryStream ms = new MemoryStream();

        bitmap.Save(ms, ImageFormat.Png);

        WriteableBitmap writeableBitmap = new WriteableBitmap(bitmap.Width, bitmap.Height);

        ms.Position = 00;

        writeableBitmap.SetSource(ms.AsRandomAccessStream());

        return writeableBitmap;
    }
}
