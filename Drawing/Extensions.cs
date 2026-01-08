using System;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Text;
using System.Globalization;
using KS.Foundation;

namespace SummerGUI
{
	public static class Extensions
	{	
		public static float FloatEpsilon = 0.0000015f;

		public static string ToAnsiString(this IntPtr ptr)
		{
			if (ptr == IntPtr.Zero)
				return String.Empty;				
			return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptr);
		}

		public static char ParseMnemonic(this string s)
		{
			if (s != null) {
				int index = s.IndexOf ('&');
				if (index >= 0 && index < s.Length - 2)
					return char.ToUpperInvariant(s [index + 1]);					
			}
			return (char)0;
		}

		public static string StripMnemonics(this string val)
		{
			if (String.IsNullOrEmpty(val))
				return String.Empty;
			return val.Replace("&", String.Empty);
		}

		public static Point ToPoint(this PointF p)
		{
			return new Point ((int)p.X, (int)p.Y);
		}

		public static PointF ToPointF(this Point p)
		{
			return (PointF)p;
		}

		public static double Distance (this Point p, Point other)
		{
			return Math.Sqrt((p.X - other.X).Power2() + (p.Y - other.Y).Power2());
		}

		public static double Distance (this PointF p, PointF other)
		{
			return Math.Sqrt((p.X - other.X).Power2() + (p.Y - other.Y).Power2());
		}

		public static double Power2 (this double d)
		{
			return d * d;
		}

		public static double Power2 (this float d)
		{
			return d * d;
		}

		public static double Power2 (this int d)
		{
			return (double)(d * d);
		}

		public static int NextPowerOf2 (this int n)
		{				
			n--;
			n |= n >> 1;
			n |= n >> 2;
			n |= n >> 4;
			n |= n >> 8;
			n |= n >> 16;
			n++;
			return n;

			/***
			if ((n & (n-1)) == 0)
				return (n);
			while ((n & (n-1)) > 0)
				n = n & (n-1);
			return n << 1;
			***/
		}

		public static Padding Scale(this Padding val, float scaling)
		{			
			return new Padding(
				val.Left.Scale(scaling),
				val.Top.Scale(scaling),
				val.Right.Scale(scaling),
				val.Bottom.Scale(scaling)
			);
		}

		public static Size Scale(this Size val, float scaling)
		{			
			return new Size(
				val.Width.Scale(scaling),
				val.Height.Scale(scaling)
			);
		}

		public static SizeF Scale(this SizeF val, float scaling)
		{			
			return new SizeF(
				val.Width.Scale(scaling),
				val.Height.Scale(scaling)
			);
		}

		public static double Scale(this double val, float scaling)
		{			
			return val * scaling;
		}

		public static int Scale(this int val, float scaling)
		{			
			if (val == Int32.MaxValue)
				return val;
			return (int)(val * scaling + 0.5);
		}

		public static long Scale(this long val, float scaling)
		{		
			if (val == Int64.MaxValue)
				return val;	
			return (long)(val * scaling + 0.5);
		}

		public static float Scale(this float val, float scaling)
		{			
			return (val * scaling);
		}			
			
		public static int Ceil(this float val)
		{			
			return (int)Math.Ceiling(val);
		}

		public static int Floor(this float val)
		{			
			return (int)Math.Floor(val);
		}

		public static int Ceil(this double val)
		{			
			return (int)Math.Ceiling(val);
		}

		public static int Floor(this double val)
		{			
			return (int)Math.Floor(val);
		}
			
		public static Size Ceil(this SizeF val)
		{
			// about 30% Faster than the Dotnet Version..
			return new Size(val.Width.Ceil(), val.Height.Ceil());
		}

		public static Size Floor(this SizeF val)
		{
			// about 30% Faster than the Dotnet Version..
			return new Size(val.Width.Floor(), val.Height.Floor());
		}

		public static Rectangle Ceil(this RectangleF val)
		{			
			return Rectangle.Ceiling(val);
		}

		public static Rectangle Floor(this RectangleF val)
		{			
			return new Rectangle(val.X.Floor(), val.Y.Floor(), val.Width.Floor(), val.Height.Floor());
		}

		public static Point Add(this Point left, Point right)
		{
			return new Point (left.X + right.X, left.Y + right.Y);
		}

		public static PointF Add(this PointF left, PointF right)
		{
			return new PointF (left.X + right.X, left.Y + right.Y);
		}

		public static PointF Add(this PointF left, float x, float y)
		{
			return new PointF (left.X + x, left.Y + y);
		}

