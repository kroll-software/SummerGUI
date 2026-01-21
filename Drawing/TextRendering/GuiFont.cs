using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenTK;
using OpenTK.Mathematics;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Pfz.Collections;	// TreadSafeDictionary
using KS.Foundation;
using HarfBuzzSharp;
using FreeTypeSharp;
using static FreeTypeSharp.FT;
using System.Security.Cryptography;

namespace SummerGUI
{
	public struct GlyphInfo : IEquatable<GlyphInfo>
	{
		public GlyphInfo(int textureId)
		{				
			TextureId = textureId;
		}

		public int TextureId;

		public Vector2 Size;     // Bitmap size in pixels
		public Vector2 Bearing;  // left/top bearing
		public float Advance;    // pen advance in pixels

		public RectangleF UV;    // meist (0,0,1,1)		

		public static GlyphInfo Empty => default;

		public override bool Equals (object obj)
		{
			return (obj is GlyphInfo) && this.Equals ((GlyphInfo)obj);
		}

		public bool Equals (GlyphInfo other)
		{
			return TextureId == other.TextureId && UV == other.UV;
		}

        public static bool operator ==(GlyphInfo c1, GlyphInfo c2)
		{
			return c1.TextureId.Equals (c2.TextureId) && c1.UV.Equals(c2.UV);
		}

		public static bool operator !=(GlyphInfo c1, GlyphInfo c2)
		{
			return !c1.TextureId.Equals (c2.TextureId) || !c1.UV.Equals (c2.UV);
		}		

		public override int GetHashCode ()
		{
			return TextureId.CombineHash(UV.GetHashCode());
		}
	}

	public struct ShapedGlyph
	{
		public uint GlyphIndex;  // Der interne Index der Font (für den Atlas)
		public float XOffset;    // Feinjustierung X (Kerning/Positionierung)
		public float YOffset;    // Feinjustierung Y
		public float XAdvance;   // Wie weit der Cursor springt
		
		// Optional für Debugging oder Fallbacks
		public int Cluster;      // Index des ursprünglichen Zeichens im String

		public static ShapedGlyph Empty => default;
	}

	public interface IGUIFont : IDisposable
	{		
		string Name  { get; }
		float Size { get; }
		float LineHeight { get; }
		float TextBoxHeight { get; }
		float CaptionHeight { get; }
		float Height { get; }		
		float YOffset { get; }
		float LineSpacing { get; set; }
		float ScaleFactor { get; }

		GlyphInfo EllipsisGlyphInfo { get; }

		bool IsDisposed { get; }

		float Ascender { get; }
		float Descender { get; }		
		float LineGap { get; }

		string FilePath { get; }
		
		SizeF Measure(string text, int start = 0, int len = -1);
		SizeF MeasureGlyphs(string text, int start = 0, int len = -1);
		SizeF Measure(string text, float width, FontFormat sf);
		SizeF MeasureMnemonicString (string text);

		bool ContainsChar (char c);
		
		int CharPos (string text, float cursorPos);		

		bool GetGlyphInfo(char c, out GlyphInfo gli);
		bool GetGlyphInfo(uint glyphIndex, out GlyphInfo gli);		

		//ShapedGlyph[] ShapeText(string text);
		IEnumerable<ShapedGlyph> ShapeText(string text);

		GlyphChar GetGlyphChar (char c, SpecialCharacterFlags flags = SpecialCharacterFlags.Default);

		void Rescale (float scaleFactor);
	}

	public unsafe class GuiFont : DisposableObject, IGUIFont
	{				
		public string FilePath { get; private set; }
		public string Name { get; private set; }
		public bool Monospace { get; private set; }
		public int GlyphCount { get; private set; }		
		public int Count { get; private set; }
		public float Size { get; private set; }

		// Automatically adjusted values
		public float Height { get; private set; }		
		public float Ascender { get; private set; }
		public float Descender { get; private set; }
		public float LineGap { get; private set; }		
		public float LineSpacing { get; set; }		

