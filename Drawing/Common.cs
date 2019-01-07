using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using KS.Foundation;

namespace SummerGUI
{	
	public enum Docking
	{		
		None,
		Fill,
		Right,
		Left,
		Bottom,
		Top,
	}

	[Flags]
	public enum Alignment
	{
		None = 0,
		Near = 1,
		Center = 2,
		Far = 4,
		Baseline = 6
	}

	public struct Padding : IEquatable<Padding>
	{				
        public static readonly Padding Empty = new Padding(0, 0, 0, 0);

		public readonly float Left;
		public readonly float Top;
		public readonly float Right;
		public readonly float Bottom;

		public Padding(float all) : this (all, all, all, all) {}
		public Padding(float leftright, float topbottom) : this (leftright, topbottom, leftright, topbottom) {}
		public Padding(float left, float top, float right, float bottom)
		{			
			Left = left;
            Top = top;            
			Right = right;
            Bottom = bottom;            
		}			

		public float Width
		{
			get{
				return Math.Max(0, Left + Right);
			}
		}

		public float Height
		{
			get{
				return Math.Max(0, Top + Bottom);
			}
		}

		public int GetHashCode (Padding obj)
		{
			unchecked {
				return ((int)(obj.Left + 13 * obj.Width) ^ (int)(obj.Top + 127 * obj.Height));
			}
		}

		public override int GetHashCode ()
		{
			return GetHashCode(this);
		}

		public override bool Equals (object obj)
		{				
			return (obj is Padding) && Equals((Padding)obj);
		}	

		public bool Equals (Padding other)
		{				
			return Left == other.Left && Top == other.Top && Width == other.Width && Height == other.Height;
		}			

		public static bool operator == (Padding p1, Padding p2)
		{
			return p1.Equals (p2);
		}

		public static bool operator != (Padding p1, Padding p2)
		{
			return !p1.Equals (p2);
		}

		public override string ToString ()
		{
			return string.Format ("[Padding: Left={0}, Top={1}, Right={2}, Bottom={3}]", Left, Top, Right, Bottom);
		}
	}
}

