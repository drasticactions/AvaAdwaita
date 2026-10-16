using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.Presenters;

/// <summary>Bitmaps through a node's color filters, pixel by pixel, cached per bitmap and filter.</summary>
internal static class FilteredBitmaps
{
    private static readonly ConditionalWeakTable<Bitmap, Dictionary<CssColorMatrix, Bitmap>> s_cache = new();

    public static Bitmap Get(Bitmap source, CssColorMatrix matrix)
    {
        var byMatrix = s_cache.GetOrCreateValue(source);
        if (!byMatrix.TryGetValue(matrix, out var filtered))
        {
            filtered = Filter(source, matrix);
            byMatrix[matrix] = filtered;
        }

        return filtered;
    }

    private static Bitmap Filter(Bitmap source, CssColorMatrix matrix)
    {
        var size = source.PixelSize;
        var stride = size.Width * 4;
        var bytes = new byte[stride * size.Height];
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            source.CopyPixels(new PixelRect(size), handle.AddrOfPinnedObject(), bytes.Length, stride);
        }
        finally
        {
            handle.Free();
        }

        // Out as straight BGRA, whatever the source's layout.
        var rgba = source.Format == PixelFormats.Rgba8888;
        var premul = source.AlphaFormat == AlphaFormat.Premul;
        for (var i = 0; i < bytes.Length; i += 4)
        {
            var (r, g, b, a) = rgba ? (bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3]) : (bytes[i + 2], bytes[i + 1], bytes[i], bytes[i + 3]);
            if (premul && a is > 0 and < 255)
            {
                (r, g, b) = ((byte)Math.Min(255, r * 255 / a), (byte)Math.Min(255, g * 255 / a), (byte)Math.Min(255, b * 255 / a));
            }

            var (fr, fg, fb, fa) = matrix.Apply(CssColor.FromRgb8(r, g, b, a)).ToRgba8();
            (bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3]) = (fb, fg, fr, fa);
        }

        var result = new WriteableBitmap(size, source.Dpi, PixelFormats.Bgra8888, AlphaFormat.Unpremul);
        using (var fb = result.Lock())
        {
            Marshal.Copy(bytes, 0, fb.Address, bytes.Length);
        }

        return result;
    }
}
