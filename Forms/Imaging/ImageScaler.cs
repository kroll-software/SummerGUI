using System;

namespace SummerGUI;

public static class ImageScaler
{
    /// <summary>
    /// Skaliert ein RGBA-Byte-Array mit bilinearer Interpolation unter Verwendung von Pointern (unsafe context).
    /// </summary>
    public static unsafe byte[] ScaleImageData(byte[] originalData, int originalWidth, int originalHeight, int newWidth, int newHeight)
    {
        byte[] newData = new byte[newWidth * newHeight * 4];
        double xScale = (double)originalWidth / newWidth;
        double yScale = (double)originalHeight / newHeight;

        // Verwenden von 'fixed' Statements, um die Arrays im Speicher zu fixieren
        fixed (byte* srcData = originalData)
        fixed (byte* dstData = newData)
        {
            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    double px = x * xScale;
                    double py = y * yScale;

                    int x1 = (int)Math.Floor(px);
                    int y1 = (int)Math.Floor(py);
                    int x2 = Math.Min(x1 + 1, originalWidth - 1);
                    int y2 = Math.Min(y1 + 1, originalHeight - 1);

                    double fx = px - x1;
                    double fy = py - y1;
                    double fx1 = 1.0 - fx;
                    double fy1 = 1.0 - fy;

                    // Pointer-Berechnungen: Positions-Offset in Bytes
                    // Multiplikation mit 4, da wir RGBA (4 Bytes pro Pixel) haben
                    int p1 = (y1 * originalWidth + x1) * 4;
                    int p2 = (y1 * originalWidth + x2) * 4;
                    int p3 = (y2 * originalWidth + x1) * 4;
                    int p4 = (y2 * originalWidth + x2) * 4;

                    int destP = (y * newWidth + x) * 4;

                    // Direkter Pointer-Zugriff und Interpolation
                    for (int i = 0; i < 4; i++) // RGBA Komponenten
                    {
                        double v1 = srcData[p1 + i];
                        double v2 = srcData[p2 + i];
                        double v3 = srcData[p3 + i];
                        double v4 = srcData[p4 + i];

                        double weightedAverage = (v1 * fx1 + v2 * fx) * fy1 + (v3 * fx1 + v4 * fx) * fy;
                        dstData[destP + i] = (byte)Math.Round(weightedAverage);
                    }
                }
            }
        }
        return newData;
    }
}
