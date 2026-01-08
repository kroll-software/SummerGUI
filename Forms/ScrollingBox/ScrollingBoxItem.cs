using System;
using KS.Foundation;

namespace SummerGUI
{
	public class ScrollingBoxItem : DisposableObject
	{
		internal System.Drawing.RectangleF rectF;
		public ScrollingBoxItem()
		{
			rectF = new System.Drawing.RectangleF(0, 0, 0, 0);
		}
	}

	public class ScrollingBoxText : ScrollingBoxItem
	{
		public string Text { get; set; }

		public ScrollingBoxText(String text)
			: base()
		{
			Text = text;
		}			
	}

	public class ScrollingBoxImage : ScrollingBoxItem
	{
		public TextureImage Image { get; private set; }

		public ScrollingBoxImage()
			: base()
		{			
		}

		public ScrollingBoxImage(string filepath, IGUIContext ctx, float opacity = 1f)
			: this()
		{		
			LoadImage (filepath, ctx, opacity);
		}

		private string m_ImageFilePath;
		public string ImageFilePath
		{
			get{
				return m_ImageFilePath;
			}
		}

		public void LoadImage (string filepath, IGUIContext ctx, float opacity = 1f)
		{						
			if (m_ImageFilePath != filepath) {
				m_ImageFilePath = filepath;

				if (Image != null) {
					Image.Dispose ();
					Image = null;
				}

				if (String.IsNullOrEmpty (filepath) || ctx == null)					
					return;

				try {					
					Image = TextureImage.FromFile(filepath, ctx);				
					Image.Opacity = opacity;
				} catch (Exception ex) {
					ex.LogError ();
				}
			}
		}

		protected override void CleanupManagedResources ()
		{
			if (Image != null)
				Image.Dispose ();
			
			base.CleanupManagedResources ();
		}
	}
}

