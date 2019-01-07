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
	public class ImageButton : Container
	{
		public event EventHandler<EventArgs> CheckedChanged;
		protected void OnCheckedChanged()
		{
			if (CheckedChanged != null)
				CheckedChanged (this, EventArgs.Empty);
		}

		public ImagePanel Image { get; private set; }
		public TextWidget Text { get; private set; }
		public bool IsToggleButton  { get; set; }

		private bool m_Checked;
		public bool Checked  
		{ 
			get {
				return m_Checked;
			}
			set {
				if (m_Checked != value) {
					m_Checked = value;
					OnCheckedChanged ();

					if (value)
						WidgetState = WidgetStates.Pressed;
				}
			}
		}

		public string ImageKey
		{
			get{
				return Image.ImageKey;
			}
			set{
				Image.ImageKey = value;
			}
		}

		public ImageList ImageList
		{
			get{
				return Image.ImageList;
			}
			set{
				Image.ImageList = value;
			}
		}

		public ImageButton (string name, string text, ImageList imageList, string imageKey)
			: this(name, Docking.Left, text, imageList, imageKey, new ButtonWidgetStyle(ColorContexts.Default))
		{
			Styles.SetStyle (new ButtonHoverWidgetStyle (ColorContexts.Default), WidgetStates.Hover);
			Styles.SetStyle (new ButtonPressedWidgetStyle (ColorContexts.Default), WidgetStates.Pressed);
			Styles.SetStyle (new ButtonDisabledWidgetStyle (ColorContexts.Default), WidgetStates.Disabled);
			Styles.SetStyle (new SilverButtonActiveWidgetStyle (), WidgetStates.Active);
			Text.Margin = new Padding (6, 4, 6, 6);	// this gives it a little 'look from above' effect

			//this.MaxSize = this.PreferredSize();
		}

		public ImageButton (string name, Docking dock, string text, ImageList imageList, string imageKey, IWidgetStyle style)
			: base(name, dock, style)
		{
			Image = new ImagePanel ("image", Docking.Left, imageList, imageKey);
			Image.Style.BorderColorPen.Width = 1;
			Image.SizeMode = ImageSizeModes.AutoSize;
			Image.VAlign = Alignment.Center;
			Image.HAlign = Alignment.Center;
			Children.Add (Image);
				
			Text = new TextWidget ("text", Docking.Fill, 
				new WidgetStyle (Color.Empty, style.ForeColorPen.Color, Color.Empty),
				text,
				SummerGUIWindow.CurrentContext.FontManager.StatusFont);
		
			Text.HAlign = Alignment.Center;
			Text.VAlign = Alignment.Center;
			//Text.Margin = new Padding (3, 2, 3, 4);
			//Text.Margin = new Padding (3);
			
			Children.Add (Text);

			Padding = new Padding (3);
			//Margin = new Padding (3);
		}
			
		public override Widget HitTest (float x, float y)
		{
			return base.HitTest (x, y) != null ? this : null;
		}

		public override void OnClick (OpenTK.Input.MouseButtonEventArgs e)
		{
			if (IsToggleButton) {
				Checked = !Checked;
			}
			base.OnClick (e);
		}			

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			base.OnLayout (ctx, bounds);
			if (Checked)
				WidgetState = WidgetStates.Pressed;
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (Text.CachedPreferredSize == SizeF.Empty || Image.CachedPreferredSize == SizeF.Empty) {
				SizeF textSize = SizeF.Empty;
				if (Text != null && (!String.IsNullOrEmpty (Text.Text) || Text.Icon > 0)) {
					textSize = Text.PreferredSize (ctx, proposedSize);
					textSize.Width += Text.Margin.Width;
					if (Text.Font != null)
						textSize.Height = Text.Font.LineHeight.Ceil ();
				}

				SizeF imgSize = SizeF.Empty;
				if (Image != null && Image.Image != null) {
					//imgSize = Image.Image.Size;
					imgSize = Image.PreferredSize (ctx, proposedSize);
					if (imgSize != SizeF.Empty) {
						imgSize.Width += Image.Margin.Width;
						imgSize.Height += Image.Margin.Height;
					}

					CachedPreferredSize = new SizeF (imgSize.Width + textSize.Width + Padding.Width, 
						imgSize.Height + textSize.Height + Padding.Height);
				} else {				
					CachedPreferredSize = textSize.Inflate (Padding);
				}
			}

			return CachedPreferredSize;
		}			
	}		
}

