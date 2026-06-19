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

				float accumulatedWidth = 0f;
				float accumulatedHeight = 0f;

				// Wir simulieren das "Aufbrauchen" des Platzes analog zu deiner Layout-Schleife
				float currentMaxLineWidth = 0f;
				float currentMaxLineHeight = 0f;

				SizeF remaining = proposedSize;

				foreach (var child in Children)
				{
					if (child == null || !child.Visible)
						continue;

					SizeF sz = child.PreferredSize(ctx, remaining).Inflate(child.Margin);					

					switch (child.Dock)
					{
						case Docking.Top:
						case Docking.Bottom:

							accumulatedHeight += sz.Height;
							accumulatedWidth = MathF.Max(accumulatedWidth, sz.Width);

							remaining.Height = MathF.Max(0, remaining.Height - sz.Height);
							break;

						case Docking.Left:
						case Docking.Right:

							accumulatedWidth += sz.Width;
							accumulatedHeight = MathF.Max(accumulatedHeight, sz.Height);

							remaining.Width = MathF.Max(0,
								remaining.Width - sz.Width);
							break;

						case Docking.Fill:
						case Docking.None:

							currentMaxLineWidth =
								MathF.Max(currentMaxLineWidth, sz.Width);

							currentMaxLineHeight =
								MathF.Max(currentMaxLineHeight, sz.Height);
							break;
					}
				}

				// Am Ende führen wir die "Stapel" und die "Füllflächen" zusammen
				float requiredWidth = MathF.Max(accumulatedWidth, currentMaxLineWidth);
				float requiredHeight = MathF.Max(accumulatedHeight, currentMaxLineHeight);				

				// Padding und Min/Max anwenden
				CachedPreferredSize = ClampMinMax(new SizeF(requiredWidth + Padding.Width, requiredHeight + Padding.Height));
			}

			return CachedPreferredSize;
		}
	}
}

