using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using KS.Foundation;
using OpenTK.Graphics.GL;
using System.Runtime.CompilerServices;

namespace SummerGUI
{	
    public static class GUIDrawingText
	{				
        public static SizeF DrawStringVBO(this IGUIContext ctx, string text, IGUIFont font, Brush brush, PointF point, FontFormat format)
		{
			float currentX = point.X;
			float baselineY = MathF.Round(point.Y + font.Ascender);

			foreach (char c in text)
			{				
				if (font.GetGlyphInfo(c, out GlyphInfo gi))
				{
					if (gi.Size.X > 0 && gi.Size.Y > 0)
					{
						RectangleF destRect = new RectangleF(
							currentX + gi.Bearing.X,
							baselineY - gi.Bearing.Y,
							gi.Size.X,
							gi.Size.Y
						);

						ctx.Batcher.AddGlyph(
							gi.TextureId,
							destRect,
							gi.UV,
							brush.Color
						);
					}

					currentX += gi.Advance;
				}				
			}			

			return new SizeF(currentX - point.X, font.Height);
		}

		private static SizeF Print(IGUIContext ctx, IGUIFont font, Brush brush, string text, RectangleF bounds, FontFormat format, Color color = default) 
		{			
			if (font.IsDisposed || string.IsNullOrEmpty (text))
				return SizeF.Empty;			

			try {				
				PointF presult;

				if (format.HasFlag(FontFormatFlags.WrapText))
				{
					SizeF contentSize = font.Measure(text, bounds.Width, format);
					RectangleF rContent = new RectangleF (0, 0, contentSize.Width, contentSize.Height);
					presult = BoxAlignment.AlignBoxes (rContent, bounds, format, font.Ascender, font.Descender);
				}
				else
				{
					float x = 0;
					float width;
					switch (format.HAlign)
					{
						case Alignment.Center:
							if (bounds.Width > 0)
							{
								if (format.HasFlag(FontFormatFlags.Mnemonics))
									width = font.MeasureMnemonicString(text).Width;
								else								
									width = font.Measure(text).Width;
								x = (bounds.Width - width) / 2f;
							}
							break;
						
						case Alignment.Far:
							if (bounds.Width > 0)
							{
								if (format.HasFlag(FontFormatFlags.Mnemonics))
									width = font.MeasureMnemonicString(text).Width;
								else								
									width = font.Measure(text).Width;
								x = bounds.Width - width;
							}
							break;
					}

					float y = 0;
					switch (format.VAlign)
					{						
						case Alignment.Center:						
							if (bounds.Height > 0)
							{
								y = (bounds.Height - font.Height) / 2f;
								//y = bounds.Height * 0.5f + (font.Ascender + font.Descender) * 0.5f;								
							}
							break;
						
						case Alignment.Baseline:
							if (bounds.Height > 0)
							{
								y = (bounds.Height - (font.Height + font.Descender)) / 2f;
							}
							break;

						case Alignment.Far:
							if (bounds.Height > 0)
							{
								y = bounds.Height - font.Height;
							}
							break;
					}

					y += font.YOffset;
					presult = new PointF (x, y);
				}
				
				bounds.Offset(presult);

				if (format.HasFlag(FontFormatFlags.Elipsis) || format.HasFlag(FontFormatFlags.Underline))
					return PrintElipsisString(ctx, font, brush, text, bounds, format);
				else if (format.HasFlag(FontFormatFlags.Mnemonics))					
					return PrintMnemonicString(ctx, font, brush, text, bounds, ModifierKeys.AltPressed);
				else if (format.HasFlag(FontFormatFlags.WrapText))					
					return PrintMultiline(ctx, font, brush, text, bounds);
				else					
					return PrintInternal (ctx, font, brush, text, bounds);

			} catch (Exception ex) {
				ex.LogError ();
				return SizeF.Empty;
			}
		}

