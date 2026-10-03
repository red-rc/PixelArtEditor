using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace PixelArtEditor.AppServices.Bitmap;

public sealed class TiledBitmap : IDisposable
{
    public const int TileSize = 256;
    public readonly int Width;
    public readonly int Height;
    public readonly int Columns;
    public readonly int Rows;
    private readonly WriteableBitmap?[] _tiles;

    public TiledBitmap(int width, int height)
    {
        Width = width;
        Height = height;
        Columns = (width + TileSize - 1) / TileSize;
        Rows = (height + TileSize - 1) / TileSize;
        _tiles = new WriteableBitmap?[Rows * Columns];
    }

    public WriteableBitmap? GetTile(int column, int row) => _tiles[row * Columns + column];

    public unsafe void Update(byte[] data, PixelRect rect, bool isDirty = false)
    {
        if (isDirty) return;

        var rectEndX = Math.Min(Width, rect.Right);
        var rectEndY = Math.Min(Height, rect.Bottom);

        for (var tileY = Math.Max(0, rect.Y) / TileSize; tileY <= (rectEndY - 1) / TileSize; tileY++)
        {
            for (var tileX = Math.Max(0, rect.X) / TileSize; tileX <= (rectEndX - 1) / TileSize; tileX++)
            {
                var tileStartX = Math.Max(rect.X, tileX * TileSize);
                var tileStartY = Math.Max(rect.Y, tileY * TileSize);
                var tileEndX = Math.Min(rectEndX, (tileX + 1) * TileSize);
                var tileEndY = Math.Min(rectEndY, (tileY + 1) * TileSize);

                var idx = tileY * Columns + tileX;
                var tile = _tiles[idx];

                if (tile is null)
                {
                    var any = false;
                    for (var y = tileY * TileSize; y < Math.Min(Height, (tileY + 1) * TileSize) && !any; y++)
                    {
                        var offset = (y * Width + tileX * TileSize) * 4;
                        var len = (Math.Min(Width, (tileX + 1) * TileSize) - tileX * TileSize) * 4;
                        any = data.AsSpan(offset, len).IndexOfAnyExcept((byte)0) >= 0;
                    }

                    if (!any) continue;

                    tile = _tiles[idx] = new WriteableBitmap(
                        new PixelSize(Math.Min(TileSize, Width - tileX * TileSize), Math.Min(TileSize, Height - tileY * TileSize)),
                        new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);

                    tileStartX = tileX * TileSize; 
                    tileStartY = tileY * TileSize;
                    tileEndX = Math.Min(Width, (tileX + 1) * TileSize);
                    tileEndY = Math.Min(Height, (tileY + 1) * TileSize);
                }

                using var fb = tile.Lock();
                var bytes = (tileEndX - tileStartX) * 4;

                fixed (byte* src = data)
                {
                    for (var y = tileStartY; y < tileEndY; y++)
                    {
                        Buffer.MemoryCopy(
                            src + ((long)y * Width + tileStartX) * 4,
                            (byte*)fb.Address + (long)(y - tileY * TileSize) * fb.RowBytes + (tileStartX - tileX * TileSize) * 4L,
                            bytes,
                            bytes);
                    }
                }
            }
        }
    }

    public void Draw(DrawingContext ctx, double offsetX, double offsetY, double scale, Rect view, double opacity)
    {
        if (opacity < 1)
        {
            using (ctx.PushOpacity(opacity))
                DrawTiles(ctx, offsetX, offsetY, scale, view);
        }
        else
            DrawTiles(ctx, offsetX, offsetY, scale, view);
    }

    private void DrawTiles(DrawingContext ctx, double offsetX, double offsetY, double scale, Rect view)
    {
        for (var tileY = 0; tileY < Rows; tileY++)
        {
            for (var tileX = 0; tileX < Columns; tileX++)
            {
                var tile = _tiles[tileY * Columns + tileX];
                if (tile is null) continue;

                var left = Math.Round(offsetX + tileX * TileSize * scale);
                var top = Math.Round(offsetY + tileY * TileSize * scale);
                var right = Math.Round(offsetX + (tileX * TileSize + tile.PixelSize.Width) * scale);
                var bottom = Math.Round(offsetY + (tileY * TileSize + tile.PixelSize.Height) * scale);

                var dst = new Rect(left, top, right - left, bottom - top);
                if (!dst.Intersects(view)) continue;

                ctx.DrawImage(tile, new Rect(0, 0, tile.PixelSize.Width, tile.PixelSize.Height), dst);
            }
        }
    }

    public void Dispose() 
    { 
        foreach (var tile in _tiles) 
            tile?.Dispose(); 
    }
}