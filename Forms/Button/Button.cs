using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{
	public enum ButtonDisplayStyles
	{		
		ImageAndText,
		Image,
		Text
	}
		

	public class Button : TextWidget
	{		
		public static readonly char EmptyIcon = (char)0;

		public override void OnTextChanged ()
		{
			if (Text != null)
				Mnemonic = Text.ParseMnemonic ();
			else
				Mnemonic = (char)0;
			base.OnTextChanged ();
		}

		public char Mnemonic { get; private set; }

		public bool IsToggleButton  { get; set; }

		bool m_Checked;
		public virtual bool Checked 
		{ 
			get { 
				return m_Checked;
			}
			set {
				if (m_Checked != value) {
					m_Checked = value;
					OnCheckedChanged ();
				}
			}
		}

		public virtual void OnCheckedChanged()
		{
			UpdateStyle ();
			ResetCachedLayout ();
			Invalidate();
		}

		public Color IconColor { get; set; }
		[DpiScalable]
		public float IconOffsetY { get; set; }

		protected ColorContexts m_ColorContext;
		public ColorContexts ColorContext
		{
			get{
				return m_ColorContext;
			}
			set{
				if (m_ColorContext != value) {
					m_ColorContext = value;
					//OnUpdateTheme ();
					Styles.OfType<ButtonWidgetStyle> ().ForEach (style => style.ColorContext = m_ColorContext);
				}
			}
		}

		ButtonDisplayStyles m_DisplayStyle;
		public ButtonDisplayStyles DisplayStyle
		{
			get{
				return m_DisplayStyle;
			}
			set{
				if (m_DisplayStyle != value) {
					m_DisplayStyle = value;
					OnDisplayModeChanged ();
				}
			}
		}

		protected void OnDisplayModeChanged()
		{			
			if (DisplayStyle == ButtonDisplayStyles.Image && String.IsNullOrEmpty (Tooltip))
				Tooltip = Text;
			Update (true);
		}

		public Button (string name, string text) : this(name, text, (char)0, ColorContexts.Default) {}
		public Button (string name, string text, ColorContexts colorContext) : this (name, text, (char)0, colorContext) {}
		public Button (string name, string text, char icon = (char)0, ColorContexts colorContext = ColorContexts.Default)
			: this(name, text, icon, new ButtonWidgetStyle(colorContext))
		{			
			Styles.SetStyle (new ButtonHoverWidgetStyle (colorContext), WidgetStates.Hover);
			Styles.SetStyle (new ButtonPressedWidgetStyle (colorContext), WidgetStates.Pressed);
			Styles.SetStyle (new ButtonDisabledWidgetStyle (colorContext), WidgetStates.Disabled);

			m_ColorContext = colorContext;
		}	

		public Button (string name, string text, char icon, IWidgetStyle style)
			: base(name, Docking.Left, style, text, WidgetExtensions.GetFont(CommonFontTags.Status))
		{			
			Padding = new Padding (10, 4, 10, 4);

			//TextOffsetY = -1;
			Text = text;
			OnTextChanged ();
			Icon = icon;

			Font = FontManager.Manager.StatusFont;
			IconFont = FontManager.Manager.SmallIcons;

			CanFocus = true;
			HandlesEnterKey = true;
		}

		public override void OnClick (MouseButtonEventArgs e)
		{
			if (IsToggleButton) {
				Checked = !Checked;
			}
			base.OnClick (e);
		}			

		public event EventHandler<EventArgs> Fire;
		public void OnFire()
		{
			if (Fire != null && IsVisibleEnabled && WidgetState == WidgetStates.Pressed)
				Fire (this, EventArgs.Empty);
		}			

		TaskTimer Timer;
		public bool IsAutofire
		{
			get{
				return Timer != null;
			}
			set{
				if (value)
					InitAutofire (50, 500);
				else {
					if (Timer != null) {
						Timer.Dispose ();
						Timer = null;
					}
				}
			}
		}

		public void InitAutofire(int period, int duetime)
		{
			if (Timer == null)
				Timer = new TaskTimer (period > 0 ? period : 50, OnFire, duetime > 0 ? duetime : 500);
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{			
			if (CachedPreferredSize == SizeF.Empty) {
				float w = 0;
				float h = 0;
								
				if (!String.IsNullOrEmpty (Text) && DisplayStyle != ButtonDisplayStyles.Image) {					
					SizeF sz = Font.MeasureMnemonicString (Text);					
					h = sz.Height * 1.2f;
					w = sz.Width;
				}

				if (Icon > 0 && IconFont != null && DisplayStyle != ButtonDisplayStyles.Text) {
					SizeF sz = IconFont.Measure (Icon.ToString ());

					if (w > 0)
						w += sz.Width * 1.42f;					
					else
						w += sz.Width;
					h = Math.Max (h, sz.Height);
				}
				
				CachedPreferredSize = ClampMinMax(new SizeF (w + Padding.Width, h + Padding.Height));				
			}

			return CachedPreferredSize;
		}

		protected bool IsMouseDown { get; private set; }
		public override void OnMouseDown (MouseButtonEventArgs e)
		{
			IsMouseDown = true;
			base.OnMouseDown (e);
			Invalidate ();
			if (IsAutofire) {				
				if (Enabled && WidgetState == WidgetStates.Pressed) {
					try {
						OnFire ();
						Timer.Start ();
					} catch (Exception ex) {
						ex.LogWarning ();
					}
				}
			}
		}

		public override void OnMouseUp (MouseButtonEventArgs e)
		{
			if (Timer != null)
				Timer.Stop ();
			base.OnMouseUp (e);
			IsMouseDown = false;
			Invalidate ();
		}			

		public override void OnMouseMove (MouseMoveEventArgs e)
		{
			base.OnMouseMove (e);
			// ToDo: Better handle Leave when pressed Style
		}

		public bool IsDefaultButton { get; set; }
		public void MakeDefaultButton()
		{
			IsDefaultButton = true;
		}

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			if (base.OnKeyDown (e))
				return true;

			if ((e.Key == Keys.Enter && (IsFocused || Selected || IsDefaultButton)) || ModifierKeys.AltPressed && e.Key.ToString ().FirstOrDefault () == Mnemonic) {
				OnClick (null);
				return true;
			}

			return false;
		}

		public override void UpdateStyle()
		{
			if (Enabled) {
				if (Checked)
					WidgetState = WidgetStates.Pressed;
				else if (IsFocused)
					WidgetState = WidgetStates.Active;
				else if (Selected)
					WidgetState = WidgetStates.Selected;				
				else
					WidgetState = WidgetStates.Default;
			}
			else
				WidgetState = WidgetStates.Disabled;
		}

		/***
		protected virtual void OnDrawSelection(IGUIContext ctx, RectangleF bounds)
		{
			float distance = 3f * ScaleFactor;
			bounds.Inflate (-distance, -distance);
			using (var pen = new Pen(Color.FromArgb(180, Style.ForeColorPen.Color), 1f * ScaleFactor, LineStyles.Dotted)) {
				ctx.DrawRectangle (pen, bounds);
			}
		}

		public override void OnPaintBackground (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaintBackground (ctx, bounds);

			if (Focused && !IsMouseDown) {
				OnDrawSelection (ctx, bounds);
			}
		}
		***/

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			//base.OnPaint (ctx, bounds);

			float iconWidth = 0;
			//FontFormat ff = FontFormat.DefaultIconFontFormatCenter;

			if (String.IsNullOrEmpty (Text))
				iconWidth = PaddingBounds.Width;
			if (Icon != 0 && IconFont != null && DisplayStyle != ButtonDisplayStyles.Text) {												
				RectangleF rt = new RectangleF (bounds.Left + Padding.Left, bounds.Top + IconOffsetY, iconWidth, bounds.Height);				
				
				if (IconColor != Color.Empty && Enabled) {
					using (Brush brush = new SolidBrush (IconColor)) {
						iconWidth = ctx.DrawString(Icon.ToString(), IconFont, brush,
							rt, FontFormat.DefaultIconFontFormatCenter).Width;		
					}
				} else {					
					iconWidth = ctx.DrawString(Icon.ToString(), IconFont, Style.ForeColorBrush,
						rt, FontFormat.DefaultIconFontFormatCenter).Width;
				}				
			}

			if (!String.IsNullOrEmpty(Text) && DisplayStyle != ButtonDisplayStyles.Image) {

				if (iconWidth > 0) {					
					float delta = iconWidth * 1.42f;
					bounds.Width -= delta;
					bounds.X += delta;
				}

				bounds.Offset (0, TextOffsetY);
				ctx.DrawString (Text, Font, Style.ForeColorBrush, bounds, FontFormat.DefaultMnemonicLineCentered);
			}
		}

		public override string ToString ()
		{
			return string.Format ("[Button: Name={0}, Text={1}", Name, Text + String.Empty);
		}
	}


	public class DefaultButton : Button
	{
		public DefaultButton(string name, string text, char icon = (char)0) 
			: base(name, text, icon, new SilverButtonWidgetStyle()) 
		{
			//IsMenu = true;

			Styles.SetStyle (new SilverButtonActiveHoverWidgetStyle (), WidgetStates.Active);
			Styles.SetStyle (new SilverButtonActiveWidgetStyle(), WidgetStates.Selected);
			Styles.SetStyle (new SilverButtonDisabledWidgetStyle(), WidgetStates.Disabled);
			Styles.SetStyle (new SilverButtonPressedWidgetStyle (), WidgetStates.Pressed);

			Styles.SetStyle (new SilverButtonDefaultHoverWidgetStyle (), WidgetStates.Custom1);
			Styles.SetStyle (new SilverButtonActiveHoverWidgetStyle (), WidgetStates.Custom2);	

			HAlign = Alignment.Center;
			VAlign = Alignment.Center;			
		}

		public DefaultButton (string name, string text, ColorContexts colorContext, char icon = (char)0, bool outLine = false)
			: base (name, text, icon, new ButtonWidgetStyle(colorContext))
		{	
			//IsMenu = true;

			if (outLine) {				
				Styles.SetStyle (new OutlineButtonWidgetStyle (colorContext), WidgetStates.Default);
				Styles.SetStyle (new OutlineButtonInvertedWidgetStyle (colorContext), WidgetStates.Hover);
				Styles.SetStyle (new OutlineButtonWidgetStyle (colorContext), WidgetStates.Active);
				Styles.SetStyle (new OutlineButtonWidgetStyle (colorContext), WidgetStates.Selected);
				Styles.SetStyle (new OutlineButtonPressedWidgetStyle (colorContext), WidgetStates.Pressed);
				Styles.SetStyle (new OutlineButtonDisabledWidgetStyle (colorContext), WidgetStates.Disabled);
			} else {
				Styles.SetStyle (new SilverButtonActiveHoverWidgetStyle (), WidgetStates.Active);
				Styles.SetStyle (new SilverButtonActiveWidgetStyle (), WidgetStates.Selected);

				Styles.SetStyle (new SilverButtonPressedWidgetStyle (), WidgetStates.Pressed);
				Styles.SetStyle (new ButtonDisabledWidgetStyle (colorContext), WidgetStates.Disabled);

				//Styles.SetStyle (new ButtonHoverWidgetStyle (colorContext), WidgetStates.Hover);
				Styles.SetStyle (new ButtonHoverWidgetStyle (colorContext), WidgetStates.Custom1);
				Styles.SetStyle (new SilverButtonActiveHoverWidgetStyle (), WidgetStates.Custom2);	
			}

			m_ColorContext = colorContext;
			HAlign = Alignment.Center;
			VAlign = Alignment.Center;
		}			

		public override void OnMouseEnter (IGUIContext ctx)
		{
			base.OnMouseEnter (ctx);
			if (IsFocused || Selected)
				WidgetState = WidgetStates.Custom2;
			else
				WidgetState = WidgetStates.Custom1;
		}			

		/****
		protected override void OnDrawSelection (IGUIContext ctx, RectangleF bounds)
		{
			//base.OnDrawSelection (ctx, bounds);
		}
		***/

		public override string ToString ()
		{
			return string.Format ("[DialogButton: Name={0}, Text={1}", Name, Text + String.Empty);
		}
	}

	public static class ButtonExtensions
	{
		public static void SetChecked(this Button button, bool check = false)
		{
			button.IsToggleButton = true;
			button.Checked = check;
		}

		public static void ApplyCommandButtonPadding(this Button button)
		{			
			button.Padding = new Padding(16 * button.ScaleFactor, button.Padding.Height);
		}
	}
}

