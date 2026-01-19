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

		public bool IsOnScreen(RectangleF rectangle)
		{						
			return !RecentClip.IsEmpty && rectangle.IntersectsWith(RecentClip);
		}

		public void SetClip(RectangleF bounds, bool bReset = false)
		{		
			// We only set the new clip when it is valid..
			// Otherwise we stay with the last recent clip			

			if (bounds.Width > 0 && bounds.Height > 0)
			{			
				RecentClip = bounds;
				ctx.Batcher.SetClip(this.ctx, bounds);

				//GL.Enable (EnableCap.ScissorTest);
				//GL.Scissor(rect.Left, ctx.Height - rect.Bottom, rect.Width + 1, rect.Height);				
			}			

			if (!bReset) {
				m_Stack.Push (RecentClip);
				ClipCount++;
			}
		}

		public void CombineClip(RectangleF bounds, bool bReset = false)
		{			
			if (RecentClip == RectangleF.Empty)
				RecentClip = new RectangleF (0, 0, ctx.Width, ctx.Height);

			bounds.Intersect (RecentClip);
			SetClip (bounds, bReset);
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
			ctx.Batcher.SetClip(this.ctx, RectangleF.Empty);
		}
	}		
}

