using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using KS.Foundation;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Pfz.Collections;	// TreadSafeDictionary
//using OpenTK.Graphics.ES20;

namespace SummerGUI
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GUIVertex
    {
        public Vector2 Position;
        public Color4 Color;
        public Vector2 TexCoord;
        public float Type; // 0.0f für Bilder/Rechtecke, 1.0f für Text
    }

    struct GLStateBackup
    {
        public int Program;
        public int VAO;
        public int ArrayBuffer;
        public int ElementArrayBuffer;
        public int ActiveTexture;
        public int Texture2D;
        public bool Blend;
        public bool DepthTest;
    }

    public class GUIRenderBatcher : DisposableObject
    {
        public static GUIRenderBatcher Batcher
		{
			get 
            {
				return Singleton<GUIRenderBatcher>.Instance;
			}
		}

        private int vbo = 0, ibo = 0;
        private int _currentVAO = 0; // Hilfsvariable im Batcher
        private GUIVertex[] vertexArray;
        private uint[] indexArray;
        private int vertexCount;
        private int indexCount;
        private int currentTexture = -1;   
        private int whiteTextureId = 0;     
        private Rectangle currentScissor;

        private const int MAX_VERTICES = 10000;
        private const int MAX_INDICES = 15000;

        private GUIShader _uiShader;
        private Matrix4 _projectionMatrix;
        private readonly int _vertexStride;
        private PrimitiveType _currentType = PrimitiveType.Triangles;        

        // Im Batcher: Ein Dictionary, um VAOs pro Kontext zu speichern
        private readonly ThreadSafeDictionary<IntPtr, int> contextVAOs = new ThreadSafeDictionary<IntPtr, int>();

        public GUIRenderBatcher()
        {
            vertexArray = new GUIVertex[MAX_VERTICES];
            indexArray = new uint[MAX_INDICES];
            _vertexStride = Marshal.SizeOf<GUIVertex>();            

            // VBO
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(MAX_VERTICES * _vertexStride), IntPtr.Zero, BufferUsageHint.DynamicDraw);

            // IBO
            ibo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(MAX_INDICES * sizeof(uint)), IntPtr.Zero, BufferUsageHint.DynamicDraw);

            // Attribute (stellen sicher, dass sie zum Core-Profile passen)
            // Position - location 0 (vec2)
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, _vertexStride, new IntPtr(0));

            // Color - location 1 (vec4)
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, _vertexStride, new IntPtr(8));

            // TexCoord - location 2 (vec2)
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, _vertexStride, new IntPtr(24));

            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, _vertexStride, new IntPtr(32));

            // Unbind VAO (sauber)
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            byte[] white = new byte[] { 255, 255, 255, 255 };
            whiteTextureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, whiteTextureId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, white);            
        }


        /// <summary>
        /// Shader Types:
        /// Solid / Image	0.0f	Standard-Rechtecke, Kreise, Bilder
        /// Text	1.0f	Text-Rendering (R-Kanal zu Alpha)
        /// Dotted Line	2.0f	Modulo 10px, 3px Punkt
        /// Dashed Line	3.0f	Modulo 20px, 12px Strich
        /// DashDot Line	4.0f	Kombiniertes Muster
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>

        public void Init(int width, int height)
        {
            // Vertex Shader: Unverändert
            string vSource = "#version 330 core\n" +
                "layout (location = 0) in vec2 aPos;\n" +
                "layout (location = 1) in vec4 aColor;\n" +
                "layout (location = 2) in vec2 aTexCoord;\n" +
                "layout (location = 3) in float aType;\n" + 
                "out vec4 fColor;\n" +
                "out vec2 fTexCoord;\n" +
                "out float fType;\n" +
                "uniform mat4 projection;\n" +
                "void main() {\n" +
                "    fColor = aColor;\n" +
                "    fTexCoord = aTexCoord;\n" +
                "    fType = aType;\n" +
                "    gl_Position = projection * vec4(aPos, 0.0, 1.0);\n" +
                "}\n";

            // Fragment Shader: Jetzt mit uGamma Uniform
            string fSource = "#version 330 core\n" +
                "in vec4 fColor;\n" +
                "in vec2 fTexCoord;\n" + 
                "in float fType;\n" +
                "out vec4 FragColor;\n" +
                "uniform sampler2D uTexture;\n" +
                "uniform float uGamma;\n" + // Die neue Variable von C#
                "void main() {\n" +
                "    if (fType >= 2.0) {\n" + 
                "        // --- LINIE-MODUS (Stippling) ---\n" +
                "        float dist = fTexCoord.x;\n" +
                "        bool discardPixel = false;\n" +
                "        \n" +
                "        if (fType < 2.5) { // Dotted\n" +
                "            float w = fTexCoord.y;\n" +
                "            if (mod(dist, 2.0 * w) > w) discardPixel = true;\n" +
                "        } else if (fType < 3.5) { // Dashed\n" +
                "            float w = fTexCoord.y;\n" +
                "            if (mod(dist, 8.0 * w) > 4.0 * w) discardPixel = true;\n" +
                "        } else if (fType < 4.5) { // DashDot\n" +
                "            float w = fTexCoord.y;\n" +
                "            float cycle = 11.0 * w;\n" +
                "            float m = mod(dist, cycle);\n" +
                "            if (!(m < 6.0 * w || (m > 8.0 * w && m < 9.0 * w))) { discardPixel = true; }\n" +
                "        }\n" +
                "        \n" +
                "        if (discardPixel) discard;\n" +
                "        FragColor = fColor;\n" +
                "        \n" +
                "    } else if (fType > 0.5) {\n" + 
                "        // --- TEXT-MODUS ---\n" +
                "        vec4 texCol = texture(uTexture, fTexCoord);\n" +
                "        \n" +
                "        // Nutze uGamma für die Korrektur. 1.0 / uGamma.\n" +
                "        // max(...) verhindert Fehler bei exakt 0.0.\n" +
                "        float alpha = pow(max(texCol.r, 0.00001), 1.0 / uGamma);\n" +
                "        \n" +
                "        FragColor = vec4(fColor.rgb, fColor.a * alpha);\n" +
                "    } else {\n" + 
                "        // --- BILD/SOLID-MODUS ---\n" +
                "        vec4 texCol = texture(uTexture, fTexCoord);\n" +
                "        FragColor = texCol * fColor;\n" +
                "    }\n" +
                "}\n";

            _uiShader = new GUIShader(vSource, fSource);
            
            // Standardwert nach dem Initialisieren setzen (wichtig!)
            _uiShader.Use();
            int location = GL.GetUniformLocation(_uiShader.Handle, "uGamma");
            if (location != -1) GL.Uniform1(location, 1.8f);
            
            UpdateSize(width, height);
        }

        public void SetGamma(float gammaValue)
        {
            _uiShader.Use();
            int location = GL.GetUniformLocation(_uiShader.Handle, "uGamma");
            if (location != -1)
            {
                // 1.0 = keine Korrektur, 1.8 = Standard, 1.4 = fettere Schrift
                GL.Uniform1(location, gammaValue);
            }
        }

        private IntPtr _lastActiveContext = IntPtr.Zero;

        public void BindContext(IGUIContext ctx)
        {            
            ctx.GlWindow.MakeCurrent();

            var clientSize = ctx.GlWindow.ClientSize;
    
            // OpenGL Viewport auf die volle Innenfläche
            GL.Viewport(0, 0, clientSize.X, clientSize.Y);

            // Projektion exakt auf die Client-Maße
            UpdateSize(clientSize.X, clientSize.Y);
            
            IntPtr currentContext = ctx.GlWindow.Context.WindowPtr; // OpenTK 4 Handle

            //if (currentContext == _lastActiveContext)
            //    return;
            
            _lastActiveContext = currentContext;            

            bool needsSetup = false;

            if (!contextVAOs.TryGetValue(currentContext, out int activeVAO))
            {
                needsSetup = true;
            }
            else if (!GL.IsVertexArray(activeVAO)) // Prüfen, ob das VAO im neuen Kontext noch existiert
            {
                needsSetup = true;                
            }

            if (needsSetup)
            {
                activeVAO = GL.GenVertexArray();
                GL.BindVertexArray(activeVAO);                

                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);

                SetupAttributes(); 

                // Update oder Add
                contextVAOs[currentContext] = activeVAO;
            }
            else
            {
                GL.BindVertexArray(activeVAO);
                // Auch wenn das VAO bekannt ist, müssen die Buffer-Bindungen für 
                // den aktuellen Kontext aufgefrischt werden, damit BufferSubData weiß, wohin.
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
            }

            _currentVAO = activeVAO;
            
            // 3. Buffer Re-Bind
            // Unbedingt beide binden, da der Batcher beim Flush sonst in ein gelöschtes VBO schreibt
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);

            // 4. Shader Re-Validierung
            // Falls der Shader im About-Dialog derselbe war (gleiche Instanz?), 
            // wurde er durch DlgAbout.Dispose() gelöscht! 
            // Du musst sicherstellen, dass das Programm-Handle noch valide ist.
            if (_uiShader != null) 
            {
                _uiShader.Use(); 
            }            
            
            // 6. Textur-Reset            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, whiteTextureId);            
            
            //GL.Disable(EnableCap.ScissorTest);
        }

        public void UnbindContext(IGUIContext ctx)
        {                
            IntPtr currentContext = ctx.GlWindow.Context.WindowPtr; // OpenTK 4 Handle

            if (contextVAOs.TryGetValue(currentContext, out int vaoId))
            {
                // Wir müssen sicherstellen, dass der Kontext aktiv ist, um das VAO zu löschen
                // Aber Vorsicht: Im OnClosed-Event ist der Kontext oft schon halb zerstört.
                // Ein sicherer Weg: Markiere die ID als "zum Löschen bereit" 
                // und lösche sie beim nächsten BindContext-Aufruf des Main-Windows.
                GL.DeleteVertexArray(vaoId);
                contextVAOs.Remove(currentContext);
                
                if (_lastActiveContext == currentContext)
                    _lastActiveContext = IntPtr.Zero;
            }
        }

        private void SetupAttributes()
        {
            // Dein Code aus dem Constructor:
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, _vertexStride, 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, _vertexStride, 8);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, _vertexStride, 24);
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, _vertexStride, 32);
        }

        public void UpdateSize(int width, int height)
        {
            // 1. Matrix berechnen
            _projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1.0f, 1.0f);
            
            // 2. UNBEDINGT an den Shader senden!
            if (_uiShader != null)
            {
                _uiShader.Use(); // Sicherstellen, dass er aktiv ist
                // Angenommen, dein Shader hat ein Property oder eine Methode dafür:
                _uiShader.SetMatrix4("projection", _projectionMatrix); 
            }            
        }

        private void SetWhiteTexture()
        {
            if (currentTexture != whiteTextureId) {
                Flush();
                currentTexture = whiteTextureId;            
            }
        }

        public void AddVertex(Vector2 position, Color4 color, float type = 0)
        {            
            if (vertexCount >= MAX_VERTICES) Flush();
            // immer TexCoord initialisieren
            vertexArray[vertexCount++] = new GUIVertex { Position = position, Color = color, TexCoord = Vector2.Zero, Type = type };
        }

        public void AddVertex(Vector2 position, Vector2 texCoord, Color4 color, float type = 0f)
        {            
            if (vertexCount >= MAX_VERTICES) Flush();
            
            vertexArray[vertexCount++] = new GUIVertex 
            { 
                Position = position, 
                TexCoord = texCoord, 
                Color = color,
                Type = type,
            };
        }

        public void AddIndex(uint index)
        {
            if (indexCount >= MAX_INDICES) Flush();
            indexArray[indexCount++] = index;
        }

        public void AddGlyph(int textureId, RectangleF dest, RectangleF uv, Color4 color)
        {            
            DrawCount++;            

            if ((textureId != currentTexture && currentTexture != -1) || (vertexCount + 4 >= MAX_VERTICES)) 
                Flush();

            currentTexture = textureId;

            // Start-Index für dieses spezifische Quad merken
            uint startVertex = (uint)vertexCount;

            // 1. Vertices (4 Stück)
            vertexArray[vertexCount++] = new GUIVertex { Position = new Vector2(dest.X, dest.Y), Color = color, TexCoord = new Vector2(uv.X, uv.Y), Type = 1f };
            vertexArray[vertexCount++] = new GUIVertex { Position = new Vector2(dest.Right, dest.Y), Color = color, TexCoord = new Vector2(uv.Right, uv.Y), Type = 1f  };
            vertexArray[vertexCount++] = new GUIVertex { Position = new Vector2(dest.Right, dest.Bottom), Color = color, TexCoord = new Vector2(uv.Right, uv.Bottom), Type = 1f  };
            vertexArray[vertexCount++] = new GUIVertex { Position = new Vector2(dest.X, dest.Bottom), Color = color, TexCoord = new Vector2(uv.X, uv.Bottom), Type = 1f  };

            // 2. Indizes (6 Stück - immer relativ zu startVertex)
            // Wenn du einen festen Index-Buffer (IBO) hast, brauchst du hier nichts tun, 
            // außer den indexCount zu erhöhen. 
            // Wenn du die Indizes jedes mal mitschickst:
            indexArray[indexCount++] = startVertex + 0;
            indexArray[indexCount++] = startVertex + 1;
            indexArray[indexCount++] = startVertex + 2;
            indexArray[indexCount++] = startVertex + 0;
            indexArray[indexCount++] = startVertex + 2;
            indexArray[indexCount++] = startVertex + 3;
        }

        public void AddTextureRectangle(RectangleF rect, RectangleF uv, Color4 color, int textureId)
        {
            DrawCount++;

            if (textureId != currentTexture)
            {
                Flush();
                currentTexture = textureId;
            }

            if (vertexCount + 4 >= MAX_VERTICES) Flush();

            uint startIdx = (uint)vertexCount;

            vertexArray[vertexCount++] = new GUIVertex { Position = new Vector2(rect.Left, rect.Top), Color = color, TexCoord = new Vector2(uv.Left, uv.Top) };
            vertexArray[vertexCount++] = new GUIVertex { Position = new Vector2(rect.Right, rect.Top), Color = color, TexCoord = new Vector2(uv.Right, uv.Top) };
            vertexArray[vertexCount++] = new GUIVertex { Position = new Vector2(rect.Right, rect.Bottom), Color = color, TexCoord = new Vector2(uv.Right, uv.Bottom) };
            vertexArray[vertexCount++] = new GUIVertex { Position = new Vector2(rect.Left, rect.Bottom), Color = color, TexCoord = new Vector2(uv.Left, uv.Bottom) };

            indexArray[indexCount++] = startIdx + 0;
            indexArray[indexCount++] = startIdx + 1;
            indexArray[indexCount++] = startIdx + 2;
            indexArray[indexCount++] = startIdx + 2;
            indexArray[indexCount++] = startIdx + 3;
            indexArray[indexCount++] = startIdx + 0;
        }

        public void AddRectangle(RectangleF rect, Color4 color)
        {
            AddRectangle(rect, color, color, color, color);
        }

        public void AddRectangle(RectangleF rect, Color4 colorTL, Color4 colorTR, Color4 colorBR, Color4 colorBL)
        {
            DrawCount++;
            if (vertexCount + 4 >= MAX_VERTICES) Flush();
            SetWhiteTexture();

            uint startIdx = (uint)vertexCount;

            vertexArray[vertexCount++] = new GUIVertex { Position = new Vector2(rect.Left, rect.Top), Color = colorTL, TexCoord = Vector2.Zero };
            vertexArray[vertexCount++] = new GUIVertex { Position = new Vector2(rect.Right, rect.Top), Color = colorTR, TexCoord = Vector2.Zero };
            vertexArray[vertexCount++] = new GUIVertex { Position = new Vector2(rect.Right, rect.Bottom), Color = colorBR, TexCoord = Vector2.Zero };
            vertexArray[vertexCount++] = new GUIVertex { Position = new Vector2(rect.Left, rect.Bottom), Color = colorBL, TexCoord = Vector2.Zero };

            indexArray[indexCount++] = startIdx + 0;
            indexArray[indexCount++] = startIdx + 1;
            indexArray[indexCount++] = startIdx + 2;
            indexArray[indexCount++] = startIdx + 2;
            indexArray[indexCount++] = startIdx + 3;
            indexArray[indexCount++] = startIdx + 0;
        }

        public void AddRoundedRectangle(RectangleF rect, Color4 color, float radius, int segments = 8)
        {            
            // Sicherheitscheck: Radius darf nicht größer als die halbe Seite sein
            radius = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2f);
            
            if (radius <= 0) {
                AddRectangle(rect, color, color, color, color);
                return;
            }            

            // Wir brauchen (segments + 1) * 4 Vertices
            int requiredVertices = (segments + 1) * 4;
            if (vertexCount + requiredVertices >= MAX_VERTICES) Flush();
            
            SetWhiteTexture();
            uint startIdx = (uint)vertexCount;

            // Hilfsfunktion zum Hinzufügen der Ecken-Punkte
            // corners: 0=TopRight, 1=BottomRight, 2=BottomLeft, 3=TopLeft
            AddCorner(new Vector2(rect.Right - radius, rect.Top + radius), radius, 270, 360, segments, color); // TR
            AddCorner(new Vector2(rect.Right - radius, rect.Bottom - radius), radius, 0, 90, segments, color);   // BR
            AddCorner(new Vector2(rect.Left + radius, rect.Bottom - radius), radius, 90, 180, segments, color);  // BL
            AddCorner(new Vector2(rect.Left + radius, rect.Top + radius), radius, 180, 270, segments, color); // TL

            // Indexing: Triangle Fan vom ersten Punkt aus oder Center-Punkt
            // Einfachheitshalber als Triangle List (Center-Punkt Methode für sauberes Mesh)
            // Hier nutzen wir einen "Center-Vertex" für den Fan-Stil oder bauen Streifen:
            for (int i = 1; i < requiredVertices - 1; i++)
            {
                indexArray[indexCount++] = startIdx;
                indexArray[indexCount++] = startIdx + (uint)i;
                indexArray[indexCount++] = startIdx + (uint)i + 1;
            }            

            DrawCount++;
        }

        private void AddCorner(Vector2 center, float radius, float startAngle, float endAngle, int segments, Color4 color)
        {
            for (int i = 0; i <= segments; i++)
            {
                float theta = MathHelper.DegreesToRadians(startAngle + (endAngle - startAngle) * i / segments);
                float x = center.X + (float)Math.Cos(theta) * radius;
                float y = center.Y + (float)Math.Sin(theta) * radius;

                vertexArray[vertexCount++] = new GUIVertex {
                    Position = new Vector2(x, y),
                    Color = color,
                    TexCoord = Vector2.Zero
                };
            }
        }

        public void AddRoundedRectangleGradient(RectangleF rect, Color4 colorTL, Color4 colorTR, Color4 colorBR, Color4 colorBL, float radius, int segments = 8)
        {
            radius = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2f);
            
            int requiredVertices = (segments + 1) * 4 + 1; // +1 für den Center-Punkt
            if (vertexCount + requiredVertices >= MAX_VERTICES) Flush();
            
            SetWhiteTexture();
            uint startIdx = (uint)vertexCount;

            // 1. Center-Vertex für den Triangle Fan (Durchschnittsfarbe)
            Color4 centerColor = AverageColor(colorTL, colorTR, colorBR, colorBL);
            vertexArray[vertexCount++] = new GUIVertex { 
                Position = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f), 
                Color = centerColor, 
                TexCoord = Vector2.Zero 
            };

            // 2. Ecken mit interpolierten Farben hinzufügen
            // Die Reihenfolge der Aufrufe bestimmt den Linienzug außen
            AddCornerGradient(new Vector2(rect.Right - radius, rect.Top + radius), radius, 270, 360, segments, rect, colorTL, colorTR, colorBR, colorBL); // TR
            AddCornerGradient(new Vector2(rect.Right - radius, rect.Bottom - radius), radius, 0, 90, segments, rect, colorTL, colorTR, colorBR, colorBL);   // BR
            AddCornerGradient(new Vector2(rect.Left + radius, rect.Bottom - radius), radius, 90, 180, segments, rect, colorTL, colorTR, colorBR, colorBL);  // BL
            AddCornerGradient(new Vector2(rect.Left + radius, rect.Top + radius), radius, 180, 270, segments, rect, colorTL, colorTR, colorBR, colorBL); // TL

            // 3. Indizes für den Fan
            uint totalCornerVertices = (uint)((segments + 1) * 4);
            for (uint i = 1; i <= totalCornerVertices; i++)
            {
                indexArray[indexCount++] = startIdx; // Center
                indexArray[indexCount++] = startIdx + i;
                // Beim letzten Vertex zurück zum ersten äußeren Vertex (startIdx + 1)
                indexArray[indexCount++] = (i == totalCornerVertices) ? startIdx + 1 : startIdx + i + 1;
            }
            DrawCount++;
        }

        private void AddCornerGradient(Vector2 center, float radius, float startAngle, float endAngle, int segments, RectangleF bounds, Color4 cTL, Color4 cTR, Color4 cBR, Color4 cBL)
        {
            for (int i = 0; i <= segments; i++)
            {
                float theta = MathHelper.DegreesToRadians(startAngle + (endAngle - startAngle) * i / segments);
                Vector2 pos = new Vector2(center.X + (float)Math.Cos(theta) * radius, center.Y + (float)Math.Sin(theta) * radius);

                // Relative Position im Rechteck (0.0 bis 1.0)
                float u = (pos.X - bounds.Left) / bounds.Width;
                float v = (pos.Y - bounds.Top) / bounds.Height;

                vertexArray[vertexCount++] = new GUIVertex {
                    Position = pos,
                    Color = GetBilinearColor(u, v, cTL, cTR, cBR, cBL),
                    TexCoord = Vector2.Zero
                };
            }
        }

        private Color4 GetBilinearColor(float u, float v, Color4 cTL, Color4 cTR, Color4 cBR, Color4 cBL)
        {
            // Horizontal oben und unten
            var top = Interpolate(cTL, cTR, u);
            var bottom = Interpolate(cBL, cBR, u);
            // Vertikal dazwischen
            return Interpolate(top, bottom, v);
        }

        private Color4 Interpolate(Color4 a, Color4 b, float t)
        {
            return new Color4(
                a.R + (b.R - a.R) * t,
                a.G + (b.G - a.G) * t,
                a.B + (b.B - a.B) * t,
                a.A + (b.A - a.A) * t);
        }

        private Color4 AverageColor(params Color4[] colors)
        {
            float r = 0, g = 0, b = 0, a = 0;
            foreach (var c in colors) { r += c.R; g += c.G; b += c.B; a += c.A; }
            return new Color4(r / colors.Length, g / colors.Length, b / colors.Length, a / colors.Length);
        }

        public void AddRoundedRectangleOutline(RectangleF rect, Color4 color, float thick, float radius, int segments = 8)
        {                        
            // Radius begrenzen und sicherstellen, dass er nicht kleiner als die halbe Dicke ist
            radius = Math.Max(thick / 2f, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2f));
            
            if (vertexCount + ((segments + 1) * 4 * 2) >= MAX_VERTICES) Flush();
            SetWhiteTexture();

            // Wir definieren den äußeren und inneren Radius
            float outerRadius = radius;
            float innerRadius = radius - thick;

            // Hilfsfunktion für einen Bogen-Abschnitt
            void AddArc(Vector2 center, float startAngle, float endAngle)
            {
                for (int i = 0; i <= segments; i++)
                {
                    float theta = MathHelper.DegreesToRadians(startAngle + (endAngle - startAngle) * i / segments);
                    float cos = (float)Math.Cos(theta);
                    float sin = (float)Math.Sin(theta);

                    // Äußerer Punkt
                    Vector2 pOuter = new Vector2(center.X + cos * outerRadius, center.Y + sin * outerRadius);
                    // Innerer Punkt
                    Vector2 pInner = new Vector2(center.X + cos * innerRadius, center.Y + sin * innerRadius);

                    uint startIdx = (uint)vertexCount;
                    vertexArray[vertexCount++] = new GUIVertex { Position = pOuter, Color = color, TexCoord = Vector2.Zero };
                    vertexArray[vertexCount++] = new GUIVertex { Position = pInner, Color = color, TexCoord = Vector2.Zero };

                    // Indizes für das Quad (außer beim letzten Punkt des Bogens)
                    if (i < segments)
                    {
                        indexArray[indexCount++] = startIdx;
                        indexArray[indexCount++] = startIdx + 1;
                        indexArray[indexCount++] = startIdx + 3;
                        indexArray[indexCount++] = startIdx + 3;
                        indexArray[indexCount++] = startIdx + 2;
                        indexArray[indexCount++] = startIdx;
                    }
                }
            }

            // Die vier Ecken zeichnen. 
            // Zwischen den Bögen entstehen automatisch die geraden Linien, 
            // wenn wir die Indizes korrekt verbinden oder die Bögen nacheinander in den Buffer schreiben.
            
            // Wir müssen hier die Bögen manuell verbinden oder die Methode leicht anpassen:
            // Um Lücken zu vermeiden, zeichnen wir einen durchgehenden Loop.
            
            // 1. Top-Right
            AddArc(new Vector2(rect.Right - radius, rect.Top + radius), 270, 360);
            // 2. Verbindung zu Bottom-Right (wird durch AddArc implizit vorbereitet)
            AddArc(new Vector2(rect.Right - radius, rect.Bottom - radius), 0, 90);
            // 3. Bottom-Left
            AddArc(new Vector2(rect.Left + radius, rect.Bottom - radius), 90, 180);
            // 4. Top-Left
            AddArc(new Vector2(rect.Left + radius, rect.Top + radius), 180, 270);

            // Letzte Lücke schließen: Verbinde den allerletzten Vertex mit dem allerersten
            uint firstIdx = (uint)(vertexCount - ((segments + 1) * 4 * 2));
            uint lastIdx = (uint)(vertexCount - 2);
            
            indexArray[indexCount++] = lastIdx;
            indexArray[indexCount++] = lastIdx + 1;
            indexArray[indexCount++] = firstIdx + 1;
            indexArray[indexCount++] = firstIdx + 1;
            indexArray[indexCount++] = firstIdx;
            indexArray[indexCount++] = lastIdx;            

            DrawCount++;
        }

        public void AddTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Color4 color)
        {   
            DrawCount++;                                 
            if (vertexCount + 3 >= MAX_VERTICES) Flush();
            SetWhiteTexture();            

            uint startVertex = (uint)vertexCount;

            // 3. Drei Vertices hinzufügen
            // UV (0,0) zeigt auf den weißen Pixel, Type 0.0f für Solid-Modus im Shader
            AddVertex(p1, new Vector2(0, 0), color, 0f);
            AddVertex(p2, new Vector2(0, 0), color, 0f);
            AddVertex(p3, new Vector2(0, 0), color, 0f);

            // FALLS du einen Index-Buffer (EBO) nutzt, musst du hier die Indizes addieren:
            indexArray[indexCount++] = startVertex;
            indexArray[indexCount++] = startVertex + 1;
            indexArray[indexCount++] = startVertex + 2;
        }

        public void AddLine(float x1, float y1, float x2, float y2, Color4 color)
        {
            DrawCount++;

            // Wir wechseln den Modi (Triangles vs Lines)
            if (_currentType != PrimitiveType.Lines)
            {
                Flush();
                _currentType = PrimitiveType.Lines;
            }

            SetWhiteTexture();

            if (vertexCount + 2 >= MAX_VERTICES) Flush();

            vertexArray[vertexCount++] = new GUIVertex { Position = new Vector2(x1, y1), Color = color, TexCoord = Vector2.Zero };
            vertexArray[vertexCount++] = new GUIVertex { Position = new Vector2(x2, y2), Color = color, TexCoord = Vector2.Zero };
            // kein Index; wird später über DrawArrays gezeichnet
        }        

        public void AddLine(float x1, float y1, float x2, float y2, Color4 color, float width = 1f, LineStyles style = LineStyles.Solid)
        {
            DrawCount++;            

            // breite Linie als Quad (indiziert)
            if (_currentType != PrimitiveType.Triangles) Flush();
            _currentType = PrimitiveType.Triangles;

            if (vertexCount + 4 >= MAX_VERTICES) Flush();

            SetWhiteTexture();            

            float dx = x2 - x1;
            float dy = y2 - y1;
            float length = (float)MathF.Sqrt(dx * dx + dy * dy);
            if (length < 0.001f) return;

            float type;
            switch (style)
            {
                case LineStyles.Dotted:
                    type = 2f;
                    break;
                case LineStyles.Dashed:
                    type = 3f;
                    break;
                case LineStyles.DashDot:
                    type = 4f;
                    break;
                default:
                    type = 0f;
                    break;
            }

            float edgeX = -dy / length * (width / 2.0f);
            float edgeY = dx / length * (width / 2.0f);            

            uint startIdx = (uint)vertexCount;

            // TexCoord.X ist der Schlüssel: 
            // Startpunkte bekommen 0, Endpunkte bekommen die volle 'length'            
            vertexArray[vertexCount++] = new GUIVertex { 
                Position = new Vector2(x1 + edgeX, y1 + edgeY), 
                Color = color, 
                TexCoord = new Vector2(0f, width), // X=Distanz, Y=Breite
                Type = type 
            };
            vertexArray[vertexCount++] = new GUIVertex { 
                Position = new Vector2(x2 + edgeX, y2 + edgeY), 
                Color = color, 
                TexCoord = new Vector2(length, width), 
                Type = type 
            };
            vertexArray[vertexCount++] = new GUIVertex { 
                Position = new Vector2(x2 - edgeX, y2 - edgeY), 
                Color = color, 
                TexCoord = new Vector2(length, width), 
                Type = type 
            };
            vertexArray[vertexCount++] = new GUIVertex { 
                Position = new Vector2(x1 - edgeX, y1 - edgeY), 
                Color = color, 
                TexCoord = new Vector2(0f, width), 
                Type = type 
            };

            indexArray[indexCount++] = startIdx + 0;
            indexArray[indexCount++] = startIdx + 1;
            indexArray[indexCount++] = startIdx + 2;
            indexArray[indexCount++] = startIdx + 2;
            indexArray[indexCount++] = startIdx + 3;
            indexArray[indexCount++] = startIdx + 0;
        }

        public void AddCircle(Vector2 center, float radius, Color4 color, int segments = 32)
        {
            DrawCount++;

            if (_currentType != PrimitiveType.Triangles) Flush();
            _currentType = PrimitiveType.Triangles;

            SetWhiteTexture();

            uint centerIdx = (uint)vertexCount;
            vertexArray[vertexCount++] = new GUIVertex { Position = center, Color = color, TexCoord = Vector2.Zero };

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * MathHelper.TwoPi / segments;
                Vector2 pos = new Vector2(center.X + (float)Math.Cos(angle) * radius, center.Y + (float)Math.Sin(angle) * radius);

                if (vertexCount >= MAX_VERTICES) Flush();

                uint currentIdx = (uint)vertexCount;
                vertexArray[vertexCount++] = new GUIVertex { Position = pos, Color = color, TexCoord = Vector2.Zero };

                if (i > 0)
                {
                    indexArray[indexCount++] = centerIdx;
                    indexArray[indexCount++] = currentIdx - 1;
                    indexArray[indexCount++] = currentIdx;
                }
            }
        }

        public void FillEllipse(Color4 color, float cx, float cy, float radiusX, float radiusY, float scale = 1)
        {
            float r = (Math.Abs(radiusX) + Math.Abs(radiusY)) / 2f;
            if (r < float.Epsilon) return;

            float da = (float)(MathF.Acos(r / (r + 0.125f)) * 2f / scale);
            int numSteps = (int)MathF.Round(MathF.PI * 2 / da);
            if (numSteps < 3) numSteps = 12;

            // Wir brauchen numSteps + 2 Vertices (Center + Ringpunkte)
            if (this.vertexCount + numSteps + 1 >= MAX_VERTICES) Flush();            
            SetWhiteTexture();

            uint centerIdx = (uint)this.vertexCount;
            AddVertex(new Vector2(cx, cy), color); // Das Zentrum

            uint firstOnRingIdx = (uint)this.vertexCount;
            AddVertex(new Vector2(cx + radiusX, cy), color); // Startpunkt bei 3 Uhr

            uint lastIdx = firstOnRingIdx;

            for (int i = 1; i <= numSteps; i++)
            {
                uint currentIdx;
                
                if (i == numSteps) 
                {
                    // DAS FINALE STÜCK: Wir nutzen wieder den allerersten Punkt auf dem Ring
                    currentIdx = firstOnRingIdx;
                }
                else 
                {
                    float angle = i * da;
                    float x = (float)(Math.Cos(angle) * radiusX) + cx;
                    float y = (float)(Math.Sin(angle) * radiusY) + cy;
                    currentIdx = (uint)this.vertexCount;
                    AddVertex(new Vector2(x, y), color);
                }

                AddIndex(centerIdx);
                AddIndex(lastIdx);
                AddIndex(currentIdx);
                
                lastIdx = currentIdx;
            }
        }

        public void AddImage(int textureId, RectangleF dest, Color4 color, RectangleF uv = default)
        {
            if (this.currentTexture != textureId)
            {
                if (vertexCount > 0) Flush();
                this.currentTexture = textureId;
            }

            if (uv == RectangleF.Empty) uv = new RectangleF(0, 0, 1, 1);

            // Hilfsvariablen für die Ecken
            Vector2 p1 = new Vector2(dest.X, dest.Y);
            Vector2 p2 = new Vector2(dest.Right, dest.Y);
            Vector2 p3 = new Vector2(dest.Right, dest.Bottom);
            Vector2 p4 = new Vector2(dest.X, dest.Bottom);

            Vector2 uv1 = new Vector2(uv.X, uv.Y);
            Vector2 uv2 = new Vector2(uv.Right, uv.Y);
            Vector2 uv3 = new Vector2(uv.Right, uv.Bottom);
            Vector2 uv4 = new Vector2(uv.X, uv.Bottom);            

            // Dreieck 1
            AddVertex(p1, uv1, color);
            AddVertex(p2, uv2, color);
            AddVertex(p3, uv3, color);

            // Dreieck 2
            AddVertex(p1, uv1, color);
            AddVertex(p3, uv3, color);
            AddVertex(p4, uv4, color);
        }

        public void AddTiledImage(int textureId, RectangleF bounds, Color4 color, RectangleF uv = default)
        {
            // 1. Alles bisherige rendern, bevor wir am State drehen
            Flush();

            // 2. Aktuelle Parameter sichern (optional, aber sauber)
            int originalWrapS;
            GL.GetTexParameter(TextureTarget.Texture2D, GetTextureParameter.TextureWrapS, out originalWrapS);
            
            // 3. Neue Parameter setzen
            int newWrapMode = (int)TextureWrapMode.Repeat;
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, newWrapMode);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, newWrapMode);

            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);            

            // 4. Zeichnen (Hier dein VBO-Call oder Buffer-Fill)
            // Beispiel: Zeichne ein Rechteck mit UV-Koordinaten > 1.0 für Tiling
            AddImage(textureId, bounds, color, uv);

            // 5. Wieder flushen, damit das Gezeichnete die Tiling-Parameter nutzt
            Flush();

            // 6. Alten Zustand wiederherstellen
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, originalWrapS);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, originalWrapS);
        }

        private GLStateBackup CaptureState()
        {
            GLStateBackup s = new GLStateBackup();

            GL.GetInteger(GetPName.CurrentProgram, out s.Program);
            GL.GetInteger(GetPName.VertexArrayBinding, out s.VAO);
            GL.GetInteger(GetPName.ArrayBufferBinding, out s.ArrayBuffer);
            GL.GetInteger(GetPName.ElementArrayBufferBinding, out s.ElementArrayBuffer);
            GL.GetInteger(GetPName.ActiveTexture, out s.ActiveTexture);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.GetInteger(GetPName.TextureBinding2D, out s.Texture2D);

            s.Blend = GL.IsEnabled(EnableCap.Blend);
            s.DepthTest = GL.IsEnabled(EnableCap.DepthTest);

            return s;
        }

        private void RestoreState(GLStateBackup s)
        {
            GL.UseProgram(s.Program);
            GL.BindVertexArray(s.VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, s.ArrayBuffer);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, s.ElementArrayBuffer);

            GL.ActiveTexture((TextureUnit)s.ActiveTexture);
            GL.BindTexture(TextureTarget.Texture2D, s.Texture2D);

            if (s.Blend) GL.Enable(EnableCap.Blend); else GL.Disable(EnableCap.Blend);
            if (s.DepthTest) GL.Enable(EnableCap.DepthTest); else GL.Disable(EnableCap.DepthTest);
        }

        public void SetClip(IGUIContext ctx, RectangleF bounds)
        {
            Rectangle rb;
            if (bounds == RectangleF.Empty)
                rb = new Rectangle(0, 0, ctx.Width, ctx.Height);
            else
                rb = Rectangle.Ceiling(bounds);                

            if (rb != currentScissor)
            {
                ClipCount++;
                currentScissor = rb;
                                
                Flush(); // Zeichne alles bisherige mit dem alten Scissor                
                                                    
                int glY = ctx.Height - (rb.Y + rb.Height) - ctx.TitleBarHeight;                    
                GL.Scissor(rb.X, glY, rb.Width, rb.Height);                
            }
        }

        public int DrawCount {get; private set;}
        public int FlushCount {get; private set;}
        public int ClipCount {get; private set;}

        public void ResetCounters()
        {
            DrawCount = 0;
            FlushCount = 0;
            ClipCount = 0;
        }

        public void Flush()
        {                        
            // nichts zu tun?
            if (vertexCount == 0 && indexCount == 0) return;            
            
            FlushCount++;
            
            _uiShader.Use();
            _uiShader.SetMatrix4("projection", _projectionMatrix);

            // Texturhandling
            if (currentTexture != -1)            
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, currentTexture);                

                _uiShader.SetInt("uTexture", 0);
                _uiShader.SetBool("uUseTexture", true);
            }
            else
            {
                _uiShader.SetBool("uUseTexture", false);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }

            GL.BindVertexArray(_currentVAO);

            // VBO-Upload (immer nötig, auch wenn DrawArrays)
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, new IntPtr(vertexCount * _vertexStride), vertexArray);

            if (indexCount > 0)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
                GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, new IntPtr(indexCount * sizeof(uint)), indexArray);

                // Zeichnen indiziert
                GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, 0);
            }
            else
            {
                // Zeichnen nicht-indiziert (z. B. Linien mittels DrawArrays)
                GL.DrawArrays(_currentType, 0, vertexCount);
            }

            // cleanup
            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.UseProgram(0);

            // reset counters
            vertexCount = 0;
            indexCount = 0;
            
            //_currentType = PrimitiveType.Triangles;            
            //currentTexture = -1;            
        }

        protected override void CleanupUnmanagedResources()
        {
            // DisposableObject: override, um GL-Resourcen zu löschen

            foreach (var vaoId in contextVAOs.Values)
            {
                GL.DeleteVertexArray(vaoId);
            }
            contextVAOs.Clear();
            
            if (vbo != 0) { GL.DeleteBuffer(vbo); vbo = 0; }
            if (ibo != 0) { GL.DeleteBuffer(ibo); ibo = 0; }            
            if (_uiShader != null) { _uiShader.Dispose(); _uiShader = null; }            

            if (whiteTextureId > 0)
            {
                GL.BindTexture (TextureTarget.Texture2D, 0);
                GL.DeleteTexture(whiteTextureId);
                whiteTextureId = 0;
            }

            base.CleanupUnmanagedResources();
        }
    }
}
