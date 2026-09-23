using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using PixelArtEditor.Models.Canvas;
using System;
using System.IO;

namespace PixelArtEditor.AppServices.ImageProcessing.Formats;

public static class DicomService
{
    private const int MaxPixels = 100_000_000;

    public static (PixelModel? model, string? error) Load(Stream stream)
    {
        try
        {
            var file = DicomFile.Open(stream);
            var pixelData = DicomPixelData.Create(file.Dataset);

            if (pixelData.NumberOfFrames != 1)
                return (null, LocalizationService.Get("DicomMultiFrameUnsupported"));

            if (pixelData.Syntax.IsEncapsulated)
                return (null, LocalizationService.Get("DicomUnsupportedEncoding"));

            if (!HasValidDimensions(pixelData.Width, pixelData.Height))
                return (null, LocalizationService.Get("DicomInvalidImage"));

            var (model, error) = pixelData.PhotometricInterpretation.Value switch
            {
                "MONOCHROME1" or "MONOCHROME2" => (ReadMonochrome(file.Dataset, pixelData), (string?)null),
                "RGB" => (ReadRgb(pixelData), (string?)null),
                _ => ((PixelModel?)null, LocalizationService.Get("DicomUnsupportedEncoding"))
            };

            model?.DicomDataset = file.Dataset.Clone();

            return (model, error);
        }
        catch (InvalidOperationException ex) { return (null, ex.Message); }
        catch (DicomFileException) { return (null, LocalizationService.Get("DicomInvalidFile")); }
        catch (Exception) { return (null, LocalizationService.Get("DicomInvalidFile")); }
    }

