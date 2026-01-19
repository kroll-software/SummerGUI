using System;

namespace SummerGUI;

public class AtlasPacker
{
    private int _curX = 0;
    private int _curY = 0;
    private int _rowHeight = 0;
    private readonly int _atlasSize;
    private readonly int _padding = 2; // Abstand zwischen Glyphen gegen "Bleeding"

    public AtlasPacker(int size) => _atlasSize = size;

    public bool TryPack(int width, int height, out int x, out int y)
    {
        // Passt es noch in die aktuelle Zeile?
        if (_curX + width + _padding > _atlasSize)
        {
            _curY += _rowHeight + _padding;
            _curX = 0;
            _rowHeight = 0;
        }

        // Passt es noch in die Textur?
        if (_curY + height + _padding > _atlasSize)
        {
            x = y = 0;
            return false;
        }

        x = _curX;
        y = _curY;

        _curX += width + _padding;
        _rowHeight = Math.Max(_rowHeight, height);
        return true;
    }
}