using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Media.Imaging;
using ScreenCaptureDemo.Helpers;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using WinRT;

namespace ScreenCaptureDemo.Services;
public class ScreenshotService
{
    [DllImport(
        "d3d11.dll",
        EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice",
        SetLastError = true,
        CharSet = CharSet.Unicode,
        ExactSpelling = true,
        CallingConvention = CallingConvention.StdCall
    )]
    private static extern uint CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    public async Task<SoftwareBitmap> CaptureDisplayAsSoftwareBitmap(DisplayId displayId)
    {
        GraphicsCaptureItem captureItem = GraphicsCaptureItem.TryCreateFromDisplayId(displayId);

        using ID3D11Device d3D11Device = D3D11.D3D11CreateDevice(
            Vortice.Direct3D.DriverType.Hardware,
            DeviceCreationFlags.BgraSupport);

        using var dxgiDevice = d3D11Device.QueryInterface<IDXGIDevice>();

        using IDirect3DDevice direct3DDevice = CreateDirect3DDeviceFromSharpDXDevice(dxgiDevice);

        TaskCompletionSource<SoftwareBitmap> tcs = new TaskCompletionSource<SoftwareBitmap>();

        Direct3D11CaptureFramePool framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            direct3DDevice,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            1,
            captureItem.Size);

        framePool.FrameArrived += async (s, e) =>
        {
            using Direct3D11CaptureFrame frame = framePool.TryGetNextFrame();
            tcs.SetResult(await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface));
        };


        using GraphicsCaptureSession session = framePool.CreateCaptureSession(captureItem);
        session.IsCursorCaptureEnabled = true;

        session.StartCapture();

        return await tcs.Task;
    }

    public Task<Bitmap> CaptureDisplay(DisplayId displayId)
    {
        GraphicsCaptureItem captureItem = GraphicsCaptureItem.TryCreateFromDisplayId(displayId);
        return Capture(captureItem);
    }

    public Task<Bitmap> CaptureAllDisplays()
    {
        GraphicsCaptureItem captureItem = GraphicsCaptureItemHelper.CreateForAllDisplays();
        return Capture(captureItem);
    }

    private async Task<Bitmap> Capture(GraphicsCaptureItem captureItem)
    {
        using ID3D11Device d3D11Device = D3D11.D3D11CreateDevice(
            Vortice.Direct3D.DriverType.Hardware,
            DeviceCreationFlags.BgraSupport);

        using var dxgiDevice = d3D11Device.QueryInterface<IDXGIDevice>();

        using IDirect3DDevice direct3DDevice = CreateDirect3DDeviceFromSharpDXDevice(dxgiDevice);

        TaskCompletionSource<Bitmap> tcs = new TaskCompletionSource<Bitmap>();

        Direct3D11CaptureFramePool framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            direct3DDevice,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            1,
            captureItem.Size);

        framePool.FrameArrived += (s, e) =>
        {
            using Direct3D11CaptureFrame frame = framePool.TryGetNextFrame();
            tcs.SetResult(ToBitmap(frame));
        };

        using GraphicsCaptureSession session = framePool.CreateCaptureSession(captureItem);
        session.IsCursorCaptureEnabled = true;
        session.IsBorderRequired = false;

        session.StartCapture();

        return await tcs.Task;
    }

    private static Bitmap ToBitmap(Direct3D11CaptureFrame frame)
    {
        using ID3D11Texture2D capturedTexture = CreateTexture2D(frame.Surface);

        ID3D11Device device = capturedTexture.Device;

        Texture2DDescription description = capturedTexture.Description;
        description.CPUAccessFlags = CpuAccessFlags.Read;
        description.BindFlags = BindFlags.None;
        description.Usage = ResourceUsage.Staging;
        description.MiscFlags = ResourceOptionFlags.None;

        ID3D11Texture2D stagingTexture = device.CreateTexture2D(description);
        device.ImmediateContext.CopyResource(stagingTexture, capturedTexture);

        MappedSubresource mappedSource = device.ImmediateContext.Map(
            stagingTexture,
            0,
            MapMode.Read,
            Vortice.Direct3D11.MapFlags.None);

        Texture2DDescription stagingDescription = stagingTexture.Description;
        return new Bitmap(
            stagingDescription.Width,
            stagingDescription.Height,
            mappedSource.RowPitch,
            PixelFormat.Format32bppArgb,
            mappedSource.DataPointer
        );
    }

    private static Bitmap CaptureSdr(Span<byte> textureData, int width, int height, int rowPitch)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        var imageRect = new Rectangle(Point.Empty, bitmap.Size);
        var bitmapData = bitmap.LockBits(imageRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        Span<byte> bitmapSpan;

        unsafe
        {
            bitmapSpan = new Span<byte>(bitmapData.Scan0.ToPointer(), bitmapData.Height * bitmapData.Stride);
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel = bitmapSpan.Slice(y * bitmapData.Stride + x * 3, 3);
                var sdrPixel = textureData.Slice(y * rowPitch + x * 4, 4);

                pixel[0] = sdrPixel[2];
                pixel[1] = sdrPixel[1];
                pixel[2] = sdrPixel[0];
            }
        }

        bitmap.UnlockBits(bitmapData);
        return bitmap;
    }

    private static IDirect3DDevice CreateDirect3DDeviceFromSharpDXDevice(IDXGIDevice dxgiDevice)
    {
        uint hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out IntPtr graphicsDevice);

        if (hr != 0)
        {
            ExceptionHelpers.ThrowExceptionForHR((int)hr);
        }

#if NET5_0_OR_GREATER
        IDirect3DDevice direct3DDevice = MarshalInterface<IDirect3DDevice>.FromAbi(graphicsDevice);
#else
        direct3DDevice = Marshal.GetObjectForIUnknown(graphicsDevice) as IDirect3DDevice;
#endif
        Marshal.Release(graphicsDevice);

        return direct3DDevice;
    }

    private static ID3D11Texture2D CreateTexture2D(IDirect3DSurface surface)
    {
#if NET5_0_OR_GREATER
        var access = surface.As<IDirect3DDxgiInterfaceAccess>();
#else
        var access = (IDirect3DDxgiInterfaceAccess)surface;
#endif

        IntPtr d3dPtr = access.GetInterface(typeof(ID3D11Texture2D).GUID);
        return new ID3D11Texture2D(d3dPtr);
    }

    [ComImport]
    [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    private interface IDirect3DDxgiInterfaceAccess
    {
        IntPtr GetInterface([In] ref Guid iid);
    };
}
