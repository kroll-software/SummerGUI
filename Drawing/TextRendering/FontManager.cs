using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Pfz.Collections;	// TreadSafeDictionary
using FreeTypeSharp;
using static FreeTypeSharp.FT;
using KS.Foundation;

namespace SummerGUI
{	
	public unsafe class FontManager : DisposableObject
	{
		public static float DPI = 96;
		
		// Korrekt: Den Pointer in der threadsafe Lazy-Instanz speichern
		private static readonly Lazy<IntPtr> s_lazyLibrary = new Lazy<IntPtr>(
			// Der Code, der nur einmal ausgeführt werden soll
			() => 
			{
				FT_Error error;
				// Lokale Pointer-Variable (muss innerhalb des unsafe-Blocks deklariert werden)
				FT_LibraryRec_* libPtr = null; 

				unsafe 
				{
					// FT_Init_FreeType erwartet einen Pointer auf den Pointer der Library (FT_Library**)
					// Wir müssen die Adresse der lokalen Variable libPtr übergeben.
					FT_LibraryRec_** libPtrAddress = &libPtr; 

					error = FT_Init_FreeType(libPtrAddress); 
				}

				if (error != FT_Error.FT_Err_Ok)
				{
					throw new InvalidOperationException("Failed to initialize FreeType library: " + error.ToString());
				}
				
				// Den nativen Pointer (FT_LibraryRec_*) in den verwalteten IntPtr umwandeln
				return (IntPtr)libPtr; 
			}, 
			System.Threading.LazyThreadSafetyMode.ExecutionAndPublication
		);

		// Statische Eigenschaft, die den initialisierten Pointer zurückgibt
		public static FT_LibraryRec_* Library
		{
			get {
				return (FT_LibraryRec_*)s_lazyLibrary.Value;
			}
		}

		public static FontManager Manager
		{
			get {
				return Singleton<FontManager>.Instance;
			}
		}
			
		public Dictionary<string, IGUIFont> Fonts { get; private set; }
		public Dictionary<string, GUIFontConfiguration> FontConfigs { get; private set; }		

		public FontManager()
		{			
			FontManager.DPI = 96;
			Fonts = new Dictionary<string, IGUIFont> ();
			FontConfigs = new Dictionary<string, GUIFontConfiguration> ();
			InitFontConfigs ();
		}

		public void AddFontConfig(GUIFontConfiguration config)
		{
			if (config == null || String.IsNullOrEmpty (config.Tag))
				this.LogWarning ("Font config or config.Tag must not be null.");			
			else if (FontConfigs.ContainsKey (config.Tag))
				this.LogWarning ("Duplicate font config.Tag: {0}", config.Tag);
			else
				FontConfigs.Add (config.Tag, config);
		}

		public void AddFontConfig(string tag, string path, float size, float yOffset = 0, string fallbackTag = "Default", GlyphFilterFlags filter = GlyphFilterFlags.All)
		{
			AddFontConfig(new GUIFontConfiguration {
				Tag = tag,
				FallbackTag = fallbackTag,
				Path = path,
				Size = size,
				YOffset = yOffset,
				Filter = filter,
			});
		}

		public void AddFontAlias(string aliasTag, string fallbackTag = "Default")
		{
			AddFontConfig(new GUIFontConfiguration {
				Tag = aliasTag,
				FallbackTag = fallbackTag,					
			});
		}

		public bool ContainsConfig(CommonFontTags tag)
		{
			return ContainsConfig(tag.ToString());
		}

		public bool ContainsConfig(string tag)
		{
			return FontConfigs.ContainsKey (tag);
		}

		public GUIFontConfiguration GetConfig(CommonFontTags tag)
		{
			return GetConfig(tag.ToString());
		}

		public GUIFontConfiguration GetConfig(string tag)
		{
			GUIFontConfiguration config;
			if (FontConfigs.TryGetValue (tag, out config))
				return config;
			return null;
		}

