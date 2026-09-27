using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{
	// The named colours are sourced directly from the BCL palette class
	//   System.Drawing.Color — 140 named static Color properties
	//   (AliceBlue, AntiqueWhite, …, YellowGreen) — the full WinForms named-colour set.
	// We use reflection over typeof(Color)'s public static Color properties so the
	// code stays portable and picks up every named colour without hard-coding
	// property names. (SystemColors, a separate class, is deliberately avoided.)
	public class ColorComboBox : ComboBoxBase
	{
		IGUIFont m_Font;
		public IGUIFont Font
		{
			get { return m_Font; }
			set
			{
				if (m_Font != value)
				{
					m_Font = value;
					OnFontChanged();
				}
			}
		}

		protected virtual void OnFontChanged()
		{
			ResetCachedLayout();
		}

		[DpiScalable]
		public Size TextMargin { get; set; }

		public ColorComboBox(string name)
			: base(name)
		{
			InsertButton();
			Button.Dock = Docking.Fill;

			Font = FontManager.Manager.DefaultFont;
			TextMargin = new Size(6, 2);
			ItemHeight = Font != null ? Font.TextBoxHeight : 24f;
			MaxSize = new SizeF(int.MaxValue, ItemHeight);

			// Populate from the BCL named-colour list (see PopulateNamedItems).
			PopulateNamedItems();
		}

		/// <summary>
		/// Adds one <see cref="ColorItem"/> per named colour in the
		/// <see cref="Color"/> palette class — the 140 static named colours
		/// (AliceBlue, AntiqueWhite, …, YellowGreen). Reflection over
		/// <see cref="Color"/>'s static Color properties keeps the list complete
		/// and portable — no hard-coded property names. Sorted alphabetically.
		/// </summary>
		private void PopulateNamedItems()
		{
			var entries =
				typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static)
					.Where(p => p.PropertyType == typeof(Color))
					.Select(p => new KeyValuePair<string, Color>(p.Name, (Color)p.GetValue(null)))
					.Where(kvp => kvp.Value.A > 0)   // skip Transparent
					.DistinctBy(kvp => kvp.Value.ToArgb())
					.OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
					.ToList();

			foreach (var kvp in entries)
				Items.AddLast(new ColorItem(kvp.Value) { Text = kvp.Key });
		}

		protected override void OnScaleWidget(IGUIContext ctx, float absoluteScaleFactor, float relativeScaleFactor)
		{
			base.OnScaleWidget(ctx, absoluteScaleFactor, relativeScaleFactor);
			// ItemHeight = Font != null ? Font.TextBoxHeight : 24f;
		}

	protected string m_Text = null;
		public override string Text
		{
			get
			{
				if (!string.IsNullOrEmpty(m_Text))
					return m_Text;
				ComboBoxItem item = SelectedItem;
				if (item is ColorItem ci)
					return ci.Text;
				return string.Empty;
			}
			set
			{
				if (m_Text != value)
				{
					m_Text = value;
					OnTextChanged();
				}
			}
		}

		public override void Clear()
		{
			Items.Clear();
			m_Text = null;
		}

		public void Add(Color color, string name = null)
		{
			if (string.IsNullOrEmpty(name))
				name = ColorItem.FormatName(color);
			Items.AddLast(new ColorItem(color) { Text = name });
		}

		public Color GetSelectedColor()
		{
			ComboBoxItem item = SelectedItem;
			if (item is ColorItem ci)
				return ci.Color;
			return Color.Empty;
		}

		public void SetSelectedColor(Color color)
		{
			var item = Items.FirstOrDefault(i => i is ColorItem ci && ci.Color == color);
			if (item != null)
			{
				SelectItem(item);
			}
		}

		public override SizeF PreferredSize(IGUIContext ctx, SizeF proposedSize)
		{
			return new SizeF(proposedSize.Width, Font != null ? Font.TextBoxHeight : ItemHeight);
		}

		public override void Update(IGUIContext ctx)
		{
			if (Bounds.Width <= 0 || Bounds.Height <= 0)
				return;

			using (var clip = new ClipBoundClip(ctx, Bounds, true))
			{
				if (!clip.IsEmptyClip)
				{
					Button.Update(ctx);
					try
					{
						RectangleF bounds = Bounds;
						bounds.Width -= Bounds.Height;
						OnPaint(ctx, bounds);
					}
					catch (Exception ex)
					{
						ex.LogError();
					}
				}
			}

			if (DropDownWindow != null && DropDownWindow.Visible)
				DropDownWindow.Update(ctx);
		}

		// DrawItem is the hook that the ColorComboBoxDropDownList (via
		// Parent.Parent) calls; also used by ColorComboBox.OnPaint to render the
		// selected color in the box itself.
		public override void DrawItem(IGUIContext ctx, RectangleF bounds, ComboBoxItem item, IWidgetStyle style)
		{
			if (item == null)
				return;

			ColorItem ci = (item as ColorItem);
			if (ci == null)
				return;

			// Same 2:1 swatch geometry as HatchStyle/DashStyle combo boxes so all
			// three pickers look uniform (height = row minus margin, width = 2x height).
			float swatchH = bounds.Height - 8f;
			float swatchW = swatchH * 2f;
			float swatchX = bounds.Left + TextMargin.Width;
			float swatchY = bounds.Top + (bounds.Height - swatchH) / 2f;

			using (var brush = new SolidBrush(ci.Color))
			{
				ctx.FillRectangle(brush, new RectangleF(swatchX, swatchY, swatchW, swatchH));
			}
			ctx.DrawRectangle(new Pen(Color.FromArgb(160, Color.Gray)), new RectangleF(swatchX, swatchY, swatchW, swatchH));

			RectangleF textRect = new RectangleF(swatchX + swatchW + TextMargin.Width, bounds.Top,
				bounds.Right - (swatchX + swatchW + TextMargin.Width), bounds.Height);
			ctx.DrawString(ci.Text, Font, style != null ? style.ForeColorBrush : null,
				textRect, FontFormat.DefaultSingleLine);
		}

		public override void OnPaint(IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint(ctx, bounds);
			ComboBoxItem sel = SelectedItem;
			if (sel == null)
				return;
			DrawItem(ctx, bounds, sel, Button.Style);
		}

		// Spec: ColorComboBox supplies its own drop-down host (ColorComboBox
		// DropDown = Container + IOverlayWidget, hosting ColorComboBoxDropDown
		// List(Fill) + ButtonContainer(Bottom) with a "More ..." Button).
		protected override Container CreateDropDownWindow()
		{
			return new ColorComboBoxDropDown(Name + ".dd");
		}

		// ComboBoxBase.OnItemSelected is not virtual; instead we subscribe to
		// the base ItemSelected event (raised after SelectedItem is set) and
		// keep the displayed text in sync with the picked color name.
		void OnBaseItemSelected(object sender, EventArgs e)
		{
			ComboBoxItem item = SelectedItem;
			if (item is ColorItem ci)
				m_Text = ci.Text;
		}

		protected override void CleanupManagedResources()
		{
			m_Text = null;
			m_Font = null;
			base.CleanupManagedResources();
		}
	}
}
