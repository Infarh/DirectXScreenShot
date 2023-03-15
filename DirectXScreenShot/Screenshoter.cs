namespace DirectXScreenShot;

public static class Screenshoter
{
    public static Bitmap TakeScreenshot()
    {
        var factory = new Factory1();
        var adapter = factory.GetAdapter1(0);

        var device = new SharpDX.Direct3D11.Device(adapter);
        var output = adapter.GetOutput(0);

        var output1 = output.QueryInterface<Output1>();

        var width = output.Description.DesktopBounds.Right;
        var height = output.Description.DesktopBounds.Bottom;

        var texture_desc = new Texture2DDescription
        {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = width,
            Height = height,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = { Count = 1, Quality = 0 },
            Usage = ResourceUsage.Staging
        };

        using var screen_texture = new Texture2D(device, texture_desc);
        using var duplicated_output = output1.DuplicateOutput(device); // это место должно успеть проинициализироваться однократно

        Thread.Sleep(20); // захватчику экрана надо время проинициализироваться

        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        SharpDX.DXGI.Resource? screen_resource = null;

        try
        {
            if (duplicated_output.TryAcquireNextFrame(10, out var duplicateFrameInformation, out screen_resource) != Result.Ok)
                return bmp;

            using (var screen_texture_2D = screen_resource.QueryInterface<Texture2D>())
                device.ImmediateContext.CopyResource(screen_texture_2D, screen_texture);

            var map_source = device.ImmediateContext.MapSubresource(screen_texture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
            var bmp_data = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
            var source_ptr = map_source.DataPointer;
            var dest_ptr = bmp_data.Scan0;

            Utilities.CopyMemory(dest_ptr, source_ptr, map_source.RowPitch * height);

            bmp.UnlockBits(bmp_data);

            device.ImmediateContext.UnmapSubresource(screen_texture, 0);
            duplicated_output.ReleaseFrame();
        }
        catch (SharpDXException ex)
        {
            throw new InvalidOperationException("Ошибка захвата экрана", ex);
        }
        finally
        {
            screen_resource?.Dispose();
        }

        return bmp;
    }
}