		public virtual void InitFontConfigs()
		{
			AddFontConfig (new GUIFontConfiguration {
				Tag = CommonFontTags.Default.ToString (),
				FallbackTag = null,
				Path = "Fonts/Roboto-Regular.ttf",
				Size = 10.25f,
			});
					
			AddFontConfig (new GUIFontConfiguration {
				Tag = CommonFontTags.Menu.ToString(),
				FallbackTag = CommonFontTags.Default.ToString(),
				Path = "Fonts/Antonio-Regular.ttf",
				Size = 12.25f,
				YOffset = 0.75f
			});

			AddFontAlias (CommonFontTags.Status.ToString(), CommonFontTags.Menu.ToString());
			AddFontAlias (CommonFontTags.MessageBox.ToString(), CommonFontTags.Menu.ToString());
			AddFontAlias (CommonFontTags.Caption.ToString(), CommonFontTags.Menu.ToString());

			AddFontConfig (new GUIFontConfiguration {
				Tag = CommonFontTags.Serif.ToString (),
				FallbackTag = CommonFontTags.Default.ToString(),
				Path = "Fonts/DroidSerif-Regular.ttf",
				Size = 11f,
				YOffset = 0f
			});
			
			AddFontConfig (new GUIFontConfiguration
				{
					Tag = CommonFontTags.Bold.ToString(),
					FallbackTag = CommonFontTags.Default.ToString(),
					Path = "Fonts/Roboto-Bold.ttf",
					Size = 11f,
				});

			AddFontConfig (new GUIFontConfiguration
				{
					Tag = CommonFontTags.Large.ToString(),
					FallbackTag = CommonFontTags.Default.ToString(),
					Path = "Fonts/Roboto-Bold.ttf",
					Size = 18f,
				});

			AddFontConfig (new GUIFontConfiguration
				{
					Tag = CommonFontTags.ExtraLarge.ToString(),
					FallbackTag = CommonFontTags.Default.ToString(),
					Path = "Fonts/Roboto-Bold.ttf",
					Size = 21f,
				});
			
			AddFontConfig (new GUIFontConfiguration
				{
					Tag = CommonFontTags.Small.ToString(),
					FallbackTag = CommonFontTags.Default.ToString(),
					Path = "Fonts/Roboto-Regular.ttf",
					Size = 8f,
				});
			
			AddFontConfig (new GUIFontConfiguration
				{
					Tag = CommonFontTags.ExtraSmall.ToString(),
					FallbackTag = CommonFontTags.Small.ToString(),
					Path = "Fonts/Roboto-Regular.ttf",
					Size = 8.25f,
				});

			AddFontConfig (new GUIFontConfiguration
				{
					Tag = CommonFontTags.Mono.ToString(),
					FallbackTag = CommonFontTags.Default.ToString(),
					Path = "Fonts/DejaVuSansMono.ttf",
					Size = 11.25f,
					//Size = 12f,
				});

			AddFontConfig (new GUIFontConfiguration
				{
					Tag = CommonFontTags.SmallIcons.ToString(),
					FallbackTag = null,
					Path = "Fonts/FontAwesome.otf",
					Size = 12f,
					YOffset = -0.25f,
					Filter = GlyphFilterFlags.OnDemand,
				});

			AddFontConfig (new GUIFontConfiguration
				{
					Tag = CommonFontTags.MediumIcons.ToString(),
					FallbackTag = CommonFontTags.SmallIcons.ToString(),
					Path = "Fonts/FontAwesome.otf",
					//Size = 22.5f,
					Size = 21f,
					YOffset = 0,
					Filter = GlyphFilterFlags.OnDemand,
				});

			AddFontConfig (new GUIFontConfiguration
				{
					Tag = CommonFontTags.LargeIcons.ToString(),
					FallbackTag = CommonFontTags.MediumIcons.ToString(),
					Path = "Fonts/FontAwesome.otf",
					Size = 42f,
					YOffset = 0,
					Filter = GlyphFilterFlags.OnDemand,
				});
		}			

		public IGUIFont FontByTag(CommonFontTags tag)
		{
			return FontByTag (tag.ToString ());
		}

		public IGUIFont FontByTag(string tag)
		{			
			IGUIFont font = null;
			while (font == null) {				
				if (String.IsNullOrEmpty(tag))
					tag = CommonFontTags.Default.ToString ();

				if (Fonts.TryGetValue (tag, out font)) {
					if (font == null) {
						GUIFontConfiguration config;
						if (FontConfigs.TryGetValue (tag, out config))
							tag = config.FallbackTag;
					}
				} else {
					GUIFontConfiguration config;
					if (FontConfigs.TryGetValue (tag, out config)) {
						try {
							InitFont (config);	
						} catch (Exception ex) {
							ex.LogError ();
							if (tag == CommonFontTags.Default.ToString())
								break;
							tag = null;
						}						
					} else {
						tag = null;
					}
				}
			}
				
			return font;
		}