		// Adjustments for Users/Configuration
		private float m_YOffsetUnscaled = 0;
		public float YOffset { get; set; }		

		//FontWrapper Font;
		//SharpFont.Face Font;
		FreeTypeSharp.FT_FaceRec_* m_Face;
		private HarfBuzzSharp.Blob m_Blob;
		private HarfBuzzSharp.Face m_HbFace;
        private HarfBuzzSharp.Font m_HbFont;	
		private int[] m_Textures;		
		
		ThreadSafeDictionary<char, GlyphInfo> CharMap;
		ThreadSafeDictionary<uint, GlyphInfo> GlyphMap;

		//private uint m_SpaceGlyphIndex;		

		private GlyphInfo m_EllipsisGlyphInfo = default;
		public GlyphInfo EllipsisGlyphInfo 
		{ 
			get
			{
				return m_EllipsisGlyphInfo;
			}		
		}

		public float LineHeight { get; private set; }
		public float CaptionHeight { get; private set; }
		public float TextBoxHeight { get; private set; }

		public float ScaleFactor { get; private set; }

		public GlyphFilterFlags Filter { get; private set; } 
		public bool OnDemand { get; private set; }

		private FontAtlasGroup m_AtlasGroup;

		private const int ATLAS_SIZE = 1024; // Standardgröße für den Font-Atlas				

		public GuiFont (GUIFontConfiguration conf)
			: this (conf.Path, conf.Size, conf.ScaleFactor, conf.Filter, conf.YOffset, conf.LineSpacing) {}

		public GuiFont (string filePath, float size, float scaleFactor, GlyphFilterFlags filter, float yoffset, float linespacing)
		{			
			try {			
				Name = Path.GetFileName(filePath);
				FilePath = filePath.FixedExpandedPath();
				Size = size;
				Filter = filter;
				OnDemand = Filter.HasFlag(GlyphFilterFlags.OnDemand);

				m_YOffsetUnscaled = yoffset;
				LineSpacing = linespacing;
				if (LineSpacing < 0.01)
					LineSpacing = 1;
				InitFont(scaleFactor);
			} catch (Exception ex) {
				ex.LogError ();
			}
		}

