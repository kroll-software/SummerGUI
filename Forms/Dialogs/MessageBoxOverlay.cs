using System;
using System.Linq;
using System.Drawing;
using System.Threading;
using OpenTK.Input;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;
using SummerGUI.Splitting;
using System.Reflection.Metadata.Ecma335;

namespace SummerGUI
{
	public class MessageBoxOverlay : ChildFormOverlay
	{		
		protected TextWidget IconText { get; set; }
		protected Panel ContentPanel { get; set; }		

		protected MultiLineTextWidget TW  { get; set; }
		protected ButtonContainer ButtonContainer { get; private set; }		

		public string Message { get; protected set; }

		IGUIContext CTX;
		public ColorContexts ColorContext { get; protected set; }

		public MessageBoxOverlay (IGUIContext ctx, ColorContexts colorContext)
			: base("messagebox")
		{
			CTX = ctx;
			ColorContext = colorContext;
			CanFocus = true;

			ContentPanel = AddChild (new Panel ("contents", Docking.Fill, new WidgetStyle (Theme.GetContextColor (colorContext),
				Theme.GetContextForeColor (colorContext),
				Color.Empty)));			

			// panel
			ButtonContainer = ContentPanel.AddChild (new ButtonContainer("buttoncontainer", Docking.Bottom, new EmptyWidgetStyle()));
			ButtonContainer.FlexDirection = FlexDirections.RowCenter;
			ButtonContainer.Padding =  new Padding(16);			
		}

        public override void OnResize()
        {			
            base.OnResize();

			SizeF sz = ContentPanel.PreferredSize(CTX, Bounds.Size);
			float rest = (Bounds.Height - sz.Height) / 2;
			ContentPanel.Margin = new Padding(0, rest, 0, rest);			
        }		

		public override void Focus ()
		{		
			if (ButtonContainer.Children.Count > 0)
				ButtonContainer.Children.First.Focus();			
		}

		public override void ShowDialog (SummerGUIWindow parent)
		{			
			base.ShowDialog (parent);
			InitAnimation ();			
			Invalidate ();
		}

		private void InitAnimation()
		{			
			if (CTX.Animator.Enabled) {
				CTX.Animator.AddAnimation (ContentPanel.Style, "AlphaBack", 0, 225, 0.33);

				if (ParentWindow.Device == Devices.Mobile) {
					Style.BackColorBrush.Color = Theme.GetContextColor (ColorContext);
					CTX.Animator.AddAnimation (Style, "AlphaBack", 0, 100, 0.33);
				} else {
					CTX.Animator.AddAnimation (Style, "AlphaBack", 0, 32, 0.33);
				}

				ContentPanel.Children.ForEach (child => CTX.Animator.AddAnimation (child.Style, "AlphaFore", 0, 255, 0.33));
			}
		}

		public virtual void OnYes()
		{
			Result = DialogResults.Yes;
			this.OnClose ();
		}

		public virtual void OnNo()
		{
			Result = DialogResults.No;
			this.OnClose ();
		}

		public virtual void OnContinue()
		{
			Result = DialogResults.Continue;
			this.OnClose ();
		}

		public virtual void OnRepeat()
		{
			Result = DialogResults.Repeat;
			this.OnClose ();
		}

		public virtual void OnIgnore()
		{
			Result = DialogResults.Ignore;
			this.OnClose ();
		}

		public override void OnClose ()
		{			
			base.OnClose ();
		}

		protected virtual void InitButtons(MessageBoxButtons buttons, ColorContexts colorContext)
		{			
			DefaultButton btn = null;
			
			if (buttons.HasFlag (MessageBoxButtons.Continue)) {
				btn = new DefaultButton ("continuebutton", "&Continue", colorContext, (char)0, true);
				btn.Click += (sender, eContinue) => OnContinue();
				ButtonContainer.AddChild (btn);
				btn.Focus ();
			}

			if (buttons.HasFlag (MessageBoxButtons.Repeat)) {
				btn = new DefaultButton ("repeatbutton", "&Repeat", colorContext, (char)0, true);
				btn.Click += (sender, eRepeat) => OnRepeat();
				ButtonContainer.AddChild (btn);
			}
			
			if (buttons.HasFlag (MessageBoxButtons.Yes)) {
				btn = new DefaultButton ("yesbutton", "&Yes", colorContext, (char)0, true);
				btn.Click += (sender, eYes) => OnYes();
				ButtonContainer.AddChild (btn);
				btn.Focus ();
			}

			if (buttons.HasFlag (MessageBoxButtons.No)) {
				btn = new DefaultButton ("nobutton", "&No", colorContext, (char)0, true);
				btn.Click += (sender, eNo) => OnNo();
				ButtonContainer.AddChild (btn);
			}

			if (buttons.HasFlag (MessageBoxButtons.OK)) {
				btn = new DefaultButton ("okbutton", "&OK", colorContext, (char)0, true);
				btn.Click += (sender, eOK) => OnOK ();					
				ButtonContainer.AddChild (btn);
				btn.Focus ();
			}				

			if (buttons.HasFlag (MessageBoxButtons.Cancel)) {
				btn = new DefaultButton ("cancelbutton", "&Cancel", colorContext, (char)0, true);			
				btn.Click += (sender, eCancel) => OnCancel();
				ButtonContainer.AddChild (btn);
			}				
		}

