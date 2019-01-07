using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Pfz.Collections;	// TreadSafeDictionary
using KS.Foundation;
using SharpFont;

namespace SummerGUI
{
	public class GuiFont : DisposableObject, IGUIFont
	{		
		public class GlyphExtents
		{
			public float AdvanceX;
			public float BearingX;
			public float BearingY;
			public float Width;
			public float Height;
		}

		public float Size { get; private set; }
		public string FilePath { get; private set; }

		// Automatically adjusted values
		public float Ascender { get; private set; }
		public float Descender { get; private set; }
		public float Height { get; private set; }

		public bool Monospace { get; private set; }
		public int GlyphCount { get; private set; }
		public string Name { get; private set; }

		// Adjustments for Users/Configuration
		public float YOffset { get; set; }
		private float YOffsetScaled;
		public float LineSpacing { get; set; }

		//FontWrapper Font;
		SharpFont.Face Font;
		private int	m_ListBase;
		private int[] m_Textures;
		//private int[] m_ExtentsX;

		//Dictionary<char, uint> CharMap;
		ThreadSafeDictionary<char, GlyphInfo> CharMap;

		//private uint m_SpaceGlyphIndex;
		private GlyphInfo m_EllipsisGlyphIndex;
		private float HalfHeight { get; set; }

		public float LineHeight { get; private set; }
		public float CaptionHeight { get; private set; }
		public float TextBoxHeight { get; private set; }

		public float ScaleFactor { get; private set; }

		public GlyphFilterFlags Filter { get; private set; } 
		public bool OnDemand { get; private set; }

		public struct GlyphInfo : IEquatable<GlyphInfo>
		{
			public static readonly GlyphInfo Empty = new GlyphInfo (0, 0);

			public readonly int ListID;
			public readonly int Width;

			public bool IsEmpty
			{
				get{
					return ListID <= 0;
				}
			}

			public GlyphInfo(int listID, int widh)
			{				
				ListID = listID;			
				Width = widh;
			}

			public override bool Equals (object obj)
			{
				return (obj is GlyphInfo) && this.Equals ((GlyphInfo)obj);
			}

			public bool Equals (GlyphInfo other)
			{
				return ListID == other.ListID;
			}				

			public override int GetHashCode ()
			{
				return ListID;
			}
		}

		private bool GetGlyphIndex(char c, out GlyphInfo gli)
		{
			GlyphInfo info;
			if (CharMap.TryGetValue (c, out info)) {				
				if (info.ListID > 0 && info.Width > 0) {
					gli = info;
					return true;
				} else {					
					gli = GlyphInfo.Empty;
					return false;
				}
			} else if (OnDemand) {
				lock (SyncObject) {
					uint glyphindex = Font.GetCharIndex (c);
					info = CompileCharacter (Font, glyphindex, c);
					CharMap.Add (c, info);
					gli = info;
				}
				return true;
			}
				
			gli = GlyphInfo.Empty;
			return false;
		}

		private void Clear()
		{
			try {
				if (Font != null) {
					Font.Dispose();
					Font = null;
				}

				GL.BindTexture (TextureTarget.Texture2D, 0);

				if (GlyphCount > 0) {
					if (m_ListBase > 0) {
						GL.DeleteLists (m_ListBase, GlyphCount);
					}
					else if (!CharMap.IsNullOrEmpty ()) {
						CharMap.Values.Select (val => val.ListID).ForEach (lid => GL.DeleteLists (lid, 1));
						CharMap.Clear();
					}
				}
				if (!m_Textures.IsNullOrEmpty())
					GL.DeleteTextures (m_Textures.Length, m_Textures);				
			} catch (Exception ex) {
				ex.LogError ();
			} finally {
				GlyphCount = 0;
				m_ListBase = 0;
				m_Textures = null;
				Height = 0;
				Count = 0;
				m_EllipsisGlyphIndex = GlyphInfo.Empty;
			}
		}

