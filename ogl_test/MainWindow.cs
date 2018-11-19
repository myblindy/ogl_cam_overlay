using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ogl_test
{
    public struct Vertex
    {
        public const int Size = (4 + 2) * 4; // size of struct in bytes

        private readonly Vector4 position;
        private readonly Vector2 uv;

        public Vertex(Vector4 position, Vector2 uv)
        {
            this.position = position;
            this.uv = uv;
        }
    }

    public partial class MainWindow : GameWindow
    {
        public MainWindow() : base(1280, 720, GraphicsMode.Default, "tst", GameWindowFlags.FixedWindow,
            DisplayDevice.Default, 4, 5, GraphicsContextFlags.ForwardCompatible)
        {
            VSync = VSyncMode.Off;
        }

        int BGProgramShaderID, FGProgramShaderID, VertexArrayID, BGTextureID, FGTextureID;
        protected override void OnLoad(EventArgs e)
        {
            GL.ClearColor(Color.Purple);
            GL.Disable(EnableCap.DepthTest);

            BGProgramShaderID = BuildShader(false);
            FGProgramShaderID = BuildShader(true);

            Vertex[] vertices =
            {
                 new Vertex(new Vector4(-1f, -1f, 1f, 1f), new Vector2(0, 1)),
                 new Vertex(new Vector4(+1f, -1f, 1f, 1f), new Vector2(1, 1)),
                 new Vertex(new Vector4(+1f, +1f, 1f, 1f), new Vector2(1, 0)),
                 new Vertex(new Vector4(-1f, +1f, 1f, 1f), new Vector2(0, 0)),
            };

            VertexArrayID = GL.GenVertexArray();
            GL.BindVertexArray(VertexArrayID);

            var buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
            GL.NamedBufferStorage(buffer, Vertex.Size * vertices.Length, vertices, BufferStorageFlags.MapWriteBit);

            GL.VertexArrayAttribBinding(VertexArrayID, 0, 0);
            GL.EnableVertexArrayAttrib(VertexArrayID, 0);
            GL.VertexArrayAttribFormat(VertexArrayID, 0, 4, VertexAttribType.Float, false, 0);

            GL.VertexArrayAttribBinding(VertexArrayID, 1, 0);
            GL.EnableVertexArrayAttrib(VertexArrayID, 1);
            GL.VertexArrayAttribFormat(VertexArrayID, 1, 4, VertexAttribType.Float, false, 16);

            GL.VertexArrayVertexBuffer(VertexArrayID, 0, buffer, IntPtr.Zero, Vertex.Size);

            FGTextureID = BuildTexture("foreground.jpg");
            BGTextureID = BuildTexture("background.jpg");
        }

        private int BuildShader(bool mask)
        {
            var vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, @"
#version 450 core

layout (location = 0) in vec4 position;
layout (location = 1) in vec2 uv;
out vec2 vs_uv;

void main(void)
{
    gl_Position = position;
    vs_uv = uv;
}
");
            GL.CompileShader(vs);

            var fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, mask
                ? @"
#version 450 core

varying in vec2 vs_uv;
uniform sampler2D tex;

void main(void)
{
    vec4 color = texture2D(tex, vs_uv.st);

    if(abs(color.x - .9) < .2 && abs(color.y - .9) < .2 && abs(color.z - .9) < .2)
        discard;

    gl_FragColor = color;
}
"
                : @"

#version 450 core

varying in vec2 vs_uv;
uniform sampler2D tex;

void main(void)
{
    gl_FragColor = texture2D(tex, vs_uv.st);
}
");
            GL.CompileShader(fs);

            var program = GL.CreateProgram();
            GL.AttachShader(program, vs);
            GL.AttachShader(program, fs);
            GL.LinkProgram(program);

            GL.DetachShader(program, vs);
            GL.DeleteShader(vs);
            GL.DetachShader(program, fs);
            GL.DeleteShader(fs);

            return program;
        }

        private int BuildTexture(string file)
        {
            using (var bgimg = new Bitmap(file))
            {
                var texid = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, texid);

                var bmpdata = bgimg.LockBits(new Rectangle(0, 0, bgimg.Width, bgimg.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmpdata.Width, bmpdata.Height, 0,
                    PixelFormat.Bgra, PixelType.UnsignedByte, bmpdata.Scan0);
                bgimg.UnlockBits(bmpdata);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                return texid;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
        }

        int Frames = 0;
        static readonly TimeSpan FrameCounterPeriod = TimeSpan.FromSeconds(1.5);
        DateTime LastFrameCountedAt = DateTime.Now;

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.BindVertexArray(VertexArrayID);

            GL.UseProgram(BGProgramShaderID);
            GL.BindTexture(TextureTarget.Texture2D, BGTextureID);
            GL.DrawArrays(PrimitiveType.Quads, 0, 4);

            GL.UseProgram(FGProgramShaderID);
            GL.BindTexture(TextureTarget.Texture2D, FGTextureID);
            GL.DrawArrays(PrimitiveType.Quads, 0, 4);

            SwapBuffers();

            ++Frames;
            var now = DateTime.Now;
            if (now - LastFrameCountedAt >= FrameCounterPeriod)
            {
                Title = $"FPS: {Frames / (now - LastFrameCountedAt).TotalSeconds:0}";
                LastFrameCountedAt = now;
                Frames = 0;
            }
        }
    }
}