		private void InitFont(float scaleFactor)
		{
			try {
				System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

				ScaleFactor = scaleFactor;				

				// Reset everything
				Clear();

				// 1. Lade die Font-Daten einmalig in den Speicher (Blob)
				m_Blob = Blob.FromFile(FilePath);
			
				// 2. HarfBuzz Setup
				m_HbFace = new HarfBuzzSharp.Face(m_Blob, 0);
				m_HbFont = new HarfBuzzSharp.Font(m_HbFace);

				m_HbFont.SetFunctionsOpenType();				
				
				// 3. FreeType Setup aus demselben Speicherblock
				// Zugriff auf die Daten via Span
				ReadOnlySpan<byte> fontSpan = m_Blob.AsSpan();

				fixed (FT_FaceRec_** fp = &m_Face)
				fixed (byte* dataPtr = fontSpan)
				{
					// Wir nutzen FT_New_Memory_Face statt FT_New_Face
					var error = FT_New_Memory_Face(
						FontManager.Library, 
						dataPtr, 
						m_Blob.Length, 
						0, 
						fp
					);

					if (error != FT_Error.FT_Err_Ok)
						throw new Exception($"FreeType Memory Face Error: {error}");
				}

				// *** Metriken ***

				float size = Size.Scale(ScaleFactor);				
				FT_Set_Char_Size(m_Face, (int)(size / 64.0f), (int)(size / 64.0f), 72, 72);				

				int pixelSize = (size * 1.3334).Ceil();				
				FT_Set_Pixel_Sizes(m_Face, (uint)pixelSize, (uint)pixelSize);				
								
				float fscale = pixelSize / (m_Face->height / 64.0f) * 1.33334f;
				int h = (int)(pixelSize * 64);
            	m_HbFont.SetScale(h, h);				
				
				Ascender = m_Face->ascender / 64.0f * fscale;
				Descender = m_Face->descender / 64.0f * fscale;
				float heightFT = m_Face->height / 64.0f * fscale;				

				Height = Ascender - Descender;
				LineGap = heightFT - Height;
				if (LineGap < 0.0001f)
					LineGap = 0;
				
				//LineHeight = Ascender * 1.42f * LineSpacing;
				LineHeight = (Height + LineGap) * LineSpacing;
				TextBoxHeight = Ascender * 1.85f + 2;
				CaptionHeight = Ascender * 1.55f + 2;

				YOffset = m_YOffsetUnscaled * scaleFactor;

				// *** Atlas Initialisierung: Nur wenn nicht On-Demand ***
				if (!OnDemand) {
					// Konstanten für die Sicherheit
					const int MAX_SAFE_ATLAS_SIZE = 4096; // 4k ist auf fast allen GPUs seit 2012 sicher
					const float PACKING_EFFICIENCY = 1.4f; // Puffer für Verschnitt beim Packen					

					// 1. Berechne die vertikale Höhe, die eine Glyphe maximal einnimmt
					// Nutze entweder dein berechnetes 'pixelSize' oder die FreeType-Metrik:
					float estimatedHeight = (float)m_Face->size->metrics.height / 64.0f;
					if (estimatedHeight <= 0) estimatedHeight = pixelSize; // Fallback

					// 2. Schätzung der Fläche pro Glyph inkl. Padding und Spacing
					// Wir nehmen ein Quadrat mit der Seitenlänge der Schrifthöhe.
					float areaPerGlyph = estimatedHeight * estimatedHeight * PACKING_EFFICIENCY; 

					// 3. Gesamtfläche für alle Glyphen (GlyphCount hast du bereits ermittelt)
					float totalArea = areaPerGlyph * GlyphCount;

					// 4. Kantenlänge berechnen und auf 2er-Potenz runden
					int side = (int)Math.Sqrt(totalArea);
					int atlasSize = side.NextPowerOf2().Clamp(512, MAX_SAFE_ATLAS_SIZE);

					m_AtlasGroup = new FontAtlasGroup(atlasSize);
				}
				
				GlyphCount = (int)m_Face->num_glyphs;
				int glyphCount = GlyphCount;				
				Monospace = (m_Face->face_flags & (int)FT_FACE_FLAG.FT_FACE_FLAG_FIXED_WIDTH) != 0;
				
				byte* psNamePtr = FT_Get_Postscript_Name(m_Face);
				if (psNamePtr != null)
				{
					// Konvertiert den ANSI-C-String (byte*) in einen C#-String
					string tmpName = Marshal.PtrToStringAnsi((IntPtr)psNamePtr);					
					if (!String.IsNullOrEmpty(tmpName))
						Name = tmpName;
				}
				
				// We support 4 different glyph loading strategies:
				//
				// (1) All: all glyphs loaded at once on start
				// (2) Filtered: all filtered glyphs loaded at once on start
				// (3) OnDemand: no glyphs loaded at start, all glyphs on demand

				if (OnDemand) {
					// Startegy (3)
					GlyphCount = 0;
				}
				else if (Filter > GlyphFilterFlags.OnDemand) {
					// Startegy (2)
					// If we have a Filter set, let's count the number of valid glyphs
					// to minimize graphics memory.
					uint glyphindex;
					//uint cc = Font.GetFirstChar(out glyphindex);
					uint cc = (uint)FT_Get_First_Char(m_Face, &glyphindex);
					int count = 0;
					while (glyphindex > 0) {
						char c = (char)cc;
						if (Filter.IsValid(c))
							count++;
						cc = (uint)FT_Get_Next_Char (m_Face, cc, &glyphindex);
					}
					GlyphCount = count;
				} else {
					// Strategy (1), loading the entire font
				}

				//m_Textures = new int[Math.Max(32, GlyphCount)];
				if (OnDemand) {
					m_Textures = new int[32]; // Buffer für On-Demand Texturen
				}
				CharMap = new ThreadSafeDictionary<char, GlyphInfo>(Math.Max(31, GlyphCount));
				GlyphMap = new ThreadSafeDictionary<uint, GlyphInfo>(Math.Max(31, GlyphCount));

				if (!OnDemand)
				{
					// Strategy (1) + (2): Alle Glyphen vorab laden
					GL.GenTextures(GlyphCount, m_Textures);

					uint glyphIndex;
					// Wir iterieren über die Unicode-Map der Font
					uint charCode = (uint)FT_Get_First_Char(m_Face, &glyphIndex);

					while (glyphIndex > 0) 
					{
						char c = (char)charCode;
						
						// Filter prüfen (z.B. nur ASCII oder bestimmter Bereich)
						if (Filter.IsValid(c)) 
						{
							try {
								// 1. Glyphe nur einmal kompilieren (falls mehrere Chars auf denselben Index zeigen)
								if (!GlyphMap.TryGetValue(glyphIndex, out GlyphInfo info))
								{
									info = CompileCharacter(m_Face, glyphIndex);
									GlyphMap.Add(glyphIndex, info);
								}

								// 2. Im Char-Lookup für klassisches Rendering registrieren
								if (!CharMap.ContainsKey(c))
								{
									CharMap.Add(c, info);
								}
							} catch (Exception ex) {
								ex.LogWarning();
							}
						}
						// Nächstes Zeichen holen
						charCode = (uint)FT_Get_Next_Char(m_Face, charCode, &glyphIndex);          
					}
					
					// Ellipsis für Text-Trimming vorab holen
					CharMap.TryGetValue(SpecialCharacters.Ellipsis, out m_EllipsisGlyphInfo);
				}				

				if (OnDemand) {
					Count.LogInformation ("Font {0} ({1}), {2}/{3} glyphs pre-loaded in {4} ms, more glyphs are loaded on demand.", Name, Size, Count, glyphCount, sw.ElapsedMilliseconds);
				} else {
					Count.LogInformation ("Font {0} ({1}), {2}/{3} glyphs loaded in {4} ms.", Name, Size, Count, glyphCount, sw.ElapsedMilliseconds);
				}
			} catch (Exception ex) {
				ex.LogError ();
			} finally {
				if (!OnDemand && m_Face != null) {					
					FT_Done_Face(m_Face);
					m_Face = null;
				}
			}			
		}