		private void InitFont(float scaleFactor)
		{
			try {
				System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

				ScaleFactor = scaleFactor;
				YOffsetScaled = YOffset * scaleFactor;

				// Reset everything
				Clear();

				Font = new Face(FontManager.Library, FilePath);

				// Go on

				float size = Size.Scale(ScaleFactor);

				Fixed26Dot6 sz = new Fixed26Dot6(size / 64);
				Font.SetCharSize(sz, sz, 72, 72);

				int pixelSize = (size * 1.3334).Ceil();
				Font.SetPixelSizes((uint)pixelSize, (uint)pixelSize);

				GlyphCount = Font.GlyphCount;
				int glyphCount = GlyphCount;
				Monospace = Font.FaceFlags.HasFlag(FaceFlags.FixedWidth);

				string tmpName = Font.GetPostscriptName();
				if (!String.IsNullOrEmpty(tmpName))
					Name = tmpName;


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
					uint cc = Font.GetFirstChar(out glyphindex);
					int count = 0;
					while (glyphindex > 0) {
						char c = (char)cc;
						if (Filter.IsValid(c))
							count++;
						cc = Font.GetNextChar (cc, out glyphindex);			
					}
					GlyphCount = count;
				} else {
					// Strategy (1), loading the entire font
				}

				m_Textures = new int[Math.Max(32, GlyphCount)];
				CharMap = new ThreadSafeDictionary<char, GlyphInfo>(Math.Max(31, GlyphCount));

				if (!OnDemand) {
					// Strategy (1) + (2): Load all or filtered glyphs
					m_ListBase = GL.GenLists (GlyphCount);
					GL.GenTextures (GlyphCount, m_Textures);

					uint glyphindex;
					uint cc = Font.GetFirstChar(out glyphindex);
					while (glyphindex > 0) {
						char c = (char)cc;
						if (!CharMap.ContainsKey(c) && Filter.IsValid(c)) {
							try {								
								CharMap.Add (c, CompileCharacter (Font, glyphindex, c));
							} catch (Exception ex) {
								ex.LogWarning();
							}
						}
						cc = Font.GetNextChar (cc, out glyphindex);			
					}
					CharMap.TryGetValue(SpecialCharacters.Ellipsis, out m_EllipsisGlyphIndex);
				}
				else {					
					try {
						GetGlyphIndex (SpecialCharacters.Ellipsis, out m_EllipsisGlyphIndex);	
					} catch (Exception ex) {
						ex.LogError();
					}
				}

				//if (Height <= 1)
				//Height = pixelSize.NextPowerOf2();
				//Height = pixelSize * 1.33335f;

				Height = pixelSize;

				float fscale = Height / Font.Height * 1.33334f;
				//float fscale = Height / Font.Height * 0.776f;
				
				Ascender = Font.Ascender * fscale;
				Descender = Font.Descender * fscale;
				//HalfHeight = Height / 2;

				Height = (Ascender).Ceil();
				HalfHeight = (int)(Height / 2);

				//LineHeight = Height * 1.42f * LineSpacing;
				LineHeight = (int)((Height * 1.42f * LineSpacing) + 0.5f);

				//TextBoxHeight = ((Height * 2f) + (ScaleFactor * 2f)).Ceil();
				//TextBoxHeight = (int)(Height * 1.85f + 0.5f);
				TextBoxHeight = (int)(Height * 1.85f + 2);
				CaptionHeight = (int)(Height * 1.55 + 2);

				YOffsetScaled = (YOffset * ScaleFactor) - HalfHeight;

				if (OnDemand) {
					Count.LogInformation ("Font {0} ({1}), {2}/{3} glyphs pre-loaded in {4} ms, more glyphs are loaded on demand.", Name, Size, Count, glyphCount, sw.ElapsedMilliseconds);
				} else {
					Count.LogInformation ("Font {0} ({1}), {2}/{3} glyphs loaded in {4} ms.", Name, Size, Count, glyphCount, sw.ElapsedMilliseconds);
				}
			} catch (Exception ex) {
				ex.LogError ();
			} finally {
				if (!OnDemand && Font != null) {					
					Font.Dispose ();
					Font = null;
				}
			}			
		}