		protected virtual void InitIconImage(char icon, ColorContexts colorContext)
		{				
			IconText = new TextWidget ("icon", Docking.Top, new EmptyWidgetStyle (), null, null);
			IconText.IconFont = FontManager.Manager.FontByTag (CommonFontTags.LargeIcons);
			IconText.Icon = icon;
			IconText.ForeColor = Color.White;			
			ContentPanel.AddChild (IconText);
		}

		protected void InitText(string message, ColorContexts colorContext)
		{
			Message = message;

			TW = new MultiLineTextWidget ("message", message, 
				new WidgetStyle(Color.Empty,
					Theme.GetContextForeColor(colorContext),
					Color.Empty));

			TW.Padding = new Padding (16);
			TW.VAlign = Alignment.Center;
			TW.HAlign = Alignment.Center;
			this.OnLayout (CTX, CTX.Bounds);

			// Layout the text
			SizeF preferref = ContentPanel.PreferredSize(CTX, new SizeF(0, 0));
			SizeF sz = TW.PreferredSize (CTX, new SizeF(CTX.Bounds.Width, float.MaxValue));

			if (sz.Height > preferref.Height) {
				ScrollableContainer container = ContentPanel.AddChild (new ScrollableContainer ("scroller"));
				container.ScrollBars = ScrollBars.Vertical;
				container.AutoScroll = true;
				TW.Dock = Docking.Top;
				container.MaxSize = new SizeF(float.MaxValue, 240);
				container.Margin = new Padding(0, 0, 0, 16);
				container.AddChild (TW);
			} else {
				ContentPanel.AddChild (TW);
			}				
		}        
			
		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{			
			switch (e.Key) {
			case Keys.Escape:				
				OnCancel();
				return true;
			case Keys.Enter:
				OnOK ();
				return true;
			default:
				return base.OnKeyDown (e);
			}				
		}

		public static DialogResults Show (string message, MessageBoxTypes msgType = MessageBoxTypes.Info, MessageBoxButtons buttons = MessageBoxButtons.OkCancel, SummerGUIWindow parent = null)
		{						
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

			MessageBoxOverlay box = new MessageBoxOverlay (parent, colorContext);

			box.InitIconImage ((char)icon, colorContext);

			// *** Buttons
			box.InitButtons (buttons, colorContext);
			box.InitText (message.TrimRightLinebreaks () + "\n", colorContext);

			if (msgType == MessageBoxTypes.Error) {
				//box.InitCopyButton ();
			}
				
			box.Style.BackColorBrush.Color = 
				Color.FromArgb(30, Color.DarkSlateGray);

			box.ShowDialog (parent);
			box.Focus ();			

            return DialogResults.OK;

			/***
			var result = box.Result;
            box?.Dispose ();
            box = null;
            return result;			
			***/
		}			
			
		public static DialogResults ShowInfo(string msg, SummerGUIWindow parent)
		{
			return Show (msg, MessageBoxTypes.Info, MessageBoxButtons.OK, parent);
		}

		public static DialogResults ShowSuccess(string msg, SummerGUIWindow parent)
		{
			return Show (msg, MessageBoxTypes.Success, MessageBoxButtons.OK, parent);
		}

		public static DialogResults ShowError(string msg, SummerGUIWindow parent)
		{
			return Show (msg, MessageBoxTypes.Error, MessageBoxButtons.OK, parent);
		}

		public static DialogResults ShowErrorRetry(string msg, SummerGUIWindow parent)
		{
			return Show (msg, MessageBoxTypes.Error, MessageBoxButtons.ContinueRepeatCancel, parent);
		}

		public static DialogResults ShowWarning(string msg, SummerGUIWindow parent)
		{
			return Show (msg, MessageBoxTypes.Warning, MessageBoxButtons.OK, parent);
		}

		public static DialogResults ShowQuestion(string msg, SummerGUIWindow parent)
		{
			return Show (msg, MessageBoxTypes.Question, MessageBoxButtons.YesNoCancel, parent);
		}

		public static DialogResults ShowContinueRepeat(string msg, SummerGUIWindow parent)
		{
			return Show (msg, MessageBoxTypes.Question, MessageBoxButtons.ContinueRepeatCancel, parent);
		}
	}
}

