using System;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using KS.Foundation;

namespace SummerGUI.Editor
{	
	public class GlyphList : ClassicLinkedList<MlGlyph>
	{
		public GlyphList() : base()	{}
		public GlyphList(IEnumerable<MlGlyph> source) : base(source) {}
	}

	public class BreakList : ClassicLinkedList<int>
	{
		public BreakList() : base()	{}
		public BreakList(IEnumerable<int> source) : base(source) {}
	}

	public struct ParagraphPosition : IEquatable<ParagraphPosition>
	{
		public static readonly ParagraphPosition Empty = new ParagraphPosition (0, 0, 0, 0, 0);
				
		public readonly int LineIndex;
		public readonly int Column;
		public readonly int Position;
		public readonly float ColumnStart;
		public readonly float ColumnWidth;

		public ParagraphPosition(int pos, int col, int lineIndex, float start, float width)
		{
			Position = pos;
			Column = col;
			LineIndex = lineIndex;
			ColumnStart = start;
			ColumnWidth = width;
		}

		public override bool Equals (object obj)
		{
			return (obj is ParagraphPosition) && Equals ((ParagraphPosition)obj);
		}

		public bool Equals (ParagraphPosition other)
		{
			//return LineIndex == other.LineIndex && Column == other.Column && Position == other.Position;
			return LineIndex == other.LineIndex && Position == other.Position;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return (LineIndex + 31) ^ ((Column + 127) * (Position + 1));
			}
		}
	}

	public class Paragraph : IComparable<Paragraph>
	{	
		public static bool IsSpaceWrapCharacter(int cp)
		{
			switch (cp)
			{
			case ' ':
			case '\n':
				return true;

			default:
				return false;
			}
		}

		public static bool IsWrapCharacter(int cp)
		{
			switch (cp)
			{
			case ' ':
			case '\n':
			case '-':
			//case '_':
			case ',':
			case ';':
			case '.':
			case '!':
			case '?':
				return true;

			default:
				return false;
			}
		}

		public int CompareTo (Paragraph other)
		{
			return Top.CompareTo(other.Top);
		}

		public int Index { get; set; }
		public int LineOffset { get; set; }
		public int PositionOffset { get; set; }

		public float Width { get; private set; }
		public float Top { get; set; }
		public float Height { get; set; }
		public float Bottom 
		{ 
			get {
				return Top + Height;
			}
		}
			
		private GlyphList m_Glyphs;
		public GlyphList Glyphs 
		{ 
			get {
				return m_Glyphs;
			}
		}

		private BreakList m_Breaks;
		public BreakList Breaks 
		{ 
			get {
				return m_Breaks;
			}
		}

		Paragraph m_Next;
		Paragraph Next
		{
			get{
				return m_Next;
			}
			set{
				if (m_Next != value) {
					m_Next = value;
					SetEndGlyph ();
				}
			}
		}

		private void SetEndGlyph()
		{
			// NOTE: Do<T>(this T) passes a copy for struct T; MlGlyph is a struct
			// so this assignment would not persist — exactly the same behavior as
			// the old GlyphChar code.  We keep the call for parity; see comment
			// inside the body for details.
			Glyphs.LastOrDefault ().Do (g => {
				if (Next == null)
					g.Char = SpecialCharacters.EndOfText;
				else
					g.Char = SpecialCharacters.Paragraph;
			});
		}

		public Paragraph (int index, float maxWidth, string text, IGUIFont font, SpecialCharacterFlags flags)
			: this (index, maxWidth)
		{			
			this.ParseString (text, font, flags);
		}

		public Paragraph (int index, float maxWidth)
		{			
			Index = index;
			Top = index;
			BreakWidth = maxWidth;

			m_Glyphs = new GlyphList ();
			m_Breaks = new BreakList ();
		}			
			
		public float TotalGlyphWidth { get; private set; }

		public int LineCount
		{
			get{
				if (Breaks == null)
					return 1;
				return Breaks.Count + 1;
			}
		}
			
		public float BreakWidth { get; private set; }

		public int Length
		{
			get{
				if (Glyphs == null)
					return 0;
				return Glyphs.Count;
			}
		}			

		public override string ToString ()
		{
			if (Glyphs.Count == 0)
				return String.Empty;
			StringBuilder sb = new StringBuilder (Glyphs.Count);
			Glyphs.ForEach(c => AppendCodePoint(sb, c.Char));
			return sb.ToString();
		}

		public void ToString (StringBuilder sb)
		{
			if (Glyphs.Count > 0) {
				Glyphs.ForEach(c => AppendCodePoint(sb, c.Char));
			}

		}

		/// <summary>
		/// Fügt <paramref name="count"/> Runen an, beginnend bei glyph-basierter
		/// <paramref name="start"/> (0-basiert), an <paramref name="sb"/> an und
		/// liefert die Anzahl der tatsächlich angehängten Runen zurück
		/// (kleiner, wenn am Absatzende).  Runen-sicher: ein astrales
		/// Surrogate-Paar bleibt zusammen, da direkt aus <see cref="MlGlyph.Char"/>
		/// (Codepoint) wieder in UTF-16 umgewandelt wird — NICHT über eine
		/// UTF-16-Substring-Operation, die ein Paar spalten könnte.
		/// </summary>
		public int AppendRunes(StringBuilder sb, int start, int count)
		{
			int total = 0;
			if (start < 0) start = 0;
			int i = start;
			while (count > 0 && i < Glyphs.Count) {
				AppendCodePoint (sb, Glyphs [i].Char);
				i++;
				total++;
				count--;
			}
			return total;
		}

		// MlGlyph stores the Unicode code point (int) instead of a UTF-16 code unit.
		// For BMP code points this is a plain char append; for astral code points
		// (e.g. emoji) the surrogate pair must be re-emitted so Text round-trips.
		private static void AppendCodePoint(StringBuilder sb, int cp)
		{
			// MlGlyph holds the Unicode code point as int:
			//   cp <= 0xFFFF  → BMP, append the single char
			//   cp >  0xFFFF  → astral (emoji, CJK extend-B…), emit surrogate pair
			//                  (UTF-16 — that's what StringBuilder / Text expose)
			if (cp <= 0 || cp > 0x10FFFF) return;
			if (cp > 0xFFFF) {
				int v = cp - 0x10000;
				sb.Append ((char)(0xD800 + ((v >> 10) & 0x3FF)));
				sb.Append ((char)(0xDC00 + (v & 0x3FF)));
				return;
			}
			if (cp >= 0xD800 && cp <= 0xDFFF) return;  // isolated surrogate, ignore
			sb.Append ((char)cp);
		}

	public bool NeedsWordWrap { get; set; }

		public void ParseString(string line, IGUIFont font, SpecialCharacterFlags flags)
			{		
			if (font == null) {
				this.LogError ("ParseString: font must not be null");
				return;
			}			

			if (line != null) {			
				// Iterate by Unicode code point (runes), NOT by UTF-16 code unit, so that
				// astral code points (surrogate pairs) stay together.  This is the
				// primary motivation for MlGlyph (int cp) vs the framework GlyphChar (char).
				foreach (Rune rune in line.EnumerateRunes ()) {
					int cp = rune.Value;
					if (cp != '\n')
						AppendChar (cp, font, flags);				
				}
			}

			// Ensure that we end up with a line-break, no matter what the string is
			AppendChar ('\n', font, flags);

			if (BreakWidth > font.Height * 2)
				WordWrap();
		}

		public bool AppendChar(int cp, IGUIFont font, SpecialCharacterFlags flags)
		{			
			// BMP path: resolve advance via the framework so the existing
			// special-character mapping (SpaceDot, Paragraph…) still applies.
			if (cp <= 0xFFFF) {
				GlyphChar g = font.GetGlyphChar ((char)cp, flags);
				if (g.Char > 0) {
					try {
						Glyphs.AddLast (new MlGlyph (cp, g.Advance));
						NeedsWordWrap = true;
						return true;
					} catch (Exception ex) {
						ex.LogError ();
					}
				}
				return false;
			}

			// Astral (surrogate-pair) code point: only source is the emoji font.
			// WICHTIG: GetAdvance (kein GL-Call) – Layout-Läufe laufen auf
			// Background-Threads ohne GL-Kontext. Die echte GL-Textur wird
			// lazy von TryGetGlyphInfo im Render-/Paint-Pfad erstellt.
			EmojiFont efont = EmojiFont.Instance;
			if (efont != null && efont.HasGlyph ((uint)cp)) {
				float adv = efont.GetAdvance ((uint)cp, font.Height);
				try {
					Glyphs.AddLast (new MlGlyph (cp, adv));
					NeedsWordWrap = true;
					return true;
				} catch (Exception ex) {
					ex.LogError ();
				}
			}
			return false;
		}

	public bool InsertChar(int pos, int cp, IGUIFont font, SpecialCharacterFlags flags)
		{	
			// BMP path
			if (cp <= 0xFFFF) {
				GlyphChar g = font.GetGlyphChar ((char)cp, flags);
				if (g.Char > 0) {
					try {	
						Glyphs.InsertAt(pos, new MlGlyph(cp, g.Advance));
						NeedsWordWrap = true;
						return true;
					} catch (Exception ex) {
						ex.LogError ();
					}
				}
				return false;
			}
		
			// Astral path – GetAdvance (kein GL); Textur später im Render-Pfad
			EmojiFont efont = EmojiFont.Instance;
			if (efont != null && efont.HasGlyph ((uint)cp)) {
				float adv = efont.GetAdvance ((uint)cp, font.Height);
				try {
					Glyphs.InsertAt(pos, new MlGlyph(cp, adv));
					NeedsWordWrap = true;
					return true;
				} catch (Exception ex) {
					ex.LogError ();
				}
			}
			return false;
		}

	public void RefreshGlyphs(IGUIFont font, SpecialCharacterFlags flags)
		{
			// Re-resolve each glyph's advance at the current font size / flag config.
			// Astral (surrogate-pair) code points keep their previous advance — the
			// emoji font's per-height cache already produced a value when we first appended.
			GlyphList glyphs = new GlyphList ();
			foreach (MlGlyph g in Glyphs) {
				if (g.Char <= 0xFFFF) {
					GlyphChar gc = font.GetGlyphChar ((char)g.Char, flags);
					glyphs.AddLast (new MlGlyph (g.Char, gc.Advance));
				} else {
					glyphs.AddLast (g);
				}
			}

			Concurrency.LockFreeUpdate (ref m_Glyphs, glyphs);
			NeedsWordWrap = true;
		}

		public bool RemoveChar(int pos)
		{						
			if (pos < 0 || pos >= Glyphs.Count - 1)
				return false;
			try {					
				Glyphs.RemoveAt(pos);
				NeedsWordWrap = true;

				// when removing a character, it looks very bad sometimes
				// if the wordwrap isn't performend immediately..
				WordWrap();
				return true;
			} catch (Exception ex) {
				ex.LogError ();
			}
			return false;
		}

		public Paragraph SplitAt(int index)
		{
			// left = dieser Paragraph (bis index)
			// right = neuer Paragraph (ab index)

			Paragraph right = new Paragraph(-1, this.BreakWidth);			
			index = index.Clamp(0, Glyphs.Count);			

			var current = this.Glyphs.Retrieve(index);
			while (current != null)
            {
                right.Glyphs.AddLast(current.Value);
				current = current.Next;
            }
			right.NeedsWordWrap = true;
						
			// Copy right side glyphs
			/***
			for (int i = index; i < Glyphs.Count; i++)
			{
				right.Glyphs.Append(Glyphs[i]);
			}
			***/

			// Remove right side glyphs from this paragraph
			Glyphs.RemoveRange(index, Glyphs.Count - index);
			this.NeedsWordWrap = true;

			return right;
		}

		public static void Merge(Paragraph para1, Paragraph para2)
		{
			if (para2 == null || para2.Glyphs.Count == 0)
				return;

			// Falls para1 mit '\n' endet, entferne diesen,
			// damit kein doppelter Absatz entsteht.
			if (para1.Glyphs.Count > 0 && para1.Glyphs.Last.Char == '\n')
			{				
				para1.Glyphs.RemoveLast();
			}

			// Falls para2 mit '\n' beginnt, entferne das,
			// es ist der Absatztrenner zwischen beiden.
			if (para2.Glyphs.Count > 0 && para2.Glyphs.First.Char == '\n')
			{
				para2.Glyphs.RemoveFirst();
			}

			// jetzt einfach Glyphs anfügen
			para1.Glyphs.AppendRange(para2.Glyphs);
			//para1.Glyphs.AddRange(para2.Glyphs);
		}

		public bool StartsWithNewline()
		{		
			return Glyphs.First.Char == '\n';		
		}

		public bool EndsWithNewline()
		{			
			return Glyphs.Last.Char == '\n';		
		}
					
		public int RemoveRange(int start, int len, IGUIFont font, SpecialCharacterFlags flags)
		{
			lock (SyncObject) {				
				try {
					int count = Glyphs.RemoveRange(start, len);
					if (count > 0) {
						NeedsWordWrap = true;
						WordWrap();
					}
					return count;	
				} catch (Exception ex) {
					ex.LogError ();
					return -1;
				}
			}
		}		

		private void WordWrap()
		{
			WordWrap (BreakWidth);
		}

		public object SyncObject = new object ();

		public void WordWrap(float breakWidth)
		{

			// ToDo: Still not perfect:
			// * Lines with ending NewLine may hang over 
			// (the non-printable NewLine character is right above breakwidth)
			// * Lines without any break-chars may hang one character over breakwidth

			lock (SyncObject) {
				if (!NeedsWordWrap && BreakWidth == breakWidth)
					return;

				BreakWidth = breakWidth;
				NeedsWordWrap = false;

				// Since here, we completely work with temporary local variables
				// and set final values once finished.

				GlyphList.Node curr = Glyphs.Head;
				GlyphList.Node prev = curr;
				BreakList lineBreaks = new BreakList ();

				float currentWidth = 0;
				float totalWidth = 0;
				float maxWidth = 0;
							
				int pos = 0;
				int prevBreakCharPos = 0;

				while (curr != null) {								
					currentWidth += curr.Value.Advance;
					totalWidth += currentWidth;

					if (currentWidth > BreakWidth && curr.Next != null) {						
						if (prevBreakCharPos == 0) {
                            lineBreaks.AddLast(pos);
							currentWidth = curr.Value.Advance;
							prev = curr;
						} else {
                            lineBreaks.AddLast(prevBreakCharPos);
							prevBreakCharPos = 0;
							currentWidth = 0;
							while (prev != curr) {
								currentWidth += prev.Value.Advance;
								prev = prev.Next;
							}								
						}
					} else if (IsSpaceWrapCharacter (curr.Value.Char) || (IsWrapCharacter (curr.Value.Char) && curr.Next != null && !IsSpaceWrapCharacter (curr.Next.Value.Char))) {
							prevBreakCharPos = pos + 1;
							prev = curr;
					}

					maxWidth = Math.Max (maxWidth, currentWidth);
					curr = curr.Next;
					pos++;
				}			

				// Update final values
				Concurrency.LockFreeUpdate(ref m_Breaks, lineBreaks);
				Width = maxWidth;
				TotalGlyphWidth = totalWidth;
			}								
		}
	}

	public static class ParagraphExtensions
	{
		public struct TextLineInfo
		{
			public ClassicLinkedList<MlGlyph>.Node StartNode;
			public int Length;
		}

		public static IEnumerable<TextLineInfo> ToLines(this Paragraph para)
		{
			var glyphNode = para.Glyphs.Head;
			var breakNode = para.Breaks.Head;
			int lastBreakIndex = 0;
			int totalGlyphsProcessed = 0;

			// 1. Alle Zeilen bis zum letzten Break ausgeben
			while (glyphNode != null && breakNode != null)
			{
				int lineLength = breakNode.Value - lastBreakIndex;
				yield return new TextLineInfo { StartNode = glyphNode, Length = lineLength };

				for (int i = 0; i < lineLength && glyphNode != null; i++)
				{
					glyphNode = glyphNode.Next;
					totalGlyphsProcessed++;
				}

				lastBreakIndex = breakNode.Value;
				breakNode = breakNode.Next;
			}

			// 2. DER FIX: Wenn noch Glyphen übrig sind, die nach dem letzten Break kommen
			if (glyphNode != null)
			{
				// Die restliche Länge ist die Gesamtzahl der Glyphen minus das, was wir schon haben
				int remainingLength = para.Glyphs.Count - totalGlyphsProcessed;
				if (remainingLength > 0)
				{
					yield return new TextLineInfo { StartNode = glyphNode, Length = remainingLength };
				}
			}
		}

		// used for general purpose
		public static ParagraphPosition PositionAtIndex(this Paragraph para, int pos)
		{
			if (para.Glyphs == null || para.Glyphs.Count == 0 || pos <= 0)
				return ParagraphPosition.Empty;
			pos = pos.Clamp (0, para.Glyphs.Count - 1);
			GlyphList.Node ng = para.Glyphs.Head;
			int breakIndex = -1;
			BreakList.Node nb = para.Breaks.Head;
			if (nb != null)
				breakIndex = nb.Value;
			int i = 0;
			int x = 0;
			float w = 0;
			int y = 0;
			while (i < pos && ng.Next != null) {
				if (i == breakIndex - 1) {					
					w = 0;
					x = 0;
					y++;
					nb = nb.Next;
					if (nb != null)
						breakIndex = nb.Value;
				} else {
					x++;
					w += ng.Value.Advance;
				}

				i++;
				ng = ng.Next;
			}
			return new ParagraphPosition(pos, x, y, w, ng.Value.Advance);
		}			

		// used for cursor placement
		public static ParagraphPosition PositionAtLineWidth(this Paragraph para, float width, int line)
		{
			if (para.Glyphs == null || para.Glyphs.Count == 0 || line < 0 || line >= para.LineCount)
				return ParagraphPosition.Empty;				
			GlyphList.Node ng = para.Glyphs.Head;
			int breakIndex = -1;
			BreakList.Node nb = para.Breaks.Head;
			if (nb != null)
				breakIndex = nb.Value;
			int i = 0;
			float w = 0;
			float whalf = (int)(ng.Value.Advance / 2f + 0.5f);
			int x = 0;
			int y = 0;
			while ((y < line || (w + whalf <= width)) && ng.Next != null) {
				if (i == breakIndex - 1) {						
					if (y >= line) {
						return new ParagraphPosition (i, x, y, w, ng.Value.Advance);
					}

					w = 0;
					whalf = (int)(ng.Value.Advance / 2f + 0.5f);
					x = 0;
					y++;
					nb = nb.Next;
					if (nb != null)
						breakIndex = nb.Value;
				} else {
					x++;
					w += ng.Value.Advance;
					whalf = (int)(ng.Value.Advance / 2f + 0.5f);
				}

				ng = ng.Next;
				i++;
			}
			return new ParagraphPosition(i, x, y, w, ng.Value.Advance);
		}

		public static int FastPositionAtIndex(this Paragraph para, int pos)
		{
			if (para.Breaks.Count == 0)
				return pos;
			BreakList.Node n = para.Breaks.Head;
			while (n != null && n.Next != null && n.Next.Value - 1 < pos)
				n = n.Next;
			if (n == null)
				return pos;
			if (pos < n.Value)
				return pos;
			return pos - n.Value;
		}

		public static IEnumerable<float> LineWidths(this Paragraph para)
		{
			if (para.Breaks.Count == 0) {
				yield return para.Glyphs.Sum (g => g.Advance);
			} else {
				BreakList.Node n = para.Breaks.Head;
				GlyphList.Node g = para.Glyphs.Head;
				float w = 0;
				int i = 0;
				while (n != null) {
					while (g != null && ++i <= n.Value) {
						w += g.Value.Advance;
						g = g.Next;
					}
					i--;
					yield return w;
					n = n.Next;
					w = 0;
				}

				while (g != null) {
					w += g.Value.Advance;
					g = g.Next;
				}
				yield return w;
			}
		}

		public static float LineWidth(this Paragraph para, int lineIndex)
		{
			if (para.Breaks.Count == 0)
				return para.Glyphs.Sum (gx => gx.Advance);
			BreakList.Node n = para.Breaks.Head.Skip(lineIndex - 1);
			if (n == null)
				return 0;
			GlyphList.Node g = para.Glyphs.Head;
			int idx = n.Value;
			g = g.Skip (idx);
			n = n.Next;
			float w = 0;
			while (g != null && (n == null || idx++ < n.Value)) {
				w += g.Value.Advance;
				g = g.Next;
			}
			return w;
		}

		public static int LineFromPosition(this Paragraph para, int pos)
		{
			if (pos == 0 || para.Breaks.Count == 0)
				return 0;
			BreakList.Node n = para.Breaks.Head;
			int line = 0;
			while (n != null && n.Value <= pos) {
				n = n.Next;
				line++;
			}
			return line;
		}

		public static int PositionAtLineIndex(this Paragraph para, int col, int line)
		{
			if (para.Breaks.Count == 0)				
				return col.Clamp (0, para.Length - 1);
			BreakList.Node n = para.Breaks.Head;
			if (line == 0)
				return col.Clamp (0, n.Value - 1);
			n = n.Skip (line - 1);
			if (n == null)
				return para.Length - 1;
			if (n.Next == null)				
				return Math.Min (n.Value + col, para.Length - 1);
			return Math.Min (n.Value + col, n.Next.Value - 1);
		}

		public static int EolIndex(this Paragraph para, int line)
		{
			if (para.Breaks.Count == 0)
				return para.Length - 1;
			BreakList.Node n = para.Breaks.Head;
			n = n.Skip (line);
			if (n == null)
				return para.Length - 1;
			return n.Value - 1;
		}
	}
}

