using System;
using KS.Foundation;
using OpenTK.Graphics.OpenGL;

namespace SummerGUI;

public class FontAtlas : DisposableObject
{
    public int TextureId { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public FontAtlas(int width = 1024, int height = 1024, FontRenderingHints renderingHint = FontRenderingHints.Smooth)
    {
        Width = width;
        Height = height;
        TextureId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, TextureId);

        // Wir erstellen eine leere Alpha-Textur (R8 reicht für Fonts oft aus)
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, 
                     width, height, 0, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
        
        // Wichtig: Entfernt Dreck
        GL.ClearTexImage(TextureId, 0, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);

        switch (renderingHint)
        {
            case FontRenderingHints.Crisp:
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                break;
            default:
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);        
                break;
        }        
    }

    public void UploadGlyph(int x, int y, int width, int height, byte[] pixels)
    {
        GL.BindTexture(TextureTarget.Texture2D, TextureId);

        // DIESE ZEILE IST ENTSCHEIDEND:
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        GL.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, width, height, 
                        PixelFormat.Red, PixelType.UnsignedByte, pixels);
        
        // Optional: Danach wieder auf Standard zurückstellen
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
    }

    protected override void CleanupUnmanagedResources()
    {
        if (TextureId > 0)
            GL.DeleteTexture(TextureId);
        TextureId = 0;
        base.CleanupUnmanagedResources();
    }    
}


public class FontAtlasGroup : DisposableObject
{
    private readonly List<FontAtlas> _atlases = new List<FontAtlas>();
    private readonly List<AtlasPacker> _packers = new List<AtlasPacker>();
    private readonly int _atlasSize;

    private readonly FontRenderingHints _renderingHint;

    public FontAtlasGroup(int atlasSize = 1024, FontRenderingHints renderingHint = FontRenderingHints.Smooth)
    {
        _atlasSize = atlasSize;
        _renderingHint = renderingHint;
        CreateNewAtlas();
    }

    private void CreateNewAtlas()
    {
        _atlases.Add(new FontAtlas(_atlasSize, _atlasSize, _renderingHint));
        _packers.Add(new AtlasPacker(_atlasSize));
    }

    public bool TryPack(int width, int height, out int atlasId, out int x, out int y, out int atlasWidth, out int atlasHeight)
    {
        // Versuche es im aktuellsten Atlas
        int lastIdx = _atlases.Count - 1;
        if (!_packers[lastIdx].TryPack(width, height, out x, out y))
        {
            // Atlas voll -> Neuen erstellen
            CreateNewAtlas();
            lastIdx++;
            if (!_packers[lastIdx].TryPack(width, height, out x, out y))
            {
                // Glyphe ist zu groß für einen leeren Atlas (unwahrscheinlich)
                atlasId = x = y = atlasWidth = atlasHeight = 0;
                return false;
            }
        }

        atlasId = _atlases[lastIdx].TextureId;
        atlasWidth = _atlases[lastIdx].Width;
        atlasHeight = _atlases[lastIdx].Height;
        
        // Die Pixel-Daten müssen in den richtigen Atlas hochgeladen werden
        // Wir geben den Atlas zurück, damit CompileCharacter ihn nutzen kann
        CurrentActiveAtlas = _atlases[lastIdx];
        
        return true;
    }

    public FontAtlas CurrentActiveAtlas { get; private set; }

    protected override void CleanupUnmanagedResources()
    {
        foreach (var atlas in _atlases) atlas.Dispose();
        _atlases.Clear();

        base.CleanupUnmanagedResources();
    }    
}
