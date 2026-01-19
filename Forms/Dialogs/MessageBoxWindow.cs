using System;
using System.Drawing;
using HarfBuzzSharp;
using OpenTK;
using OpenTK.Graphics.ES11;
using OpenTK.Input;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SummerGUI
{	
	public enum MessageBoxTypes
	{			
		Info,
		Question,
		Warning,
		Error,
		Help,
		Success
	}		

	[Flags]
	public enum MessageBoxButtons
	{					
		Cancel = 0x1,
		OK = 0x2,
		Yes = 0x4,
		No = 0x8,
		Continue = 0x10,
		Repeat = 0x20,
		OkCancel = OK + Cancel,
		YesNo = Yes + No,
		YesNoCancel = Yes + No + Cancel,
		ContinueCancel = Continue + Cancel,
		RepeatCancel = Repeat + Cancel,
		ContinueRepeatCancel = Continue + Repeat + Cancel
	}

	/***
	public interface IChildWindowOpener
	{
		ApplicationWindow AppWindow { get; }
		System.Drawing.Rectangle Bounds { get; }
	}
	***/

	public class MessageBoxWindow : ChildFormWindow
	{	
		protected TextWidget IconText { get; set; }
		protected Panel ImagePanelContainer { get; private set; }
		protected Button CopyButton { get; private set; }
		protected ButtonContainer ButtonContainer { get; private set; }

		public string Message { get; protected set; }

		protected MessageBoxWindow(string name, string caption, int width, int height, SummerGUIWindow parent)
			: base(name, caption, width, height, parent, true)
		{			
			ButtonContainer = Controls.AddChild (new ButtonContainer("buttoncontainer"));
		}

		protected virtual void InitIconImage(char icon, ColorContexts colorContext)
		{	
			ImagePanelContainer = Controls.AddChild (new Panel ("imagecontainer", Docking.Left, new BrightPanelWidgetStyle()));
			IconText = ImagePanelContainer.AddChild (new TextWidget ("icon", Docking.Top, new EmptyWidgetStyle (), null, null));
			IconText.SetIconFontByTag(CommonFontTags.LargeIcons);
			IconText.Icon = icon;
			IconText.Style.ForeColorBrush.Color = Theme.GetContextColor(colorContext);
			IconText.Padding = new Padding (6, 18, 6, 6);
		}

		public void InitCopyButton()
		{		
			CopyButton = ImagePanelContainer.AddChild (new Button ("copy", "Copy", (char)FontAwesomeIcons.fa_copy, ColorContexts.Default));
			CopyButton.Dock = Docking.Bottom;
			CopyButton.Margin = new Padding (6, 0, 6, 6);
			CopyButton.Click += delegate {				
				PlatformExtensions.SetClipboardText(Message);
			};
		}
			
		public virtual void OnYes()
		{
			Result = DialogResults.Yes;
			this.Close ();
		}

		public virtual void OnNo()
		{
			Result = DialogResults.No;
			this.Close ();
		}

		public virtual void OnContinue()
		{
			Result = DialogResults.Continue;
			this.Close ();
		}

		public virtual void OnRepeat()
		{
			Result = DialogResults.Repeat;
			this.Close ();
		}
			
		protected virtual void InitButtons(MessageBoxButtons buttons)
		{			
			// panel
			DefaultButton btn = null;

			if (buttons.HasFlag (MessageBoxButtons.Continue)) {
				btn = ButtonContainer.AddChild (new DefaultButton ("continuebutton", "&Continue"));
				btn.Click += (sender, eContinue) => OnContinue();
			}

			if (buttons.HasFlag (MessageBoxButtons.Repeat)) {
				btn = ButtonContainer.AddChild (new DefaultButton ("repeatbutton", "&Repeat"));
				btn.Click += (sender, eRepeat) => OnRepeat();
			}

			if (buttons.HasFlag (MessageBoxButtons.Yes)) {
				btn = ButtonContainer.AddChild (new DefaultButton ("yesbutton", "&Yes"));
				btn.Click += (sender, eYes) => OnYes();
			}

			if (buttons.HasFlag (MessageBoxButtons.No)) {
				btn = ButtonContainer.AddChild (new DefaultButton ("nobutton", "&No"));;
				btn.Click += (sender, eNo) => OnNo();				
			}

			if (buttons.HasFlag (MessageBoxButtons.OK)) {
				btn = ButtonContainer.AddChild (new DefaultButton ("okbutton", "&OK"));
				btn.Click += (sender, eOK) => OnOK();
			}

			if (buttons.HasFlag (MessageBoxButtons.Cancel)) {
				btn = ButtonContainer.AddChild (new DefaultButton ("cancelbutton", "&Cancel"));
				btn.Click += (sender, eCancel) => OnCancel();
			}				

			ButtonContainer.Children.First.Focus();
		}

		protected void InitText(string message)
		{
			Message = message;			

			MultiLineTextWidget text = new MultiLineTextWidget ("message", message);			

			text.Padding = new Padding (16);
			text.VAlign = Alignment.Center;
			text.HAlign = Alignment.Center;
			text.ForeColor = Color.Black;		

			Controls.OnLayout (this, (Rectangle)ClientRectangle);

			// Layout the text

			RectangleF bounds = Controls.ClientRectangle;
			SizeF sz = text.PreferredSize (this, bounds.Size);

			if (sz.Height > bounds.Height) {
				ScrollableContainer container = this.Controls.AddChild (new ScrollableContainer ("scroller"));
				container.ScrollBars = ScrollBars.Vertical;
				container.AutoScroll = true;
				text.Dock = Docking.Top;
				container.AddChild (text);
			} else {
				this.Controls.AddChild (text);
			}
		}

        protected override void OnPaint(RectangleF bounds)
        {
            base.OnPaint(bounds);			
        }

		protected override void OnInitFonts ()
		{
			FontManager.Manager.GetConfig (CommonFontTags.Menu).OnDemand = true;
			base.OnInitFonts ();
		}

		protected override void OnInitCursors ()
		{
			//base.OnInitCursors ();
		}

		public static int DefaultWidth = 420;
		public static int DefaultHeight = 180;

		public static DialogResults Show (string caption, string message, MessageBoxTypes msgType = MessageBoxTypes.Info, MessageBoxButtons buttons = MessageBoxButtons.OkCancel, SummerGUIWindow parent = null)
		{	
			float scaling = 1;
			if (parent != null)
				scaling = parent.ScaleFactor;

			MessageBoxWindow box = new MessageBoxWindow ("MsgBox", caption, (DefaultWidth * scaling).Ceil(), (DefaultHeight * scaling).Ceil(), parent);

			// *** Icon

			FontAwesomeIcons icon = FontAwesomeIcons.fa_anchor;
			ColorContexts colorContext = ColorContexts.Default;

			// init icon
			switch (msgType) {
			case MessageBoxTypes.Info:
				icon = FontAwesomeIcons.fa_info_circle;			
				colorContext = ColorContexts.Information;
				break;
			case MessageBoxTypes.Success:
				icon = FontAwesomeIcons.fa_exclamation_circle;
				colorContext = ColorContexts.Success;
				break;
			case MessageBoxTypes.Warning:
				icon = FontAwesomeIcons.fa_warning;
				colorContext = ColorContexts.Warning;
				break;
			case MessageBoxTypes.Error:
				icon = FontAwesomeIcons.fa_times_circle;
				colorContext = ColorContexts.Danger;
				break;
			case MessageBoxTypes.Question:
				icon = FontAwesomeIcons.fa_question_circle;
				colorContext = ColorContexts.Question;
				break;
			case MessageBoxTypes.Help:
				icon = FontAwesomeIcons.fa_life_ring;
				colorContext = ColorContexts.Success;
				break;
			}
				
			box.InitIconImage ((char)icon, colorContext);

			// *** Buttons
			box.InitButtons (buttons);
			box.InitText (message);

			if (msgType == MessageBoxTypes.Error) {
				box.InitCopyButton ();
			}				

			box.ShowInTaskBar = false;
			box.Show (parent);
			if (box != null)
				box.Dispose ();			

			return box.Result;
		}

		public static string DefaultCaption = "";
			
		public static DialogResults ShowInfo(string msg, SummerGUIWindow parent)
		{
			string caption = DefaultCaption;
			if (String.IsNullOrEmpty (caption))
				caption = "Information";

			return Show (caption, msg, MessageBoxTypes.Info, MessageBoxButtons.OK, parent);
		}

		public static DialogResults ShowSuccess(string msg, SummerGUIWindow parent)
		{
			string caption = DefaultCaption;
			if (String.IsNullOrEmpty (caption))
				caption = "Success";

			return Show (caption, msg, MessageBoxTypes.Success, MessageBoxButtons.OK, parent);
		}

		public static DialogResults ShowError(string msg, SummerGUIWindow parent)
		{
			string caption = DefaultCaption;
			if (String.IsNullOrEmpty (caption))
				caption = "Error";

			return Show (caption, msg, MessageBoxTypes.Error, MessageBoxButtons.OK, parent);
		}

		public static DialogResults ShowWarning(string msg, SummerGUIWindow parent)
		{
			string caption = DefaultCaption;
			if (String.IsNullOrEmpty (caption))
				caption = "Warning";

			return Show (caption, msg, MessageBoxTypes.Warning, MessageBoxButtons.OK, parent);
		}

		public static DialogResults ShowQuestion(string msg, SummerGUIWindow parent)
		{
			string caption = DefaultCaption;
			if (String.IsNullOrEmpty (caption))
				caption = "Question";

			return Show (caption, msg, MessageBoxTypes.Question, MessageBoxButtons.YesNoCancel, parent);
		}

		public static DialogResults ShowContinueRepeat(string msg, SummerGUIWindow parent)
		{
			string caption = DefaultCaption;
			if (String.IsNullOrEmpty (caption))
				caption = "Question";

			return Show (caption, msg, MessageBoxTypes.Question, MessageBoxButtons.ContinueRepeatCancel, parent);
		}
			
		protected override void OnKeyDown (KeyboardKeyEventArgs e)
		{			
			switch (e.Key) {
			case Keys.Escape:
				Close ();
				break;			
			default:
				base.OnKeyDown (e);
				break;
			}				
		}
	}
}

