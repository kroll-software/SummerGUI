using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{
	/// <summary>
	/// Combo box for selecting a UI font. It follows the <b>simple</b> combo box pattern —
	/// it inherits from <see cref="ComboListBox"/> (classic drop-down, no "More …" button) and is
	/// therefore not editable and shows its selection in the closed state as well.
	/// <para>
	/// One <see cref="ComboBoxItem"/> per bundled font file (`.ttf` / `.otf`) is offered in
	/// alphabetical order; icon fonts (FontAwesome) are deliberately excluded. Each item stores
	/// the resolved font file path in <see cref="ComboBoxItem.Value"/> and a pretty name in
	/// <see cref="ComboBoxItem.Text" />. Only <see cref="DrawItem" /> is overridden so every entry
	/// renders a white preview swatch as wide as the sample text needs at <see cref="PreviewSize" />, with
	/// the sample drawn live in that font, next to the name — visually consistent with
	/// <see cref="HatchStyleComboBox" /> and <see cref="DashStyleComboBox" />. The sample is clipped
	/// into its swatch so no font can bleed outside the box.
	/// </para>
	/// <para>
	/// Previews are created <b>on demand</b>: a preview <see cref="IGUIFont"/> is only allocated
	/// the first time a row is drawn and cached by path. Because it is created in
	/// <see cref="GlyphFilterFlags.OnDemand"/> mode it pre-loads no glyphs — only the few glyphs
	/// actually drawn ("Ag") become tiny single textures. Nothing selected never touches the GPU.
	/// All previews are disposed together with the combo box.
	/// </para>
	/// <para>
	/// The selected font is exposed via <see cref="SelectedFontPath"/>. The caller can then create
	/// an <c>IGUIFont</c> at any size, e.g.:
	/// <code>FontManager.CreateFont(new GUIFontConfiguration { Path = cbb.SelectedFontPath, Size = 14f })</code>
	/// </para>
	/// </summary>
	public class FontComboBox : ComboListBox
	{
		/// <summary>Point size at which the per-row preview sample (<see cref="PreviewText"/>) is created.</summary>
		public float PreviewSize { get; set; } = 11f;

		/// <summary>Background colour of the preview swatch (filled before the sample text).</summary>
		public Color PreviewBackColor { get; set; } = Color.White;

		/// <summary>Colour of the preview sample text.</summary>
		public Color PreviewTextColor { get; set; } = Color.Black;

		/// <summary>Sample text drawn in the preview font (kept short: it is rendered on demand).</summary>
		public string PreviewText { get; set; } = "The quick brown fox";

		private static readonly string[] s_includedExtensions = { ".ttf", ".otf" };
		private static readonly string[] s_excludedFiles = { "fontawesome.otf" };

		// On-demand preview fonts, cached by absolute path (GUI thread only).
		private Dictionary<string, IGUIFont> m_Previews;

		public FontComboBox(string name)
				: base(name)
		{
			m_Previews = new Dictionary<string, IGUIFont>();
			PopulateItems();
			if (Items.Count > 0)
				SelectedIndex = 0;
		}

		private void PopulateItems()
		{
			List<string> files = new List<string>();
			try
			{
				// "Fonts" is resolved against the SummerGUI assembly directory and copied next to
				// the executable by the project file, so this works in both library and demo.
				string folderPath = "Fonts".FixedExpandedPath();
				if (Directory.Exists(folderPath))
				{
					files = Directory.GetFiles(folderPath)
						.Where(f => s_includedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
						.Where(f => !s_excludedFiles.Contains(Path.GetFileName(f).ToLowerInvariant()))
						.OrderBy(f => Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase)
						.ToList();
				}
			}
			catch (Exception ex)
			{
				ex.LogWarning();
			}

			foreach (string path in files)
			{
				string name = Path.GetFileNameWithoutExtension(path);
				name = string.Join(" ", name.Replace('-', ' ').Replace('_', ' ')
					.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

				// unsorted, thread-safe list → append (AddLast); Value = path, Text = name
				Items.AddUnsorted(name, path);
			}
		}

		/// <summary>The currently selected font file path (null if nothing is selected).</summary>
		public string SelectedFontPath
		{
			get
			{
				object v = SelectedValue;
				return (v is string s) ? s : null;
			}
		}

		/// <summary>Selects the item for the given font file path (no-op if not present).</summary>
		public void SetSelectedFont(string path)
		{
			if (String.IsNullOrEmpty(path))
				return;
			string p = path.FixedExpandedPath().ToLowerInvariant();
			for (int i = 0; i < Items.Count; i++)
			{
				string v = Items[i].Value as string;
				if (v != null && v.ToLowerInvariant() == p)
				{
					SelectedIndex = i;
					return;
				}
			}
		}

		/// <summary>
		/// Returns a cached on-demand preview font for <paramref name="path"/> at <paramref name="size"/>,
		/// creating it on first use (cached by path+size). Returns null if the font cannot be loaded
		/// (a warning is logged).
		/// </summary>
		private IGUIFont GetPreview(string path, float size)
		{
			if (String.IsNullOrEmpty(path) || m_Previews == null)
				return null;

			string key = path.ToLowerInvariant() + "@" + size.ToString("0.##");
			IGUIFont font;
			if (m_Previews.TryGetValue(key, out font) && font != null && !font.IsDisposed)
				return font;

			IGUIFont created = null;
			try
			{
				GUIFontConfiguration conf = new GUIFontConfiguration
				{
					Tag = "FontComboBoxPreview:" + key,
					Path = path,
					Size = size,
					ScaleFactor = FontManager.Manager.ScaleFactor,
					Filter = GlyphFilterFlags.OnDemand,
					RenderingHint = FontRenderingHints.Smooth
				};
				created = FontManager.CreateFont(conf);
			}
			catch (Exception ex)
			{
				ex.LogWarning();
			}

			if (created != null)
				m_Previews[key] = created;
			return created;
		}

		public override void DrawItem(IGUIContext ctx, RectangleF bounds, ComboBoxItem item, IWidgetStyle style)
		{
			if (item == null)
				return;

			bounds.Inflate(-TextMargin.Width, 0);

			string path = (item.Value is string s) ? s : null;
			string name = !String.IsNullOrEmpty(item.Text) ? item.Text
				: (path != null) ? Path.GetFileNameWithoutExtension(path) : "";
			float gap = TextMargin.Width;

			// --- swatch as wide as the sample needs at PreviewSize ---
			IGUIFont preview = GetPreview(path, PreviewSize);
			SizeF measured = (preview != null) ? preview.Measure(PreviewText) : SizeF.Empty;
			if (measured.Width <= 0f)
				measured = new SizeF(4f * gap, PreviewSize);

			float swatchW = measured.Width + 2f * (float)gap;

			// --- keep room for the name on the right ---
			float nameW = (!String.IsNullOrEmpty(name) && Font != null) ? Font.Measure(name).Width : 0f;
			float swatchMax = bounds.Width - nameW - gap;
			if (swatchW > swatchMax && swatchMax > 0f)
			{
				float scale = swatchMax / swatchW;
				preview = GetPreview(path, Math.Max(PreviewSize * scale, 4f));
				swatchW = swatchMax;
			}

			float swatchH = bounds.Height - 8f;
			RectangleF swatch = new RectangleF(bounds.Left, bounds.Top + (float)(bounds.Height - swatchH) / 2f, swatchW, swatchH);

			using (var bg = new SolidBrush(PreviewBackColor))
			{
				ctx.FillRectangle(bg, swatch);
			}

			if (preview != null)
			{
				// clip the sample into the swatch — no font may bleed outside its box
				using (var clip = new ClipBoundClip(ctx, swatch, true))
				{
					RectangleF sample = new RectangleF(swatch.Left + gap / 2f, swatch.Top, swatch.Width - gap, swatch.Height);
					using (var fg = new SolidBrush(PreviewTextColor))
					{
						ctx.DrawString(PreviewText, preview, fg, sample, FontFormat.DefaultSingleLine);
					}
				}
			}
			using (var border = new Pen(Color.Gray, 1f))
			{
				ctx.DrawRectangle(border, swatch);
			}

			// --- font name to the right of the swatch ---
			float textLeft = swatch.Right + gap;
			RectangleF textRect = new RectangleF(textLeft, bounds.Top, bounds.Right - textLeft, bounds.Height);
			ctx.DrawString(name, Font, (style != null) ? style.ForeColorBrush : null,
				textRect, FontFormat.DefaultSingleLine);
		}

		protected override void CleanupManagedResources()
		{
			// Release the on-demand preview fonts we created.
			if (m_Previews != null)
			{
				foreach (IGUIFont f in m_Previews.Values)
				{
					if (f != null)
					{
						try { f.Dispose(); } catch { }
					}
				}
				m_Previews.Clear();
				m_Previews = null;
			}
			base.CleanupManagedResources();
		}
	}
}