		public static Color ToGray(this Color color)
		{
			int gray = (int)(color.R * 0.3f + color.G * 0.59f + color.B * 0.11f);
			return Color.FromArgb (color.A, gray, gray, gray);
		}			

		/*** ***/
		public static SizeF Inflate(this SizeF sz, Padding margin)
		{
			return new SizeF (sz.Width + margin.Width, sz.Height + margin.Height);
		}

		public static RectangleF Inflate(this RectangleF rec, Padding margin)
		{
			return new RectangleF (rec.Left + margin.Left, rec.Top + margin.Top, rec.Width - margin.Width, rec.Height - margin.Height);
		}


		public static SizeF MaxSize(this SizeF s1, SizeF s2)
		{
			return new SizeF (Math.Max(s1.Width, s2.Width), Math.Max(s1.Height, s2.Height));
		}

		public static double Lerp(this double min, double max, double fraction) 
		{
			return (1 - fraction) * min + fraction * max;
		}			

		public static PointF Lerp(this PointF min, PointF max, double fraction) 
		{
			return new PointF((float)Lerp(min.X, max.X, fraction), (float)Lerp(min.Y, max.Y, fraction));
		}

		public static Color Lerp(this Color min, Color max, double fraction) 
		{
			return Color.FromArgb((int)Lerp(min.A, max.A, fraction), (int)Lerp(min.R, max.R, fraction), (int)Lerp(min.G, max.G, fraction), (int)Lerp(min.B, max.B, fraction));
		}

		public static int Clamp(this int value, int min, int max) {
			if (value > max)
				value = max;			
			if (value < min)
				value = min;			
			return value;
		}

		public static float Clamp(this float value, float min, float max) {            
			if (value > max)
				value = max;			
			if (value < min)
				value = min;			
			return value;
		}

		public static double Clamp(this double value, double min, double max) {
			if (value > max)
				value = max;			
			if (value < min)
				value = min;			
			return value;
		}

		public static decimal Clamp(this decimal value, decimal min, decimal max) {
			if (value > max)
				value = max;			
			if (value < min)
				value = min;			
			return value;
		}
			
		public static Rectangle Combine(this Rectangle rect, Rectangle other)
		{
			if (other.Width <= 0 || other.Height <= 0)
				return rect;

			if (rect.Width <= 0 || rect.Height <= 0)
				return other;

			return new Rectangle (Math.Min(rect.X, other.X), Math.Min(rect.Y, other.Y), Math.Max(rect.Width, other.Width), Math.Max(rect.Height, other.Height));
		}

		public static RectangleF Combine(this RectangleF rect, RectangleF other)
		{
			return new RectangleF (Math.Min(rect.X, other.X), Math.Min(rect.Y, other.Y), Math.Max(rect.Width, other.Width), Math.Max(rect.Height, other.Height));
		}

		public static bool IsWeekend(this DateTime date)
		{
			DayOfWeek dow = date.DayOfWeek;
			return dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday;
		}

		public static string FormatForDisplay(this string s)
		{
			if (String.IsNullOrWhiteSpace (s))
				return String.Empty;
			
			StringBuilder sb = new StringBuilder (s.Length + 8);

			bool flag = true;
			foreach (char c in s) {
				if (c == Char.ToUpper (c) || c.IsNumeric ()) {
					if (!flag)
						sb.Append (' ');
					flag = true;
				} else
					flag = false;
				
				sb.Append (c);
			}

			return sb.ToString ();
		}

		public static string StripTrailingZeros(this string s)
		{
			if (String.IsNullOrEmpty (s))
				return String.Empty;

			char sep = Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
			int comma = s.LastIndexOf (sep) + 1;
			if (comma == 0)
				return s;

			int len = s.Length - 1;
			int j = len;
			while (j > comma && s [j] == '0')
				j--;

			if (j == len)
				return s;

			if (j <= 0)				
				return "0";

			return s.Substring (0, j + 1);

			/**
			while (s.Length > 0 && s [s.Length - 1] == '0')
				s = s.Substring (0, s.Length - 1);

			return s;
			**/
		}

		// Correct System.Threading.Timer Suspend / Resume

		public static void Suspend(this Timer timer)
		{			
			if (timer != null)
				timer.Change (Timeout.Infinite, Timeout.Infinite);
		}

		public static void Resume(this Timer timer, int dueTime)
		{	
			if (timer != null)
				timer.Change (dueTime, Timeout.Infinite);
		}

		public static void Resume(this Timer timer, int dueTime, int period)
		{		
			if (timer != null)	
				timer.Change (dueTime, period);
		}			
	}
}

