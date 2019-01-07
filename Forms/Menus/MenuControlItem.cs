using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using KS.Foundation;

namespace SummerGUI.Menus
{	
	public struct MenuControlItem : ILayoutItem, IEquatable<MenuControlItem>
	{
		public static readonly MenuControlItem Empty = new MenuControlItem(RectangleF.Empty, null, false);

		private RectangleF m_Bounds;
		public RectangleF Bounds 
		{ 
			get {
				return m_Bounds;
			}
		}
			
		public readonly bool ToolTip;
		public readonly IGuiMenuItem Item;

		public MenuControlItem(RectangleF bounds, IGuiMenuItem item, bool toolTip)
		{
			m_Bounds = bounds;
			ToolTip = toolTip;
			Item = item;
		}

		public ILayoutItem Clone()
		{
			return new MenuControlItem(this.Bounds, this.Item, ToolTip);
		}

		public override bool Equals (object obj)
		{
			return (obj is MenuControlItem) && Equals ((MenuControlItem)obj);
		}

		public bool Equals (MenuControlItem other)
		{
			return m_Bounds == other.Bounds && ToolTip == other.ToolTip && Item == other.Item;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return (Bounds.GetHashCode () + 17) ^ (ToolTip.SafeHash () + 1) ^ (Item.SafeHash () * 31);
			}
		}
	}		
}
