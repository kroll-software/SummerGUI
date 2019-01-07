using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using KS.Foundation;

namespace SummerGUI
{
	// Some device attributes, 
	// automatically respected on device change

	[AttributeUsage(AttributeTargets.Interface)]
	public class VisibleOnDesktopAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Interface)]
	public class VisibleOnTabletAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Interface)]
	public class VisibleOnMobileAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Interface)]
	public class HiddenOnDesktopAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Interface)]
	public class HiddenOnTabletAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Interface)]
	public class HiddenOnMobileAttribute : Attribute {}


	[AttributeUsage(AttributeTargets.Interface)]
	public class GuiMenuInterfaceAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface)]
	public class GuiMenuItemAttribute : Attribute 
	{
		public string Title { get; set; }
		public string Group { get; set; }
		public string Parent { get; set; }
		public GuiMenuItemAttribute()
		{
		}
	}
}