		public IGUIFont this [string tag]
		{
			get {				
				return FontByTag (tag);
			}
		}

		public IGUIFont this [CommonFontTags tag]
		{
			get {				
				return FontByTag (tag);
			}
		}			

		public IGUIFont DefaultFont 
		{ 
			get {
				return this[CommonFontTags.Default];
			}
		}

		public IGUIFont BoldFont 
		{ 
			get {
				return this[CommonFontTags.Bold];
			}
		}

		public IGUIFont SmallFont 
		{ 
			get {
				return this[CommonFontTags.Small];
			}
		}

		public IGUIFont ExtraSmallFont
		{ 
			get {
				return this[CommonFontTags.ExtraSmall];
			}
		}

		public IGUIFont StatusFont 
		{ 
			get {
				return this[CommonFontTags.Status];
			}
		}

		public IGUIFont MessageBoxFont 
		{ 
			get {
				return this[CommonFontTags.MessageBox];
			}
		}

		public IGUIFont CaptionFont 
		{ 
			get {
				return this[CommonFontTags.Caption];
			}
		}

		public IGUIFont MenuFont 
		{ 
			get {
				return this[CommonFontTags.Menu];
			}
		}

		public IGUIFont MonoFont 
		{ 
			get {
				return this[CommonFontTags.Mono];
			}
		}

		public IGUIFont SerifFont 
		{ 
			get {
				return this[CommonFontTags.Serif];
			}
		}			

		public IGUIFont SmallIcons
		{ 
			get {
				return this[CommonFontTags.SmallIcons];
			}
		}

		public void ReScaleFonts(float ScaleFactor)
		{
			try {
				Fonts.Values.OfType<IGUIFont>().ForEach (f => f.Rescale(ScaleFactor));	
			} catch (Exception ex) {
				ex.LogError ("ReScaleFonts");
			}
		}

		public void InitFont(GUIFontConfiguration config)
		{
			if (config == null)
				throw new ArgumentNullException ("config");

			if (String.IsNullOrEmpty (config.Tag))
				throw new ArgumentException ("config.Tag must not be null or empty.");

			if (Fonts.ContainsKey (config.Tag)) {
				//this.LogWarning ("Font was already loaded: {0}", config.Tag);
				return;
			}

			//config.ScaleFactor = Owner.ScaleFactor;

			IGUIFont font = null;
			if (!String.IsNullOrEmpty (config.Path)) {
				font = FontManager.CreateFont (config);
				if (font == null)
					this.LogWarning ("Failed to load font: {0}", config.Tag);
			}

			// Add even when font is null, which means: loading failed
			Fonts.Add (config.Tag, font);
		}

		public void InitDefaultFont()
		{
			InitFont (GetConfig(CommonFontTags.Default));
		}

		public void InitStatusFont()
		{
			InitFont (GetConfig(CommonFontTags.Status));
		}

		public void InitBoldFont()
		{
			InitFont (GetConfig(CommonFontTags.Bold));
		}

		public void InitSmallFont()
		{
			InitFont (GetConfig(CommonFontTags.Small));
		}

		public void InitExtraSmallFont()
		{
			InitFont (GetConfig(CommonFontTags.ExtraSmall));
		}			

		public void InitMonoFont()
		{
			InitFont (GetConfig(CommonFontTags.Mono));
		}

		public void InitSerifFont()
		{
			InitFont (GetConfig(CommonFontTags.Serif));
		}

		public void InitSmallIcons()
		{
			InitFont (GetConfig(CommonFontTags.SmallIcons));
		}

		public void InitAllFonts()
		{
			foreach (GUIFontConfiguration config in FontConfigs.Values)
				InitFont (config);
		}			

		public static IGUIFont CreateFont(GUIFontConfiguration config)
		{			
			return CreateFont<GuiFont>(config);
		}

		public static IGUIFont CreateFont(Type type, GUIFontConfiguration config)
		{				
			try {
				return Activator.CreateInstance(type, config) as IGUIFont;
			} catch (Exception ex) {
				ex.LogError ();
				return null;
			}
		}

		public static IGUIFont CreateFont<T>(GUIFontConfiguration config) where T : IGUIFont
		{			
			try {
				return Activator.CreateInstance(typeof(T), config) as IGUIFont;
			} catch (Exception ex) {
				ex.LogError ();
				return null;
			}
		}

