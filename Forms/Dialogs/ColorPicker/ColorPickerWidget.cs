using System;
using System.Linq;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI.ColorPicker
{
	public class ColorPickerWidget : Container
    {        
        private Color _selectedColor = Color.Red; // Interner Zustand

        public Color Color
        {
            get
            {
                // Gibt immer den aktuell ausgewählten Farbwert zurück
                return _selectedColor;
            }
            set
            {
                // 1. Die Original-Referenz für die Vorher-Hälfte in der PreviewBox setzen
                PreviewBox.OriginalColor = value;
                PreviewBox.Invalidate();

                // 2. Unseren internen Zustand aktualisieren
                _selectedColor = value;

                // 3. Alle Controls (GamutBox, Slider, Textboxen) initialisieren.
                // Da 'source: null' übergeben wird, stellen sich alle Controls 
                // perfekt auf den neuen Wert ein.
                UpdateAllControls(source: null);
            }
        }

        public ColorPickerGamutBox GamutBox { get; private set; }

        public ColorPickerAlphaSlider AlphaSlider { get; private set; }
        public ColorPickerLightnessSlider LightnessSlider { get; private set; }

        public ColorPickerPreviewBox PreviewBox { get; private set; }

        TableLayoutContainer tableLayout;

        public NumberTextBox txtRed { get; private set; }
        public NumberTextBox txtGreen { get; private set; }
        public NumberTextBox txtBlue { get; private set; }
        public NumberTextBox txtAlpha { get; private set; }
        public TextBox txtHexColor { get; private set; }

        // Das magische Flag gegen Endlosschleifen
        private bool isUpdating = false;

        // Das zentrale Farbfeld (RGB + Alpha)
        private Color currentRGBColor = Color.Red;

        public Color SelectedColor
        {
            get => currentRGBColor;
            set
            {
                if (currentRGBColor != value)
                {
                    currentRGBColor = value;
                    UpdateAllControls(source: null);
                }
            }
        }

        public ColorPickerWidget (string name, Docking dock, IWidgetStyle style)
			: base(name, dock, style)
		{		
			TabIndex = -1;	

            GamutBox = this.AddChild(new ColorPickerGamutBox("GamutBox", Docking.Left, null));            

            LightnessSlider = this.AddChild(new ColorPickerLightnessSlider("LightnessSlider", Docking.Left));
            AlphaSlider = this.AddChild(new ColorPickerAlphaSlider("AlphaSlider", Docking.Left));            
            //AlphaSlider.Margin = new Padding(8);

            tableLayout = this.AddChild(new TableLayoutContainer("tableLayout"));
            tableLayout.AutoSize = true;
            //tableLayout.BackColor = Color.BlueViolet;
            tableLayout.CellPadding = new SizeF(3, 6);
            
            PreviewBox = tableLayout.AddChild(new ColorPickerPreviewBox("PreviewBox", Docking.Right, null), 0, 0, columnSpan: 2);
            PreviewBox.HAlign = Alignment.Center;

            PreviewBox.MinSize = new SizeF(80, 60);
            PreviewBox.MaxSize = PreviewBox.MinSize;

            PreviewBox.OriginalColor = Color.FromArgb(128, Color.Red);
            PreviewBox.NewColor = Color.FromArgb(255, Color.Blue);

            int rowIndex = 1;
            TextLabel lblRed = tableLayout.AddChild(new TextLabel("lblRed", "Red"), rowIndex, 0);
            lblRed.Dock = Docking.Right;
            lblRed.OffsetY = -1;
            txtRed = tableLayout.AddChild(new NumberTextBox("txtRed"), rowIndex, 1);
            txtRed.MinSize = new SizeF(60, 0);
            txtRed.MaxValue = 255;

            rowIndex++;
            TextLabel lblGreen = tableLayout.AddChild(new TextLabel("lblGreen", "Green"), rowIndex, 0);
            lblGreen.Dock = Docking.Right;
            lblGreen.OffsetY = -1;
            txtGreen = tableLayout.AddChild(new NumberTextBox("txtGreen"), rowIndex, 1);
            txtGreen.MinSize = new SizeF(60, 0);
            txtGreen.MaxValue = 255;

            rowIndex++;
            TextLabel lblBlue = tableLayout.AddChild(new TextLabel("lblBlue", "Blue"), rowIndex, 0);
            lblBlue.Dock = Docking.Right;
            lblBlue.OffsetY = -1;
            txtBlue = tableLayout.AddChild(new NumberTextBox("txtBlue"), rowIndex, 1);
            txtBlue.MinSize = new SizeF(60, 0);
            txtBlue.MaxValue = 255;

            rowIndex++;
            TextLabel lblAlpha = tableLayout.AddChild(new TextLabel("lblAlpha", "Alpha"), rowIndex, 0);
            lblAlpha.Dock = Docking.Right;
            lblAlpha.OffsetY = -1;
            txtAlpha = tableLayout.AddChild(new NumberTextBox("txtAlpha"), rowIndex, 1);
            txtAlpha.MinSize = new SizeF(60, 0);
            txtAlpha.MaxValue = 255;

            rowIndex++;
            TextLabel lblHexColor = tableLayout.AddChild(new TextLabel("lblHexColor", "Hex #"), rowIndex, 0);
            lblHexColor.Dock = Docking.Right;
            lblHexColor.OffsetY = -1;
            txtHexColor = tableLayout.AddChild(new TextBox("txtHexColor"), rowIndex, 1);
            txtHexColor.MinSize = new SizeF(60, 0);
            txtHexColor.MaxLength = 8;

            tableLayout.Columns[0].SizeMode = TableSizeModes.Content;
            tableLayout.Columns[1].SizeMode = TableSizeModes.Fill;

            InitializeEvents();
        }

        private void InitializeEvents()
        {
            // 1. GamutBox (Maus wählt Farbe auf der Gamut-Fläche)
            GamutBox.ColorChanged += (sender, color) =>
            {
                if (isUpdating) return;
                
                Color baseColor = Color.FromArgb(color.R, color.G, color.B);
                
                float h, s;
                RgbToHsl(baseColor, out h, out s, out _);
                
                float currentLightness = (255 - LightnessSlider.Value) / 255f; 
                Color rgb = HslToRgb(h, s, currentLightness);

                // KORREKTUR: _selectedColor statt currentRGBColor beschreiben!
                _selectedColor = Color.FromArgb(AlphaSlider.Value, rgb.R, rgb.G, rgb.B);
                
                UpdateAllControls(source: GamutBox);
            };

            // 2. Lightness-Slider (Maus ändert Helligkeit)
            LightnessSlider.ValueChanged += (sender, value) =>
            {
                if (isUpdating) return;

                float h, s;
                RgbToHsl(GamutBox.SelectedColor, out h, out s, out _);
                
                float newLightness = (255 - value) / 255f;
                Color rgb = HslToRgb(h, s, newLightness);

                // KORREKTUR: _selectedColor statt currentRGBColor beschreiben!
                _selectedColor = Color.FromArgb(AlphaSlider.Value, rgb.R, rgb.G, rgb.B);
                
                UpdateAllControls(source: LightnessSlider);
            };

            // 3. Alpha-Slider (Maus ändert Transparenz)
            AlphaSlider.ValueChanged += (s, value) =>
            {
                if (isUpdating) return;

                // KORREKTUR: _selectedColor statt currentRGBColor beschreiben!
                _selectedColor = Color.FromArgb(value, _selectedColor.R, _selectedColor.G, _selectedColor.B);
                
                UpdateAllControls(source: AlphaSlider);
            };

            // 4. RGB & Alpha Textboxen (Benutzer tippt Zahlen ein)
            EventHandler<EventArgs> textRGBChanged = (s, e) =>
            {
                if (isUpdating) return;

                int r = (int)txtRed.Value;
                int g = (int)txtGreen.Value;
                int b = (int)txtBlue.Value;
                int a = (int)txtAlpha.Value;

                // KORREKTUR: _selectedColor statt currentRGBColor beschreiben!
                _selectedColor = Color.FromArgb(a, r, g, b);
                
                UpdateAllControls(source: s);
            };

            /***
            txtRed.TextChanged += textRGBChanged;
            txtGreen.TextChanged += textRGBChanged;
            txtBlue.TextChanged += textRGBChanged;
            txtAlpha.TextChanged += textRGBChanged;
            ***/

            txtRed.ValueChanged += textRGBChanged;
            txtGreen.ValueChanged += textRGBChanged;
            txtBlue.ValueChanged += textRGBChanged;
            txtAlpha.ValueChanged += textRGBChanged;

            // 5. Hex-Textbox (Benutzer gibt Text ein)
            txtHexColor.TextChanged += (s, e) =>
            {
                if (isUpdating) return;

                string hex = txtHexColor.Text.Trim().Replace("#", "");
                if (TryParseHex(hex, out Color parsedColor))
                {
                    // KORREKTUR: _selectedColor statt currentRGBColor beschreiben!
                    _selectedColor = parsedColor;
                    UpdateAllControls(source: txtHexColor);
                }
            };
        }

        /// <summary>
        /// Bringt alle Controls auf den Stand von 'currentRGBColor'.
        /// 'source' ist das Steuerelement, das die Änderung initiiert hat (damit wir es nicht überschreiben).
        /// </summary>
        private void UpdateAllControls(object source)
        {
            isUpdating = true; // Sperre aktivieren!

            try
            {
                // 1. Preview-Box aktualisieren (immer!)
                PreviewBox.NewColor = _selectedColor;
                PreviewBox.Invalidate();

                // 2. Gamut-Box aktualisieren
                // Wenn source == null ist oder die Änderung von den Textboxen kommt (nicht von den Slidern/GamutBox)
                if (source == null || (source != GamutBox && source != LightnessSlider && source != AlphaSlider))
                {
                    GamutBox.SelectedColor = Color.FromArgb(255, _selectedColor.R, _selectedColor.G, _selectedColor.B);
                }

                // 3. Slider-Werte vorbereiten
                RgbToHsl(_selectedColor, out float h, out float s, out float l);

                // Die reine "Basisfarbe" bei 50% Helligkeit für die Slider ermitteln
                Color baseColorAt50 = HslToRgb(h, s, 0.5f);

                // Lightness-Slider aktualisieren
                // Wenn source == null ist, MUSS der Value (Gripper) zwingend gesetzt werden!
                if (source == null || (source != LightnessSlider && source != GamutBox))
                {
                    LightnessSlider.BaseColor = baseColorAt50;
                    LightnessSlider.Value = 255 - (int)Math.Round(l * 255f);
                }
                else if (source == GamutBox)
                {
                    // Wenn die GamutBox aktiv ist, nur die Hintergrundfarbe anpassen, nicht den Wert (Gripper)
                    LightnessSlider.BaseColor = baseColorAt50;
                }

                // Alpha-Slider aktualisieren
                // Wenn source == null ist, MUSS der Value (Gripper) zwingend gesetzt werden!
                if (source == null || (source != AlphaSlider && source != GamutBox))
                {
                    AlphaSlider.BaseColor = Color.FromArgb(255, _selectedColor.R, _selectedColor.G, _selectedColor.B);
                    AlphaSlider.Value = _selectedColor.A;
                }
                else if (source == GamutBox)
                {
                    // Wenn die GamutBox aktiv ist, nur die Hintergrundfarbe anpassen, nicht den Wert (Gripper)
                    AlphaSlider.BaseColor = Color.FromArgb(255, _selectedColor.R, _selectedColor.G, _selectedColor.B);
                }

                // 4. Textboxen aktualisieren (RGB & Alpha)
                if (source == null || source != txtRed)   txtRed.Value   = _selectedColor.R;
                if (source == null || source != txtGreen) txtGreen.Value = _selectedColor.G;
                if (source == null || source != txtBlue)  txtBlue.Value  = _selectedColor.B;
                if (source == null || source != txtAlpha) txtAlpha.Value = _selectedColor.A;

                // 5. Hex-Textbox aktualisieren
                if (source == null || source != txtHexColor)
                {
                    txtHexColor.Text = $"{_selectedColor.A:X2}{_selectedColor.R:X2}{_selectedColor.G:X2}{_selectedColor.B:X2}";
                }
            }
            finally
            {
                isUpdating = false; // Sperre sicher wieder aufheben!
            }
        }

        protected override void LayoutChildren(IGUIContext ctx, RectangleF bounds)
        {
            //base.LayoutChildren(ctx, bounds);

            RectangleF recGamut = new RectangleF(bounds.Left, bounds.Top, bounds.Height, bounds.Height);
            GamutBox.SetBounds(recGamut);

            float sliderWidth = 24;
            RectangleF recSlider = new RectangleF(GamutBox.Bounds.Right + 8, bounds.Top, sliderWidth, bounds.Height);
            
            LightnessSlider.SetBounds(recSlider);

            recSlider.Offset(recSlider.Width + 8, 0);
            AlphaSlider.SetBounds(recSlider);

            float restWidth = bounds.Width - GamutBox.Width - (sliderWidth * 2) - 28;
            RectangleF recTable = new RectangleF(AlphaSlider.Bounds.Right + 8, bounds.Top, restWidth, bounds.Height);            
            tableLayout.OnLayout(ctx, recTable);
            tableLayout.SetBounds(recTable);            
        }

        #region Mathematische Farb-Hilfsfunktionen

        private static void RgbToHsl(Color rgb, out float h, out float s, out float l)
        {
            float r = rgb.R / 255f;
            float g = rgb.G / 255f;
            float b = rgb.B / 255f;

            float min = Math.Min(r, Math.Min(g, b));
            float max = Math.Max(r, Math.Max(g, b));
            float delta = max - min;

            l = (max + min) / 2f;

            if (delta == 0f)
            {
                h = 0f;
                s = 0f;
            }
            else
            {
                s = l <= 0.5f ? delta / (max + min) : delta / (2f - max - min);

                if (r == max)
                    h = (g - b) / delta + (g < b ? 6f : 0f);
                else if (g == max)
                    h = (b - r) / delta + 2f;
                else
                    h = (r - g) / delta + 4f;

                h /= 6f;
            }
        }

        private static Color HslToRgb(float h, float s, float l)
        {
            float r, g, b;

            if (s == 0f)
            {
                r = g = b = l; 
            }
            else
            {
                float q = l < 0.5f ? l * (1f + s) : l + s - (l * s);
                float p = 2f * l - q;

                r = HueToRgb(p, q, h + 1f / 3f);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1f / 3f);
            }

            return Color.FromArgb((int)(r * 255f), (int)(g * 255f), (int)(b * 255f));
        }

        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0f) t += 1f;
            if (t > 1f) t -= 1f;
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 1f / 2f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }

        private static bool TryParseHex(string hex, out Color color)
        {
            color = Color.Empty;
            try
            {
                if (hex.Length == 6) // "RRGGBB"
                {
                    int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                    int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                    int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                    color = Color.FromArgb(255, r, g, b);
                    return true;
                }
                else if (hex.Length == 8) // "AARRGGBB"
                {
                    int a = Convert.ToInt32(hex.Substring(0, 2), 16);
                    int r = Convert.ToInt32(hex.Substring(2, 2), 16);
                    int g = Convert.ToInt32(hex.Substring(4, 2), 16);
                    int b = Convert.ToInt32(hex.Substring(6, 2), 16);
                    color = Color.FromArgb(a, r, g, b);
                    return true;
                }
            }
            catch { }
            return false;
        }

        #endregion
    }
}