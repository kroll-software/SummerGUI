using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;
using SummerGUI;
using Pfz.Collections;

namespace SummerGUI
{
	public class ImageList : DisposableObject
	{			
	
		public static string MissingImagePath;

		public string Name { get; private set; } 

		/// <summary>
		/// Images will be rescaled when added.
		/// Default is Size.Empty = no rescaling
		/// This means: images may have different sizes.
		/// </summary>
		/// <value>The size of each image in the list, when specified.</value>
		public Size ImageSize { get; private set; }

		/// <summary>
		/// Beware of this !
		/// By default it's set to true.
		/// </summary>
		/// <value><c>true</c> if should dispose images; otherwise, <c>false</c>.</value>
		public bool ShouldDisposeImages { get; private set; }

		public TextureImage MissingImage { get; private set; }

		protected ThreadSafeDictionary<string, TextureImage> m_Images;

		public IGUIContext Context { get; private set; }

		public ImageList(IGUIContext context, string name) : this(context, name, Size.Empty, true) {}
		public ImageList(IGUIContext context, string name, Size imageSize) : this(context, name, imageSize, true) {}
		public ImageList(IGUIContext context, string name, Size imageSize, bool shouldDisposeImages)
		{			
			Context = context;
			m_Images = new ThreadSafeDictionary<string, TextureImage>();
			Name = name;
			ImageSize = imageSize;
			ShouldDisposeImages = shouldDisposeImages;
			LoadMissingImage ();
		}

		void LoadMissingImage()
		{
			try {				
				if (!String.IsNullOrEmpty (MissingImagePath) && File.Exists (MissingImagePath)) {
					Task.Run(() => {
						TextureImage oldImage = MissingImage;
						try {
							MissingImage = TextureImage.FromFile(MissingImagePath, Context, null, ImageSize);	
						} catch (Exception ex) {
							ex.LogError();
						} finally {
							if (oldImage != null)
								oldImage.Dispose();
						}
					});
				}	
			} catch (Exception ex) {
				ex.LogError();
			}
		}


		public static async Task<ImageList> FromFolderAsync(IGUIContext ctx, string folderPath, string name = null, bool includeSubFolders = false, Size imageSize = default(Size))
		{
			return await Task<ImageList>.Run(() => FromFolder (ctx, folderPath, name, includeSubFolders, imageSize));
		}

		public static ImageList FromFolder(IGUIContext ctx, string folderPath, string name = null, bool includeSubFolders = false, Size imageSize = default(Size))
		{
			try {
				folderPath = folderPath.BackSlash (false).FixedExpandedPath ();
				if (String.IsNullOrEmpty (folderPath) || !Directory.Exists (folderPath))
					return null;
				if (String.IsNullOrEmpty (name))
					name = Path.GetDirectoryName(folderPath);
				FileEnumerator enu = new FileEnumerator (folderPath, includeSubFolders);
				enu.FileMask = "*.png;*.jpg;*.jpeg;*.gif;*.tif;*.tiff";
				enu.ScanToList ();
				if (enu.Count > 0) {
					ImageList ret = new ImageList (ctx, name, imageSize, true);
					foreach (var path in enu.FileList) {
						ret.AddImage (Path.GetFileName (path), path);
					}
					return ret;
				}	
			} catch (Exception ex) {
				ex.LogError ();
			}

			return null;
		}

		public bool ContainsKey(string key)
		{
			return m_Images.ContainsKey(key);
		}

		public bool TryGetValue(string key, out TextureImage image)
		{
			if (String.IsNullOrEmpty(key)){
				image = null;
				return false;
			}
			return m_Images.TryGetValue(key, out image);
		}

		public bool AddImage(string key, string filePath)
		{					
			try {				
				TextureImage image = TextureImage.FromFile (filePath, Context, key, ImageSize);	
				return m_Images.TryAdd (key, image);
			} catch (Exception ex) {
				ex.LogError ();
				return false;
			}
		}			

		public bool AddImage(TextureImage image)
		{
			if (image == null || String.IsNullOrEmpty (image.Key)) {
				this.LogWarning ("Cannot add empty image or image with empty Key");
				return false;
			}
			return m_Images.TryAdd (image.Key, image);
		}

		public void AddOrReplaceImage(TextureImage image)
		{
			if (image == null || String.IsNullOrEmpty (image.Key)) {				
				return;
			}
			TextureImage img;
			if (m_Images.TryGetValueAndAddOrReplace (image.Key, image, out img) && ShouldDisposeImages)
				img.Dispose ();
		}			

		public TextureImage GetImage(string key)
		{
			TextureImage img;
			if (TryGetValue (key, out img))
				return img;			
			return MissingImage;
		}

		public TextureImage this[string key]
		{
			get {
				return GetImage(key);
			}
		}
			
		public IEnumerable<string> Keys
		{
			get {
				return m_Images.Keys;
			}
		}

		public IEnumerable<KeyValuePair<string, TextureImage>> Images
		{
			get {
				return m_Images;
			}
		}			

		public int Count
		{
			get {
				return m_Images.Count;
			}
		}

		public string GetKeyForIndex(int index)
		{
			return m_Images.Keys.Skip(index).FirstOrDefault();
		}

		public void Clear()
		{
			if (ShouldDisposeImages)
				m_Images.Values.DisposeListObjects();
			m_Images.Clear();
		}

		protected override void CleanupManagedResources()
		{
			Clear ();
			base.CleanupManagedResources();
		}
	}
}