		// ************* Implementation / Forwards to selected IGUIFont

		public SizeF Measure(CommonFontTags fontTag, string text, int start = 0, int len = -1)
		{
			return Measure (fontTag.ToString(), text, start, len);
		}

		public SizeF Measure(string fontTag, string text, int start = 0, int len = -1)
		{
			IGUIFont font = FontByTag (fontTag);
			if (font == null)
				return SizeF.Empty;
			return font.Measure (text, start, len);
		}

		public SizeF Measure(CommonFontTags fontTag, string text, float width, FontFormat format)
		{
			return Measure (fontTag.ToString(), text, width, format);
		}

		public SizeF Measure(string fontTag, string text, float width, FontFormat format)
		{
			IGUIFont font = FontByTag (fontTag);
			if (font == null)
				return SizeF.Empty;
			return font.Measure (text, width, format);
		}

		public SizeF MeasureMnemonicString (CommonFontTags fontTag, string text)
		{
			return MeasureMnemonicString (fontTag.ToString(), text);
		}

		public SizeF MeasureMnemonicString (string fontTag, string text)
		{
			IGUIFont font = FontByTag (fontTag);
			if (font == null)
				return SizeF.Empty;
			return font.MeasureMnemonicString (text);
		}

		public SizeF Print(IGUIContext ctx, CommonFontTags fontTag, string text, RectangleF bounds, FontFormat format, Color color = default(Color))
		{
			return Print (ctx, fontTag.ToString (), text, bounds, format, color);
		}

		public SizeF Print(IGUIContext ctx, string fontTag, string text, RectangleF bounds, FontFormat format, Color color = default(Color))
		{
			IGUIFont font = FontByTag (fontTag);
			if (font == null)
				return SizeF.Empty;

			font.Begin ();
			try {
				return font.Print (text, bounds, format, color);	
			} catch (Exception ex) {
				ex.LogError ();
				return SizeF.Empty;
			}
			finally {
				font.End ();
			}
		}

		public SizeF PrintSelectedString (IGUIContext ctx, CommonFontTags fontTag, string text, int selStart, int selLength, RectangleF bounds, float offsetX, FontFormat format, Color foreColor, Color selectionBackColor, Color selectionForeColor)
		{
			return PrintSelectedString (ctx, fontTag.ToString (), text, selStart, selLength, 
				bounds, offsetX, format, foreColor, selectionBackColor, selectionForeColor);
		}

		public SizeF PrintSelectedString (IGUIContext ctx, string fontTag, string text, int selStart, int selLength, RectangleF bounds, float offsetX, FontFormat format, Color foreColor, Color selectionBackColor, Color selectionForeColor)
		{
			IGUIFont font = FontByTag (fontTag);
			if (font == null)
				return SizeF.Empty;
			font.Begin ();
			try {
				return font.PrintSelectedString (text, selStart, selLength, bounds, offsetX, format, foreColor, selectionBackColor, selectionForeColor);
			} catch (Exception ex) {
				ex.LogError ();
				return SizeF.Empty;
			}
			finally {
				font.End ();
			}
		}

		public void PrintTextLine (IGUIContext ctx, CommonFontTags fontTag, uint[] glyphs, RectangleF bounds, Color foreColor)
		{
			PrintTextLine (ctx, fontTag.ToString (), glyphs, bounds, foreColor);
		}

		public void PrintTextLine (IGUIContext ctx, string fontTag, uint[] glyphs, RectangleF bounds, Color foreColor)
		{
			IGUIFont font = FontByTag (fontTag);
			if (font == null)
				return;			
			font.Begin ();
			try {
				font.PrintTextLine (glyphs, bounds, foreColor);
			} catch (Exception ex) {
				ex.LogError ();
			}
			finally {
				font.End ();
			}
		}

		protected override void CleanupUnmanagedResources ()
		{			
			Fonts.Values.OfType<IGUIFont>().ForEach (f => f.Dispose());
			Fonts.Clear ();

			FT_LibraryRec_* libPtr = (FT_LibraryRec_*)s_lazyLibrary.Value;
			if (libPtr != null)
			{
				FT_Done_FreeType(libPtr);					
				// Wichtig: Wir können den Lazy-Value nicht auf null setzen, aber wir können ihn ignorieren.
				// Die Ressourcen sind freigegeben.
			}
			
			base.CleanupUnmanagedResources ();
		}
	}		
}

