using System.Drawing;
using System.Drawing.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using ScreenCaptureDemo.Services;
using Windows.Graphics;
using Windows.Graphics.Display;

namespace ScreenCaptureDemo.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private readonly ScreenshotService _screenshot;

    [ObservableProperty]
    private ImageSource? _screenshotImageSource;

    public MainViewModel(ScreenshotService screenshot)
    {
        _screenshot = screenshot;
    }

    [RelayCommand]
    public async Task CapturePrimaryDisplay()
    {
        DisplayId[] displayIds = DisplayServices.FindAll();
        Bitmap bitmap = await _screenshot.CaptureDisplay(displayIds[0]);

        bitmap.Save(@"C:\Users\adstep\Desktop\Screenshot.png");


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
