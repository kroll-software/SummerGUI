using System;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{
	/// <summary>
	/// Combo box for selecting a <see cref="HatchStyle"/> (the 53 WinForms-compatible
	/// fill textures). It follows the <b>simple</b> combo box pattern — it inherits from
	/// <see cref="ComboListBox"/> (classic drop-down, no "More ..." button) and is therefore
	/// not editable; only <see cref="DrawItem"/> is overridden so every list entry shows an elongated
	/// (twice as wide as it is tall) live preview of the hatch next to its name.
	/// <para>
	/// The selected <see cref="HatchStyle"/> is stored in <see cref="ComboBoxItem.Value"/>
	/// — no dedicated item type is needed.
	/// </para>
	/// </summary>
	public class HatchStyleComboBox : ComboListBox
	{
		/// <summary>Motif colour of the preview swatches.</summary>
		public Color PreviewMotifColor { get; set; } = Color.Black;

		/// <summary>Background colour of the preview swatches.</summary>
		public Color PreviewBackColor { get; set; } = Color.White;

		public HatchStyleComboBox(string name)
			: base(name)
		{
			foreach (HatchStyle style in Enum.GetValues(typeof(HatchStyle)))
			{
				if ((int)style < (int)HatchStyle.Horizontal) continue;   // skip Min alias
				if ((int)style > (int)HatchStyle.SolidDiamond) continue; // skip Max/LargeGrid alias

				// unsorted, thread-safe list → append (AddLast) to keep enum order
				Items.AddUnsorted(style.ToString(), style);
			}

			SelectedIndex = 0;
		}

		/// <summary>The currently selected hatch style.</summary>
		public HatchStyle SelectedHatchStyle
		{
			get
			{
				object v = SelectedValue;
				return (v is HatchStyle hs) ? hs : (HatchStyle)HatchStyle.LargeConfetti;
			}
		}

		/// <summary>Selects the item for the given style (no-op if not present).</summary>
		public void SetSelectedHatchStyle(HatchStyle style)
		{
			for (int i = 0; i < Items.Count; i++)
			{
				if (Items[i].Value is HatchStyle hs && hs == style)
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

			// --- elongated (2:1) live preview of the hatch texture ---
			float swatchH = bounds.Height - 8f;
			float swatchW = swatchH * 2f;
			float swatchX = bounds.Left;
			float swatchY = bounds.Top + (bounds.Height - swatchH) / 2f;
			RectangleF swatch = new RectangleF(swatchX, swatchY, swatchW, swatchH);

			object v = item.Value;
			HatchStyle hs = (v is HatchStyle h) ? h : (HatchStyle)HatchStyle.LargeConfetti;

			using (var brush = new HatchBrush(hs, PreviewBackColor, PreviewMotifColor))
			{
				ctx.FillRectangle(brush, swatch);
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
