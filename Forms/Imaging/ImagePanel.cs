using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{
	public enum ImageSizeModes
	{
		None,
		AutoSize,
		Stretch,
		ShrinkToFit,
		ShrinkToFitHorizontal,
		ShrinkToFitVertical,
		AlwaysFit,
		TileHorizontal,
		TileVertical,
		TileAll
	}
		
	public class ImagePanel : Widget
	{
		ImageSizeModes m_SizeMode;
		public ImageSizeModes SizeMode 
		{ 
			get {
				return m_SizeMode;
			}
			set {
				m_SizeMode = value;
				ResetCachedLayout ();
			}
		}

		ImageList m_ImageList;
		public ImageList ImageList 
		{ 
			get {
				return m_ImageList;
			}
			set {
				if (m_ImageList != value) {
					m_ImageList = value;
					LoadImageFromImageList ();
				}
			}
		}			

		string m_ImageKey;
		public string ImageKey 
		{ 
			get {
				return m_ImageKey;
			}
			set {
				if (m_ImageKey != value) {
					m_ImageKey = value;
					LoadImageFromImageList ();
				}
			}
		}

		void LoadImageFromImageList()
		{
			if (IsDisposed)
				return;
			FilePath = null;
			if (Image != null) {
				if (ShouldDisposeImage)
					Image.Dispose ();
				Image = null;
			}
			if (m_ImageList != null && !String.IsNullOrEmpty(m_ImageKey)) {
				TextureImage image;
				if (ImageList.TryGetValue (m_ImageKey, out image))
					Image = image;
				else
					Image = null;
				ShouldDisposeImage = false;
			}
			ResetCachedLayout ();
		}

		public TextureImage Image { get; private set; }
		public bool ShouldDisposeImage { get; set; }

		public ImagePanel (string name, Docking dock, ImageList imageList, string imageKey)
			: this(name, dock, imageList, imageKey, new EmptyWidgetStyle()) {}

		public ImagePanel (string name, Docking dock, ImageList imageList, string imageKey, WidgetStyle style)
			: base(name, dock, style)
		{
			SizeMode = ImageSizeModes.ShrinkToFit;
			VAlign = Alignment.Center;
			HAlign = Alignment.Center;
			m_ImageList = imageList;
			ImageKey = imageKey;
			ShouldDisposeImage = false;
		}

		public ImagePanel (string name, Docking dock, TextureImage image)
			: this(name, dock, image, new EmptyWidgetStyle()) {}

		public ImagePanel (string name, Docking dock, TextureImage image, WidgetStyle style)
			: base(name, dock, style)
		{
			SizeMode = ImageSizeModes.ShrinkToFit;
			VAlign = Alignment.Center;
			HAlign = Alignment.Center;
			Image = image;
			ShouldDisposeImage = false;
		}

		public ImagePanel (string name, Docking dock, string filePath)
			: this(name, dock, filePath, new EmptyWidgetStyle()) {}

		public ImagePanel (string name, Docking dock, string filePath, WidgetStyle style)
			: base(name, dock, style)
		{
			SizeMode = ImageSizeModes.ShrinkToFit;
			VAlign = Alignment.Center;
			HAlign = Alignment.Center;
			FilePath = filePath;
			ShouldDisposeImage = true;
		}

		public void LoadImageAsync(string filePath, IGUIContext ctx)
		{
			if (IsDisposed || String.IsNullOrEmpty (filePath))
				return;

			try {
				if (Image != null && ShouldDisposeImage)
					Image.Dispose ();
				Image = TextureImage.FromFile (filePath.FixedExpandedPath(), ctx);
				//Image.Opacity = 0.999f;
			} catch (Exception ex) {
				ex.LogError ();
			}
			finally {				
				Update (true);
				m_InitialFilePath = null;
			}
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{				
			if (CachedPreferredSize == SizeF.Empty) {
				if (Image == null) {
					return base.PreferredSize (ctx, proposedSize);
				} else {			
					SizeF sz = DestRect (new RectangleF (PointF.Empty, proposedSize)).Size;
					float borderX2 = Style.Border * 2f;
					//borderX2 = 0;
					CachedPreferredSize = new SizeF (sz.Width + Padding.Width + borderX2, sz.Height + Padding.Height + borderX2);
				}
			}
			return CachedPreferredSize;
		}

		private string m_FilePath;
		public string FilePath
		{
			get {
				return m_FilePath;
			}
			set {
				if (m_FilePath != value) {
					m_FilePath = value;
					m_InitialFilePath = m_FilePath;
					ResetCachedLayout ();
				}
			}
		}

		private string m_InitialFilePath;
		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{	
			if (!IsDisposed && Image == null && !String.IsNullOrEmpty (m_InitialFilePath)) {
				// das ist aber noch nicht async..
				LoadImageAsync (m_InitialFilePath, ctx);
				m_InitialFilePath = null;
			}

			base.OnLayout (ctx, bounds);
		}


		public override void SetBounds (float x, float y, float width, float height)
		{
			if (Image == null) {
				base.SetBounds (x, y, width, height);
				return;
			}

			int borderX2 = (int)(Style.Border * 2f);
			RectangleF dst = DestRect (new RectangleF (x, y, width, height));
			base.SetBounds (x, y, Math.Max(width, dst.Width + Padding.Width + borderX2), Math.Max(height, dst.Height + Padding.Height + borderX2));
		}

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);		

			if (Image != null) {
				RectangleF dst = DestRect (bounds);
				Image.BlendColor = Style.ForeColorPen.Color;
				Image.Paint (ctx, dst);
			}
		}

		protected RectangleF DestRect(RectangleF bounds)
		{			
			RectangleF rDest = RectangleF.Empty;
			if (Image == null)
				return rDest;			

			float border = Style.Border;
			float borderX2 = Style.Border * 2f;

			RectangleF paddedBounds = new RectangleF (bounds.Left + Padding.Left + border, bounds.Top + Padding.Top + border, bounds.Width - Padding.Width - borderX2, bounds.Height - Padding.Height - borderX2);
			bounds = paddedBounds;

			float w = (float)Image.Width;
			float h = (float)Image.Height;
			float zoom = 1;

			switch (SizeMode) {
			case ImageSizeModes.None:
			case ImageSizeModes.AutoSize:
				rDest = new RectangleF (PointF.Empty, Image.Size);
				break;
			case ImageSizeModes.ShrinkToFitHorizontal:
				if (w > bounds.Width) {
					zoom = bounds.Width / w;
					rDest.Width = w * zoom;
					rDest.Height = h * zoom;
				} 
				else
					rDest = new RectangleF (PointF.Empty, Image.Size);
				break;
			case ImageSizeModes.ShrinkToFitVertical:
				if (h > bounds.Height) {
					zoom = bounds.Height / h;
					rDest.Width = w * zoom;
					rDest.Height = h * zoom;
				}
				else
					rDest = new RectangleF (PointF.Empty, Image.Size);
				break;
			case ImageSizeModes.ShrinkToFit:
				if (w > bounds.Width || h > bounds.Height) {
					zoom = Math.Min (bounds.Width / w, bounds.Height / h);
					rDest.Width = w * zoom;
					rDest.Height = h * zoom;
				}
				else
					rDest = new RectangleF (PointF.Empty, Image.Size);
				break;

			case ImageSizeModes.AlwaysFit:
				zoom = Math.Min (bounds.Width / w, bounds.Height / h);
				rDest.Width = w * zoom;
				rDest.Height = h * zoom;			
				break;

			case ImageSizeModes.Stretch:
			case ImageSizeModes.TileHorizontal:
			case ImageSizeModes.TileVertical:
			case ImageSizeModes.TileAll:
				rDest = new RectangleF(PointF.Empty, bounds.Size);
				break;
			}

			switch (HAlign) {
			case Alignment.Center:
				//rDest.X = bounds.Width / 2;
				rDest.X = Math.Max(0, (bounds.Width - rDest.Width) / 2f);
				break;				
			case Alignment.Far:
				rDest.X = bounds.Width - rDest.Width;
				break;				
			}

			switch (VAlign) {
			case Alignment.Center:				
				rDest.Y = Math.Max(0, (bounds.Height - rDest.Height) / 2);
				break;				
			case Alignment.Far:
				rDest.Y = bounds.Height - rDest.Height;
				break;				
			}
				
			rDest.Offset(TooltipLocation.X + Padding.Left + border, TooltipLocation.Y + Padding.Top + border);
			return rDest;
		}

		protected override void CleanupManagedResources ()
		{
			if (Image != null && ShouldDisposeImage)
				Image.Dispose ();			
			m_ImageList = null;
			base.CleanupManagedResources ();
		}
	}
}

