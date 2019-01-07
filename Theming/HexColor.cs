using System;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI.Theming
{
	public static class HexColor
	{		
		public static Color String2Color(string strColor, Color DefaultColor)
		{
			Color returnValue;
			if (Strings.IsNumeric(strColor))
			{
				returnValue = Long2Color(System.Convert.ToInt32(strColor), DefaultColor);
			}
			else
			{
				returnValue = Hex2Color(strColor, DefaultColor);
			}
			return returnValue;
		}
		
		public static string Color2Hex(int lColor)
		{
			string returnValue;
			int R = 0;
			int G = 0;
			int B = 0;
			
			//On Error Resume Next  - Cannot Convert to CSharp
			ExtractRGB(lColor, ref R, ref G, ref B);
			//returnValue = "#" + LeadingZero(Conversion.Hex(R)) + LeadingZero(Conversion.Hex(G)) + LeadingZero(Conversion.Hex(B));
			returnValue = "#" + LeadingZero(System.Convert.ToString(R, 16)) 
				+ LeadingZero(System.Convert.ToString(G, 16))
				+ LeadingZero(System.Convert.ToString(B, 16));

			return returnValue;
		}
		
		public static string Color2Hex(int R, int G, int B)
		{
			string returnValue;
			try
			{
				returnValue = "#" + LeadingZero(System.Convert.ToString(R, 16)) 
					+ LeadingZero(System.Convert.ToString(G, 16))
					+ LeadingZero(System.Convert.ToString(B, 16));
			}
			catch (Exception)
			{
				returnValue = "";
			}
			return returnValue;
		}
		
		public static string Color2Hex(Color C)
		{
			string returnValue;
			try
			{
				returnValue = "#" + LeadingZero(System.Convert.ToString(C.R, 16)) 
					+ LeadingZero(System.Convert.ToString(C.G, 16))
					+ LeadingZero(System.Convert.ToString(C.B, 16));
			}
			catch (Exception)
			{
				returnValue = "";
			}
			return returnValue;
		}
		
		public static System.Drawing.Color Hex2Color(string strColor, System.Drawing.Color DefaultColor)
		{
			System.Drawing.Color returnValue;
			System.Drawing.Color C = Color.Black;
			if (strColor.Length!= 7)
			{
				returnValue = DefaultColor;
				return returnValue;
			}
			
			string R;
			string G;
			string B;
			
			try
			{
				R = Strings.StrMid(strColor, 2, 2);
				G = Strings.StrMid(strColor, 4, 2);
				B = Strings.StrMid(strColor, 6, 2);
				
				returnValue = Color.FromArgb(HexValue(R), HexValue(G), HexValue(B));
			}
			catch (Exception)
			{
				returnValue = DefaultColor;
			}
			return returnValue;
		}
		
		public static System.Drawing.Color Long2Color(int L, System.Drawing.Color DefaultColor)
		{
			System.Drawing.Color returnValue;
			int R = 0;
			int G = 0;
			int B = 0;
			
			try
			{
				ExtractRGB(L, ref R, ref G, ref B);
				returnValue = Color.FromArgb(R, G, B);
				
			}
			catch (Exception)
			{
				returnValue = DefaultColor;
			}
			return returnValue;
		}
		
		private static string LeadingZero(string S)
		{
			while (S.Length< 2)
			{
				S = "0" + S;
			}
			return S;
		}
		
		public static void ExtractRGB (int ColorVal, ref int R, ref int G, ref int B)
		{
			R = ColorVal & 0xFF;
			G = (ColorVal / 0x100) & 0xFF;
			B = (ColorVal / 0x10000) & 0xFF;
		}
		
		private static int SingleHexValue(string strHex)
		{
			int returnValue;
			returnValue = 0;
			if (strHex.Length!= 1)
			{
				return returnValue;
			}
			if (Strings.IsNumeric(strHex))
			{
				returnValue = System.Convert.ToInt32(strHex);
			}
			else
			{
				switch (strHex.ToUpper())
				{
					case "A":
						
						returnValue = 10;
						break;
					case "B":
						
						returnValue = 11;
						break;
					case "C":
						
						returnValue = 12;
						break;
					case "D":
						
						returnValue = 13;
						break;
					case "E":
						
						returnValue = 14;
						break;
					case "F":
						
						returnValue = 15;
						break;
					default:
						
						break;
				}
			}
			return returnValue;
		}
		
		private static int HexValue(string strHex)
		{
			if (strHex.Length!= 2) return 0;
			return (SingleHexValue(Strings.StrLeft(strHex, 1)) * 16) + SingleHexValue(Strings.StrRight(strHex, 1));
		}
	}
	
}