		private static SizeF PrintMultiline(IGUIContext ctx, IGUIFont font, Brush brush, string text, RectangleF bounds)
		{                   
			if (string.IsNullOrEmpty(text))
				return SizeF.Empty;

			// Wir shapen einmal den gesamten Text, um die Cluster und Advances zu haben
			ShapedGlyph[] shapes = font.ShapeText(text).ToArray();

			float currentLineWidth = 0;
			float maxLineWidth = 0;
			int startIdx = 0;           // Index im shapes-Array
			int lastBreakShapeIdx = -1; // Letzte Umbruchmöglichkeit im shapes-Array
			float widthAtLastBreak = 0;
			int lines = 1;
			
			// Wir verschieben eine Kopie der Bounds für jede Zeile
			RectangleF lineBounds = bounds;

			for (int i = 0; i < shapes.Length; i++) 
			{
				var si = shapes[i];
				char c = si.Cluster < text.Length ? text[si.Cluster] : (char)0;

				// 1. Manueller Zeilenumbruch
				if (c == '\n')				
				{
					RenderLine(ctx, font, brush, text, shapes, startIdx, i, ref lineBounds);
					maxLineWidth = MathF.Max(maxLineWidth, currentLineWidth);
					
					startIdx = i + 1;
					currentLineWidth = 0;
					lastBreakShapeIdx = -1;
					lines++;
					continue;
				}

				// Merke dir Umbruchstellen
				if (char.IsWhiteSpace(c) || c == '-' || c.IsWrapCharacter()) 
				{
					lastBreakShapeIdx = i;
					widthAtLastBreak = currentLineWidth + si.XAdvance;
				}

				// 2. Automatischer Zeilenumbruch (Wrap)
				if (currentLineWidth + si.XAdvance > bounds.Width && i > startIdx) 
				{
					if (lastBreakShapeIdx != -1) 
					{
						// Umbruch am letzten Leerzeichen/Bindestrich
						RenderLine(ctx, font, brush, text, shapes, startIdx, lastBreakShapeIdx + 1, ref lineBounds);
						maxLineWidth = MathF.Max(maxLineWidth, widthAtLastBreak);
						
						i = lastBreakShapeIdx; // Setze Schleife nach dem Break fort
						startIdx = i + 1;
					}
					else 
					{
						// Notumbruch: Wort ist länger als bounds.Width -> direkt hier umbrechen
						RenderLine(ctx, font, brush, text, shapes, startIdx, i, ref lineBounds);
						maxLineWidth = MathF.Max(maxLineWidth, currentLineWidth);
						startIdx = i;
						i--; // Dieses Zeichen in der nächsten Zeile erneut prüfen
					}
					
					currentLineWidth = 0;
					lastBreakShapeIdx = -1;
					lines++;
				}
				else 
				{
					currentLineWidth += si.XAdvance;
				}
			}

			// Die letzte Zeile rendern
			if (startIdx < shapes.Length) 
			{
				RenderLine(ctx, font, brush, text, shapes, startIdx, shapes.Length, ref lineBounds);
				maxLineWidth = MathF.Max(maxLineWidth, currentLineWidth);
			}

			float totalHeight = font.Height + (font.LineHeight * (lines - 1));
			return new SizeF(maxLineWidth, totalHeight);
		}

		// Hilfsmethode, um eine Teilmenge der Shapes zu zeichnen
		private static void RenderLine(IGUIContext ctx, IGUIFont font, Brush brush, string text, ShapedGlyph[] shapes, int start, int end, ref RectangleF lineBounds)
		{
			if (start >= end) return;

			float currentX = lineBounds.X;
			float baselineY = MathF.Round(lineBounds.Y + font.Ascender);

			for (int j = start; j < end; j++)
			{
				var si = shapes[j];
				if (font.GetGlyphInfo(si.GlyphIndex, out var gi))
				{
					if (gi.Size.X > 0 && gi.Size.Y > 0)
					{
						ctx.Batcher.AddGlyph(
							gi.TextureId,
							new RectangleF(
								MathF.Round(currentX + gi.Bearing.X + si.XOffset), 
								MathF.Round(baselineY - gi.Bearing.Y + si.YOffset), 
								gi.Size.X, 
								gi.Size.Y),
							gi.UV,
							brush.Color
						);
					}
					currentX += si.XAdvance;
				}
			}
			// Nach dem Rendern der Zeile die Bounds für die nächste Zeile nach unten schieben
			lineBounds.Y += font.LineHeight;
		}

