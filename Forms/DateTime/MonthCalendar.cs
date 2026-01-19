using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;


using KS.Foundation;

namespace SummerGUI
{
	public class MonthCalendarStyle : WidgetStyle
	{		
		public override void InitStyle ()
		{
			SetBackColor (SystemColors.Window);
			SetForeColor (Theme.Colors.Base02);
			SetBorderColor (Color.Empty);
		}			
	}

	public class MonthCalendar : Container
	{		
		public static Color DayHoverBackColor = Color.FromArgb(86, Theme.Colors.Base00);
		public static Color TodayBackColor = Color.FromArgb(158, Theme.Colors.Orange);
		public static Color CurrentDayBackColor = Color.FromArgb(168, Theme.Colors.Cyan);

	
		public event EventHandler<EventArgs> SelectionChanged;
		public void OnSelectionChanged()
		{
			if (SelectionChanged != null)
				SelectionChanged (this, EventArgs.Empty);
		}

		private DateTime[] m_MonthView { get; set; }
		private string[] m_Weekdays { get; set; }

		public IGUIFont DayFont { get; set; }
		public IGUIFont TitleFont { get; set; }
		public IGUIFont IconFont { get; set; }

		FontFormat Format;
		TaskTimer Timer;

		public MonthCalendar (string name, IGUIFont dayFont, IGUIFont titleFont)
			: base (name, Docking.Fill, new MonthCalendarStyle())
		{
			m_Weekdays = new string[7];
			for (int i = 0; i < 7; i++)
				m_Weekdays [i] = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName ((DayOfWeek)i);			

			DayFont = dayFont;
			TitleFont = titleFont;
			this.SetIconFontByTag(CommonFontTags.SmallIcons);

			Format = new FontFormat (Alignment.Center, Alignment.Center, FontFormatFlags.Elipsis);

			this.Padding = new Padding (8);
			Timer = new TaskTimer (150, TimerAction, 500);

			MinCircleRadius = DayFont.Measure ("0").Height * 0.55f;
			MaxCircleRadius = MinCircleRadius * 1.5f;

			ShowDate ();
		}			
			
		private DateTime m_CurrentDate;
		public DateTime CurrentDate
		{
			get{
				return m_CurrentDate;
			}
			set{
				if (m_CurrentDate != value.Date)
					ShowDate (value);
			}
		}

		public void ShowDate()
		{
			ShowDate (DateTime.Now);
		}

		public void ShowDate(DateTime date)
		{
			if (date == DateTime.MinValue)
				date = DateTime.Now;

			DateTime[] mv = new DateTime[42];

			DateTime firstDayOfMonth = new DateTime (date.Year, date.Month, 1);
			DayOfWeek dowStart = firstDayOfMonth.DayOfWeek;

			// fill prev days
			for (int i = 0; i < 42; i++)
				mv [i] = firstDayOfMonth.AddDays (i - (int)dowStart);

			m_MonthView = mv;
			m_CurrentDate = date.Date;

			OnSelectionChanged ();
		}

		protected DateTime HoverDate;

		[DpiScalable]
		public float MinCircleRadius { get; set; }

		[DpiScalable]
		public float MaxCircleRadius { get; set; }

		protected virtual DateTime MouseToDate(float x, float y)
		{
			RectangleF r = new RectangleF (x, y, 1, 1);
			if (!r.IntersectsWith (Bounds))
				return DateTime.MinValue;

			szDay = new SizeF ((Bounds.Width - Padding.Width) / 7f, (Bounds.Height - Padding.Height) / 8f);
			if (szDay.Width <= 0 || szDay.Height <= 0)
				return DateTime.MinValue;

			int col = (int)((r.X - Bounds.Left - Padding.Left) / szDay.Width);
			int row = (int)((r.Y - Bounds.Top - Padding.Top) / szDay.Height) - 2;

			if (col < 0 || col > 6 || row < 0 || row > 6)
				return DateTime.MinValue;

			int idx = (row * 7) + col;
			if (idx < 0 || idx >= m_MonthView.Length)
				return DateTime.MinValue;

			return m_MonthView[idx];
		}

		bool prevArrowHover;
		bool nextArrowHover;

		private bool CheckArrows(float x, float y)
		{
			RectangleF pt = new RectangleF (x, y, 1, 1);
			prevArrowHover = prevRec.IntersectsWith (pt);
			nextArrowHover = nextRec.IntersectsWith (pt);
			if (prevArrowHover || nextArrowHover) {
				HoverDate = DateTime.MinValue;
				Invalidate ();
				return true;
			}
			return false;
		}

		public override void OnMouseMove (MouseMoveEventArgs e)
		{
			base.OnMouseMove (e);

			if (CheckArrows (e.X, e.Y))
				return;

			DateTime dthover = MouseToDate(e.X, e.Y);
			if (dthover != HoverDate)
			{
				HoverDate = dthover;
				Invalidate ();
			}
		}

		public override void OnMouseLeave (IGUIContext ctx)
		{
			base.OnMouseLeave (ctx);
			if (HoverDate != DateTime.MinValue)
			{
				prevArrowHover = false;
				nextArrowHover = false;
				HoverDate = DateTime.MinValue;
				Invalidate ();
			}
		}

		int tmrMonthDelta;
		public void TimerAction()
		{			
			if (Timer.Delay > 2)
				Timer.Delay -= Timer.Delay / 20;
			DateTime tmp = CurrentDate.AddMonths (tmrMonthDelta).Date;
			if (Math.Abs ((DateTime.Now - tmp).TotalDays / 365) < 1000)
				CurrentDate = tmp;
			else {
				Timer.Stop ();
				tmrMonthDelta = -tmrMonthDelta;
				MessageBoxOverlay.ShowSuccess ("You won. I loose.", ParentWindow);
			}
			Invalidate ();
		}

