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

			// ToDo: GUG BUG BUG
			//CanFocus = false;
			TabIndex = -1;	
		}

		// ToDo: not yet perfect, only works in 'most' cases
		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (CachedPreferredSize == SizeF.Empty) {

				float totalW = 0;
				float totalH = 0;
				float maxH = 0;
				float maxW = 0;

				for (int i = 0; i < Children.Count; i++) {
					Widget child = Children [i];
					if (child != null && child.Visible && !child.IsOverlay) {						
						SizeF sz = child.PreferredSize (ctx, proposedSize).Inflate (child.Margin);
						switch (child.Dock) {
						case Docking.Fill:
							totalW += sz.Width;
							totalH += sz.Height;
							maxW = Math.Max (maxW, sz.Width);
							maxH = Math.Max (maxH, sz.Height);
							break;
						case Docking.Left:
						case Docking.Right:
							totalW += sz.Width;
							maxH = Math.Max (maxH, sz.Height);
							break;
						case Docking.Top:
						case Docking.Bottom:
							totalH += sz.Height;
							maxW = Math.Max (maxW, sz.Width);
							break;
						}						
					}
				}				

				CachedPreferredSize = new SizeF (maxW + Padding.Width, totalH + Padding.Height);
			}
			return CachedPreferredSize;
		}			
	}
}