		private static SizeF PrintMnemonicString(IGUIContext ctx, IGUIFont font, Brush brush, string text, RectangleF bounds, bool showMnemonics)
		{                       
			float adv = 0;          
			float currentX = bounds.Left;
			float baselineY = MathF.Round(bounds.Y + font.Ascender);
			
			float mstart = -1, mend = 0;
			bool flag = false;
						
			foreach (ShapedGlyph si in font.ShapeText(text))
			{
				if (font.GetGlyphInfo(si.GlyphIndex, out var gi)) 
				{
					char c = si.Cluster < text.Length ? text[si.Cluster] : (char)0;
					if (c == '&') 
					{
						flag = true;
						mstart = currentX;
						continue; 
					}

					if (flag)
					{
						flag = false;
						mend = currentX + si.XAdvance;
					}

					// Normales Zeichnen
					if (gi.Size.X > 0 && gi.Size.Y > 0)
					{
						RectangleF destRect = new RectangleF(
							MathF.Round(currentX + gi.Bearing.X + si.XOffset),
							MathF.Round(baselineY - gi.Bearing.Y + si.YOffset),
							gi.Size.X,
							gi.Size.Y
						);

						ctx.Batcher.AddGlyph(gi.TextureId, destRect, gi.UV, brush.Color);                      
					}

					currentX += si.XAdvance;
					adv += si.XAdvance;
				}
			}
			
			// Unterstrich zeichnen (DPI skaliert)
			if (showMnemonics && mstart >= 0) 
			{                
				float thickness = Math.Max(1f, 1f * font.ScaleFactor);
				float yPos = MathF.Round(baselineY + thickness * 2f); // Knapp unter der Baseline				
				ctx.Batcher.AddRectangle(new RectangleF(mstart, yPos, mend - mstart, thickness), brush.Color);
			}

			return new SizeF(adv, font.Height);            
		}

		private static SizeF PrintInternal (IGUIContext ctx, IGUIFont font, Brush brush, string text, RectangleF bounds)
		{							
			float adv = 0;			
			float currentX = bounds.Left;
			float baselineY = MathF.Round(bounds.Y + font.Ascender);
			
			GlyphInfo gi;
			
			foreach (ShapedGlyph si in font.ShapeText(text))	
			{		
				if (font.GetGlyphInfo (si.GlyphIndex, out gi)) 
				{										
					if (gi.Size.X > 0 && gi.Size.Y > 0)
					{
						RectangleF destRect = new RectangleF(
							MathF.Round(currentX + gi.Bearing.X + si.XOffset),
							MathF.Round(baselineY - gi.Bearing.Y + si.YOffset),
							gi.Size.X,
							gi.Size.Y
						);

						ctx.Batcher.AddGlyph(
							gi.TextureId,
							destRect,
							gi.UV,
							brush.Color
						);						
					}

					adv += si.XAdvance;
					currentX += si.XAdvance;
				}
			}

			return new SizeF (adv, font.Height);			
		}

		private static SizeF PrintElipsisString(IGUIContext ctx, IGUIFont font, Brush brush, string text, RectangleF bounds, FontFormat format)
		{					
			if (String.IsNullOrEmpty(text) || font == null || ctx == null)
				return SizeF.Empty;			

			ShapedGlyph[] shapes = font.ShapeText(text).ToArray();			

			float adv = 0;
			int i;
			int len = shapes.Length;
			
			float baselineY = MathF.Round(bounds.Y + font.Ascender);
			float width = bounds.Width;

			GlyphInfo elipsis = font.EllipsisGlyphInfo;
			//bool nextIsMnemonic = false;			
			GlyphInfo [] glyphs = new GlyphInfo[len];			

			GlyphInfo gi;
			ShapedGlyph si;
			for (i = 0; i < len; i++) {
				si = shapes[i];				
				if (font.GetGlyphInfo (si.GlyphIndex, out gi)) {					
					if (adv + si.XAdvance > width && i > 0) {						
						i--;
						while (i > 1 && adv + elipsis.Advance > width) 
						{							
							adv -= shapes[i].XAdvance;								
							i--;
						}
						glyphs[i] = elipsis;
						adv += elipsis.Advance;
						i++;
						break;
					}

					glyphs[i] = gi;					
					adv += si.XAdvance;
				}
			}

			// Print Glyps
			float currentX = bounds.X;			
			for (int k = 0; k < i; k++)
			{				
				gi = glyphs[k];
				si = shapes[k];
				if (gi.Size.X > 0 && gi.Size.Y > 0)
				{					
					RectangleF destRect = new RectangleF(
						MathF.Round(currentX + gi.Bearing.X + si.XOffset),
						MathF.Round(baselineY - gi.Bearing.Y + si.YOffset),
						gi.Size.X,
						gi.Size.Y
					);

					ctx.Batcher.AddGlyph(
						gi.TextureId,
						destRect,
						gi.UV,
						brush.Color
					);					
				}
				currentX += si.XAdvance;				
			}

			if (format.HasFlag(FontFormatFlags.Underline))
			{
				float thickness = Math.Max(1f, 1f * font.ScaleFactor);
				float yPos = baselineY + thickness * 2f; // Knapp unter der Baseline				
				ctx.Batcher.AddRectangle(new RectangleF(bounds.Left, yPos, adv, thickness), brush.Color);	
			}

			if (i < len)
				return new SizeF (width + 1f, font.Height);	// +1 signals callers it was elipsis
			else
				return new SizeF (adv, font.Height);
		}
		