		public void Rescale (float scaleFactor)
		{
			if (ScaleFactor == scaleFactor)
				return;
			InitFont(scaleFactor);
		}

		public unsafe GlyphInfo CompileCharacter(FT_FaceRec_* face, uint glyphindex)
		{			
			//const int FT_LOAD_TARGET_LIGHT = 0x00010000;

			//FT_Error error = FT_Load_Glyph(face, glyphindex, FT_LOAD.FT_LOAD_FORCE_AUTOHINT);			
			FT_Error error = FT_Load_Glyph(face, glyphindex, FT_LOAD.FT_LOAD_DEFAULT);
			
			if (error != FT_Error.FT_Err_Ok) return GlyphInfo.Empty;

			error = FT_Render_Glyph(face->glyph, FT_Render_Mode_.FT_RENDER_MODE_NORMAL);			
			if (error != FT_Error.FT_Err_Ok) return GlyphInfo.Empty;

			FT_Bitmap_ bmp = face->glyph->bitmap;
			int width = (int)bmp.width;
			int rows = (int)bmp.rows;

			if (width <= 0 || rows <= 0) {
				return new GlyphInfo {
					TextureId = 0, Size = Vector2.Zero, Bearing = Vector2.Zero,
					Advance = face->glyph->advance.x / 64.0f, UV = RectangleF.Empty
				};
			}

			// Die rohen Pixel-Daten
			byte[] pixels = new byte[width * rows];
			fixed (byte* pPixels = pixels)
			{
				byte* src = bmp.buffer;
				int pitch = bmp.pitch;

				// Fall 1: Die Daten liegen bereits kompakt vor (kein Padding pro Zeile)
				if (pitch == width)
				{
					System.Buffer.MemoryCopy(src, pPixels, pixels.Length, pixels.Length);
				}
				// Fall 2: Wir müssen den Pitch berücksichtigen (Zeile für Zeile)
				else
				{
					byte* pDest = pPixels;
					for (int y = 0; y < rows; y++)
					{
						// Kopiere eine Zeile
						System.Buffer.MemoryCopy(src, pDest, width, width);
						
						// Pointer-Arithmetik: Einfache Addition statt Multiplikation
						src += pitch;      // Springe zur nächsten Quell-Zeile (inkl. Padding)
						pDest += width;    // Springe zur nächsten Ziel-Zeile (kompakt)
					}
				}
			}					

			// --- FALL 1: ATLAS GRUPPEN NUTZUNG MIT GUTTER ---			
			if (m_AtlasGroup != null) 
			{
				// Wir fordern 2 Pixel mehr Platz an (1px Gutter an jeder Seite)
				int padding = 2;
				int halfpadding = 1;
				if (m_AtlasGroup.TryPack(width + padding, rows + padding, out int texId, out int x, out int y, out int aWidth, out int aHeight)) 
				{
					// Wir laden den Buchstaben versetzt um 1 Pixel hoch
					// Damit bleibt rundherum ein leerer Rand
					m_AtlasGroup.CurrentActiveAtlas.UploadGlyph(x + halfpadding, y + halfpadding, width, rows, pixels);
					Count++;

					return new GlyphInfo {
						TextureId = texId,
						Size = new Vector2(width, rows),
						Bearing = new Vector2(face->glyph->bitmap_left, face->glyph->bitmap_top),
						Advance = face->glyph->advance.x / 64.0f,
						
						// UV-Koordinaten müssen exakt auf den inneren Bereich (x+1, y+1) zeigen
						UV = new RectangleF(
							(x + halfpadding) / (float)aWidth,
							(y + halfpadding) / (float)aHeight,
							width / (float)aWidth,
							rows / (float)aHeight)
					};
				}
			}			

			// --- FALL 2: EINZELTEXTUREN (OnDemand / FontAwesome) ---
			int expandedW = (width + 1).NextPowerOf2();
			int expandedH = rows.NextPowerOf2();
			byte[] expandedPixels = new byte[expandedW * expandedH];

			for (int y = 0; y < rows; y++)
				for (int x = 0; x < width; x++)
					expandedPixels[x + y * expandedW] = pixels[x + y * width];

			int tex;
			GL.GenTextures(1, out tex);
			GL.BindTexture(TextureTarget.Texture2D, tex);
			// (Parameter wie bisher setzen...)
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, 
						expandedW, expandedH, 0, PixelFormat.Red, PixelType.UnsignedByte, expandedPixels);

