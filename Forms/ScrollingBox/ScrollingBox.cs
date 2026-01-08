using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{
	public enum StringAlignment
    {
        Near = 0, // Linksbündig (bei Linksläufigkeit)
        Center = 1, // Zentriert
        Far = 2 // Rechtsbündig (bei Linksläufigkeit)
    }

	public class ScrollingBoxWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base3);
			SetForeColor (Theme.Colors.Base02);
			SetBorderColor (Theme.Colors.Base01);
		}
	}

	public class ScrollingBox : Widget
	{
		public enum ArrowDirection
		{
			Up,
			Down
		}

		public ArrowDirection MovingDirection { get; set; }
		public ScrollingBoxCollection Items { get; protected set; }
		public FontFormat Format { get; set; }
		private bool startingPositionHasBeenSetAfterHeight;

		IGUIFont m_Font;
		public IGUIFont Font 
		{ 
			get {
				return m_Font;
			}
			set {
				m_Font = value;
				ResetCachedLayout ();
			}
		}

		public void AddParagraph(string text)
		{
			if (String.IsNullOrEmpty (text))
				return;

			Strings.SplitLines(text).ForEach(line => Items.Add(line));				
			RecalculateItems ();
		}

		public void AddImage(string filepath, IGUIContext ctx, float opacity = 1f)
		{
			if (String.IsNullOrEmpty (filepath) || ctx == null)
				return;

			Items.Add (new ScrollingBoxImage (filepath, ctx, opacity));
			RecalculateItems ();
		}
			
		public int AnimationRate { get; set; }

		public ScrollingBox (string name)
			: this(name, Docking.Fill, new ScrollingBoxWidgetStyle())
		{
		}

		public ScrollingBox (string name, Docking dock, IWidgetStyle style)
			: base (name, dock, style)
		{
			Font = FontManager.Manager.DefaultFont;

			// ToDo: DPI Scaling: Faster ! (0 or 1)
			AnimationRate = 2;

			Padding = new Padding (12);

			Items = new ScrollingBoxCollection();

			Alignment = StringAlignment.Center;
			//Format = FontFormat.DefaultSingleLineCentered;
			Format = FontFormat.DefaultMultiLine;
			ImagePadding = new Padding (0, 21);
			IsAnimationRunning = true;
		}			

		public bool IsAnimationRunning { get; protected set; }

		public void Start()
		{
			if (IsAnimationRunning)
				return;
			IsAnimationRunning = true;
			Invalidate ();
		}

		public void Stop()
		{
			if (!IsAnimationRunning)
				return;
			IsAnimationRunning = false;
		}

		public override void OnResize ()
		{
			base.OnResize ();
			RecalculateItems();
			this.Invalidate();
		}

		public override void OnMouseEnter (IGUIContext ctx)
		{
			base.OnMouseEnter (ctx);
			Stop ();
		}

		public override void OnMouseLeave (IGUIContext ctx)
		{
			base.OnMouseLeave (ctx);
			Start ();
		}

		public void ToggleAnimation()
		{
			if (IsAnimationRunning)
				Stop ();
			else
				Start ();
		}

		public override void OnMouseDown (MouseButtonEventArgs e)
		{
			base.OnMouseDown (e);
			ToggleAnimation ();
		}


		public StringAlignment Alignment { get; set; }

		public float TotalItemHeight { get; protected set; }

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			/***
			IGUIFont font = GetFont (ctx);
			if (font == null)
				return base.PreferredSize (ctx, proposedSize);
			else
			***/
			return new SizeF (proposedSize.Width, TotalItemHeight);
		}

		protected virtual void RecalculateItems()
		{
			SizeF sizeF = new SizeF();
			float availWidth = Width - Padding.Width;
			TotalItemHeight = 0;

			for (int i = 0; i < Items.Count; i++)
			{
				ScrollingBoxItem item = Items[i];

				item.rectF.X = Padding.Left;
				item.rectF.Width = (float)availWidth;

				if (item.GetType() == typeof(ScrollingBoxText))
				{
					ScrollingBoxText textItem = (ScrollingBoxText)item;
					if (String.IsNullOrEmpty (textItem.Text))
						sizeF = new SizeF (availWidth, Font.LineHeight);
					else {
						SizeF sz = Font.Measure (textItem.Text, availWidth, Format);
						sizeF = new SizeF (sz.Width, sz.Height + Font.LineHeight - Font.Height);
					}
				}
				else if (item.GetType() == typeof(ScrollingBoxImage))
				{
					ScrollingBoxImage imgItem = (ScrollingBoxImage)item;
					sizeF = imgItem.Image.Size;
					sizeF = new SizeF (sizeF.Width + ImagePadding.Left + ImagePadding.Right, sizeF.Height + ImagePadding.Top + ImagePadding.Bottom);

					imgItem.rectF.Width = sizeF.Width;
					switch (Alignment)
					{
					case StringAlignment.Near:
						item.rectF.X = Padding.Left + ImagePadding.Left;
						break;
					case StringAlignment.Center:
						item.rectF.X = (availWidth / 2) - (sizeF.Width / 2) + Padding.Left + ImagePadding.Left;
						break;
					case StringAlignment.Far:
						item.rectF.X = Width - sizeF.Width - Padding.Right - ImagePadding.Right;
						break;
					}
				}

				if (i == 0) {
					sizeF = new SizeF (sizeF.Width, sizeF.Height + Padding.Top);
				}
					
				item.rectF.Height = sizeF.Height;
				TotalItemHeight += sizeF.Height;

				if (i == 0)
				{
					if (!startingPositionHasBeenSetAfterHeight)
					{
						item.rectF.Y = Height;
						startingPositionHasBeenSetAfterHeight = true;
					}
				}
				else
				{					
					item.rectF.Y = Items[i - 1].rectF.Bottom;
				}
			}
		}

		[DpiScalable]
		public Padding ImagePadding { get; set; }

		protected virtual void PositionItems()
		{
			for (int i = 0; i < Items.Count; i++)
			{
				ScrollingBoxItem item = Items[i];

				if (MovingDirection == ArrowDirection.Up)
				{
					if (item.rectF.Y + item.rectF.Height < 0)                    
					{
						if (i == 0)
						{
							// Goto the bottom of the screen list items
							item.rectF.Y = Items[Items.Count - 1].rectF.Y + Height + item.rectF.Height;
						}
						else
						{
							item.rectF.Y = Items[i - 1].rectF.Bottom;
						}
					}
					else
					{
						// Move up the screen
						item.rectF.Y -= 1;                        
					}
				}
				else if (MovingDirection == ArrowDirection.Down)
				{
					if (item.rectF.Y > this.Height)
					{
						if (i == Items.Count - 1)
						{
							// Goto the top of the screen list items
							item.rectF.Y = Items[0].rectF.Y - Height - item.rectF.Height;
						}
						else
						{
							item.rectF.Y = Items[i + 1].rectF.Y - item.rectF.Height;
						}
					}
					else
					{
						// Move down the screen
						item.rectF.Y += 1;
					}
				}
			}	

			this.Invalidate();
		}
			
		int animationTimer = 60;
		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			base.OnLayout (ctx, bounds);

			if (IsAnimationRunning) {
				animationTimer--;
				if (animationTimer <= 0) {				
					PositionItems ();
					animationTimer = AnimationRate;
				} else {
					Invalidate ();
				}
			}
		}

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);

			if (Font == null)
				return;
			for(int i = 0; i < Items.Count; i++)
			{
				ScrollingBoxItem item = Items[i];

				RectangleF r = item.rectF;
				r.Offset (bounds.Left, bounds.Top);
				if (i == 0)
					r.Offset (0, Padding.Top);

				if (bounds.IntersectsWith(r))
				{
					var txt = item as ScrollingBoxText;
					if (txt != null)
					{
						ctx.DrawString(txt.Text, Font, Style.ForeColorBrush, r, Format);
					}
					else {
						ScrollingBoxImage img = item as ScrollingBoxImage;
						if (img != null)
							img.Image.Paint (ctx, new RectangleF (
								r.X + ImagePadding.Left,
								r.Top + ImagePadding.Top,
								r.Width - ImagePadding.Left - ImagePadding.Right,
								r.Height - ImagePadding.Top - ImagePadding.Bottom));
					}
				}
			}
		}

		protected override void CleanupManagedResources ()
		{
			if (Items != null) {
				for (int i = 0; i < Items.Count; i++)
					if (Items [i] != null)
						Items [i].Dispose ();	
			}
			base.CleanupManagedResources();
		}
	}
}

