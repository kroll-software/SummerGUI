using System;
using KS.Foundation;

namespace SummerGUI
{
	/// <summary>
	/// The overlay window that hosts a single <see cref="MonthCalendar"/>.
	/// Follows the ComboBox drop-down pattern: it is created lazily by the
	/// <see cref="DateTimePicker"/>, added as a child (which auto-registers it
	/// with the root as an overlay) and closed via <see cref="IOverlayWidget.Closing"/>
	/// — an outside click on the root triggers that.
	/// </summary>
	public class CalendarPopup : OverlayContainer
	{
		/// <summary>The hosted calendar.</summary>
		public MonthCalendar Calendar { get; private set; }

		/// <summary>
		/// The maximum width the popup is allowed to render at. Because this
		/// widget is a Fill child of its parent <i>and</i> a root overlay, it
		/// is laid out to its parent's full width by default — too wide for a
		/// month grid. <see cref="SetBounds"/> clamps any wider request so the
		/// calendar stays a compact square.
		/// </summary>
		public const float MaxWidth = 300f;

		public CalendarPopup (MonthCalendar calendar)
			: base ("calendarpopup", Docking.Fill, new DropDownWidgetStyle())
		{
			if (calendar == null)
				throw new ArgumentNullException ("calendar");

			Calendar = calendar;
			Calendar.Dock = Docking.Fill;
			AddChild (Calendar);

			OverlayMode = OverlayModes.Overlay;
			CanFocus = true;
		}

		// Clamp any full-width request from the parent layout so the month
		// calendar renders as a compact square instead of spanning the table row.
		public override void SetBounds (float x, float y, float width, float height)
		{
			if (width > MaxWidth)
				width = MaxWidth;
			// MonthCalendar needs ~8 rows (title + weekday header + 6 week rows);
			// keep the aspect ratio so cells stay square.
			if (height > width * 1.3f)
				height = width * 1.2f;
			base.SetBounds (x, y, width, height);
		}

		public override bool OnKeyDown (OpenTK.Windowing.Common.KeyboardKeyEventArgs e)
		{
			if (!IsFocused)
				return false;

			switch (e.Key) {
			case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter:
			case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape:
				OnClose ();
				return true;
			}

			// Let the calendar handle arrows, PageUp/Down …
			if (Calendar != null && Calendar.OnKeyDown (e))
				return true;

			return base.OnKeyDown (e);
		}
	}
}