			Count++;
			return new GlyphInfo {
				TextureId = tex,
				Size = new Vector2(width, rows),
				Bearing = new Vector2(face->glyph->bitmap_left, face->glyph->bitmap_top),
				Advance = face->glyph->advance.x / 64.0f,
				UV = new RectangleF(0, 0, width / (float)expandedW, rows / (float)expandedH)
			};
		}

		public bool GetGlyphInfo(char c, out GlyphInfo gli)
		{
			// Erst im Char-Cache schauen
			if (CharMap.TryGetValue(c, out gli)) return true;

			// Sonst Index holen und die neue Methode nutzen
			uint index = FT_Get_Char_Index(m_Face, c);
			if (GetGlyphInfo(index, out gli))
			{
				// Für das nächste Mal im Char-Cache merken
				CharMap.Add(c, gli);
				return true;
			}

			return false;
		}

		public bool GetGlyphInfo(uint glyphIndex, out GlyphInfo gli)
		{
			if (GlyphMap.TryGetValue(glyphIndex, out gli))
			{
				return true;
			}
			else if (OnDemand)
			{
				lock (SyncObject)
				{
					// Überladung von CompileCharacter, die nur den Index nutzt
					gli = CompileCharacter(m_Face, glyphIndex); 
					GlyphMap.Add(glyphIndex, gli);
					return true;
				}
			}
			gli = GlyphInfo.Empty;
			return false;
		}