		public GuiFont (GUIFontConfiguration conf)
			: this (conf.Path, conf.Size, conf.ScaleFactor, conf.Filter, conf.YOffset, conf.LineSpacing)
		{
		}

		public GuiFont (string filePath, float size, float scaleFactor, GlyphFilterFlags filter, float yoffset, float linespacing)
		{			
			try {			
				Name = Path.GetFileName(filePath);
				FilePath = filePath.FixedExpandedPath();
				Size = size;
				Filter = filter;
				OnDemand = Filter.HasFlag(GlyphFilterFlags.OnDemand);

				YOffset = yoffset;
				LineSpacing = linespacing;
				if (LineSpacing < 0.01)
					LineSpacing = 1;
				InitFont(scaleFactor);
			} catch (Exception ex) {
				ex.LogError ();
			}
		}

		public void Rescale (float scaleFactor)
		{
			if (ScaleFactor == scaleFactor)
				return;
			InitFont(scaleFactor);
		}

		public int CharPos(string text, float cursorPos)
		{
			if (text == null)
				return 0;

			float adv = 0;
			GlyphInfo gi;
			for (int i = 0; i < text.Length; i++) {
				if (GetGlyphIndex(text [i], out gi)) {
					if (adv + (gi.Width / 2f) >= cursorPos)
						return i;
					adv += gi.Width;
				}
			}
			return text.Length;
		}

		public SizeF Measure(string text, int start = 0, int len = -1)
		{			
			if (String.IsNullOrEmpty(text))
				return SizeF.Empty;

			if (len < 0)
				len = text.Length;
			else
				len = Math.Min (len, text.Length);

			float adv = 0;
			GlyphInfo gi;
			for (int i = start; i < len; i++) {
				if (GetGlyphIndex(text [i], out gi)) {
					adv += gi.Width;
				}
			}

			return new SizeF(adv, Height);
		}

		public SizeF MeasureMnemonicString(string text)
		{			
			if (String.IsNullOrEmpty(text))
				return SizeF.Empty;
			float adv = 0;
			GlyphInfo gi;
			for (int i = 0; i < text.Length; i++) {
				if (text [i] != '&' && GetGlyphIndex(text [i], out gi)) {
					adv += gi.Width;
				}
			}
			return new SizeF(adv, Height);
		}

		private SizeF PrintElipsisString(string text, float width, FontFormat format)
		{
			if (String.IsNullOrEmpty(text))
				return SizeF.Empty;

			const int rightDotDistance = 2;
			int[] textbytes = new int[text.Length];
			float adv = 0;
			int i;
			int len = text.Length;
			for (i = 0; i < len; i++) {								
				GlyphInfo gi;
				if (GetGlyphIndex (text [i], out gi)) {					
					if (adv + gi.Width > width && i > 0) {
						try {					
							// add ellipsis character to where it fits
							float desiredSpace = m_EllipsisGlyphIndex.Width + (rightDotDistance * ScaleFactor);
							i--;
							while (i > 1 && adv + desiredSpace > width) {
								if (GetGlyphIndex (text [i], out gi))
									adv -= gi.Width;								
								i--;							
							}
							textbytes [i] = m_EllipsisGlyphIndex.ListID;
							adv += m_EllipsisGlyphIndex.Width;
							i++;
						} catch (Exception ex) {
							ex.LogError ();
						}							
						break;
					}						
					adv += gi.Width;
					textbytes [i] = gi.ListID;
				}
			}
				
			GL.CallLists (i, ListNameType.Int, textbytes);

			if (format.HasFlag (FontFormatFlags.Underline)) {
				float y = -3f * ScaleFactor;	// ToDo: DPI Scaling
				GL.Disable(EnableCap.Texture2D);
				GL.Disable(EnableCap.Texture1D);
				GL.Disable(EnableCap.TextureRectangle);
				GL.LineWidth (1f * ScaleFactor);	// ToDo: DPI Scaling
				GL.Begin (PrimitiveType.Lines);
				GL.Vertex2 (0, y);
				GL.Vertex2 (-adv + 0.5f, y);
				GL.End ();
			}
				
			if (i < len)
				return new SizeF (width + 1, Height);
			else
				return new SizeF (adv, Height);
		}

