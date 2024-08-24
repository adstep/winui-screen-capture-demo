using System.Runtime.InteropServices;
using Windows.Graphics.Capture;

namespace ScreenCaptureDemo.Helpers;

internal class GraphicsCaptureItemHelper
{
    static readonly Guid GraphicsCaptureItemGuid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow(
            [In] IntPtr window,
            [In] ref Guid iid);

        IntPtr CreateForMonitor(
            [In] IntPtr monitor,
            [In] ref Guid iid);
    }

    public static GraphicsCaptureItem CreateForAllDisplays()
    {
        var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
        var itemPointer = interop.CreateForMonitor(IntPtr.Zero, GraphicsCaptureItemGuid);
        return GraphicsCaptureItem.FromAbi(itemPointer);
    }
}
