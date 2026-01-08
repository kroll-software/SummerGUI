using System;

namespace SummerGUI
{
	public enum CommonFontTags
	{
		Default,
		Status,
		MessageBox,
		Caption,
		Menu,
		Bold,
		Italics,
		Condensed,
		Small,
		Large,
		ExtraSmall,
		ExtraLarge,
		Mono,
		Roman,
		Serif,
		SmallIcons,
		MediumIcons,
		LargeIcons
	}
		
	[Flags]
	public enum GlyphFilterFlags
	{
		All = 0,
		OnDemand = 1,
		Punctation = 2,
		Numeric = 4,
		Alpha = 8,
		Ascii = 16,
		Lower = 32,
		Upper = 64,
		Symbol = 128
	}


	public class GUIFontConfiguration
	{		
		public GUIFontConfiguration()
		{			
			YOffset = 0;
			LineSpacing = 1f;
			ScaleFactor = 1f;
		}

		public GUIFontConfiguration(string tag, string path, float size, GlyphFilterFlags filter = GlyphFilterFlags.All)
			:this()
		{
			Tag = tag;
			Path = path;
			Size = size;
			Filter = filter;
		}

		public float ScaleFactor { get; set; }
		public string Tag { get; set; }
		public string FallbackTag { get; set; }

		public string Path { get; set; }
		public float Size { get; set; }
		public float YOffset { get; set; }
		public float LineSpacing { get; set; }

		public bool OnDemand
		{
			get{
				return Filter.HasFlag (GlyphFilterFlags.OnDemand);
			}
			set{				
				if (value)
					Filter |= GlyphFilterFlags.OnDemand;
				else 
					Filter &= ~GlyphFilterFlags.OnDemand;
			}
		}

		public GlyphFilterFlags Filter { get; set; } 
	}
}