		public static SizeF DrawString(this IGUIContext ctx, string text, IGUIFont font, Brush brush, RectangleF bounds, FontFormat format)		
		{
			return Print(ctx, font, brush, text, bounds, format, brush.Color);			
		}

		public static SizeF DrawString(this IGUIContext ctx, string text, IGUIFont font, Brush brush, float x, float y, FontFormat format)
		{
			SizeF contentSize = font.Measure(text);

			switch (format.HAlign) {
			case Alignment.Near:
				break;
			case Alignment.Center:
				x -= contentSize.Width / 2f;
				break;
			case Alignment.Far:
				x -= contentSize.Width;
				break;
			}
			
			switch (format.VAlign) {
			case Alignment.Near:
				y -= contentSize.Height / 2f;
				break;
			case Alignment.Center:			
				y += contentSize.Height / 2f;
				break;
			case Alignment.Far:	
			case Alignment.Baseline:		
				y += contentSize.Height / 2;
				break;
			}

			y += font.YOffset;
										
			return Print(ctx, font, brush, text, new RectangleF (x, y, contentSize.Width, contentSize.Height), format, brush.Color);
		}

		public static SizeF DrawSelectedString(this IGUIContext ctx, string text, IGUIFont font, int selStart, int selLength, RectangleF bounds, float offsetX, FontFormat format, Color foreColor, Color selectionBackColor, Color selectionForeColor)
		{
			if (string.IsNullOrEmpty(text)) 
				return SizeF.Empty;			
			
			float startX = MathF.Floor(bounds.X + offsetX);
			
			// 2. Selektions-Rechteck vorab zeichnen (damit es hinter dem Text liegt)
			if (selLength > 0)
			{
				float selLeft = GetWidthUntil(font, text, selStart);
				float selWidth = GetWidthUntil(font, text.Substring(selStart), selLength);
				
				// Wir nutzen den Batcher für das Rechteck (mit der WhiteTexture)
				ctx.Batcher.AddRectangle(new RectangleF(startX + selLeft, bounds.Y, selWidth, bounds.Height), selectionBackColor);
			}

			float currentX = bounds.Left + offsetX;
			float currentY = bounds.Top + (bounds.Height - font.LineHeight) / 2f;
			currentY += font.YOffset;

			// 3. Glyphen zeichnen
			for (int i = 0; i < text.Length; i++)
			{
				bool isSelected = i >= selStart && i < selStart + selLength;
				Color activeColor = isSelected ? selectionForeColor : foreColor;

				GlyphInfo glyphInfo;
				if (font.GetGlyphInfo(text[i], out glyphInfo))
				{
					RectangleF dest = new RectangleF(
						MathF.Round(currentX + glyphInfo.Bearing.X),
						MathF.Round(currentY + (font.Ascender - glyphInfo.Bearing.Y)),
						glyphInfo.Size.X,
						glyphInfo.Size.Y
					);

					// Ab in den Atlas-Batcher!
					ctx.Batcher.AddGlyph(glyphInfo.TextureId, dest, glyphInfo.UV, activeColor);
					currentX += glyphInfo.Advance;
				}
			}

			return new SizeF(currentX - startX, font.Height);
		}

		// Hilfsfunktion zur Breitenberechnung für die Selektion
		private static float GetWidthUntil(IGUIFont font, string text, int length)
		{
			float w = 0;
			for (int i = 0; i < length && i < text.Length; i++)
			{
				if (font.GetGlyphInfo(text[i], out var gi))
					w += gi.Advance;
			}
			return w;
		}
		
		public static SizeF MeasureString(this IGUIContext ctx, string text, IGUIFont font, RectangleF rect, FontFormat format)
		{
			return font.Measure (text, rect.Width, format);
		}

		public static SizeF MeasureMnemonicString(this IGUIContext ctx, string text, IGUIFont font)
		{
			return font.MeasureMnemonicString (text);
		}
    }
}