using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;


namespace SummerGUI
{
	public class NotificationWidget : Container
	{
		public event EventHandler<EventArgs> Close;
		public void OnClose()
		{			
			if (CloseTimer != null)
				CloseTimer.Enabled = false;
			if (Close != null)
				Close (this, EventArgs.Empty);
		}

		public ColorContexts NotificationStyle { get; private set; }

		public double AutoCloseSeconds { get; private set; }
		public bool CloseButtonVisible { get; private set; }

		public MultiLineTextWidget Text { get; private set; }
		public char Icon { get; private set; }
		public DateTime Created { get; private set; }

		public NotificationWidget (string name, string text, ColorContexts notificationStyle, char icon = (char)0, bool canClose = true, double autoCloseSeconds = 0)
			: base(name, Docking.Top, new NotificationWidgetStyle(notificationStyle))
		{
			Created = DateTime.Now;

			Text = AddChild (new MultiLineTextWidget ("notificationText", text, Style));
			Text.Padding = new Padding (8);

			Margin = new Padding (0, 1, 0, 0);

			NotificationStyle = notificationStyle;

			if (icon > 0) {
				Icon = icon;
			} else {
				switch (NotificationStyle) {
				case ColorContexts.Information:
					Icon = (char)FontAwesomeIcons.fa_info_circle;
					break;
				case ColorContexts.Danger:
				case ColorContexts.Warning:
					Icon = (char)FontAwesomeIcons.fa_warning;
					break;
				case ColorContexts.Success:
					Icon = (char)FontAwesomeIcons.fa_check;
					break;
				}
			}

			if (Icon > 0) {
				TextWidget TWI = AddChild (new TextWidget ("notificationIcon", Docking.Left, Style, null, null));
				TWI.Icon = Icon;
				TWI.ZIndex = 1;
			}

			CloseButtonVisible = canClose;
			if (CloseButtonVisible) {
				Button btnClose = AddChild (new Button ("close", null, (char)FontAwesomeIcons.fa_close, notificationStyle));
				btnClose.Dock = Docking.Right;
				btnClose.ZIndex = 1;
				btnClose.MaxSize = new SizeF (btnClose.MaxSize.Width, Int32.MaxValue);
				btnClose.Styles.OfType<ButtonWidgetStyle> ().ForEach (s => {
					s.ButtonStyle = ButtonStyles.Flat;
					s.Border = 0;
				});
				btnClose.Click += delegate {
					OnClose();
				};
			}
				
			AutoCloseSeconds = autoCloseSeconds;
			if (AutoCloseSeconds > 0) {
				CloseTimer = new DelayedAction ((int)(AutoCloseSeconds * 1000), OnClose);
			}				
		}
			
		protected override void OnParentChanged ()
		{
			base.OnParentChanged ();
			if (Parent != null && CloseTimer != null)
				CloseTimer.Start ();
		}

		readonly DelayedAction CloseTimer;
			
		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{	
			if (CachedPreferredSize == SizeF.Empty) {
				proposedSize.Width -= Children.Where (c => c != Text).Sum (c => c.PreferredSize (ctx).Width);
				proposedSize.Width = Math.Max (proposedSize.Width, MinSize.Width);
				CachedPreferredSize = new SizeF (proposedSize.Width, Text.PreferredSize (ctx, proposedSize).Height);
			}
			return CachedPreferredSize;
		}			


		protected override void CleanupManagedResources ()
		{
			if (CloseTimer != null)
				CloseTimer.Dispose ();
			base.CleanupManagedResources ();
		}
	}

	public class NotificationPanel : Container
	{
		public int MaxNotifications { get; set; }

		public int Count
		{
			get{
				return Children.Count;
			}
		}

		public override bool Visible {
			get {
				return base.Visible && Count > 0;
			}
			set {
				base.Visible = value;
			}
		}

		public NotificationPanel (string name)
			: base(name, Docking.Top, new DarkPanelWidgetStyle())
		{			
			MaxNotifications = 1;
		}

		private void RemoveNotification(Widget notify, bool animate = false)
		{	
			if (IsDisposed)
				return;
			// ToDo: a nice animation
			Children.Remove(notify);
			notify.Dispose ();
			Update (true);
		}

		static int NotificationCount;
		public void AddNotification(string message, ColorContexts context, char icon = (char)0, bool canClose = true, double autoCloseSeconds = 0)
		{
			if (MaxNotifications <= 0)
				return;

			int number = 1;
			unchecked {
				number = Interlocked.Increment(ref NotificationCount);
			}

			NotificationWidget notify = null;

			try {
				notify = AddChild(new NotificationWidget ("notification" + number.ToString(), message, context, icon, canClose, autoCloseSeconds));
				while (Children.Count > 0 && Children.Count > MaxNotifications) {					
					Widget tmp = Children.OfType<NotificationWidget>().OrderBy(c => c.Created).FirstOrDefault();
					if (tmp != null) {
						RemoveNotification(tmp, false);
					} else {
						// just for the case ..
						this.LogWarning("Unable to remove a Notification.");
						break;
					}
				}
			} catch (Exception ex) {
				ex.LogError ();
			} finally {
				Update (true);
			}

			if (notify != null) {
				notify.Close += delegate {
					RemoveNotification (notify, true);
				};
			}				
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{	
			if (CachedPreferredSize == SizeF.Empty) {
				try {
					CachedPreferredSize = new SizeF (proposedSize.Width, Children.Sum (n => n.PreferredSize (ctx, proposedSize).Height));
				} catch (Exception ex) {
					ex.LogWarning ();
					CachedPreferredSize = SizeF.Empty;
				}
			}

			return CachedPreferredSize;
		}
	}
}

