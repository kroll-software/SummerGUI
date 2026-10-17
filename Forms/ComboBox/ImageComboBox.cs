using System;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{
	/// <summary>
	/// Eintrag für den <see cref="ImageComboBox"/>: trägt zusätzlich zu
	/// Text/Value den <see cref="ImageKey"/> — den Key des Icons in der
	/// <see cref="ImageComboBox.Images"/>-ImageList (null oder leer = ohne Icon).
	/// </summary>
	public class ImageComboBoxItem : ComboBoxItem
	{
		/// <summary>Icon-Key in der ImageList (z. B. der Dateiname aus ImageList.FromFolder).</summary>
		public string ImageKey { get; set; }

		public ImageComboBoxItem (string text)
			: base (text, null)
		{
		}

		public ImageComboBoxItem (string text, string imageKey)
			: base (text, null)
		{
			ImageKey = imageKey;
		}

		public ImageComboBoxItem (string text, object value, string imageKey)
			: base (text, value)
		{
			ImageKey = imageKey;
		}
	}

	/// <summary>
	/// Nicht-editierbare ComboBox mit einem Icon pro Eintrag
	/// (WinForms-/Janus-ImageList-Prinzip, hier mit den Mitteln von SummerGUI).
	/// Die Icons werden stets in der Schriftgrösse des ComboBoxs gezeichnet.
	/// </summary>
	/// <remarks>
	/// Ownership (selbes Muster wie ToolBar und ListView):
	/// Die ImageList gehört ihrem Ersteller — wer sie angelegt hat, disposed sie
	/// (z. B. das Formular). <see cref="Images"/> wird vom ComboBox nur entlieh:
	/// bei CleanupManagedResources wird die Referenz abgebrochen, nicht disposed.
	/// Die Texturen gehören der ImageList; ob sie bei deren Dispose freigegeben
	/// werden, entscheidet die ImageList-Option ShouldDisposeImages.
	/// So kann nirgends doppelt disposed werden.
	/// </remarks>
	public class ImageComboBox : ComboListBox
	{
		ImageList m_Images;

		/// <summary>
		/// Icon-Quelle (optional). Ausserhalb angelegte (z. B. über
		/// ImageList.FromFolder) Listen gehören ihrem Ersteller —
		/// der ComboBox disposed sie nicht.
		/// </summary>
		public ImageList Images
		{
			get { return m_Images; }
			set
			{
				if (!ReferenceEquals (m_Images, value))
				{
					m_Images = value;
					Invalidate ();
				}
			}
		}

		/// <summary>Erzeugt einen leeren ImageComboBox.</summary>
		public ImageComboBox (string name)
			: base (name)
		{
		}

		/// <summary>
		/// Erzeugt einen ImageComboBox mit der übergebenen (externen) ImageList.
		/// </summary>
		public ImageComboBox (string name, ImageList images)
			: base (name)
		{
			Images = images;
		}

		/// <summary>Fügt einen Eintrag ohne Icon an.</summary>
		public void AddItem (string text)
		{
			Items.AddLast (new ImageComboBoxItem (text));
		}

		/// <summary>
		/// Fügt einen Eintrag mit Icon an; <paramref name="imageKey"/>
		/// ist der Key in <see cref="Images"/>.
		/// </summary>
		public void AddItem (string text, string imageKey)
		{
			Items.AddLast (new ImageComboBoxItem (text, imageKey));
		}

		/// <summary>ImageKey des gewählten Eintrags (null, wenn nichts gewählt oder ohne Key).</summary>
		public string SelectedImageKey
		{
			get
			{
				ImageComboBoxItem item = SelectedItem as ImageComboBoxItem;
				return (item != null) ? item.ImageKey : null;
			}
		}

		/// <summary>
		/// Wählt den ersten Eintrag mit dem angegebenen <paramref name="imageKey"/> aus;
		/// tut nichts, wenn kein Eintrag diesen Key trägt.
		/// (Linearer Scan — die Eintragsliste ist bewusst unsortiert.)
		/// </summary>
		public void SetSelectedImageKey (string imageKey)
		{
			int count = Items.Count;
			for (int i = 0; i < count; i++)
			{
				ImageComboBoxItem item = Items [i] as ImageComboBoxItem;
				if (item != null && item.ImageKey == imageKey)
				{
					SelectedIndex = i;
					return;
				}
			}
		}

		/// <summary>Zeichnet einen Eintrag: Icon in Schriftgrösse links, Text rechts daneben.</summary>
		public override void DrawItem (IGUIContext ctx, RectangleF bounds, ComboBoxItem item, IWidgetStyle style)
		{
			if (item == null)
				return;

			bounds.Inflate (-TextMargin.Width, 0);

			float textLeft = bounds.Left;

			ImageComboBoxItem it = item as ImageComboBoxItem;
			if (Images != null && it != null && !String.IsNullOrEmpty (it.ImageKey))
			{
				TextureImage tex;
				if (Images.TryGetValue (it.ImageKey, out tex) && tex != null && tex.Width > 0 && tex.Height > 0)
				{
					// Icon stets in Schriftgrösse: Höhe = Font-Höhe, Breite = Seitenverhältnis,
					// vertikal in der Zeile zentriert.
					float iconH = Math.Min (Font.TextBoxHeight, bounds.Height - 2f);
					float iconW = tex.Width * (iconH / tex.Height);
					float iconX = bounds.Left;
					float iconY = bounds.Top + (bounds.Height - iconH) / 2f;
					tex.Paint (ctx, new RectangleF (iconX, iconY, iconW, iconH));

					textLeft = iconX + iconW + 4f;
				}
			}

			float textW = Math.Max (1f, bounds.Right - textLeft);
			ctx.DrawString (item.Text, Font, (style != null) ? style.ForeColorBrush : null,
				new RectangleF (textLeft, bounds.Top, textW, bounds.Height), FontFormat.DefaultSingleLine);
		}

		/// <summary>
		/// Bricht nur die Referenz auf die ImageList ab. Die ImageList disposed ihr
		/// Ersteller, die Texturen disposed die ImageList selbst (abgesehen von ihrer
		/// eigenen Option) — doppeltes Dispose ausgeschlossen.
		/// </summary>
		protected override void CleanupManagedResources ()
		{
			m_Images = null;
			base.CleanupManagedResources ();
		}
	}
}
