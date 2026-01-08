using System;
using System.Drawing;
using System.Linq;
using OpenTK.Input;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI.Editor
{
	public class TextEditorRowColumnWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base02);
			SetForeColor (Theme.Colors.Base1);
			//SetBorderColor (Theme.Colors.Base01);
			//SetBorderColor (Color.Empty);
			SetBorderColor (Theme.Colors.Base03);
		}

		public override void DrawBorder (IGUIContext ctx, Widget widget)
		{
			if (widget.CanPaint && BorderColorPen != null) {
				RectangleF bounds = widget.Bounds;
				ctx.DrawLine (BorderColorPen, bounds.Right, bounds.Top, bounds.Right, bounds.Bottom);
			}
		}
	}

	public class TextEditorRowColumn : Widget
	{
		public enum DisplayModes
		{
			None,
			LineNumber,
			ParagraphNumber
		}

		public DisplayModes DisplayMode { get; set; }

		public MultiLineTextBox Owner { get; private set; } 

		private FontFormat Format;

		public TextEditorRowColumn (MultiLineTextBox owner)
			: base("rowcolumn", Docking.Left, new TextEditorRowColumnWidgetStyle())
		{
			Owner = owner;
			DisplayMode = DisplayModes.ParagraphNumber;
			MinSize = new Size (40, 0);
			Format = new FontFormat (Alignment.Far, Alignment.Center, FontFormatFlags.None);
			Padding = new Padding (6, 0, 6, 0);
		}
			
		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (CachedPreferredSize == SizeF.Empty) {
				if (Owner == null)
					return base.PreferredSize (ctx, proposedSize);
				else
					CachedPreferredSize = new SizeF (Math.Max (MinSize.Width, Owner.Font.Measure (Owner.RowManager.LineCount.ToString ()).Width.Ceil () + Padding.Width), 
						Math.Max (proposedSize.Height, Owner.RowManager.Height + Owner.Padding.Height));
			}
			return CachedPreferredSize;
		}	

		protected bool IsMouseMoving { get; private set; }

		public override void OnMouseDown (MouseButtonEventArgs e)
		{	
			base.OnMouseDown (e);
			Owner.RowManager.SetCursorPosition (-1, e.Y - Top - Owner.Padding.Top);
			Paragraph para = Owner.RowManager.CurrentParagraph;
			Owner.SelStart = para.PositionOffset;
			Owner.SelLength = para.Length - 1;
			Owner.RowManager.SetCursorAbsPosition(para.PositionOffset + para.Length - 1);
			IsMouseMoving = true;
			Invalidate ();
		}

		public override void OnMouseUp (MouseButtonEventArgs e)
		{
			IsMouseMoving = false;
			base.OnMouseUp (e);
		}

		public override void OnMouseMove (MouseMoveEventArgs e)
		{
			base.OnMouseMove (e);

			if (IsMouseMoving) {
				Invalidate (1);
			}
		}
			
		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);

			if (DisplayMode == DisplayModes.None)
				return;

			MultiLineTextManager rowManager = Owner.RowManager;
			float paddingTop = Owner.Padding.Top;

			int selectedRowIndex = rowManager.CurrentParagraphIndex;
			Brush foreColorBrush = Style.ForeColorBrush;

			int lineHeight = rowManager.LineHeight;
			int offsetY = (int)Owner.ScrollOffsetY;

			int rowIndex = rowManager.FirstParagraphOnScreen;
			//float halfHeight = bounds.Height / 2f;

			while (rowIndex < rowManager.Paragraphs.Count) {
				Paragraph para = rowManager.Paragraphs [rowIndex];
				Rectangle rowBounds;

				if (DisplayMode == DisplayModes.LineNumber) {
					rowBounds = new Rectangle ((int)(bounds.Left + 0.5f), (int)(paddingTop + bounds.Top + para.Top + offsetY + 0.5f), (int)(bounds.Width + 0.5f), lineHeight);

					for (int i = 0; i < para.LineCount; i++) {
						if (rowBounds.Bottom > bounds.Top) {
							int line = para.Index + i;
							if (line == selectedRowIndex) {
								ctx.FillRectangle (Theme.Brushes.Base01, new RectangleF (bounds.Left, rowBounds.Top, bounds.Width, rowBounds.Height));
								foreColorBrush = Theme.Brushes.Base2;
							} else {
								foreColorBrush = Style.ForeColorBrush;
							}

							ctx.DrawString ((line + 1).ToString (), rowManager.Font, foreColorBrush,
								new RectangleF (bounds.Left, rowBounds.Top, bounds.Width - Padding.Right, lineHeight), Format);
						}

						rowBounds.Offset (0, lineHeight);
						if (rowBounds.Top > bounds.Bottom)
							break;
					}
				} else {
					rowBounds = new Rectangle ((int)(bounds.Left + 0.5f), (int)(paddingTop + bounds.Top + para.Top + offsetY + 0.5f), (int)(bounds.Width + 0.5f), para.LineCount * lineHeight);

					if (rowBounds.Bottom > bounds.Top) {
						int line = para.Index;
						if (line == selectedRowIndex) {
							ctx.FillRectangle (Theme.Brushes.Base01, new RectangleF (bounds.Left, rowBounds.Top, bounds.Width, rowBounds.Height));
							foreColorBrush = Theme.Brushes.Base2;
						} else {
							foreColorBrush = Style.ForeColorBrush;
						}							

						float top = Math.Min(rowBounds.Bottom - lineHeight, Math.Max (rowBounds.Top, bounds.Top));						
						ctx.DrawString ((line + 1).ToString (), rowManager.Font, foreColorBrush,
							new RectangleF (bounds.Left, top, bounds.Width - Padding.Right, lineHeight), Format);
					}						
				}

				if (rowBounds.Top > bounds.Bottom)
					break;

				rowIndex++;
			}
		}

		protected override void CleanupManagedResources ()
		{			
			Owner = null;
			base.CleanupManagedResources ();
		}
	}
}