		public override void OnMouseDown (MouseButtonEventArgs e)
		{			
			if (CheckArrows (LastMouseDownMousePosition.X, LastMouseDownMousePosition.Y)) {
				Timer.Delay = 500;
				if (prevArrowHover) {
					tmrMonthDelta = -1;
					TimerAction ();
					Invalidate ();
					Timer.Start ();
				} else if (nextArrowHover) {
					tmrMonthDelta = 1;
					TimerAction ();
					Invalidate ();
					Timer.Start ();
				} else {
					tmrMonthDelta = 0;
				}
			} else {
				tmrMonthDelta = 0;
				base.OnMouseDown (e);
			}
		}

		public override void OnMouseUp (MouseButtonEventArgs e)
		{
			if (Timer.Enabled) {
				Timer.Stop ();
				tmrMonthDelta = 0;
			}
			else
				base.OnMouseUp (e);
		}

		public override void OnClick (MouseButtonEventArgs e)
		{
			if (tmrMonthDelta == 0) {
				DateTime dt = MouseToDate (LastMouseDownMousePosition.X, LastMouseDownMousePosition.Y);
				if (dt != CurrentDate && dt.IsDefined()) {
					CurrentDate = dt;
					Invalidate ();
				}
			}	
			base.OnClick (e);
		}			

		private RectangleF prevRec;
		private RectangleF nextRec;
		private SizeF szDay;
		private float buttonSize;
		private RectangleF rTitle;

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			base.OnLayout (ctx, bounds);

			buttonSize = IconFont.Height;
			prevRec = new RectangleF (bounds.Left + Padding.Left, bounds.Top + Padding.Top, buttonSize, buttonSize);
			nextRec = new RectangleF (bounds.Right - Padding.Right - buttonSize, bounds.Top + Padding.Top, buttonSize, buttonSize);
			szDay = new SizeF ((bounds.Width - Padding.Width) / 7f, (bounds.Height - Padding.Height) / 8f);
			rTitle = new RectangleF (bounds.Left + Padding.Left + buttonSize + 2, bounds.Top + Padding.Top, bounds.Width - Padding.Width - buttonSize - buttonSize - 4, buttonSize);
		}

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);
			if (szDay.Width <= 0 || szDay.Height <= 0)
				return;

			// prev / next buttons
			ctx.DrawString (((char)FontAwesomeIcons.fa_long_arrow_left).ToString (), 
				IconFont, prevArrowHover ? Theme.Brushes.Blue : Theme.Brushes.Base02, 
				prevRec, FontFormat.DefaultIconFontFormatCenter);			
			ctx.DrawString (((char)FontAwesomeIcons.fa_long_arrow_right).ToString (), 
				IconFont, nextArrowHover ? Theme.Brushes.Blue : Theme.Brushes.Base02, 
				nextRec, FontFormat.DefaultIconFontFormatCenter);			

			DateTime date = CurrentDate;
			string title = String.Format("{0} {1}", CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(date.Month).ToUpper(), date.Year.ToString());
			using (var clip = new ClipBoundClip (ctx, rTitle)) {
				ctx.DrawString (title, TitleFont, Theme.Brushes.Base02, rTitle, Format);
			}

			for (int i = 0; i < 7; i++) {
				RectangleF rx = new RectangleF (bounds.Left + Padding.Left + (i * szDay.Width), szDay.Height + bounds.Top + Padding.Top, szDay.Width, szDay.Height);
				string wd = m_Weekdays [i].Substring (0, 1);
				if (i == 0 || i == 6)
					ctx.DrawString (wd, DayFont, Theme.Brushes.Orange, rx, Format);
				else
					ctx.DrawString (wd, DayFont, Theme.Brushes.Cyan, rx, Format);
			}

			DateTime firstDayOfMonth = new DateTime (CurrentDate.Year, CurrentDate.Month, 1);
			DayOfWeek dowStart = firstDayOfMonth.DayOfWeek;

			int dayMax = (int)dowStart < 6 ? 35 : 42;
			int currMonth = CurrentDate.Month;		

			DateTime today = DateTime.Now.Date;
			Brush textBrusch = new SolidBrush(Style.ForeColorBrush.Color);
			float radius = Math.Max(MinCircleRadius, Math.Min(MaxCircleRadius, szDay.Width * 0.35f));

			for (int i = 0; i < dayMax; i++) {
				int col = i % 7;
				int row = (i / 7);
				RectangleF rx = new RectangleF(bounds.Left + Padding.Left + (col * szDay.Width), bounds.Top + Padding.Top + ((row + 2) * szDay.Height), szDay.Width, szDay.Height);

				DateTime dt = m_MonthView [i];
				string wd = dt.Day.ToString();

				Color color = Color.Empty;
				if (dt == today)
					color = TodayBackColor;
				else if (dt == CurrentDate)
					color = CurrentDayBackColor;				
				else  if (dt == HoverDate)
					color = DayHoverBackColor;


				Color textColor = Color.Empty;

				if (!color.IsEmpty) {
					using (var brush = new SolidBrush(color))
					{
						ctx.FillCircle (brush, rx.Left + (rx.Width / 2f), rx.Top + (rx.Height / 2f) - 1f, radius, 1);
					}

					textColor = Style.ForeColorBrush.Color;
				}
				else
				{
					textColor = (col == 0 || col == 6) ? Theme.Colors.Orange : Style.ForeColorBrush.Color;
				}

				if (dt.Month != currMonth)
					textColor = Color.FromArgb (128, textColor);
				textBrusch.Color = textColor;

				ctx.DrawString (wd, DayFont, textBrusch, rx, Format);
			}
		}
	}
}

