using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using KS.Foundation;

namespace SummerGUI
{
    public class GUIShader : IDisposable
    {
        public int Handle { get; private set; }

        public GUIShader(string vertexSource, string fragmentSource)
        {
            int vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
            int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);
            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(Handle);
                throw new Exception($"Error linking shader program: {infoLog}");
            }

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        private int CompileShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Error compiling {type}: {infoLog}");
            }
            return shader;
        }

        public void Use() => GL.UseProgram(Handle);

        public void SetMatrix4(string name, Matrix4 data)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location != -1)
            {
                // GL.UniformMatrix4 expects columnMajor=false arg in this overload: we pass false
                GL.UniformMatrix4(location, false, ref data);
            }
        }

        public void SetInt(string name, int value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location != -1)
            {
                GL.Uniform1(location, value);
            }
        }

        public void SetBool(string name, bool value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location != -1)
            {
                GL.Uniform1(location, value ? 1 : 0);
            }
        }

        public void Dispose()
        {
            if (Handle != 0)
            {
                GL.DeleteProgram(Handle);
                Handle = 0;
            }
            GC.SuppressFinalize(this);
        }
    }
}
