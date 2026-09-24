namespace PixelArtEditor.AppServices.ImageProcessing;

public class ImageConverterService
{
    public static unsafe byte[] ConvertToGrayscale(byte[] bgraData)
    {
        var result = new byte[bgraData.Length];

        fixed (byte* srcPtr = bgraData)
        fixed (byte* dstPtr = result)
        {
            uint* src = (uint*)srcPtr;
            uint* dst = (uint*)dstPtr;

            for (var i = 0; i < bgraData.Length / 4; i++)
            {
                var packed = src[i];
                var b = packed & 0xFF;
                var g = (packed >> 8) & 0xFF;
                var r = (packed >> 16) & 0xFF;
                var a = (packed >> 24) & 0xFF;
                var gray = (uint)((r * 77 + g * 150 + b * 29) >> 8);
                dst[i] = gray | (gray << 8) | (gray << 16) | (a << 24);
            }
        }

        return result;
    }

    public static unsafe byte[] StripAlpha(byte[] bgraData)
    {
        var result = new byte[bgraData.Length];

        fixed (byte* srcPtr = bgraData)
        fixed (byte* dstPtr = result)
        {
            uint* src = (uint*)srcPtr;
            uint* dst = (uint*)dstPtr;

            for (var i = 0; i < bgraData.Length / 4; i++)
                dst[i] = src[i] | 0xFF000000;
        }

        return result;
    }
}