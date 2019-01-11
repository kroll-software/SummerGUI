using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{

	// This class provides the fastest and easiest way to use OpenGL
	// by compiling OpenGL-Calls into a "CallList".
	// OpenGL CallLists work like a 'Makro-Recorder'.

	public abstract class OpenGlListBase : IDisposable
	{
		public IGUIContext GFX { get; private set; }
		public int ListHandle { get; private set; }

		protected OpenGlListBase(IGUIContext gfx)
		{
			GFX = gfx;
		}

		const string UndisposedWarning = "MEMORY LEAKS in graphics adapter: Ensure to dispose SummerGUI.OpenGlListBase inherited objects!";

		~OpenGlListBase()
		{
			if (!IsDisposed) {
				IsDisposed.LogError (UndisposedWarning);
			}
		}

		public virtual void Flush()
		{
			// draw everything here
		}

		public void Compile()
		{
			CompileList (ListMode.Compile);
		}

		public void CompileAndCall()
		{
			CompileList (ListMode.CompileAndExecute);
		}

		void CompileList(ListMode mode)
		{
			if (!IsDisposed) {				
				if (ListHandle == 0)
					ListHandle = GL.GenLists (1);	 // Shouldn't GL.GenLists be uint ???
				GL.NewList (ListHandle, mode);
				Flush ();
				GL.EndList ();
			}
		}			

		public void CallList()
		{
			if (!IsDisposed && IsCompiled) {
				GL.CallList (ListHandle);
			}
		}

		public bool IsCompiled
		{
			get{ 
				return ListHandle != 0;
			}
		}

		public virtual void DeleteList()
		{
			if (IsCompiled) {
				GL.DeleteLists (ListHandle, 1);
				ListHandle = 0;
			}
		}
			
		public bool IsDisposed { get; private set; }
		public virtual void Dispose()
		{			
			if (!IsDisposed) {
				IsDisposed = true;
				DeleteList ();
				GFX = null;
			}
			GC.SuppressFinalize (this);
		}
	}		

	public abstract class QueueDrawingBufferBase<T> : OpenGlListBase
	{	
		protected Queue<T> Queue { get; private set; }

		protected QueueDrawingBufferBase(IGUIContext gfx)
			: base(gfx)
		{		
			Queue = new Queue<T> ();	
		}

		public int Count
		{
			get{
				return Queue.Count;
			}
		}

		public virtual void OnClear()
		{
		}

		public void Clear()
		{
			OnClear ();
			Queue.Clear ();
		}
			
		public virtual void ClearAndDispose()
		{
			Clear();
			Dispose ();
		}

		public override void Dispose()
		{			
			if (!IsDisposed) {
				if (Count > 0) {
					Flush ();
				}
				base.Dispose ();
			}
		}
	}

	public abstract class StackDrawingBufferBase<T> : OpenGlListBase
	{	
		protected Stack<T> Stack { get; private set; }

		protected StackDrawingBufferBase(IGUIContext gfx)
			: base(gfx)
		{		
			Stack = new Stack<T> ();	
		}

		public int Count
		{
			get{
				return Stack.Count;
			}
		}

		public virtual void OnClear()
		{
		}

		public void Clear()
		{
			OnClear ();
			Stack.Clear ();
		}

		public virtual void ClearAndDispose()
		{
			Clear();
			Dispose ();
		}

		public override void Dispose()
		{			
			if (!IsDisposed) {
				if (Count > 0) {
					Flush ();
				}
				base.Dispose ();
			}
		}
	}

	// ********** Concrete Classes **************

	public abstract class DrawingBufferRowBase
	{		
	}

	// ********** LineDrawBuffer **************
	public class LineDrawingBufferRow : DrawingBufferRowBase
	{
		public Pen Pen { get; set; }
		public float X1 { get; set; }
		public float Y1 { get; set; }
		public float X2 { get; set; }
		public float Y2 { get; set; }
	}

	public class LineDrawingBuffer : QueueDrawingBufferBase<LineDrawingBufferRow>
	{	
		public LineDrawingBuffer(IGUIContext ctx) : this(ctx, LineStyles.Solid) {}
		public LineDrawingBuffer(IGUIContext ctx, LineStyles style) : base(ctx) 
		{
			Style = style;
		}

		public float LineWidth { get; private set; }
		public LineStyles Style { get; private set; }

		public void AddLine (Pen pen, float x1, float y1, float x2, float y2)
		{	
			if (!IsDisposed) {
				LineWidth = pen.Width;
				Queue.Enqueue (new LineDrawingBufferRow {
					Pen = pen,
					X1 = x1,
					Y1 = y1,
					X2 = x2,
					Y2 = y2
				});
			}
		}

		public override void Flush ()
		{
			if (Count > 0) 
			{				
				using (new PaintWrapper (RenderingFlags.HighQuality)) 
				{					
					GL.LineWidth (LineWidth);
					//float hw = LineWidth / 2f;
					float hw = 0.5f;

					switch (Style) {
					case LineStyles.Solid:
						break;
					case LineStyles.Dotted:
						//GL.LineStipple (1, 0x0101);
						GL.Enable (EnableCap.LineStipple);
						GL.LineStipple (2, 0xAAAA);
						break;
					case LineStyles.Dashed:
						GL.Enable (EnableCap.LineStipple);
						GL.LineStipple (1, 0x00FF);
						break;
					case LineStyles.DashDot:
						GL.Enable (EnableCap.LineStipple);
						GL.LineStipple (1, 0x1C47);
						break;
					}


					GL.Begin (PrimitiveType.Lines);

					//foreach (LineDrawingBufferRow row in this) {												
					while (Count > 0) {
						LineDrawingBufferRow row = Queue.Dequeue ();
						if (row.Pen != null) {							
							GL.Color4 (row.Pen.Color);
							if (row.Y1 == row.Y2) {								
								GL.Vertex2 (row.X2, row.Y2 - hw);
								GL.Vertex2 (row.X1, row.Y1 - hw);							
							} else if (row.X1 == row.X2) {								
								GL.Vertex2 (row.X2 - hw, row.Y2);
								GL.Vertex2 (row.X1 - hw, row.Y1);
							}
						}
					}

					GL.End ();
					GL.Disable (EnableCap.LineStipple);
				}
			}
		}			
	}

	// ********** RectangleDrawBuffer **************
	public class RectangleDrawingBufferRow : DrawingBufferRowBase
	{
		public Brush Brush { get; set; }
		public float X1 { get; set; }
		public float Y1 { get; set; }
		public float X2 { get; set; }
		public float Y2 { get; set; }
		public bool CanPaint
		{
			get{
				return Brush != null && X2 > X1 && Y2 > Y1;
			}
		}
	}

	public class RectangleDrawingBuffer : QueueDrawingBufferBase<RectangleDrawingBufferRow>
	{			
		public RectangleDrawingBuffer(IGUIContext ctx) : base(ctx) 
		{
		}			

		public void AddRectangle (Brush brush, float x, float y, float width, float height)
		{	
			if (!IsDisposed) {				
				Queue.Enqueue (new RectangleDrawingBufferRow {
					Brush = brush,
					X1 = x,
					Y1 = y,
					X2 = x + width,
					Y2 = y + height
				});
			}
		}

		public void AddRectangle (Brush brush, System.Drawing.RectangleF rectangle)
		{	
			if (!IsDisposed) {				
				Queue.Enqueue (new RectangleDrawingBufferRow {
					Brush = brush,
					X1 = rectangle.X,
					Y1 = rectangle.Y,
					X2 = rectangle.Right,
					Y2 = rectangle.Bottom
				});
			}
		}

		public override void Flush ()
		{
			if (Count > 0) 
			{				
				using (new PaintWrapper (RenderingFlags.HighQuality)) 
				{					
					//GL.Begin (PrimitiveType.Quads);

					//foreach (LineDrawingBufferRow row in this) {												
					while (Count > 0) {
						RectangleDrawingBufferRow row = Queue.Dequeue ();
						if (row.CanPaint) {
							if (row.Brush as SolidBrush != null) {
								GL.Color4 (row.Brush.Color);
								GL.Rect (row.X1, row.Y1, row.X2, row.Y2);
							} else if (row.Brush as LinearGradientBrush != null) {
								var lgb = row.Brush as LinearGradientBrush;
								switch (lgb.Direction) 
								{
								case GradientDirections.Horizontal:
									GL.Color4 (lgb.Color);
									GL.Begin (PrimitiveType.Polygon);
									GL.Vertex2 (row.X1, row.Y2);
									GL.Color4 (lgb.GradientColor);
									GL.Vertex2 (row.X2, row.Y2);
									GL.Vertex2 (row.X2, row.Y1);
									GL.Color4 (lgb.Color);
									GL.Vertex2 (row.X1, row.Y1);
									GL.End ();
									break;

								case GradientDirections.Vertical:
									GL.Color4 (lgb.GradientColor);
									GL.Begin (PrimitiveType.Polygon);
									GL.Vertex2 (row.X1, row.Y2);
									GL.Vertex2 (row.X2, row.Y2);
									GL.Color4 (lgb.Color);
									GL.Vertex2 (row.X2, row.Y1);
									GL.Vertex2 (row.X1, row.Y1);
									GL.End ();
									break;

								case GradientDirections.TopLeft:
									GL.Begin (PrimitiveType.Polygon);
									GL.Color4 (lgb.Color);
									GL.Vertex2 (row.X1, row.Y2);
									GL.Vertex2 (row.X2, row.Y2);
									GL.Vertex2 (row.X2, row.Y1);
									GL.Color4 (lgb.GradientColor);
									GL.Vertex2 (row.X1, row.Y1);
									GL.End ();
									break;

								case GradientDirections.ForwardDiagonal:
									GL.Color4 (lgb.Color);
									GL.Begin (PrimitiveType.Polygon);
									GL.Vertex2 (row.X1, row.Y2);
									GL.Color4 (lgb.GradientColor);
									GL.Vertex2 (row.X2, row.Y2);
									GL.Vertex2 (row.X2, row.Y1);
									GL.Vertex2 (row.X1, row.Y1);
									GL.End ();	
									break;

								case GradientDirections.BackwardDiagonal:
									GL.Color4 (lgb.GradientColor);
									GL.Begin (PrimitiveType.Polygon);
									GL.Vertex2 (row.X1, row.Y2);
									GL.Vertex2 (row.X2, row.Y2);
									GL.Vertex2 (row.X2, row.Y1);
									GL.Color4 (lgb.Color);
									GL.Vertex2 (row.X1, row.Y1);
									GL.End ();
									break;							
								}
							} else if (row.Brush as HatchBrush != null) {
								// ToDo:
								GL.Color4 (row.Brush.Color);
								GL.Rect (row.X1, row.X2, row.X2, row.Y2);
							}								
						}
					}

					//GL.End ();
				}
			}
		}			
	}

	// ********** StringDrawBuffer **************
	public class StringDrawingBufferRow : DrawingBufferRowBase
	{
		public IGUIFont Font  { get; set; }
		public Brush Brush  { get; set; }
		public String Text  { get; set; }
		public System.Drawing.RectangleF Rect { get; set; }
	}		

	public class StringDrawingBuffer : QueueDrawingBufferBase<StringDrawingBufferRow>
	{	
		public StringDrawingBuffer(IGUIContext ctx) : base(ctx) 
		{
		}

		public void AddString (string text, IGUIFont font, Brush brush, System.Drawing.Rectangle rect, System.Drawing.StringFormat sf)
		{
			AddString (text, font, brush, rect, FontFormat.DefaultSingleLine);
		}

		public void AddString (string text, IGUIFont font, Brush brush, System.Drawing.Rectangle rect, FontFormat sf)
		{	
			//System.Drawing.SizeF contentSize = font.Measure( text, (float)rect.Width, FontFormat.DefaultSingleLine);
			//System.Drawing.RectangleF rContent = new System.Drawing.RectangleF (0, 0, contentSize.Width, contentSize.Height);
			//System.Drawing.PointF presult = BoxAlignment.AlignBoxes (rContent, rect, sf);

			Queue.Enqueue (new StringDrawingBufferRow {
				Font = font,
				Brush = brush,
				Text = text,
				Rect = rect,
			});
		}

		public override void Flush ()
		{
			if (Count > 0) 
			{
				using (new PaintWrapper (RenderingFlags.HighQuality)) 
				{
					IGUIFont lastInitializedFont = null;

					//GL.Scale ((float)GFX.OriginalWidth / GFX.Width, (float)GFX.OriginalHeight / GFX.Height, 1);

					//foreach (StringDrawingBufferRow row in this) {						
					while (Count > 0) {
						StringDrawingBufferRow row = Queue.Dequeue ();
						if (row.Font != null) {	
							if (row.Font != lastInitializedFont) {
								if (lastInitializedFont != null)
									lastInitializedFont.End ();

								row.Font.Begin (GFX);
								lastInitializedFont = row.Font;
							}

							/***
							GL.PushMatrix();
							GL.Translate(row.Rect.Left, row.Rect.Top, 0);
							row.Font.Print(row.Text, row.Rect, FontFormat.DefaultSingleLine);
							GL.PopMatrix();
							***/

							row.Font.Print(row.Text, row.Rect, FontFormat.DefaultSingleLine);
						}
					}

					if (lastInitializedFont != null)
						lastInitializedFont.End ();					
				}
			}
		}	
	}

	public class PolygonDrawingBuffer : QueueDrawingBufferBase<System.Drawing.PointF>
	{					
		public PolygonDrawingBuffer(IGUIContext gfx, System.Drawing.Color color)
			: base(gfx)
		{
			Color = color;
		}
			
		public System.Drawing.Color Color { get; private set; }

		public void AddPoint (float x, float y)
		{	
			AddPoint(new System.Drawing.PointF(x, y));
		}
			
		System.Drawing.PointF lastPoint;

		public void AddPoint (System.Drawing.PointF point)
		{	
			if (!IsDisposed) {				
				if (point != lastPoint) {
					Queue.Enqueue(point);
					lastPoint = point;
				}
			}
		}			

		public override void Flush ()
		{
			if (Count > 0) 
			{							
				using (new PaintWrapper (RenderingFlags.HighQuality)) 
				{	
					GL.Color4 (Color);
					//GL.Begin (PrimitiveType.Polygon);
					GL.Begin (PrimitiveType.LineLoop);
					while (Queue.Count > 0) {						
						var p = Queue.Dequeue();
						GL.Vertex2 (p.X, p.Y);
					}
					GL.End ();
				}
			}
		}
	}

}