		public bool ContainsChar(char c)
		{
			return CharMap.ContainsKey (c);
		}

		public float CharWidth(char c)
		{
			GlyphInfo gi;
			if (GetGlyphIndex (c, out gi))
				return gi.Width;
			return 0;
		}						

		public GlyphChar GetGlyph(char c, SpecialCharacterFlags flags)
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
					if (GetGlyphIndex (g, out gi)) {
						return new GlyphChar(c, (uint)gi.ListID, gi.Width);
					}
				} catch (Exception ex) {
					ex.LogError ();
				} 
				return GlyphChar.Empty;
			}
		}

		public SizeF PrintSelectedString(string text, int selStart, int selLength, RectangleF bounds, float offsetX, FontFormat format, Color foreColor, Color selectionBackColor, Color selectionForeColor)
		{
			if (string.IsNullOrEmpty (text))
				return SizeF.Empty;

			RectangleF rContent;
			PointF presult;

			SizeF contentSize;
			if (format.Flags.HasFlag(FontFormatFlags.WrapText))
				contentSize = Measure( text, bounds.Width, format);
			else
				contentSize = Measure(text);

			rContent = new RectangleF (0, 0, contentSize.Width, contentSize.Height);
			presult = BoxAlignment.AlignBoxes (rContent, bounds, format, Ascender, Descender);								

			GL.Color3 (foreColor);
			GL.Translate(Math.Floor(bounds.X + presult.X + offsetX), 
				-Math.Ceiling(Ascender - Descender + YOffsetScaled + bounds.Y + presult.Y), 0f);

			int[] textbytes;
			float w = 0;

			if (selLength == 0)
				selStart = 0;
			else {
				if (selStart > 0) {
					textbytes = new int[selStart];
					for (int i = 0; i < selStart; i++) {
						GlyphInfo gi;
						if (GetGlyphIndex (text [i], out gi)) {
							w += gi.Width;
							textbytes [i] = gi.ListID;				
						}
					}

					GL.CallLists (selStart, ListNameType.Int, textbytes);
					//GL.Translate (w, 0, 0);
				}
								
				selLength = Math.Min (selLength, text.Length - selStart);
				if (selLength > 0) {
					textbytes = new int[selLength];
					float wStart = w;
					for (int i = selStart; i < selStart + selLength; i++) {
						GlyphInfo gi;
						if (GetGlyphIndex (text [i], out gi)) {
							w += gi.Width;
							textbytes [i - selStart] = gi.ListID;
						}
					}

					using (new PaintWrapper (null)) {
						GL.Color4 (selectionBackColor);
						GL.Rect (bounds.Left + wStart + offsetX, bounds.Top, bounds.Left + w + offsetX, bounds.Bottom);
					}

					//GL.ListBase(m_ListBase);
					GL.Enable(EnableCap.Texture2D);

					// doesn't work on windows computers with ATI card
					//GL.Enable(EnableCap.TextureRectangle);	

					GL.Color3 (selectionForeColor);
					GL.CallLists (selLength, ListNameType.Int, textbytes);
				}
			}

			int start = selStart + selLength;
			int len = text.Length - start;
			if (len > 0) {
				textbytes = new int[len];
				for (int i = start; i < text.Length; i++) {
					GlyphInfo gi;
					if (GetGlyphIndex (text [i], out gi)) {
						textbytes [i - start] = gi.ListID;				
					}
				}
				GL.Color3 (foreColor);
				GL.CallLists (len, ListNameType.Int, textbytes);
			}

			return new SizeF (w, Height);
		}

		private SizeF PrintMenomicString (string text, RectangleF bounds, bool showMnemonics)
		{			
			int[] textbytes = new int[text.Length];
			float w = 0;
			float mstart = -1, mend = 0;
			for (int i = 0; i < text.Length; i++) {								
				char c = text [i];
				GlyphInfo gi;
				if (GetGlyphIndex (c, out gi)) {
					if (c == '&') {
						if (mstart < 0) {
							mstart = w;
							mend = w + gi.Width;
						}
					} else {
						w += gi.Width;
						textbytes [i] = gi.ListID;
					}
				}
			}

			GL.CallLists (text.Length, ListNameType.Int, textbytes);
			if (showMnemonics && mstart >= 0 && mend > mstart) {				
				float y = -3.5f * ScaleFactor;	// ToDo: DPI Scaling
				GL.Disable(EnableCap.Texture2D);
				GL.Disable(EnableCap.Texture1D);
				GL.Disable(EnableCap.TextureRectangle);
				GL.LineWidth (ScaleFactor);	// ToDo: DPI Scaling
				GL.Begin (PrimitiveType.Lines);
				GL.Vertex2 (mend - w - 1f, y);
				GL.Vertex2 (mstart - w, y);
				GL.End ();
			}

			return new SizeF (w, Height);
		}

		public void PrintTextLine (uint[] glyphs, RectangleF bounds, Color foreColor)
		{
			if (glyphs == null)
				return;			
			GL.Color3 (foreColor);

			RectangleF rContent;
			PointF presult;

			rContent = new RectangleF (0, 0, bounds.Width, Height);
			presult = BoxAlignment.AlignBoxes (rContent, bounds, FontFormat.DefaultSingleLine, Ascender, Descender);

			GL.Translate(Math.Floor(bounds.X + presult.X), 
				-Math.Ceiling(Ascender - Descender + YOffsetScaled + bounds.Y + presult.Y), 0f);

			GL.CallLists (glyphs.Length, ListNameType.Int, glyphs);
		}

		public virtual bool IsBreakChar(char c)
		{
			return c.IsWrapCharacter();
		}

		public SizeF Measure(string text, float width, FontFormat sf)
		{
			if (String.IsNullOrEmpty(text))
				return SizeF.Empty;

			float adv = 0;
			float maxAdv = 0;
			int lines = 1;
			GlyphInfo gi;

			int lastStopChar = 0;
			int maxStop = text.Length - 1;
			int i;
			int start = 0;
			for (i = start; i < text.Length; i++) {	
				bool breakFlag = false;
				char c = text [i];
				if (c == '\n') {
					breakFlag = true;
					lastStopChar = i;
				} else {
					if (IsBreakChar (c) && i < maxStop) {						
						lastStopChar = i;				
					}

					if (GetGlyphIndex (c, out gi)) {					
						float a = gi.Width;
						if (adv + a > width && i > 0) {
							breakFlag = true;
							if (lastStopChar <= start) {								
								adv = a;
							}
						}
						else
							adv += a;
					}						
				}

				if (breakFlag) {
					maxAdv = Math.Max (maxAdv, adv);

					if (lastStopChar > start) {
						i = lastStopChar;
						start = i + 1;
						adv = 0;
					} else {
						start = i;					
					}

					lastStopChar = 0;
					lines++;
				}
			}				

			if (lines > 1)				
				return new SizeF (Math.Max (maxAdv, adv), Height + (LineHeight * (lines - 1)));	
				//return new SizeF (Math.Max (maxAdv, adv), LineHeight * lines);	
			return new SizeF(adv, Height);
		}

		private SizeF PrintMultiline (string text, float width)
		{						
			if (String.IsNullOrEmpty(text))
				return SizeF.Empty;

			float adv = 0;
			float maxAdv = 0;
			int start = 0;
			int lastStopChar = 0;
			int maxStop = text.Length - 1;
			float stopCharX = 0;
			int lines = 1;
			int i;
			for (i = start; i < text.Length; i++) {
				bool breakFlag = false;
				char c = text [i];
				if (c == '\n') {
					breakFlag = true;
					lastStopChar = i;
					stopCharX = adv;
				}
				else {
					if (IsBreakChar(c) && i < maxStop) {
						lastStopChar = i;
						stopCharX = adv;
					}

					GlyphInfo gi;
					if (GetGlyphIndex(c, out gi)) {						
						if (adv + gi.Width > width && i > 0) {
							breakFlag = true;
							if (lastStopChar <= start)
								stopCharX = gi.Width;
						}
						else
							adv += gi.Width;
					}
				}

				if (breakFlag) {
					maxAdv = Math.Max (maxAdv, adv);
					if (lastStopChar > start) {
						i = lastStopChar;
						adv = stopCharX;
					}

					string textLine = text.Substring(start, i - start);
					PrintInternal (textLine);

					// line++
					GL.Translate (-adv, -LineHeight, 0);

					if (lastStopChar > start) {
						start = i + 1;
						adv = 0;
					}
					else {
						start = i;
						adv = stopCharX;
					}

					lastStopChar = 0;
					lines++;
				}
			}

			if (i > start) {
				string textLine = text.Substring(start, i - start);
				PrintInternal (textLine);
			}

			if (lines > 1)				
				return new SizeF (Math.Max (maxAdv, adv), Height + (LineHeight * (lines - 1)));
				//return new SizeF (Math.Max (maxAdv, adv), LineHeight * lines);
			return new SizeF(adv, Height);
		}


		private SizeF PrintInternal (string text)
		{	
			float adv = 0;
			int[] textbytes = new int[text.Length];
			for (int i = 0; i < text.Length; i++) {								
				GlyphInfo gi;
				if (GetGlyphIndex (text [i], out gi)) {
					textbytes [i] = gi.ListID;				
					adv += gi.Width;
				}
			}

			GL.CallLists (text.Length, ListNameType.Int, textbytes);
			return new SizeF (adv, Height);
		}

		public SizeF Print(string text, RectangleF bounds, FontFormat format, Color color = default(Color)) 
		{			
			if (this.IsDisposed || string.IsNullOrEmpty (text))
				return SizeF.Empty;

			try {

				RectangleF rContent;
				PointF presult;

				// Abkürzung, häufigster Fall
				if (format.HAlign == Alignment.Near && !format.HasFlag(FontFormatFlags.WrapText)) {
					rContent = new RectangleF (0, 0, bounds.Width, bounds.Height);

					float y = 0;
					if (bounds.Height > this.Height) {				
						switch (format.VAlign) {
						case  Alignment.Near:
							y = 0;
							break;

						case Alignment.Center:
							y = (bounds.Height - Height) / 2;
							break;

						case Alignment.Baseline:
							y = (bounds.Height - (Height + Descender)) / 2;
							break;

						case Alignment.Far:
							y = bounds.Height - Height;
							break;
						}
					}

					//presult = new Point (0, (int)(y + 0.5f));
					presult = new Point (0, y.Ceil());

				} else {
					SizeF contentSize;
					if (format.HasFlag(FontFormatFlags.WrapText))				
						contentSize = Measure(text, bounds.Width, format);
					else if (format.HasFlag(FontFormatFlags.Mnemonics))				
						contentSize = MeasureMnemonicString(text);
					else
						contentSize = Measure(text);

					rContent = new RectangleF (0, 0, contentSize.Width, contentSize.Height);
					presult = BoxAlignment.AlignBoxes (rContent, bounds, format, Ascender, Descender);
				}

				if (color != Color.Empty)
					GL.Color3 (color.R, color.G, color.B);
				else
					GL.Color3 (Theme.Colors.Base03);

				GL.Translate(Math.Floor(bounds.X + presult.X), 
					-Math.Ceiling(Ascender - Descender + YOffsetScaled + bounds.Y + presult.Y), 0f);

				if (format.HasFlag(FontFormatFlags.Elipsis) || format.HasFlag(FontFormatFlags.Underline))
					return PrintElipsisString(text, bounds.Width, format);
				else if (format.HasFlag(FontFormatFlags.Mnemonics))
					return PrintMenomicString(text, bounds, ModifierKeys.AltPressed);
				else if (format.HasFlag(FontFormatFlags.WrapText))
					return PrintMultiline(text, bounds.Width);
				else
					return PrintInternal (text);

			} catch (Exception ex) {
				ex.LogError ();
				return SizeF.Empty;
			}				
		}			

		public void Begin(IGUIContext ctx) 
		{			
			GL.PushAttrib(AttribMask.ListBit | AttribMask.CurrentBit | AttribMask.EnableBit | AttribMask.TransformBit);

			GL.MatrixMode(MatrixMode.Modelview);
			GL.Disable(EnableCap.Lighting);
			GL.Enable(EnableCap.Texture2D);
			GL.Disable(EnableCap.DepthTest);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.ListBase(m_ListBase);

			//float[] modelviewMatrix = new float[16];
			//GL.GetFloat(GetPName.ModelviewMatrix, modelviewMatrix);
			GL.PushMatrix();
			GL.LoadIdentity();
			//GL.Translate(x, y, 0);
			GL.Scale (1, -1, 1);
			//GL.MultMatrix(modelviewMatrix);
		}

		public void End() 
		{			
			GL.PopMatrix();					
			GL.PopAttrib();	
		}

		public int Count { get; private set; }

		public bool IsSpecialChar(char c)
		{
			return c == (char)182 || c == (char)183;
		}

		public unsafe GlyphInfo CompileCharacter (Face face, uint glyphindex, char character)
		{									
			// Load or generate new Texture and store the Handle in m_Textures
			if (m_Textures.Length <= Count)
				Array.Resize (ref m_Textures, Math.Max (32, m_Textures.Length * 2));
			int TextureIndex = m_Textures [Count];
			if (TextureIndex == 0) {
				int[] textures = new int[1];
				GL.GenTextures (1, textures);
				if (textures [0] == 0)
					return GlyphInfo.Empty;				
				m_Textures [Count] = textures [0];
				TextureIndex = textures [0];
			}

			int ListIndex = (int)glyphindex;
			//if (m_ListBase == 0) {				
			if (OnDemand) {
				ListIndex = GL.GenLists (1);
				if (ListIndex == 0)
					return GlyphInfo.Empty;
			}
				
			try {
				face.LoadGlyph (glyphindex, LoadFlags.ForceAutohint, LoadTarget.Normal);	
			} catch (Exception ex) {
				ex.LogWarning ();
				return GlyphInfo.Empty;
			}

			Glyph glyph = face.Glyph.GetGlyph ();
			if (glyph == null)
				return GlyphInfo.Empty;

			Height = Math.Max(Height, (float)face.Glyph.Metrics.Height);

			//glyph.ToBitmap (SharpFont.RenderMode.Normal, new FTVector26Dot6(0.15, 0.15), true);
			glyph.ToBitmap (SharpFont.RenderMode.Normal, new FTVector26Dot6(0, 0), true);

			BitmapGlyph bmg = glyph.ToBitmapGlyph ();
			int width = bmg.Bitmap.Width;
			int rows = bmg.Bitmap.Rows;
			int size = width * rows;

			if (size <= 0)
			{
				glyph.Dispose ();

				//if (Filter == GlyphFilterFlags.All)
				//	m_ExtentsX[(int)glyphindex] = 0;
				int spaceWidth = 0;
				if (character == 32)
				{
					Count++;
					spaceWidth = (Size * ScaleFactor / 3f).Ceil();
					GL.NewList (m_ListBase + ListIndex, ListMode.Compile);	// evtl character
					GL.Translate (spaceWidth, 0, 0);
					GL.EndList();
					return new GlyphInfo(ListIndex, spaceWidth);
				}		
				return GlyphInfo.Empty;
			}

			Count++;

			int	expandedBitmapWidth = (width + 1).NextPowerOf2();
			int	expandedBitmapHeight = rows.NextPowerOf2();
			byte[]	expandedBitmapBytes = new byte[expandedBitmapWidth * expandedBitmapHeight];

			fixed (byte* p = bmg.Bitmap.BufferData)
			fixed (byte* q = expandedBitmapBytes)
			{				
				try {
					byte* pTemp = p;
					for (int countY = 0; countY < expandedBitmapHeight; countY++) {
						for (int countX = 0; countX < expandedBitmapWidth; countX++) {						
							byte* qTemp = q + (countX + countY * expandedBitmapWidth);
							if ((countX >= width || countY >= rows))			
								*qTemp = 0;
							else							
								*qTemp = *(pTemp + countX);
						}
						pTemp += width;
					}

					if (IsSpecialChar (character)) {
						for (int i = 0; i < expandedBitmapBytes.Length; i++) {
							byte* qTemp = q + i;
							*qTemp = (byte)(*qTemp / 2);
						}
					}
				} catch (Exception ex) {
					ex.LogError ();
				}
			}

			GL.BindTexture  (TextureTarget.Texture2D, TextureIndex);

			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

			//GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			GL.TexImage2D (TextureTarget.Texture2D, 
				0, 							// level-of-detail
				PixelInternalFormat.Alpha,	// texture-format 32bit
				(int)expandedBitmapWidth, 	// texture-width
				(int)expandedBitmapHeight, 	// texture-height
				0,							// border
				PixelFormat.Alpha, // pixel-data-format
				PixelType.UnsignedByte, 	// pixel-data-type
				expandedBitmapBytes);			

			// ---------------------------------------------------------------------------
			//Create a display list (of precompiled GL commands) and bind a texture to it.
			GL.NewList (m_ListBase + ListIndex, ListMode.Compile);
			GL.BindTexture (TextureTarget.Texture2D, TextureIndex);

			// Account for freetype spacing rules.

			float glyphWidth = (float)glyph.Advance.X;
			//float left = (glyphWidth - bmg.Left) / 2f;

			GL.Translate (bmg.Left, 0, 0);
			GL.PushMatrix ();
			GL.Translate (0, bmg.Top - rows, 0);

			float x = width / (float)expandedBitmapWidth;
			float y = rows / (float)expandedBitmapHeight;

			// Draw the quad.
			GL.Begin (PrimitiveType.Quads);
			GL.TexCoord2 (0, 0); GL.Vertex2 (0, rows);
			GL.TexCoord2 (0, y); GL.Vertex2 (0, 0);
			GL.TexCoord2 (x, y); GL.Vertex2 (width, 0);
			GL.TexCoord2 (x, 0); GL.Vertex2 (width, rows);
			GL.End ();
			GL.PopMatrix ();

			//GL.Translate (face.Glyph.Metrics.HorizontalAdvance - bmg.Left, 0, 0);


			GL.Translate (glyphWidth - bmg.Left, 0, 0);


			// Advance for the next character.
			/*** 
			if (!Monospace)
				GL.Translate (face.Glyph.Metrics.HorizontalAdvance - bmg.Left, 0, 0);
			else				
				GL.Translate (glyphWidth, 0, 0);
			***/

			GL.EndList();

			// ---------------------------------------------------------------------------
			//m_ExtentsX[glyphindex] = face.Glyph.Metrics.HorizontalAdvance.Ceiling();
			//m_ExtentsX[ListIndex] = glyphWidth.Ceil();

			glyph.Dispose ();
			return new GlyphInfo(ListIndex, glyphWidth.Ceil());
		}

		protected Size PushProjectionMatrixAndSetParallelProjectionForViewport() 
		{
			int[] viewport = new int[4];

			// Save the coefficients of the six user-definable clipping planes.
			GL.PushAttrib(AttribMask.TransformBit);

			// Determine the viewport coordinates.
			GL.GetInteger(GetPName.Viewport, viewport);

			// Save the projection matrix.
			GL.MatrixMode(MatrixMode.Projection);
			GL.PushMatrix();

			// Reset/initialize the projection matrix.
			GL.LoadIdentity();

			// Set the GDI-text-mode alike coordinate system based on the viewport coordinates.
			GL.Ortho(viewport[0], viewport[2], viewport[1], viewport[3], 0, 1);

			// Recall the coefficients of the six user-definable clipping planes.
			GL.PopAttrib();

			return new Size (viewport[2] - viewport[0], viewport[3] - viewport[1]);
		}

		protected void PopProjectionMatrix ()
		{
			// Save the coefficients of the six user-definable clipping planes.
			GL.PushAttrib(AttribMask.TransformBit);

			// Recall the projection matrix.
			GL.MatrixMode(MatrixMode.Projection);
			GL.PopMatrix();

			// Recall the coefficients of the six user-definable clipping planes.
			GL.PopAttrib();			
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

