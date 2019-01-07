using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KS.Foundation;

namespace SummerGUI
{
	public struct CellInfo : IEquatable<CellInfo>, IComparable<CellInfo>, IComparable<RowInfo>
	{
		public static readonly CellInfo Empty = new CellInfo(-1, 0);

		public readonly int Row;
		public readonly int Column;

		public CellInfo(int row, int col)
		{
			Row = row;
			Column = col;
		}

		public int CompareTo(CellInfo other)
		{
			int ret = Row.CompareTo (other.Row);
			if (ret == 0)
				return Column.CompareTo (other.Column);
			return ret;
		}

		public int CompareTo(RowInfo other)
		{
			return Row.CompareTo (other.RowIndex);
		}

		public override int GetHashCode ()
		{		
			unchecked {
				return (Row * Column).CombineHash (Row + Column);
			}
		}

		public override bool Equals(object obj)
		{			
			return (obj is CellInfo) && Equals((CellInfo)obj);
		}

		public bool Equals(CellInfo other)
		{
			return Row == other.Row && Column == other.Column;
		}

		public static bool operator ==(CellInfo c1, CellInfo c2)
		{
			return c1.Equals(c2);
		}

		public static bool operator !=(CellInfo c1, CellInfo c2)
		{
			return !c1.Equals(c2);
		}

		public override string ToString ()
		{
			return String.Format ("Row:{0}, Col:{1}", Row, Column);
		}
	}

	public struct RowInfo : IEquatable<RowInfo>, IComparable<RowInfo>
	{
		public static readonly RowInfo Empty = new RowInfo();

		public readonly int RowIndex;
		public readonly float RowTop;
		public readonly float RowBottom;

		public float RowHeight
		{
			get
			{
				return RowBottom - RowTop;
			}
		}

		public RowInfo(int rowIndex, float rowTop, float rowBottom)
		{
			RowIndex = rowIndex;
			RowTop = rowTop;
			RowBottom = rowBottom;
		}			

		public override bool Equals(object obj)
		{
			return (obj is RowInfo) && Equals((RowInfo)obj);
		}

		public bool Equals(RowInfo other)
		{			
			return RowIndex == other.RowIndex && Math.Abs (RowTop - other.RowTop) < Extensions.FloatEpsilon && Math.Abs (RowBottom - other.RowBottom) < Extensions.FloatEpsilon;
		}

		public static bool operator ==(RowInfo r1, RowInfo r2)
		{
			return r1.Equals(r2);
		}

		public static bool operator !=(RowInfo r1, RowInfo r2)
		{
			return !r1.Equals(r2);
		}

		public override int GetHashCode()
		{
			return RowIndex.CombineHash (RowTop.GetHashCode(), RowBottom.GetHashCode());
		}

		public int CompareTo(RowInfo other)
		{
			return RowIndex.CompareTo(other.RowIndex);
		}

		public override string ToString ()
		{
			return string.Format ("[RowInfo: RowIndex={0}, RowTop={1}, RowHeight={2}]", RowIndex, RowTop, RowHeight);
		}
	}


	public struct ColumnInfo : IEquatable<ColumnInfo>, IComparable<ColumnInfo>
	{
		public static readonly RowInfo Empty = new RowInfo();

		public readonly int Index;
		public readonly int CollIndex;
		public readonly float X;
		public readonly float Width;

		public ColumnInfo(int index, int collIndex, float x, float width)
		{
			Index = index;
			CollIndex = collIndex;
			X = x;
			Width = width;
		}        

		public int CompareTo(ColumnInfo other)
		{
			return X.CompareTo (other.X);
		}

		public override int GetHashCode ()
		{		
			return CollIndex;
		}

		public override bool Equals(object obj)
		{			
			return (obj is ColumnInfo) && Equals((ColumnInfo)obj);
		}

		public bool Equals(ColumnInfo other)
		{
			return CollIndex == other.CollIndex;
		}

		public static bool operator ==(ColumnInfo c1, ColumnInfo c2)
		{
			return c1.Equals(c2);
		}

		public static bool operator !=(ColumnInfo c1, ColumnInfo c2)
		{
			return !c1.Equals(c2);
		}

		public override string ToString ()
		{
			return String.Format ("Index:{0}, CollIndex:{1}, X:{2}", Index, CollIndex, X);
		}
	}		
}
