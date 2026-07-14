using System;
using System.Linq;
using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;
using OpenTK.Windowing.Common;

namespace SummerGUI.ColorPicker
{
	public class ColorPickerPreviewBox : Container
    {
        public Color OriginalColor { get; set; }
        public Color NewColor { get; set; }

        Brush brush;

        public ColorPickerPreviewBox(string name, Docking dock, IWidgetStyle style)
            : base(name, dock, style)
        {       
            TabIndex = -1;
            brush = new SolidBrush(Color.Black);

            OriginalColor = Color.Gray;
            NewColor = Color.White;

            string filePath = "Assets/Images/img_bg_transparent.gif".FixedExpandedPath();        

            TextureImage image = TextureImage.FromFile(filePath);
            var backgroundsettings = new BackgroundImageSettings(image);
            backgroundsettings.SizeMode = ImageSizeModes.TileAll;            
            this.BackgroundImage = backgroundsettings;
        }

        public override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Y < Bounds.Top + Bounds.Height / 2)
                this.Tooltip = "Original Color";
            else
                this.Tooltip = "Selected Color";                        

            Root?.ShowTooltip(this.Tooltip, new PointF(e.X, e.Y));
        }

        public override void OnPaint(IGUIContext ctx, RectangleF bounds)
        {
            base.OnPaint(ctx, bounds);        

            RectangleF rectTop = new RectangleF(bounds.Left, bounds.Top, bounds.Width, bounds.Height / 2);
            RectangleF rectBottom = new RectangleF(bounds.Left, bounds.Top + bounds.Height / 2, bounds.Width, bounds.Height / 2);

            // Fill Original Color on top half
            brush.Color = OriginalColor;
            ctx.FillRectangle(brush, rectTop);

            // Fill New Color on bottom half
            brush.Color = NewColor;
            ctx.FillRectangle(brush, rectBottom);
        }        
    }    

}