		public GlyphChar GetGlyphChar(char c, SpecialCharacterFlags flags = SpecialCharacterFlags.Default)
		{
			lock (SyncObject) {
				char g = c;
				switch (c) {
				case ' ':
					if (flags.HasFlag (SpecialCharacterFlags.WhiteSpace))
						g = SpecialCharacters.SpaceDot;
					break;
				case '\n':
					if (flags.HasFlag (SpecialCharacterFlags.LineBreaks))
						g = SpecialCharacters.Paragraph;				
					break;
				case '\r':
					return GlyphChar.Empty;			
				}

				try {
					GlyphInfo gi;
					if (GetGlyphInfo (g, out gi)) {
						return new GlyphChar(c, gi.Advance);
					}
				} catch (Exception ex) {
					ex.LogError ();
				} 
				return GlyphChar.Empty;
			}
		}		

		public bool ContainsChar(char c)
		{
			return CharMap.ContainsKey (c);
		}

		public int CharPos(string text, float cursorPos)
		{
			if (text == null)
				return 0;

			float adv = 0;
			GlyphInfo gi;
			for (int i = 0; i < text.Length; i++) {
				if (GetGlyphInfo(text [i], out gi)) {
					if (adv + (gi.Advance / 2f) >= cursorPos)
						return i;
					adv += gi.Advance;
				}
			}
			return text.Length;
		}
		
		public IEnumerable<ShapedGlyph> ShapeText(string text)
		{
			using var buffer = new HarfBuzzSharp.Buffer();
			buffer.AddUtf8(text);
			buffer.GuessSegmentProperties();
			m_HbFont.Shape(buffer);

			var hbInfos = buffer.GlyphInfos;
			var hbPositions = buffer.GlyphPositions;		

			for (int i = 0; i < buffer.Length; i++)
			{
				yield return new ShapedGlyph
				{
					GlyphIndex = hbInfos[i].Codepoint,
					XOffset = hbPositions[i].XOffset / 64.0f,
					YOffset = hbPositions[i].YOffset / 64.0f,
					XAdvance = hbPositions[i].XAdvance / 64.0f,
					Cluster = (int)hbInfos[i].Cluster
				};				
			}

			yield break;
		}

		public SizeF Measure(string text, int start = 0, int len = -1)
		{               
			if (string.IsNullOrEmpty(text))
				return SizeF.Empty;

			string measureText;
			if (len == -1 && start == 0)
				measureText = text;
			else			
				measureText = Strings.StrMid(text, start + 1, len);			

			return new SizeF(ShapeText(measureText).Sum(g => g.XAdvance), Height);
		}

		public SizeF MeasureGlyphs(string text, int start = 0, int len = -1)
		{               
			if (string.IsNullOrEmpty(text))
				return SizeF.Empty;

			string measureText;
			if (len == -1 && start == 0)
				measureText = text;
			else			
				measureText = Strings.StrMid(text, start + 1, len);

			float adv = 0;
			GlyphInfo gi;
			foreach (char c in measureText)
			{
				if (GetGlyphInfo(c, out gi))
					adv += gi.Advance;
			}
						
			return new SizeF(adv, Height);
		}

		public SizeF MeasureMnemonicString(string text)
		{                   
			if (string.IsNullOrEmpty(text))
				return SizeF.Empty;
			
			float adv = 0;
			foreach (ShapedGlyph si in ShapeText(text))
			{
				if (GetGlyphInfo(si.GlyphIndex, out var gi)) 
				{
					char c = si.Cluster < text.Length ? text[si.Cluster] : (char)0;
					if (c != '&')
					{
						adv += si.XAdvance;						
					}
				}
			}
			
			return new SizeF(adv, Height);
		}

