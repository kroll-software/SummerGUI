using System;
using OpenTK;
using System.Linq;
using KS.Foundation;

namespace SummerGUI
{
	public struct StraightLine : IEquatable<StraightLine>
	{
		public readonly Vector3d Point;
		public  readonly Vector3d Direction;

		public StraightLine(Vector3d point, Vector3d direction)
		{
            Point = point;
            Direction = direction;
		}

		public override int GetHashCode ()
		{
			return Point.GetHashCode () ^ Direction.GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			return (obj is StraightLine) && Equals ((StraightLine)obj);
		}

		public bool Equals (StraightLine line)
		{
			return Point.Equals(line.Point) && Direction.Equals(line.Direction);
		}
	}

	public class BoundingBox
	{
		public readonly object SyncObject = new object ();

		double XMin;
		double XMax;
		double YMin;
		double YMax;
		double ZMin;
		double ZMax;

		double SumX;
		double SumY;
		double SumZ;

		public int Count { get; private set; }
		public void Reset()
		{
			XMin = 0;
			XMax = 0;
			YMin = 0;
			YMax = 0;
			ZMin = 0;
			ZMax = 0;

			SumX = 0;
			SumY = 0;
			SumZ = 0;

			Count = 0;
			Update ();
		}

		public void Update ()
		{
			UpdateCenterPoint ();
			UpdateSchwerpunkt ();
			UpdateCenterAxis ();
		}

		public BoundingBox () : this(null) {}
		public BoundingBox (Vector3d[] points)
		{
			if (points != null) {
				foreach (Vector3d p in points)
					AddPoint (p);
			}
			Update ();
		}			

		public void CombineBox(BoundingBox box)
		{	
			if (box == null || box.Count < 1)
				return;
			
			if (box.XMin < XMin) XMin = box.XMin;
			if (box.XMax > XMax) XMax = box.XMax;

			if (box.YMin < YMin) YMin = box.YMin;
			if (box.YMax > YMax) YMax = box.YMax;

			if (box.ZMin < ZMin) ZMin = box.ZMin;
			if (box.ZMax > ZMax) ZMax = box.ZMax;

			SumX += box.SumX;
			SumY += box.SumY;
			SumZ += box.SumZ;

			Count += box.Count;
			Update ();
		}

		public void AddPoint(Vector3d p)
		{
			if (Count == 0) {
				XMin = XMax = p.X;
				YMin = YMax = p.Y;
				ZMin = ZMax = p.Z;
			} else {					
				if (p.X < XMin)
					XMin = p.X;
				if (p.X > XMax)
					XMax = p.X;
				if (p.Y < YMin)
					YMin = p.Y;
				if (p.Y > YMax)
					YMax = p.Y;
				if (p.Z < ZMin)
					ZMin = p.Z;
				if (p.Z > ZMax)
					ZMax = p.Z;
			}

			SumX += p.X;
			SumY += p.Y;
			SumZ += p.Z;

			Count++;
		}

		public Vector3d CenterPoint { get; private set; }

		private void UpdateCenterPoint()
		{
			CenterPoint = new Vector3d ((XMax + XMin) / 2d, (YMax + YMin) / 2d, (ZMax + ZMin) / 2d);
		}

		public Vector3d Schwerpunkt  { get; private set; }

		private void UpdateSchwerpunkt()
		{
			Schwerpunkt = new Vector3d (SumX / Count, SumY / Count, SumZ / Count);
		}

		public StraightLine CenterAxis  { get; private set; }

		private void UpdateCenterAxis()
		{
			Vector3d Diagonal = new Vector3d (XMax - XMin, YMax - YMin, ZMax - ZMin);
			Vector3d Opposite1, Opposite2;

			double max = MaxSize - 0.5;

			if (max < Diagonal.X) {
				// x-axis			
				Opposite1 = new Vector3d (0, YMin + ((YMax - YMin) / 2.0), 0);
				Opposite2 = new Vector3d (0, 0, ZMin + ((ZMax - ZMin) / 2.0));
			} else if (max < Diagonal.Y) {
				// y-axis
				Opposite1 = new Vector3d (XMin + ((XMax - XMin) / 2.0), 0, 0);
				Opposite2 = new Vector3d (0, 0, ZMin + ((ZMax - ZMin) / 2.0));
			} else {
				// z-axis
				Opposite1 = new Vector3d (XMin + ((XMax - XMin) / 2.0), 0, 0);
				Opposite2 = new Vector3d (0, YMin + ((YMax - YMin) / 2.0), 0);
			}

			CenterAxis = new StraightLine (Opposite1, Opposite2 - Opposite1);
		}

		public Vector3d Size
		{
			get{
				return new Vector3d (XMax - XMin, YMax - YMin, ZMax - ZMin);
			}
		}

		public double MaxSize
		{
			get{
				return Math.Max((XMax - XMin), Math.Max((YMax - YMin), (ZMax - ZMin)));
			}
		}

		public double MinSize
		{
			get{
				return Math.Min((XMax - XMin), Math.Min((YMax - YMin), (ZMax - ZMin)));
			}
		}

		public double CubicSize
		{
			get{
				return (XMax - XMin) * (YMax - YMin) * (ZMax - ZMin);
			}
		}

		public double AbsoluteSize
		{
			get{
				return (XMax - XMin) + (YMax - YMin) + (ZMax - ZMin);
			}
		}
	}		

	public static class CenterboxExtensions {
		public static double DistanceToStraightLine(this Vector3d p, StraightLine line) {
			return Vector3d.Cross (p - line.Point, line.Direction).Length / line.Direction.Length;
		}
	}
}

