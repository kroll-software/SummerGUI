using System;
using OpenTK;
using System.Linq;
using KS.Foundation;

namespace SummerGUI
{
	public class PunktWolke
	{
		public readonly object SyncObject = new object ();

		double SumX;
		double SumY;
		double SumZ;

		public int Count { get; private set; }
		public void Reset()
		{			
			Count = 0;
			SumX = 0;
			SumY = 0;
			SumZ = 0;
		}
			
		public PunktWolke () : this(null) {}
		public PunktWolke (Vector3d[] points)
		{
			if (points != null) {
				foreach (Vector3d p in points)
					AddPoint (p);
			}
		}

		public void CombineWolke(PunktWolke cloud)
		{	
			if (cloud == null || cloud.Count < 1)
				return;
			SumX += cloud.SumX;
			SumY += cloud.SumY;
			SumZ += cloud.SumZ;
			Count += cloud.Count;
		}

		public void AddPoint(Vector3d p)
		{
			SumX += p.X;
			SumY += p.Y;
			SumZ += p.Z;
			Count++;
		}

		public Vector3d CenterPoint
		{
			get{
				return new Vector3d (SumX / Count, SumY / Count, SumZ / Count);
			}
		}			
	}
}

