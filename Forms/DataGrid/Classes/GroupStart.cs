using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI.DataGrid
{
	public struct GroupStart : IEquatable<GroupStart>
	{
		public readonly int Level;
		public readonly int X;
		public readonly int YTop;
		public readonly int YBottom;

		public GroupStart(int level)
		{
			Level = level;
			X = 0;
			YTop = 0;
			YBottom = 0;
		}

		public GroupStart(int level, int x, int yTop, int yBottom)
		{
			Level = level;
			X = x;
			YTop = yTop;
			YBottom = yBottom;            
		}

		public override bool Equals (object obj)
		{
			return (obj is GroupStart) && Equals ((GroupStart)obj);
		}

		public bool Equals (GroupStart other)
		{
			return Level == other.Level && X == other.X && YTop == other.YTop && YBottom == other.YBottom;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return (Level + 1) ^ (YTop + YBottom + 31) ^ (X + 19999);
			}
		}
	}
}

