using System;
using System.Linq;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{
	public class Panel : Container
	{		
		public Panel (string name) : this(name, Docking.None, null) {}
		public Panel (string name, Docking dock) : this(name, dock, null) {}
		public Panel (string name, Docking dock, IWidgetStyle style)
			: base(name, dock, style)
		{

			// ToDo: BUG?
			//CanFocus = false;
			TabIndex = -1;	
		}		
		
		public override SizeF PreferredSize(IGUIContext ctx, SizeF proposedSize)
		{
			if (CachedPreferredSize == SizeF.Empty) {

				float leftRightTotalWidth = 0f;   // Summe der Breiten für Left/Right
				float topBottomTotalHeight = 0f;  // Summe der Höhen für Top/Bottom

				float maxHeightFromLeftRight = 0f; // max. Höhe unter Left/Right (wir brauchen das für Gesamt-Höhe)
				float maxWidthFromTopBottom = 0f;  // max. Breite unter Top/Bottom (wir brauchen das für Gesamt-Breite)

				float maxFillWidth = 0f;  // Fill beeinflusst Größe als MAX, nicht Summe
				float maxFillHeight = 0f;

				foreach (var child in Children) {
					if (child == null || !child.Visible || child.IsOverlay)
						continue;

					// Annahme: Inflate fügt Margin beidseitig hinzu und gibt eine neue SizeF zurück
					SizeF sz = child.PreferredSize(ctx, proposedSize).Inflate(child.Margin);

					switch (child.Dock) {
						case Docking.Left:
						case Docking.Right:
							leftRightTotalWidth += sz.Width;
							maxHeightFromLeftRight = MathF.Max(maxHeightFromLeftRight, sz.Height);
							break;

						case Docking.Top:
						case Docking.Bottom:
							topBottomTotalHeight += sz.Height;
							maxWidthFromTopBottom = MathF.Max(maxWidthFromTopBottom, sz.Width);
							break;

						case Docking.Fill:
							maxFillWidth = MathF.Max(maxFillWidth, sz.Width);
							maxFillHeight = MathF.Max(maxFillHeight, sz.Height);
							break;

						default:
							// Falls es andere Docking-Optionen gibt: sicherheitshalber als Max behandeln
							maxFillWidth = MathF.Max(maxFillWidth, sz.Width);
							maxFillHeight = MathF.Max(maxFillHeight, sz.Height);
							break;
					}
				}

				// Gesamtbreite = Summe Left/Right + das größere von (max Fill width, max width von Top/Bottom)
				float requiredWidth = leftRightTotalWidth + MathF.Max(maxFillWidth, maxWidthFromTopBottom);

				// Gesamthöhe = Summe Top/Bottom + das größere von (max Fill height, max height von Left/Right)
				float requiredHeight = topBottomTotalHeight + MathF.Max(maxFillHeight, maxHeightFromLeftRight);

				requiredWidth = MathF.Max(MinSize.Width, requiredWidth);
				requiredWidth = MathF.Min(MaxSize.Width, requiredWidth);

				requiredHeight = MathF.Max(MinSize.Height, requiredHeight);
				requiredHeight = MathF.Min(MaxSize.Height, requiredHeight);

				// Padding hinzufügen (Width/Height sind üblicherweise Padding.Left+Right / Padding.Top+Bottom)
				CachedPreferredSize = new SizeF(requiredWidth + Padding.Width, requiredHeight + Padding.Height);
			}

			return CachedPreferredSize;
		}        
	}
}

