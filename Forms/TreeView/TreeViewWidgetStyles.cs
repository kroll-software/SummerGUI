using System.Drawing;

namespace SummerGUI
{
	/// <summary>
	/// Default TreeView appearance (light Solarized-style palette).
	/// Override <see cref="WidgetStyle.InitStyle"/> in a derived class to restyle.
	/// </summary>
	public class TreeViewStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base3);
			SetForeColor (Theme.Colors.Base02);

			SetBorderColor (Theme.Colors.Base02);
			Border = 1f;
		}
	}

	/// <summary>
	/// Selection highlight for the active (focused) TreeView.
	/// </summary>
	public class TreeViewSelectedStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.HighLightBlue);
			SetForeColor (Theme.Colors.White);
		}
	}

	/// <summary>
	/// Selection highlight for an unfocused (inactive) TreeView.
	/// </summary>
	public class TreeViewSelectedStyleInactive : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.HighLightBlue.ToGray ());
			SetForeColor (Theme.Colors.White);
			SetBorderColor (Color.Empty);
		}
	}

	/// <summary>
	/// Glyph color (folder / file / +/-) used on top of the selection highlight.
	/// </summary>
	public class TreeItemSelectedGlyphStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base02);
			SetBorderColor (Color.Empty);
		}
	}
}
