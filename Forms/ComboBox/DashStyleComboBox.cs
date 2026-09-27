using System;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{
	/// <summary>
	/// Combo box for selecting a <see cref="DashStyle"/> (line dash family). It follows
	/// the <b>simple</b> combo box pattern — inherits from <see cref="ComboListBox"/> (classic
	/// drop-down, no "More ..." button) and is therefore not editable; only <see cref="DrawItem"/> is overridden
	/// so every list entry shows an elongated (twice as wide as it is tall) preview of the
	/// dash lines next to its name.
	/// <para>
	/// The selected <see cref="DashStyle"/> is stored in <see cref="ComboBoxItem.Value"/>
	/// — no dedicated item type is needed. Only the five named families are offered
	/// (<see cref="DashStyle.Custom"/> is excluded: it requires a caller-supplied pattern
	/// and therefore has no single previewable shape).
	/// </para>
	/// </summary>
	public class DashStyleComboBox : ComboListBox
	{
		/// <summary>Colour of the preview dash lines.</summary>
		public Color PreviewLineColor { get; set; } = Color.Black;

		/// <summary>Background colour of the preview swatch (filled before the lines).</summary>
		public Color PreviewBackColor { get; set; } = Color.White;

		public DashStyleComboBox(string name)
			: base(name)
		{
			// enum order, unsorted thread-safe list → append (AddLast)
			Items.AddUnsorted(DashStyle.Solid.ToString(), DashStyle.Solid);
			Items.AddUnsorted(DashStyle.Dash.ToString(), DashStyle.Dash);
			Items.AddUnsorted(DashStyle.Dot.ToString(), DashStyle.Dot);
			Items.AddUnsorted(DashStyle.DashDot.ToString(), DashStyle.DashDot);
			Items.AddUnsorted(DashStyle.DashDotDot.ToString(), DashStyle.DashDotDot);

			SelectedIndex = 0;
		}

		/// <summary>The currently selected dash style.</summary>
		public DashStyle SelectedDashStyle
		{
			get
			{
				object v = SelectedValue;
				return (v is DashStyle ds) ? ds : DashStyle.Solid;
			}
		}

		/// <summary>Selects the item for the given style (no-op if not present).</summary>
		public void SetSelectedDashStyle(DashStyle style)
		{
			for (int i = 0; i < Items.Count; i++)
			{
				if (Items[i].Value is DashStyle ds && ds == style)
				{
					SelectedIndex = i;
					return;
				}
			}
		}

		public override void DrawItem(IGUIContext ctx, RectangleF bounds, ComboBoxItem item, IWidgetStyle style)
		{
			if (item == null)
				return;

			bounds.Inflate(-TextMargin.Width, 0);

			// --- elongated (2:1) preview: three dash lines of the selected family ---
			float swatchH = bounds.Height - 8f;
			float swatchW = swatchH * 2f;
			float swatchX = bounds.Left;
			float swatchY = bounds.Top + (bounds.Height - swatchH) / 2f;
			RectangleF swatch = new RectangleF(swatchX, swatchY, swatchW, swatchH);

			object v = item.Value;
			DashStyle ds = (v is DashStyle d) ? d : DashStyle.Solid;

			// white background: the lines are drawn dark and would be invisible on the dark theme
			using (var bg = new SolidBrush(PreviewBackColor))
			{
				ctx.FillRectangle(bg, swatch);
			}

			using (var pen = new Pen(PreviewLineColor, 2f, ds))
			{
				// three evenly spaced horizontal lines so the dash pattern reads clearly
				for (int k = 1; k <= 3; k++)
				{
					float y = swatch.Top + (k * swatch.Height) / 3f;
					ctx.DrawLine(pen, swatch.Left + 1f, y, swatch.Right - 1f, y);
				}
			}
			using (var border = new Pen(Color.Gray, 1f))
			{
				ctx.DrawRectangle(border, swatch);
			}

			// --- item text to the right of the swatch ---
			float textLeft = swatchX + swatchW + TextMargin.Width;
			RectangleF textRect = new RectangleF(textLeft, bounds.Top, bounds.Right - textLeft, bounds.Height);
			ctx.DrawString(item.Text, Font, (style != null) ? style.ForeColorBrush : null,
				textRect, FontFormat.DefaultSingleLine);
		}
	}
}
