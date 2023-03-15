using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectXScreenShot.Infrastructure.Extensions;

public static class IntPtrEx
{
    public static void ToSpan(this int v, ref Span<byte> span)
    {
        span[0] = (byte)(v & 0xff);
        span[1] = (byte)((v >> 8) & 0xff);
        span[2] = (byte)((v >> 16) & 0xff);
        span[3] = (byte)((v >> 24) & 0xff);
    }
}