    public static void Save(Stream stream, byte[] rgba, int width, int height, DicomDataset? sourceDataset = null)
    {
        if (!HasValidDimensions(width, height) || rgba.Length != checked(width * height * 4))
            throw new InvalidOperationException(LocalizationService.Get("DicomInvalidImage"));

        DicomDataset dataset;
        if (sourceDataset is not null)
        {
            dataset = sourceDataset.Clone();
            dataset.Remove(DicomTag.PixelData);
        }
        else
        {
            dataset = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian);
            var uidGenerator = new DicomUIDGenerator();
            var sopInstanceUid = uidGenerator.Generate(DicomUID.SecondaryCaptureImageStorage);
            var now = DateTime.Now;

            dataset.AddOrUpdate(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage);
            dataset.AddOrUpdate(DicomTag.SOPInstanceUID, sopInstanceUid);
            dataset.AddOrUpdate(DicomTag.StudyInstanceUID, uidGenerator.Generate(DicomUID.StudyRootQueryRetrieveInformationModelFind));
            dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, uidGenerator.Generate(DicomUID.StudyRootQueryRetrieveInformationModelFind));
            dataset.AddOrUpdate(DicomTag.Modality, "OT");
            dataset.AddOrUpdate(DicomTag.ConversionType, "WSD");
            dataset.AddOrUpdate(DicomTag.ContentDate, now);
            dataset.AddOrUpdate(DicomTag.ContentTime, now);
        }

        var nowExport = DateTime.Now;
        dataset.AddOrUpdate(DicomTag.InstanceCreationDate, nowExport);
        dataset.AddOrUpdate(DicomTag.InstanceCreationTime, nowExport);

        dataset.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)3);
        dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "RGB");
        dataset.AddOrUpdate(DicomTag.PlanarConfiguration, (ushort)0);
        dataset.AddOrUpdate(DicomTag.Rows, checked((ushort)height));
        dataset.AddOrUpdate(DicomTag.Columns, checked((ushort)width));
        dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)8);
        dataset.AddOrUpdate(DicomTag.BitsStored, (ushort)8);
        dataset.AddOrUpdate(DicomTag.HighBit, (ushort)7);
        dataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)0);
        dataset.AddOrUpdate(DicomTag.NumberOfFrames, 1);

        dataset.Remove(DicomTag.WindowCenter);
        dataset.Remove(DicomTag.WindowWidth);
        dataset.Remove(DicomTag.RescaleSlope);
        dataset.Remove(DicomTag.RescaleIntercept);
        dataset.Remove(DicomTag.RescaleType);
        dataset.Remove(DicomTag.SmallestImagePixelValue);
        dataset.Remove(DicomTag.LargestImagePixelValue);
        dataset.Remove(DicomTag.PixelPaddingValue);

        var pixelData = DicomPixelData.Create(dataset, true);
        pixelData.Width = checked((ushort)width);
        pixelData.Height = checked((ushort)height);
        pixelData.AddFrame(new MemoryByteBuffer(ToOpaqueRgb(rgba)));

        new DicomFile(dataset).Save(stream);
    }

    private static PixelModel ReadRgb(DicomPixelData pixelData)
    {
        if (pixelData.SamplesPerPixel != 3 || pixelData.BitsAllocated != 8 || pixelData.PlanarConfiguration != PlanarConfiguration.Interleaved)
            throw new InvalidOperationException(LocalizationService.Get("DicomUnsupportedEncoding"));

        var source = pixelData.GetFrame(0).Data;
        var expectedLength = checked(pixelData.Width * pixelData.Height * 3);
        if (source.Length < expectedLength)
            throw new InvalidOperationException(LocalizationService.Get("DicomInvalidImage"));

        var data = new byte[checked(pixelData.Width * pixelData.Height * 4)];
        for (int src = 0, dst = 0; src < expectedLength; src += 3, dst += 4)
        {
            data[dst] = source[src];
            data[dst + 1] = source[src + 1];
            data[dst + 2] = source[src + 2];
            data[dst + 3] = byte.MaxValue;
        }

        return CreateRgbaModel(pixelData.Width, pixelData.Height, data);
    }

    private static PixelModel ReadMonochrome(DicomDataset dataset, DicomPixelData pixelData)
    {
        if (pixelData.SamplesPerPixel != 1 || (pixelData.BitsAllocated != 8 && pixelData.BitsAllocated != 16))
            throw new InvalidOperationException(LocalizationService.Get("DicomUnsupportedEncoding"));

        var source = pixelData.GetFrame(0).Data;
        var bytesPerPixel = pixelData.BitsAllocated / 8;
        var count = checked(pixelData.Width * pixelData.Height);
        if (source.Length < checked(count * bytesPerPixel))
            throw new InvalidOperationException(LocalizationService.Get("DicomInvalidImage"));

        var values = new double[count];
        var slope = dataset.GetSingleValueOrDefault(DicomTag.RescaleSlope, 1d);
        var intercept = dataset.GetSingleValueOrDefault(DicomTag.RescaleIntercept, 0d);
        var signed = pixelData.PixelRepresentation == PixelRepresentation.Signed;
        var min = double.MaxValue;
        var max = double.MinValue;

        for (var i = 0; i < count; i++)
        {
            var raw = bytesPerPixel == 1
                ? source[i]
                : source[i * 2] | (source[i * 2 + 1] << 8);
            var value = signed && bytesPerPixel == 2 ? (short)raw : raw;
            values[i] = value * slope + intercept;
            min = Math.Min(min, values[i]);
            max = Math.Max(max, values[i]);
        }

        var center = dataset.GetSingleValueOrDefault(DicomTag.WindowCenter, double.NaN);
        var width = dataset.GetSingleValueOrDefault(DicomTag.WindowWidth, double.NaN);
        if (!double.IsNaN(center) && !double.IsNaN(width) && width > 1)
        {
            min = center - width / 2d;
            max = center + width / 2d;
        }

        var invert = pixelData.PhotometricInterpretation == PhotometricInterpretation.Monochrome1;
        var data = new byte[checked(count * 4)];
        var range = max - min;
        for (var i = 0; i < count; i++)
        {
            var normalized = range > 0 ? Math.Clamp((values[i] - min) / range, 0d, 1d) : 0d;
            var gray = (byte)Math.Round(normalized * 255d);
            if (invert) gray = (byte)(255 - gray);

            var offset = i * 4;
            data[offset] = gray;
            data[offset + 1] = gray;
            data[offset + 2] = gray;
            data[offset + 3] = byte.MaxValue;
        }

        return CreateRgbaModel(pixelData.Width, pixelData.Height, data);
    }

    private static PixelModel CreateRgbaModel(int width, int height, byte[] data) => new()
    {
        Width = width,
        Height = height,
        Mode = ColorMode.RGBA,
        BitDepth = Models.Canvas.BitDepth.Bit8,
        Alpha = AlphaFormat.Straight,
        ColorSpace = Models.Canvas.ColorSpace.sRGB,
        DpiX = 96f,
        DpiY = 96f,
        Data = data
    };

    private static byte[] ToOpaqueRgb(byte[] rgba)
    {
        var rgb = new byte[checked(rgba.Length / 4 * 3)];
        for (int src = 0, dst = 0; src < rgba.Length; src += 4, dst += 3)
        {
            var alpha = rgba[src + 3];
            rgb[dst] = (byte)(rgba[src] * alpha / 255);
            rgb[dst + 1] = (byte)(rgba[src + 1] * alpha / 255);
            rgb[dst + 2] = (byte)(rgba[src + 2] * alpha / 255);
        }

        return rgb;
    }

    private static bool HasValidDimensions(int width, int height)
        => width > 0 && height > 0 && (long)width * height <= MaxPixels;
}
