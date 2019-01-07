using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using KS.Foundation;

namespace SummerGUI.DataGrid
{
	public enum MouseControlItemTypes
	{
		Empty,
		PlusMinus,
		ColumnHeaderBorder,
		Tooltip,
		ColumnHeader
	}

	public struct MouseControlItem : ILayoutItem, IEquatable<MouseControlItem>
	{
		public static readonly MouseControlItem Empty = new MouseControlItem(RectangleF.Empty, MouseControlItemTypes.Empty, null);

		readonly RectangleF m_Bounds;
		public RectangleF Bounds 
		{ 
			get {
				return m_Bounds;
			}
		}

		public readonly MouseControlItemTypes ItemType;
		public readonly object Tag;

		public MouseControlItem(RectangleF bounds, MouseControlItemTypes itemType, object tag)
		{
			m_Bounds = bounds;
			ItemType = itemType;
			Tag = tag;
		}

		public ILayoutItem Clone()
		{
			return new MouseControlItem(Bounds, ItemType, Tag);
		}

		public override bool Equals (object obj)
		{
			return (obj is MouseControlItem) && Equals ((MouseControlItem)obj);
		}

		public bool Equals (MouseControlItem other)
		{
			return m_Bounds == other.Bounds && ItemType == other.ItemType && Tag == other.Tag;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return (Bounds.GetHashCode () + 17) ^ ((int)ItemType + 31) ^ (Tag.SafeHash () * 131);
			}
		}
	}		
}
