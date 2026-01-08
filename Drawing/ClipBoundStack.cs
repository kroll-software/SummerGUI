using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{
	/***
	public class ClipBoundClip : IDisposable
	{			
		public bool IsEmptyClip { get; private set; }

		public ClipBoundClip(IGUIContext ctx, RectangleF newClip, bool combine = true)
			: this(ctx)
		{			
			if (combine)
				ClipBoundStack.CombineClip (ctx, newClip);
			else
				ClipBoundStack.SetClip (ctx, newClip);
			IsEmptyClip = ClipBoundStack.IsEmptyClip;
		}
			
		public ClipBoundClip(IGUIContext ctx)
		{
			CTX = ctx;
		}

		~ClipBoundClip()
		{
			Dispose ();
		}

		private IGUIContext CTX;
		public void Dispose()
		{
			if (CTX != null) {
				ClipBoundStack.ResetClip (CTX);
				CTX = null;
			}
		}
	}
	***/

	public struct ClipBoundClip : IDisposable
	{			
		public readonly bool IsEmptyClip;

		public ClipBoundClip(IGUIContext ctx, RectangleF newClip, bool combine = true)
		{			
			CTX = ctx;
			if (combine)
				ctx.ClipBoundStack.CombineClip (newClip);
			else
				ctx.ClipBoundStack.SetClip (newClip);
			IsEmptyClip = ctx.ClipBoundStack.IsEmptyClip;
		}
			
		public override bool Equals (object obj)
		{
			return false;
		}

		public override int GetHashCode ()
		{
			return 0;
		}
			
		private IGUIContext CTX;
		public void Dispose()
		{
			if (CTX != null) {
				CTX.ClipBoundStack.ResetClip ();
				CTX = null;
			}
			GC.SuppressFinalize (this);
		}
	}


	public class ClipBoundStackClass
	{		
		readonly Stack<RectangleF> m_Stack;

		IGUIContext ctx;

		public ClipBoundStackClass(IGUIContext owner)
		{
			ctx = owner;
			m_Stack = new Stack<RectangleF> ();
		}

		~ClipBoundStackClass()
		{
			ctx = null;
		}

		private int ClipCount = 0;

		public int Count
		{
			get{
				return m_Stack.Count;
			}
		}

		public RectangleF RecentClip { get; private set; }

		public bool IsEmptyClip
		{
			get{
				return RecentClip.IsEmpty;
				//return RecentClip.Width <= 0 || RecentClip.Height <= 0;
			}
		}

		public bool IsOnScreen(Rectangle rectangle)
		{						
			RectangleF r = rectangle;
			return IsOnScreen(r);
		}

		public bool IsOnScreen(RectangleF rectangle)
		{						
			return !RecentClip.IsEmpty && rectangle.IntersectsWith(RecentClip);
		}

		/***
		static ClipBoundStack()
		{
			Instance = new Stack<RectangleF> ();
		}

		public static readonly Stack<RectangleF> Instance;
		***/


		public void SetClip(RectangleF r, bool bReset = false)
		{		
			// We only set the new clip when it is valid..
			// Otherwise we stay with the last recent clip

			Rectangle rect = new Rectangle((int)r.Left, (int)r.Top, r.Width.Ceil(), (int)(r.Height + 0.5f) + 1);

			if (rect.Width > 0 && rect.Height > 0)
			{
				GL.Enable (EnableCap.ScissorTest);
				GL.Scissor(rect.Left, ctx.Height - rect.Bottom, rect.Width + 1, rect.Height);
				RecentClip = rect;
			}

			if (!bReset) {
				m_Stack.Push (RecentClip);
				ClipCount++;
			}
		}

		public void CombineClip(RectangleF rect, bool bReset = false)
		{			
			if (RecentClip == Rectangle.Empty)
				RecentClip = new Rectangle (0, 0, ctx.Width, ctx.Height);

			rect.Intersect (RecentClip);
			SetClip (rect, bReset);
		}

		public void ResetClip()
		{						
			if (m_Stack.Count > 0) {
				m_Stack.Pop ();
				ClipCount--;
			}

			if (m_Stack.Count > 0)
				SetClip (m_Stack.Peek (), true);			
			else
				SetClip (RectangleF.Empty, true);
		}

		public void Clear()
		{
			m_Stack.Clear ();
			GL.Scissor(0, 0, ctx.Width, ctx.Height);
			GL.Disable (EnableCap.ScissorTest);
		}
	}		
}