		public SizeF Measure(string text, float maxWidth, FontFormat format)
		{       
			if (string.IsNullOrEmpty(text))
				return SizeF.Empty;

			float maxLineWidth = 0;
			int lineCount = 1;
			
			bool wrapEnabled = format.HasFlag(FontFormatFlags.WrapText) && maxWidth > 0;

			// Wir arbeiten mit ReadOnlySpan für maximale Performance ohne Substring-Allokationen
			ReadOnlySpan<char> span = text.AsSpan();
			int start = 0;
			int lastBreakIdx = -1;

			for (int i = 0; i < span.Length; i++)
			{
				char c = span[i];

				// 1. Manueller Zeilenumbruch
				if (c == '\n')
				{
					float lineWidth = MeasureSegment(span.Slice(start, i - start));
					maxLineWidth = Math.Max(maxLineWidth, lineWidth);
					
					start = i + 1;
					lineCount++;
					lastBreakIdx = -1;
					continue;
				}

				// Merke dir Umbruchstellen (Leerzeichen/Bindestrich/IsWrapCharacter)
				if (char.IsWhiteSpace(c) || c == '-' || c.IsWrapCharacter())
				{
					lastBreakIdx = i;
				}

				// 2. Automatischer Zeilenumbruch (Wrap)
				if (wrapEnabled)
				{
					// Wir messen das aktuelle Wort/Segment bis hierhin
					float currentWidth = MeasureSegment(span.Slice(start, i - start + 1));

					if (currentWidth > maxWidth)
					{
						if (lastBreakIdx != -1 && lastBreakIdx > start)
						{
							// Umbruch am letzten Space
							float lineWidth = MeasureSegment(span.Slice(start, lastBreakIdx - start + 1));
							maxLineWidth = Math.Max(maxLineWidth, lineWidth);
							
							i = lastBreakIdx; // Springe zurück
							start = i + 1;
						}
						else
						{
							// Wort zu lang -> Notumbruch (umbruch direkt vor dem aktuellen Zeichen)
							float lineWidth = MeasureSegment(span.Slice(start, i - start));
							maxLineWidth = Math.Max(maxLineWidth, lineWidth);
							start = i;
							i--; // Dieses Zeichen in der nächsten Zeile prüfen
						}
						lineCount++;
						lastBreakIdx = -1;
					}
				}
			}

			// Restliche Zeile messen
			if (start < span.Length)
			{
				maxLineWidth = Math.Max(maxLineWidth, MeasureSegment(span.Slice(start)));
			}

			float totalHeight = Height + (lineCount - 1) * LineHeight;
			return new SizeF(maxLineWidth, totalHeight);
		}

		// Private Helper-Methode, die HarfBuzz nutzt
		private float MeasureSegment(ReadOnlySpan<char> segment)
		{
			if (segment.IsEmpty) return 0;
			return ShapeText(segment.ToString()).Sum(g => g.XAdvance);			
		}		
				
		private void Clear()
		{
			try {
				CharMap?.Clear();
				GlyphMap?.Clear();

				m_HbFont?.Dispose();
				m_HbFont = null;
				
				m_HbFace?.Dispose();
				m_HbFace = null;

				if (m_Face != null) {
					FT_Done_Face(m_Face);
					m_Face = null;
				}

				m_Blob?.Dispose();
				m_Blob = null;

				GL.BindTexture (TextureTarget.Texture2D, 0);
				
				if (m_Textures != null)
				{
					foreach (var t in m_Textures)
					{
						if (t > 0)
							GL.DeleteTexture(t);
					}					
				}

				if (m_AtlasGroup != null)
				{
					m_AtlasGroup.Dispose();
					m_AtlasGroup = null;
				}				
				
			} catch (Exception ex) {
				ex.LogError ();
			} finally {
				GlyphCount = 0;
				m_Textures = null;
				Height = 0;
				Count = 0;
				m_EllipsisGlyphInfo = GlyphInfo.Empty;
			}
		}

		protected override void CleanupManagedResources ()
		{						
			base.CleanupManagedResources ();
		}

		protected override void CleanupUnmanagedResources ()
		{
			// THESE are unmanaged resources, aren't they ?
			Clear();
			base.CleanupUnmanagedResources ();
		}
	}			
}

