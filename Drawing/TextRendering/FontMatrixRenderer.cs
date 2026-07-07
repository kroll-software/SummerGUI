using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using KS.Foundation;
using System.Text;
using HarfBuzzSharp;
using FreeTypeSharp;
using static FreeTypeSharp.FT;
using OpenTK.Graphics.OpenGL;

namespace SummerGUI
{
    public unsafe class FontMatrixRenderer : DisposableObject
    {
        private FT_FaceRec_* m_Face;

        private HarfBuzzSharp.Blob m_Blob;
    
        private HarfBuzzSharp.Face m_HbFace;
        private HarfBuzzSharp.Font m_HbFont;
        
        public float Height { get; private set; }
        public float Ascender { get; private set; }
        public float Descender { get; private set; }

        public string FilePath { get; private set; }

        public FontMatrixRenderer(string filePath, float size, float scaleFactor)
        {
            FilePath = filePath.FixedExpandedPath();
            m_Blob = Blob.FromFile(FilePath);
            
            // 1. FreeType Setup
            ReadOnlySpan<byte> fontSpan = m_Blob.AsSpan();
            fixed (FT_FaceRec_** fp = &m_Face)
            fixed (byte* dataPtr = fontSpan)
            {
                FT_New_Memory_Face(FontManager.Library, dataPtr, m_Blob.Length, 0, fp);
            }
            
            float scaledSize = size * scaleFactor;
            this.Height = (int)MathF.Ceiling(scaledSize * 1.3334f); 

            // Jetzt FT setzen
            FT_Set_Pixel_Sizes(m_Face, 0, (uint)this.Height);

            // Metriken berechnen (Exakt wie in deinem alten Code)
            // Wichtig: (float) casten, um Integer-Division zu vermeiden
            float ratio = (float)this.Height / m_Face->height;
            this.Ascender = m_Face->ascender * ratio;
            this.Descender = m_Face->descender * ratio;

            // 2. HarfBuzz Setup
            m_HbFace = new HarfBuzzSharp.Face(m_Blob, 0);
            m_HbFont = new HarfBuzzSharp.Font(m_HbFace);
            
            // Nutze hier auch die berechnete Height
            int h = (int)(this.Height * 64);
            m_HbFont.SetScale(h, h);
        }        

        public unsafe byte[] RenderStringToMatrix(string text, out int totalWidth, out int totalHeight)
        {
            totalHeight = (int)this.Height;
            using var buffer = new HarfBuzzSharp.Buffer();
            buffer.AddUtf8(text);
            buffer.GuessSegmentProperties();
            m_HbFont.Shape(buffer);

            var infos = buffer.GlyphInfos;
            var positions = buffer.GlyphPositions;

            float accumWidth = 0;
            for (int i = 0; i < positions.Length; i++) accumWidth += positions[i].XAdvance / 64.0f;            
            totalWidth = (int)MathF.Ceiling(accumWidth) + 6;
            
            float curX = 0;            

            byte[] matrix = new byte[totalWidth * totalHeight];
            fixed (byte* pMat = matrix)
            {                
                for (int i = 0; i < infos.Length; i++)
                {
                    FT_Load_Glyph(m_Face, infos[i].Codepoint, FT_LOAD.FT_LOAD_FORCE_AUTOHINT);  // Vorsicht, hier besser
                    FT_Render_Glyph(m_Face->glyph, FT_Render_Mode_.FT_RENDER_MODE_NORMAL);

                    var slot = m_Face->glyph;
                    byte* gBuf = slot->bitmap.buffer;
                    int rows = (int)slot->bitmap.rows;
                    int cols = (int)slot->bitmap.width;
                    
                    //int topOff = (int)(this.Ascender - slot->bitmap_top + positions[i].YOffset);
                    int topOff = (int)(this.Ascender - slot->bitmap_top);
                    int startX = (int)Math.Ceiling(curX + slot->bitmap_left);

                    for (int y = 0; y < rows; y++)
                    {
                        if (y + topOff < 0 || y + topOff >= totalHeight) continue;
                        for (int x = 0; x < cols; x++)
                        {
                            if (startX + x < 0 || startX + x >= totalWidth) continue;
                            byte val = gBuf[y * cols + x];
                            byte* pTarget = pMat + ((y + topOff) * totalWidth) + (startX + x);
                            if (val > *pTarget) *pTarget = val;
                        }
                    }
                    curX += positions[i].XAdvance / 64.0f;                    
                    //curX += (int)slot->advance.x / 64.0f;
                }
            }            
            return matrix;
        }

        protected override void CleanupUnmanagedResources()
        {            
            m_HbFont?.Dispose();
            m_HbFont = null;
            
            m_HbFace?.Dispose();
            m_HbFace = null;

            if (m_Face != null)
            {
                //FT_Done_Face(m_Face);
                m_Face = null;
            }

            m_Blob?.Dispose();
			m_Blob = null;
            
            base.CleanupUnmanagedResources();
        }        
    }
}