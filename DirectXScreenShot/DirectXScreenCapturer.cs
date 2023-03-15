using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DirectXScreenShot;

public class DirectXScreenCapturer : IDisposable
{
    private Factory1 _Factory;
    private Adapter1 _Adapter;
    private SharpDX.Direct3D11.Device _Device;
    private Output _Output;
    private Output1 _Output1;
    private Texture2DDescription _Texture;

    private Texture2D _ScreenTexture;

    public DirectXScreenCapturer(int OutputIndex = 0, int AdapterIndex = 0)
    {
        _Factory = new Factory1();
        _Adapter = _Factory.GetAdapter1(AdapterIndex);
        _Device = new SharpDX.Direct3D11.Device(_Adapter);
        _Output = _Adapter.GetOutput(OutputIndex);
        _Output1 = _Output.QueryInterface<Output1>();

        _Texture = new Texture2DDescription
        {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = _Output.Description.DesktopBounds.Right,
            Height = _Output.Description.DesktopBounds.Bottom,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = { Count = 1, Quality = 0 },
            Usage = ResourceUsage.Staging
        };

        _ScreenTexture = new Texture2D(_Device, _Texture);
    }

    private Result ProcessFrame(Action<DataBox, Texture2DDescription> DataProcessor, int TimeoutMilliseconds = 5)
    {
        using var duplicated_output = _Output1.DuplicateOutput(_Device);
        var result = duplicated_output.TryAcquireNextFrame(
            TimeoutMilliseconds,
            out var duplicateFrameInformation,
            out var screenResource);

        if (!result.Success) return result;

        using var screen_texture_2D = screenResource.QueryInterface<Texture2D>();

        _Device.ImmediateContext.CopyResource(screen_texture_2D, _ScreenTexture);
        var map_source = _Device.ImmediateContext.MapSubresource(_ScreenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

        DataProcessor?.Invoke(map_source, _Texture);

        _Device.ImmediateContext.UnmapSubresource(_ScreenTexture, 0);
        screenResource.Dispose();
        duplicated_output.ReleaseFrame();

        return result;
    }

    public (Result result, bool IsBlackFrame, Image? image) GetFrameImage(int TimeoutMilliseconds = 5)
    {
        var image = new Bitmap(_Texture.Width, _Texture.Height, PixelFormat.Format24bppRgb);
        var is_black = true;
        var result = ProcessFrame(ProcessImage);

        if (!result.Success) image.Dispose();

        return (result, is_black, result.Success ? image : null);

        unsafe void ProcessImage(DataBox Data, Texture2DDescription Texture)
        {
            var data = image.LockBits(new(0, 0, Texture.Width, Texture.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            //data.DataPointer
            //Unsafe.Read(.ToPointer())

            //var gc = GCHandle.FromIntPtr(Data.DataPointer);


            var data_head = (byte*)Data.DataPointer.ToPointer();

            for (var x = 0; x < Texture.Width; x++)
                for (var y = 0; y < Texture.Height; y++)
                {
                    var pix_ptr = (byte*)(data.Scan0 + y * data.Stride + x * 3);

                    var pos = (x + y * Texture.Width) * 4;

                    var argb = Marshal.ReadInt32(Data.DataPointer, (x + y * Texture.Width) * 4);

                    var r = data_head[pos + 2];
                    var g = data_head[pos + 1];
                    var b = data_head[pos + 0];

                    if (is_black && (r != 0 || g != 0 || b != 0)) is_black = false;

                    pix_ptr[0] = b;
                    pix_ptr[1] = g;
                    pix_ptr[2] = r;
                }

            image.UnlockBits(data);
        }
    }

    #region IDisposable

    private bool _IsDisposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!_IsDisposed)
        {
            if (disposing)
            {
                _Factory.Dispose();
                _Adapter.Dispose();
                _Device.Dispose();
                _Output.Dispose();
                _Output1.Dispose();
                _ScreenTexture.Dispose();
            }

            _Factory = null!;
            _Adapter = null!;
            _Device = null!;
            _Output = null!;
            _Output1 = null!;
            _ScreenTexture = null!;

            _IsDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    } 

    #endregion
}

public class SharpDXDevice : SharpDX.DXGI.Device
{
    public SharpDXDevice(nint nativePtr) : base(nativePtr)
    {
    }
}